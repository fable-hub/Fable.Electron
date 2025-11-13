module Tests.Common.Mocha

open Fable.Core
open Fable.Core.JS

[<Global>]
let describe (name: string) (f: unit -> Promise<unit>) = jsNative

[<Global>]
let it (msg: string) (f: unit -> Promise<unit>) = jsNative

[<Erase>]
module it =
    [<Emit("it.skip($0, $1)")>]
    let skip (msg: string) (f: unit -> Promise<unit>) = jsNative

    [<Emit("it.only($0, $1)")>]
    let only (msg: string) (f: unit -> Promise<unit>) = jsNative
