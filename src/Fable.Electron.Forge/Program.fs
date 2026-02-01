// Minimal global bindings for Electron Forge Vite
[<AutoOpen>]
module Fable.Electron.Forge

open System.ComponentModel
open Fable.Core


[<EditorBrowsable(EditorBrowsableState.Advanced)>]
[<StringEnum>]
type Mode =
    | Production
    | Development
    | Staging

[<AllowNullLiteral; EditorBrowsable(EditorBrowsableState.Advanced)>]
type ENV =
    abstract member DEV: bool with get
    abstract member MODE: Mode with get
    abstract member BASE_URL: string with get
    abstract member PROD: string with get
    abstract member SSR: string with get

    [<EmitIndexer>]
    abstract member Item: key: string -> string with get

[<Emit("import.meta.env")>]
let env: ENV = jsNative

[<Emit("MAIN_WINDOW_VITE_DEV_SERVER_URL")>]
let MAIN_WINDOW_VITE_DEV_SERVER_URL: string = jsNative

[<Emit("MAIN_WINDOW_VITE_NAME")>]
let MAIN_WINDOW_VITE_NAME: string = jsNative

[<Erase>]
module SquirrelStartup =
    [<ImportDefault "electron-squirrel-startup">]
    let started: bool = jsNative