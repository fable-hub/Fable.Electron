module WDIO
open Fable.Core.JS
open Fable.Core.Testing
open Fable.Core
[<AllowNullLiteral>]
type IWdioElement =
    abstract getHTML: unit -> JS.Promise<string>
    abstract getText: unit -> JS.Promise<string>
    abstract click: unit -> JS.Promise<unit>
    abstract isEnabled: unit -> JS.Promise<bool>
type IBrowser =
    abstract ``$``: selector: string -> Promise<IWdioElement>
    abstract waitUntil: bool -> Promise<unit>
    abstract url: string -> Promise<unit>
    abstract getWindowHandles: unit -> Promise<string[]>
    abstract switchToWindow: string -> Promise<unit>
type IBrowser with
    [<Emit("$0.execute($1, $2)")>]
    member inline _.execute(fn: 'T -> 'Result, arg: 'T): Promise<'Result> = jsNative
    [<Emit("$0.execute($1)")>]
    member inline _.execute(fn: unit -> 'Result): Promise<'Result> = jsNative
[<Import("browser", "@wdio/globals")>]
let browser: IBrowser = jsNative

type IAssert =
    abstract toBeDisplayed: unit -> Promise<unit>
    abstract toExist: unit -> Promise<unit>
    abstract toBePresent: unit -> Promise<unit>
    abstract toBeExisting: unit -> Promise<unit>
    abstract toBeFocused: unit -> Promise<unit>
    abstract toHaveAttribute: string * string -> Promise<unit>
    abstract toHaveAttr: string * string -> Promise<unit>
    abstract toHaveElementClass: string * {| message: string |} -> Promise<unit>
    abstract toBeClickable: unit -> Promise<unit>
    abstract toBeDisabled: unit -> Promise<unit>
    abstract toBeEnabled: unit -> Promise<unit>
    abstract toHaveText: string -> Promise<unit>
    abstract toHaveHTML: string -> Promise<unit>
[<Global>]
let expect (element: IWdioElement): IAssert = jsNative
