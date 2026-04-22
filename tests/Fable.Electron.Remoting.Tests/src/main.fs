module Fable.Electron.Playground.main

open Fable.Electron
open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS
open Node.Api
open Node.Base
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Shared

if SquirrelStartup.started then
    app.quit ()

let windows = ResizeArray<BrowserWindow>()

let createWindow () =
    let mainWindowOptions =
        BrowserWindowConstructorOptions(
            width = 800,
            height = 600,
            webPreferences = WebPreferences(preload = path.join (__dirname, "preload.js"))
        )

    let mainWindow = BrowserWindow(mainWindowOptions)

    if isNullOrUndefined MAIN_WINDOW_VITE_DEV_SERVER_URL then
        mainWindow.loadFile (path.join (__dirname, $"../renderer/{MAIN_WINDOW_VITE_NAME}/index.html"))
    else
        mainWindow.loadURL MAIN_WINDOW_VITE_DEV_SERVER_URL
    |> ignore

    mainWindow.webContents.openDevTools Enums.WebContents.OpenDevTools.Options.Mode.Right

    mainWindow.onClosed (fun () ->
        if windows.Remove(mainWindow) then
            printfn $"Removed %i{mainWindow.id} from window array"
        else
            failwith $"Failed to remove %i{mainWindow.id} from window array"
    )

    windows.Add mainWindow


let mutable counter =
    { Counter.ClickCount = 0
      Value = 0
      Disabled = false }

let setCounter value = counter <- value

let windowLoggerApi (event: IpcMainEvent) = {
    Log = fun msg ->
        promise {
            printfn $"Logging from window {event.sender.id}: {msg}"
            let windowId = event.sender.id
            let now = System.DateTime.Now.Ticks
            let logMessage = $"[Window {windowId}-{now}]:{msg}"
            Browser.Dom.console.log logMessage
            return logMessage
        }
}

app
    .whenReady()
    .``then`` (fun () ->
        createWindow ()

        let broker =
            Remoting.createHandler()
            |> Remoting.setWindows (windows.ToArray())
            |> Remoting.buildClient<TextHandler>

        let handler =
            { Increment =
                fun () ->
                    promise {
                        setCounter
                            { counter with
                                ClickCount =
                                    if counter.Disabled then
                                        counter.ClickCount
                                    else
                                        counter.ClickCount + 1
                                Value =
                                    if counter.Disabled then
                                        counter.Value
                                    else
                                        counter.Value + 1 }

                        broker.SetValue counter.Value

                        if counter.Disabled then
                            return Error()
                        else
                            return Ok counter.Value
                    }
              SetValue =
                fun (value: int) ->
                    promise {
                        setCounter
                            { counter with
                                ClickCount =
                                    if counter.Disabled then
                                        counter.ClickCount
                                    else
                                        counter.ClickCount + 1
                                Value =
                                    if counter.Disabled then
                                        counter.Value
                                    else
                                        value }

                        broker.SetValue counter.Value

                        if counter.Disabled then
                            return Error()
                        else
                            return Ok counter.Value
                    }
              Decrement =
                fun () -> promise {
                    setCounter
                        { counter with
                            ClickCount =
                                if counter.Disabled then
                                    counter.ClickCount
                                else
                                    counter.ClickCount + 1
                            Value =
                                if counter.Disabled then
                                    counter.Value
                                else
                                    counter.Value - 1 }

                    broker.SetValue counter.Value

                    if counter.Disabled then
                        return Error()
                    else
                        return Ok counter.Value
                }
              Disable =
                fun () -> promise {
                    if counter.Disabled then
                        return Error()
                    else
                        setCounter { counter with Disabled = true }
                        broker.SetDisabled true
                        return Ok()
                }
              Enable =
                fun () -> promise {
                    if counter.Disabled then
                        setCounter { counter with Disabled = false }
                        broker.SetDisabled false
                        return Ok()
                    else
                        return Error()
                }
              Value = fun () -> promise { return counter.Value }
              ClickCount = fun () -> promise { return counter.ClickCount } }

        Remoting.createHandler() |> Remoting.fromValue handler
        Remoting.createHandler() |> Remoting.fromIpcMainEvent windowLoggerApi

        app.onActivate (fun _ ->
            if BrowserWindow.getAllWindows().Length = 0 then
                createWindow ()
        )
    )
|> ignore

app.onWindowAllClosed (fun () -> app.quit ())
app.onBeforeQuit (fun e -> Browser.Dom.console.log ("Quitting"))