module WDIO

open Fable.Core.JS
open Fable.Core

[<AllowNullLiteral>]
type IWdioElement =
    abstract ``$``: selector: string -> Promise<IWdioElement>
    abstract getHTML: unit -> JS.Promise<string>
    abstract getText: unit -> JS.Promise<string>
    abstract click: unit -> JS.Promise<unit>
    abstract isEnabled: unit -> JS.Promise<bool>

type IBrowser =
    abstract ``$``: selector: string -> Promise<IWdioElement>
    abstract waitUntil: bool -> Promise<unit>
    abstract url: string -> Promise<unit>
    abstract getWindowHandles: unit -> Promise<string[]>
    abstract getWindowHandle: unit -> Promise<string>
    abstract switchToWindow: string -> Promise<unit>

type IBrowser with
    [<Emit("$0.execute($1, $2)")>]
    member inline _.execute(fn: 'T -> 'Result, arg: 'T) : Promise<'Result> = jsNative

    [<Emit("$0.execute($1)")>]
    member inline _.execute(fn: unit -> 'Result) : Promise<'Result> = jsNative

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
    abstract toHaveComputedLabel: string -> Promise<unit>
    abstract toHaveComputedRole: string -> Promise<unit>
    abstract toHaveHref: string -> Promise<unit>
    abstract toHaveLink: string -> Promise<unit>
    abstract toHaveId: string -> Promise<unit>
    abstract toHaveText: string -> Promise<unit>
    abstract toHaveHTML: string -> Promise<unit>
    abstract toBeDisplayedInViewport: unit -> Promise<unit>
    abstract toHaveChildren: unit -> Promise<unit>
    abstract toHaveChildren: int -> Promise<unit>
    abstract toHaveChildren: {| gte: int |} -> Promise<unit>
    abstract toHaveChildren: {| lte: int |} -> Promise<unit>
    abstract toHaveChildren: {| gt: int |} -> Promise<unit>
    abstract toHaveChildren: {| lt: int |} -> Promise<unit>
    abstract toHaveChildren: {| gte: int; lte: int |} -> Promise<unit>
    abstract toHaveChildren: {| gte: int; lt: int |} -> Promise<unit>
    abstract toHaveChildren: {| gt: int; lte: int |} -> Promise<unit>
    abstract toHaveChildren: {| gt: int; lt: int |} -> Promise<unit>
    abstract toHaveWidth: int -> Promise<unit>
    abstract toHaveHeight: int -> Promise<unit>
    abstract toHaveSize: {| width: int; height: int |} -> Promise<unit>
    abstract toBeElementsArrayOfSize: int -> Promise<unit>
    abstract toBeElementsArrayOfSize: {| gte: int |} -> Promise<unit>
    abstract toBeElementsArrayOfSize: {| lte: int |} -> Promise<unit>
    abstract toBeElementsArrayOfSize: {| gt: int |} -> Promise<unit>
    abstract toBeElementsArrayOfSize: {| lt: int |} -> Promise<unit>
    abstract toBeElementsArrayOfSize: {| gte: int; lte: int |} -> Promise<unit>
    abstract toBeElementsArrayOfSize: {| gte: int; lt: int |} -> Promise<unit>
    abstract toBeElementsArrayOfSize: {| gt: int; lte: int |} -> Promise<unit>
    abstract toBeElementsArrayOfSize: {| gt: int; lt: int |} -> Promise<unit>

[<Global>]
let expect (element: IWdioElement) : IAssert = jsNative