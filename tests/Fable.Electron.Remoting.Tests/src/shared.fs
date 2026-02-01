module Shared

open Fable.Core.JS
open Fable.Electron

type Counter =
    { ClickCount: int
      Value: int
      Disabled: bool }

type CounterHandler =
    { Increment: unit -> Promise<Result<int, unit>>
      Decrement: unit -> Promise<Result<int, unit>>
      SetValue: int -> Promise<Result<int, unit>>
      Disable: unit -> Promise<Result<unit, unit>>
      Enable: unit -> Promise<Result<unit, unit>>
      Value: unit -> Promise<int>
      ClickCount: unit -> Promise<int> }

type TextHandler =
    { SetValue: int -> unit
      SetDisabled: bool -> unit }

type WindowLogger =
    { Log: IpcMainEvent -> string -> Promise<string> }