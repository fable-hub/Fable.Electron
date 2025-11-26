namespace Fake.Core

open Fake.Core.TargetOperators

[<AutoOpen>]
module TargetOperatorExtensions =
    /// All dependencies in y must occur before x, but x does
    /// not depend on any of y.
    /// ie it defines all of y as soft dependencies of x
    /// Returns x
    let (<==?) (x: string) (y: string list) : string =
        y |> List.iter (fun y -> y ?=> x |> ignore)
        x

    let (===>) (x: string) (y: string list) =
        y |> List.iter (fun y -> x ==> y |> ignore)

    /// All dependencies in y depend on x as a soft dependency
    /// Returns x
    let (?==>) (x: string) (y: string list) : string =
        y |> List.iter ((?=>) x >> ignore)
        x

    /// x is a dependent of any of y if their condition is true
    /// returns x
    let (?=?>) (x: string) (y: (string * bool) list) : string =
        y |> List.iter ((=?>) x >> ignore)
        x

    /// For each target in y, they are dependent on x if the condition
    /// is true. Returns x
    let (<?=?) (x: string) (y: (string * bool) list) : string =
        y |> List.iter (fun (y, cond) -> y =?> (x, cond) |> ignore)
        x
