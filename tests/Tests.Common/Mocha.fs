module Tests.Common.Mocha

open Fable.Core
open Fable.Core.JS

[<Global>]
let describe (name: string) (f: unit -> Promise<unit>) = jsNative

[<Global>]
let it (msg: string) (f: unit -> Promise<unit>) : unit = jsNative

[<Global>]
let before (msg: string) (f: unit -> Promise<unit>) : unit = jsNative

[<Global>]
let after (msg: string) (f: unit -> Promise<unit>) : unit = jsNative

[<Global>]
let beforeEach (msg: string) (f: unit -> Promise<unit>) : unit = jsNative

[<Global>]
let afterEach (msg: string) (f: unit -> Promise<unit>) : unit = jsNative

[<Erase>]
module describe =
    [<Emit("describe.skip($0, $1)")>]
    let skip (msg: string) (f: unit -> unit) : unit = jsNative

    [<Emit("describe.only($0, $1)")>]
    let only (msg: string) (f: unit -> unit) : unit = jsNative

    [<Emit "describe($0)">]
    let toDo (msg: string) : unit = jsNative

[<Erase>]
module it =
    [<Emit("it.skip($0, $1)")>]
    let skip (msg: string) (f: unit -> Promise<unit>) : unit = jsNative

    [<Emit("it.only($0, $1)")>]
    let only (msg: string) (f: unit -> Promise<unit>) : unit = jsNative

    [<Emit "it($0)">]
    let toDo (msg: string) : unit = jsNative
