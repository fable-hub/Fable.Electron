module Fable.Electron.Playground.renderer

open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop
open Fable.Electron.Remoting.Renderer
open Browser.Dom
open Browser.Types
open Shared

importSideEffects "./index.css"

console.log "This message is being logged by 'renderer.js', included via VITE"

let api = Remoting.createIpc () |> Remoting.buildProxySender<CounterHandler>

let windowLoggerApi = Remoting.createIpc () |> Remoting.buildProxySender<WindowLogger>

let windowLoggerFromValueApi =
    Remoting.createIpc () |> Remoting.buildProxySender<WindowLoggerFromValue>

let incButton =
    document.getElementById ("counter-button-increment") :?> HTMLButtonElement

let decButton =
    document.getElementById ("counter-button-decrement") :?> HTMLButtonElement

let set42Button =
    document.getElementById ("counter-set-42") :?> HTMLButtonElement

let disableButton =
    document.getElementById ("counter-button-disable") :?> HTMLButtonElement

let enableButton =
    document.getElementById ("counter-button-enable") :?> HTMLButtonElement

let counterText = document.getElementById ("counter-text") :?> HTMLHeadingElement

let windowLoggerButton =
    document.getElementById ("window-logger-button") :?> HTMLButtonElement

let windowLoggerOutput =
    document.getElementById ("window-logger-output") :?> HTMLDivElement

let windowLoggerButtonMultipleArgs =
    document.getElementById ("window-logger-button-multiple-args") :?> HTMLButtonElement

let windowLoggerOutputMultipleArgs =
    document.getElementById ("window-logger-output-multiple-args") :?> HTMLDivElement

let windowLoggerFromValueButtonMultipleArgs =
    document.getElementById ("window-logger-from-value-button-multiple-args") :?> HTMLButtonElement

let windowLoggerFromValueOutputMultipleArgs =
    document.getElementById ("window-logger-from-value-output-multiple-args") :?> HTMLDivElement

let mainSignalStatus =
    document.getElementById ("main-signal-status") :?> HTMLDivElement

let mainSignalCount =
    document.getElementById ("main-signal-count") :?> HTMLDivElement

let mainSignalLast =
    document.getElementById ("main-signal-last") :?> HTMLDivElement

let mainSignalUnmountButton =
    document.getElementById ("main-signal-unmount") :?> HTMLButtonElement

let mutable mainSignalUpdateCount = 0
let mutable disposeMainSignalHandler: unit -> unit = fun () -> ()

incButton.addEventListener (
    "click",
    fun e ->
        api
            .Increment()
            .``then`` (
                function
                | Ok value -> console.log $"Counter value incremented to {value}"
                | _ ->
                    if disableButton.attributes.getNamedItem("disabled").nodeValue = "disabled" then
                        console.log "Counter value did not change - disabled"
                    else
                        failwith "Unable to change counter value but counter is not disabled"
            )
        |> ignore
)

decButton.addEventListener (
    "click",
    fun e ->
        api
            .Decrement()
            .``then`` (
                function
                | Ok value -> console.log $"Counter value decremented to {value}"
                | _ ->
                    if disableButton.attributes.getNamedItem("disabled").nodeValue = "disabled" then
                        console.log "Counter value did not change - disabled"
                    else
                        failwith "Unable to change counter value but counter is not disabled"
            )
        |> ignore
)

set42Button.addEventListener(
    "click",
    fun e ->
        api
            .SetValue(42)
            .``then`` (
                function
                | Ok value -> console.log $"Counter value set to {value}"
                | _ ->
                    if disableButton.attributes.getNamedItem("disabled").nodeValue = "disabled" then
                        console.log "Counter value did not change - disabled"
                    else
                        failwith "Unable to change counter value but counter is not disabled"
            )
        |> ignore
)

disableButton.addEventListener (
    "click",
    fun e ->
        api
            .Disable()
            .``then`` (
                function
                | Ok _ -> console.log "Disabled"
                | Error _ -> failwith "Should not be able to click disabled"
            )
        |> ignore
)

enableButton.addEventListener (
    "click",
    fun e ->
        api
            .Enable()
            .``then`` (
                function
                | Ok _ -> console.log "Enabled"
                | Error _ -> failwith "Should not be able to click enabled"
            )
        |> ignore
)

windowLoggerButton.addEventListener(
    "click",
    fun e ->
        console.log("Calling window logger API from renderer...")
        (windowLoggerApi
          .Log "Hello from Renderer!")
          .``then`` (
                fun (result: string) ->
                    windowLoggerOutput.innerText <- result
            )
        |> ignore
)

windowLoggerButtonMultipleArgs.addEventListener(
    "click",
    fun e ->
        console.log("Calling window logger API with multiple args from renderer...")
        (windowLoggerApi
          .LogMultipleArgs "Hello from Renderer!" 42 true)
          .``then`` (
                fun (result: string) ->
                    windowLoggerOutputMultipleArgs.innerText <- result
            )
        |> ignore
)

windowLoggerFromValueButtonMultipleArgs.addEventListener(
    "click",
    fun e ->
        console.log("Calling window logger fromValue API with multiple args from renderer...")
        (windowLoggerFromValueApi
          .LogMultipleArgs "Hello from Renderer!" 42 true)
          .``then`` (
                fun (result: string) ->
                    windowLoggerFromValueOutputMultipleArgs.innerText <- result
            )
        |> ignore
)

let handler =
    { SetValue = fun value -> counterText.innerText <- string value
      SetDisabled =
        function
        | true ->
            disableButton.setAttribute ("disabled", "disabled")
            enableButton.removeAttribute ("disabled")
        | false ->
            disableButton.removeAttribute ("disabled")
            enableButton.setAttribute ("disabled", "disabled")

    }

let mainSignalHandler =
    { Tick =
        fun value ->
            mainSignalUpdateCount <- mainSignalUpdateCount + 1
            mainSignalCount.innerText <- string mainSignalUpdateCount
            mainSignalLast.innerText <- string value }

disposeMainSignalHandler <-
    Remoting.createIpc ()
    |> Remoting.buildProxyReceiverDisposable mainSignalHandler

mainSignalUnmountButton.addEventListener(
    "click",
    fun _ ->
        mainSignalStatus.innerText <- "unmounted"
        disposeMainSignalHandler ()
)

Remoting.createIpc () |> Remoting.buildProxyReceiver handler


Browser.Dom.console.log (Browser.Dom.window)