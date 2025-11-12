[<AutoOpen>]
module ElectronApi.Json.Parser.Utils

open System
open System.Collections.Frozen
open System.Collections.Generic
open System.Text.RegularExpressions

let isReserved = Spec.reserved.Contains

let appendApostropheToReservedKeywords =
    fun s -> if Spec.reserved.Contains s then s + "'" else s

let stropInvalidIdentifiers: string -> string =
    function
    | text when text.Contains('-', System.StringComparison.Ordinal) -> $"``{text}``"
    | text -> text

let stropReservedKeywords =
    fun s ->
        if Spec.reserved.Contains s || s[0] |> Char.IsAsciiLetter |> not || s.Contains('.') then
            "``" + s + "``"
        else
            s

// Taken from Fable.Transforms
let dashify (separator: string) (input: string) =
    Regex.Replace(
        input,
        "[a-z]?[A-Z]",
        fun m ->
            if m.Value.Length = 1 then
                m.Value.ToLowerInvariant()
            else
                m.Value.Substring(0, 1) + separator + m.Value.Substring(1, 1).ToLowerInvariant()
    )

let inline private prelude (input: string) : string =
    match Spec.remaps.TryGetValue(input) with
    | false, _ -> input
    | true, value -> value

let toCamelCase (input: string) =
    let input = prelude input

    let value =
        Regex.Replace(input, "[-_ ]([a-z])", _.Groups.[1].Value.ToUpperInvariant())

    value.Substring(0, 1).ToLower() + value.Substring(1)

let toPascalCase (input: string) =
    // let input = prelude input // redundant; already done in toCamelCase
    let camel = (toCamelCase input).Trim('`')
    camel.Substring(0, 1).ToUpper() + camel.Substring(1)

module XmlDcs =
    module Boundaries =
        [<Literal>]
        let openRemarks = "<remarks>"

        [<Literal>]
        let closeRemarks = "</remarks>"

        [<Literal>]
        let openSummary = "<summary>"

        [<Literal>]
        let closeSummary = "</summary>"

        [<Literal>]
        let openExample = "<example>"

        [<Literal>]
        let closeExample = "</example>"

        [<Literal>]
        let openCode = "<code>"

        [<Literal>]
        let closeCode = "</code>"

        [<Literal>]
        let openFSharpCode = "<code lang=\"fsharp\">"

        [<Literal>]
        let closeFSharpCode = closeCode

        [<Literal>]
        let openPara = "<para>"

        [<Literal>]
        let closePara = "</para>"

        module Token =
            [<Struct>]
            type BoundaryToken =
                | BoundaryToken of string * string

                member inline this.Open = let (BoundaryToken(value, _)) = this in value
                member inline this.Close = let (BoundaryToken(_, value)) = this in value

            let private create opener closer = BoundaryToken(opener, closer)
            let remarks: BoundaryToken = create openRemarks closeRemarks
            let summary: BoundaryToken = create openSummary closeSummary
            let example: BoundaryToken = create openExample closeExample
            let code: BoundaryToken = create openCode closeCode
            let fsharpCode: BoundaryToken = create openFSharpCode closeFSharpCode
            let para: BoundaryToken = create openPara closePara

    [<Literal>]
    let br = "<br/>"

    let wrapAround (openBoundary: string) (closeBoundary: string) (contents: string) =
        openBoundary + contents + closeBoundary

    let wrapWith (openBoundary: string) (closeBoundary: string) (contents: string list) =
        [ openBoundary; yield! contents; closeBoundary ]

    let inline wrapStringWith (boundaryToken: Boundaries.Token.BoundaryToken) (text: string) : string =
        wrapAround boundaryToken.Open boundaryToken.Close text

    let inline wrapStringsWith (boundaryToken: Boundaries.Token.BoundaryToken) (texts: string list) : string list =
        wrapWith boundaryToken.Open boundaryToken.Close texts

module Directives =
    open Fantomas.Core.SyntaxOak
    open Fantomas.FCS.Text

    let inline wrapWithIf
        (text: string)
        (node: 'T when 'T: (member AddAfter: TriviaNode -> unit) and 'T: (member AddBefore: TriviaNode -> unit))
        =
        let beforeTrivia = TriviaNode(TriviaContent.Directive $"#if {text}", Range.Zero)
        let afterTrivia = TriviaNode(TriviaContent.Directive "#endif", Range.Zero)
        node.AddBefore(beforeTrivia)
        node.AddAfter(afterTrivia)
        node
