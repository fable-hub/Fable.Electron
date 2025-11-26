namespace ElectronApi.Json.Parser

open System.Collections.Frozen
open Fantomas.Core.SyntaxOak
open Fantomas.FCS.Text

module Spec =
    [<Literal>]
    let rootNamespace = "Fable.Electron"

    [<Literal>]
    let osWinDefine = "ELECTRON_OS_WIN"

    [<Literal>]
    let osMasDefine = "ELECTRON_OS_MAS"

    [<Literal>]
    let osMacDefine = "ELECTRON_OS_MAC"

    [<Literal>]
    let osLinDefine = "ELECTRON_OS_LIN"

    /// <summary>
    /// There exists a union which takes more than 9 types, and they are all touchbar classes.
    /// For this reason, we define our own union type with an erase to satisfy this.
    /// </summary>
    [<Literal>]
    let touchBarItemsName = "TouchBarItem"

    [<Literal>]
    let eventEmitterName = "EventEmitter"


    /// Remapping of any lower case enums into a format that is regexd as separate words
    /// This DOES NOT effect the 'Source' of a name.
    let remaps =
        let inline (==>) l r =
            System.Collections.Generic.KeyValuePair(l, r)

        [ "iskeypad" ==> "is keypad"
          "isautorepeat" ==> "is auto repeat"
          "leftbuttondown" ==> "left button down"
          "middlebuttondown" ==> "middle button down"
          "rightbuttondown" ==> "right button down"
          "capslock" ==> "caps lock"
          "numlock" ==> "num lock"

          ]
            .ToFrozenDictionary()

    /// <summary>
    /// Reserved keywords
    /// </summary>
    let reserved =
        [ "abstract"
          "and"
          "as"
          "assert"
          "base"
          "begin"
          "class"
          "default"
          "delegate"
          "do"
          "done"
          "downcast"
          "downto"
          "elif"
          "else"
          "end"
          "exception"
          "extern"
          "false"
          "finally"
          "fixed"
          "fun"
          "function"
          "global"
          "if"
          "in"
          "inherit"
          "inline"
          "interface"
          "internal"
          "lazy"
          "let"
          "match"
          "member"
          "module"
          "mutable"
          "namespace"
          "new"
          "null"
          "of"
          "open"
          "or"
          "override"
          "private"
          "public"
          "rec"
          "return"
          "static"
          "struct"
          "then"
          "to"
          "true"
          "try"
          "type"
          "upcast"
          "use"
          "val"
          "void"
          "when"
          "while"
          "with"
          "yield"
          "const"
          // not actually reserved, but better not to obfuscate
          "not"
          "select"
          // reserved because they are keywords in OCaml
          "asr"
          "land"
          "lor"
          "lsl"
          "lsr"
          "lxor"
          "mod"
          "sig"
          // reserved for future expansion
          "break"
          "checked"
          "component"
          "constraint"
          "continue" (* "event" *)
          "external"
          "include"
          "mixin"
          "parallel"
          "process"
          "protected"
          "pure"
          "sealed"
          "tailcall"
          "trait"
          "virtual"
          "params" ]
            .ToFrozenSet()

    [<AutoOpen>]
    module private Helpers =
        let makeTextNode = fun text -> SingleTextNode(text, Range.Zero)

        let makeIdentListNode =
            fun text -> IdentListNode([ IdentifierOrDot.Ident(makeTextNode text) ], Range.Zero)

        let makeAttributeNode =
            fun text ->
                AttributeListNode(
                    makeTextNode "[<",
                    [ AttributeNode(makeIdentListNode text, None, None, Range.Zero) ],
                    makeTextNode ">]",
                    Range.Zero
                )
    //%TouchBarItemsImpl%START%
    let touchBarItemsDef =
        let touchBarItems =
            [ "Button"
              "ColorPicker"
              "Group"
              "Label"
              "Popover"
              "Scrubber"
              "SegmentedControl"
              "Slider"
              "Spacer" ]

        let attributes =
            MultipleAttributeListNode([ makeAttributeNode "Erase" ], Range.Zero)

        let typeNameNode =
            TypeNameNode(
                None,
                Some attributes,
                makeTextNode "type",
                None,
                makeIdentListNode touchBarItemsName,
                None,
                [],
                None,
                Some(makeTextNode "="),
                None,
                Range.Zero
            )

        TypeDefnUnionNode(
            typeNameNode,
            None,
            [ let makeCaseNode =
                  fun text ->
                      UnionCaseNode(
                          None,
                          None,
                          Some(makeTextNode "|"),
                          makeTextNode text,
                          [ FieldNode(
                                None,
                                None,
                                None,
                                None,
                                None,
                                None,
                                Type.Anon(makeTextNode ("TouchBar" + text)),
                                Range.Zero
                            ) ],
                          Range.Zero
                      )

              yield! touchBarItems |> List.map makeCaseNode ],
            [],
            Range.Zero
        )
        |> TypeDefn.Union
//%TouchBarItemsImpl%END%
