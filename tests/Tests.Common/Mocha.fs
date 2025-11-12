module Tests.Common.Mocha

open Fable.Core
open Fable.Core.JS

let [<Global>] describe (name: string) (f: unit -> Promise<unit>) = jsNative
let [<Global>] it (msg: string) (f: unit -> Promise<unit>) = jsNative
[<Erase>]
module it =
    let [<Emit("it.skip($0, $1)")>] skip (msg: string) (f: unit -> Promise<unit>) = jsNative
    let [<Emit("it.only($0, $1)")>] only (msg: string) (f: unit -> Promise<unit>) = jsNative
