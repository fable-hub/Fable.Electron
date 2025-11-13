module Fable.Electron.Playground.preload

open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop
open Fable.Electron
open Fable.Electron.Remoting.Preload
open Shared

Remoting.init |> Remoting.buildTwoWayBridge<CounterHandler>
Remoting.init |> Remoting.buildBridge<TextHandler>
