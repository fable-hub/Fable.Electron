module Fable.Electron.Playground.preload

open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop
open Fable.Electron
open Fable.Electron.Remoting.Preload
open Shared

Remoting.createIpc () |> Remoting.buildTwoWayBridge<CounterHandler>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<WindowLogger>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<WindowLoggerFromValue>
Remoting.createIpc () |> Remoting.buildBridge<TextHandler>
Remoting.createIpc () |> Remoting.buildBridge<MainSignalHandler>
