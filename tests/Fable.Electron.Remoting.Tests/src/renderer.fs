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

let api =
    Remoting.init
    |> Remoting.buildClient<CounterHandler>

let incButton =
    document.getElementById("counter-button-increment") :?> HTMLButtonElement
let decButton =
    document.getElementById("counter-button-decrement") :?> HTMLButtonElement
let disableButton =
    document.getElementById("counter-button-disable") :?> HTMLButtonElement
let enableButton =
    document.getElementById("counter-button-enable") :?> HTMLButtonElement
let counterText =
    document.getElementById("counter-text") :?> HTMLHeadingElement
    
incButton.addEventListener("click", fun e ->
    api.Increment().``then``(function
        | Ok value -> console.log $"Counter value incremented to %i{value}"
        | _ ->
            if disableButton.attributes.getNamedItem("disabled").nodeValue = "disabled" then
                console.log "Counter value did not change - disabled"
            else failwith "Unable to change counter value but counter is not disabled"
        )
    |> ignore
    )

decButton.addEventListener("click", fun e ->
    api.Decrement().``then``(function
        | Ok value -> console.log $"Counter value decremented to %i{value}"
        | _ ->
            if disableButton.attributes.getNamedItem("disabled").nodeValue = "disabled" then
                console.log "Counter value did not change - disabled"
            else failwith "Unable to change counter value but counter is not disabled"
        )
    |> ignore
    )

disableButton.addEventListener("click", fun e ->
    api.Disable().``then``(function
        | Ok _ -> console.log "Disabled"
        | Error _ -> failwith "Should not be able to click disabled"
        )
    |> ignore
    )
enableButton.addEventListener("click", fun e ->
    api.Enable().``then``(function
        | Ok _ -> console.log "Enabled"
        | Error _ -> failwith "Should not be able to click enabled"
        )
    |> ignore
    )

let handler = {
    SetValue = fun value -> counterText.innerText <- string value
    SetDisabled = function
        | true ->
            disableButton.setAttribute("disabled", "disabled")
            enableButton.removeAttribute("disabled")
        | false ->
            disableButton.removeAttribute("disabled")
            enableButton.setAttribute("disabled", "disabled")
            
}

Remoting.init
|> Remoting.buildHandler handler

