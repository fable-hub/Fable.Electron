module ElectronApi.Json.Parser.Fantomas

open Fantomas.Core.SyntaxOak
open Fantomas.Core
open Fantomas.FCS
open Fantomas.FCS.Text

/// <summary>
/// Provides extensions for Oak nodes to create nodes given simple primitive types
/// </summary>
[<AutoOpen>]
module Utils =
    type SingleTextNode with
        static member make text = SingleTextNode(text, Range.Zero)
        /// text -> ?text
        static member makeOptional(text: string) = SingleTextNode.make $"?{text}"
        /// text -> ?text
        member this.MakeOptional = this.Text |> SingleTextNode.makeOptional

    type MultipleTextsNode with
        static member make texts =
            MultipleTextsNode(
                [ for text in texts do
                      SingleTextNode.make text ],
                Range.Zero
            )

        static member make text =
            SyntaxOak.MultipleTextsNode.make [ text ]

    type IdentifierOrDot with
        static member makeIdent text =
            IdentifierOrDot.Ident <| SingleTextNode.make text

        static member makeDot = IdentifierOrDot.KnownDot <| SingleTextNode.make "."

        /// This.That -> IdentifierOrDotList [ This; KnownDot; That ]
        static member ListFromString(text: string) =
            text.Split '.'
            |> Array.map IdentifierOrDot.makeIdent
            |> fun arr ->
                let length = arr.Length

                arr
                |> Array.mapi (fun idx ident ->
                    if idx = length then
                        [ ident ]
                    else
                        [ ident; IdentifierOrDot.makeDot ])
            |> Array.toList
            |> List.collect id

    type IdentListNode with
        static member make(text: string) =
            IdentListNode([ IdentifierOrDot.makeIdent text ], Range.Zero)

        static member make(text: string list) =
            IdentListNode(
                text
                |> List.collect (fun text -> [ IdentifierOrDot.makeIdent text; IdentifierOrDot.makeDot ])
                |> List.cutOffLast,
                Range.Zero
            )

        member this.identList =
            this.Content
            |> List.choose (function
                | IdentifierOrDot.Ident ident -> ident.Text |> Some
                | _ -> None)

        member this.firstIdent = this.identList |> List.head
        member this.lastIdent = this.identList |> List.last
        member this.isStropped = this.firstIdent |> _.StartsWith('`')

        member this.isKebab =
            this.Content
            |> List.exists (function
                | IdentifierOrDot.Ident ident -> ident.Text.Contains('-')
                | IdentifierOrDot.KnownDot ident -> ident.Text.Contains('-')
                | _ -> false)

        member this.toCamelCase =
            this.Content
            |> List.map (function
                | IdentifierOrDot.Ident ident
                | IdentifierOrDot.KnownDot ident -> ident.Text
                | _ -> "")
            |> List.toArray
            |> String.concat ""
            |> _.Trim('`')
            |> _.Split('-')
            |> Array.mapi (fun i str ->
                if i = 0 then
                    str
                else
                    let chars = str.ToCharArray()
                    chars[0] <- chars[0] |> System.Char.ToUpperInvariant
                    chars |> string)
            |> String.concat ""

    type AttributeNode with
        static member make text =
            AttributeNode(IdentListNode.make text, None, None, Range.Zero)

    type AttributeListNode with
        static member makeOpening = SingleTextNode.make "[<"
        static member makeClosing = SingleTextNode.make ">]"

        static member make texts =
            AttributeListNode(
                AttributeListNode.makeOpening,
                [ for text in texts do
                      AttributeNode.make text ],
                AttributeListNode.makeClosing,
                Range.Zero
            )

        static member make text = AttributeListNode.make [ text ]

    type MultipleAttributeListNode with
        static member make(texts: string seq seq) =
            MultipleAttributeListNode(
                [ for text in texts do
                      AttributeListNode.make text ],
                Range.Zero
            )

        static member make(text: string seq) = MultipleAttributeListNode.make [ text ]
        static member make(text: string) = MultipleAttributeListNode.make [ text ]

    type PatParenNode with
        static member makeOpening = SingleTextNode.make "("
        static member makeClosing = SingleTextNode.make ")"

        static member make pattern =
            PatParenNode(PatParenNode.makeOpening, pattern, PatParenNode.makeClosing, Range.Zero)

    type PatParameterNode with
        static member make ident typ =
            PatParameterNode(None, Pattern.Null(ident), typ, Range.Zero)

    type PatTupleNode with
        static member makeItem(node: SingleTextNode) : Choice<Pattern, SingleTextNode> = Choice2Of2 node
        static member makeItem(node: Pattern) : Choice<Pattern, SingleTextNode> = Choice1Of2 node

        static member makeConstructor(members: (SingleTextNode * Type) list) =
            PatTupleNode(
                [ for ident, typ in members do
                      yield
                          Pattern.Parameter(PatParameterNode.make ident.MakeOptional (Some typ))
                          |> PatTupleNode.makeItem

                      yield SingleTextNode.make "," |> PatTupleNode.makeItem ]
                |> List.cutOffLast,
                Range.Zero
            )

    type TypeDefnRegularNode with
        member this.TypeDefn = this :> ITypeDefn

    type XmlDocNode with
        static member make(docs: string seq) =
            XmlDocNode(
                [| yield "/// <summary>"
                   for line in docs do
                       for subline in line |> Seq.chunkBySize 120 |> Seq.map (string >> sprintf "/// %s") do
                           yield subline
                   yield "/// </summary>" |],
                Range.Zero
            )

    type TypeNameNode with
        static member Create(identifier, ?constructor, ?docs, ?attributes, ?withWith) =
            let withWith = defaultArg withWith false

            TypeNameNode(
                docs,
                attributes,
                SingleTextNode.make "type",
                None,
                IdentListNode.make identifier,
                None,
                [],
                constructor,
                (if not withWith then Some(SingleTextNode.make "=") else None),
                (if withWith then Some(SingleTextNode.make "with") else None),
                Range.Zero
            )

        static member makeSimple(identifier: string, ?docs: string list, ?attributes: string list, ?suffix: string) =
            TypeNameNode(
                docs
                |> Option.bind (function
                    | [] -> None
                    | texts -> Some texts)
                |> Option.map XmlDocNode.make,
                attributes
                |> Option.bind (function
                    | [] -> None
                    | texts -> Some texts)
                |> Option.map MultipleAttributeListNode.make,
                SingleTextNode.make "type",
                None,
                IdentListNode.make identifier,
                None,
                [],
                None,
                SingleTextNode.make "=" |> Some,
                suffix |> Option.map SingleTextNode.make,
                Range.Zero
            )

    type Expr with
        static member makeIdent text = Expr.Ident(SingleTextNode.make text)

        static member makeString(text: string) =
            let text = text.Trim()
            // ExprQuoteNode.make text |> Expr.Quote
            Expr.makeIdent $"\"{text}\""

    type TypeAppPrefixNode with
        static member Create(identifier, postIdentifier, types) =
            TypeAppPrefixNode(
                identifier,
                postIdentifier,
                SingleTextNode.make "<",
                types,
                SingleTextNode.make ">",
                Range.Zero
            )

        static member Create(identifier, types) =
            TypeAppPrefixNode.Create(identifier, None, types)

    type MemberDefnAutoPropertyNode with
        static member Create(identifier, typ, expr, ?isStatic, ?attributes, ?docs, ?isMutable) =
            let isStatic = defaultArg isStatic false
            let isMutable = defaultArg isMutable true

            MemberDefnAutoPropertyNode(
                docs,
                attributes,
                MultipleTextsNode.make
                    [ if isStatic then
                          "static"
                      "member"
                      "val" ],
                None,
                SingleTextNode.make identifier,
                Some typ,
                SingleTextNode.make "=",
                expr,
                Some(MultipleTextsNode.make (if isMutable then "with get, set" else "with get")),
                Range.Zero
            )

    type BindingReturnInfoNode with
        static member Create(typ) =
            BindingReturnInfoNode(SingleTextNode.make ":", typ, Range.Zero)

    type PropertyGetSetBindingNode with
        static member Create(?isSet, ?expr, ?typ, ?isInline, ?attributes) =
            let isSet = defaultArg isSet false
            let expr = defaultArg expr (Expr.Ident(SingleTextNode.make "jsNative"))
            let isInline = defaultArg isInline false

            let inlineNode =
                if isInline then
                    SingleTextNode.make "inline" |> Some
                else
                    None

            PropertyGetSetBindingNode(
                inlineNode,
                attributes,
                None,
                SingleTextNode.make (if isSet then "set" else "get"),
                [ if isSet then
                      PatParameterNode.make (SingleTextNode.make "value") typ |> Pattern.Parameter
                  else
                      Pattern.Null(SingleTextNode.make "")
                  |> PatParenNode.make
                  |> Pattern.Paren ],
                (if isSet || typ.IsNone then
                     None
                 else
                     typ |> Option.map BindingReturnInfoNode.Create),
                SingleTextNode.make "=",
                expr,
                Range.Zero
            )

    type MemberDefnPropertyGetSetNode with
        static member Create
            (identifier: string, typ, ?isMutable, ?exprGet, ?exprSet, ?isStatic, ?isInline, ?attributes, ?docs)
            =
            let isStatic = defaultArg isStatic false
            let isMutable = defaultArg isMutable true
            let exprGet = defaultArg exprGet (Expr.makeIdent "jsNative")
            let exprSet = defaultArg exprSet (Expr.makeIdent "()")

            MemberDefnPropertyGetSetNode(
                docs,
                attributes,
                MultipleTextsNode.make
                    [ if isStatic then
                          "static"
                      "member" ],
                (isInline
                 |> Option.bind (function
                     | true -> SingleTextNode.make "inline" |> Some
                     | false -> None)),
                None,
                IdentListNode.make
                    [ if not isStatic then
                          "_"
                      identifier ],
                SingleTextNode.make "with",
                PropertyGetSetBindingNode.Create(isSet = false, expr = exprGet, typ = typ),
                (if isMutable then Some(SingleTextNode.make "and") else None),
                (if isMutable then
                     Some(PropertyGetSetBindingNode.Create(isSet = true, expr = exprSet, typ = typ))
                 else
                     None),
                Range.Zero
            )

    type BindingNode with
        static member Create
            (
                identifier: string,
                ?parameters,
                ?docs,
                ?attributes,
                ?isStatic,
                ?isInline,
                ?returnType,
                ?expr,
                ?isMutable,
                ?isMethod
            ) =
            let isStatic = defaultArg isStatic false
            let isInline = defaultArg isInline false
            let isMutable = defaultArg isMutable false
            let isMethod = defaultArg isMethod false
            let expr = defaultArg expr (Expr.makeIdent "jsNative")

            BindingNode(
                docs,
                attributes,
                MultipleTextsNode.make
                    [ if isStatic then
                          "static"
                      "member" ],
                isMutable,
                (if isInline then
                     Some(SingleTextNode.make "inline")
                 else
                     None),
                None,
                Choice1Of2(
                    IdentListNode.make
                        [ if not isStatic then
                              "_"
                          identifier ]
                ),
                None,
                (parameters
                 |> Option.defaultValue
                     [ if isMethod then
                           SingleTextNode.make "" |> Pattern.Null |> PatParenNode.make |> Pattern.Paren ]),
                (returnType |> Option.map BindingReturnInfoNode.Create),
                SingleTextNode.make "=",
                expr,
                Range.Zero
            )

    type MemberDefnAbstractSlotNode with
        static member Create(identifier, typ, ?docs, ?attributes, ?isMember, ?isMutable, ?isStatic) =
            let isMutable = defaultArg isMutable true
            let isStatic = defaultArg isStatic false
            let isMember = defaultArg isMember true

            MemberDefnAbstractSlotNode(
                docs,
                attributes,
                MultipleTextsNode.make
                    [ if isStatic then
                          "static"
                      "abstract"
                      if isMember then
                          "member" ],
                SingleTextNode.make identifier,
                None,
                typ,
                Some(
                    MultipleTextsNode.make
                        [ "with"
                          if isMutable then
                              "get,"
                              "set"
                          else
                              "get" ]
                ),
                Range.Zero
            )

    type PatNamedNode with
        static member Create(identifier, ?isInternal, ?isPrivate) =
            let isInternal = defaultArg isInternal false
            let isPrivate = defaultArg isPrivate false

            PatNamedNode(
                (if isInternal then SingleTextNode.make "internal" |> Some
                 elif isPrivate then SingleTextNode.make "private" |> Some
                 else None),
                SingleTextNode.make identifier,
                Range.Zero
            )

    type PatParameterNode with
        static member Create(identifier, ?typ, ?attributes, ?isPrivate, ?isInternal) =
            PatParameterNode(
                attributes,
                (PatNamedNode.Create(identifier, ?isPrivate = isPrivate, ?isInternal = isInternal)
                 |> Pattern.Named),
                typ,
                Range.Zero
            )

    type PatTupleNode with
        static member Create
            (identTypeTuples: (string * Type option) list, ?separator: string, ?directiveWithoutPrefix: string)
            =
            let maybeDirectiveBefore, maybeDirectiveAfter =
                match directiveWithoutPrefix with
                | Some directiveString ->
                    (fun (node: Node) ->
                        TriviaNode(TriviaContent.Directive $"#if {directiveString}", Range.Zero)
                        |> node.AddBefore

                        node),
                    (fun (node: Node) ->
                        TriviaNode(TriviaContent.Directive "#endif", Range.Zero) |> node.AddAfter
                        node)
                | None -> id, id

            let separator = defaultArg separator ","

            identTypeTuples
            |> List.collect (function
                | ident, typ ->
                    [ SingleTextNode.make separator |> maybeDirectiveBefore :?> SingleTextNode
                      |> Choice2Of2
                      PatParameterNode.Create(ident, ?typ = typ) |> maybeDirectiveAfter :?> PatParameterNode
                      |> Pattern.Parameter
                      |> Choice1Of2 ])
            |> function
                | [] -> PatTupleNode([], Range.Zero)
                | _ :: Choice1Of2(Pattern.Parameter node) :: tail ->
                    PatTupleNode(
                        (node |> maybeDirectiveBefore :?> PatParameterNode
                         |> Pattern.Parameter
                         |> Choice1Of2)
                        :: tail,
                        Range.Zero
                    )
                | _ ->
                    failwith "UNREACHABLE"
                    PatTupleNode([], Range.Zero)

    type ImplicitConstructorNode with
        static member Create(identTypeTuples: (string * Type option) list, ?docs, ?attributes, ?isPrivate) =
            let isPrivate = defaultArg isPrivate false

            ImplicitConstructorNode(
                docs,
                attributes,
                (if isPrivate then
                     Some(SingleTextNode.make "private")
                 else
                     None),
                PatTupleNode.Create(identTypeTuples)
                |> Pattern.Tuple
                |> PatParenNode.make
                |> Pattern.Paren,
                None,
                Range.Zero
            )

    type TypeSignatureParameterNode with
        static member Create(identifier, typ, ?isRequired, ?attributes) =
            let isRequired = defaultArg isRequired true

            TypeSignatureParameterNode(
                attributes,
                Some(
                    if isRequired then
                        SingleTextNode.make identifier
                    else
                        SingleTextNode.makeOptional identifier
                ),
                typ,
                Range.Zero
            )

    type TypeParenNode with
        static member Create(typ) =
            TypeParenNode(PatParenNode.makeOpening, typ, PatParenNode.makeClosing, Range.Zero)

    type Type with
        /// <summary>
        /// Wraps a type with parenthesis (useful in delegates where an inline function
        /// must be wrapped with parenthesis).
        /// </summary>
        member this.WrapLambdasInParen() =
            match this with
            | Type.Funs _ -> TypeParenNode.Create this |> Type.Paren
            | _ -> this

    type TypeTupleNode with
        static member Create(typePairs: Choice<Type, SingleTextNode> list) = TypeTupleNode(typePairs, Range.Zero)

        static member Create(types: Type list, ?separator, ?doNotCutOffLast) =
            let doNotCutOffLast = defaultArg doNotCutOffLast false
            let separator = defaultArg separator "*"

            types
            |> List.collect (fun typ -> [ Choice1Of2 typ; Choice2Of2 <| SingleTextNode.make separator ])
            |> if doNotCutOffLast then id else List.cutOffLast

    type TypeFunsNode with
        static member Create(types: Type list, returnValue: Type, ?curried) =
            let curried = defaultArg curried true
            let separator = (if curried then "->" else "*") |> SingleTextNode.make |> Choice2Of2

            types
            |> function
                | [] -> [ Type.Anon <| SingleTextNode.make "unit" ]
                | types -> types
            |> List.collect (fun typ -> [ Choice1Of2 typ; separator ])
            |> List.cutOffLast
            |> TypeTupleNode.Create
            |> fun typs -> Type.Tuple typs, SingleTextNode.make "->"
            |> List.singleton
            |> fun typs -> TypeFunsNode(typs, returnValue, Range.Zero)

    type MemberDefnInterfaceNode with
        static member Create(identifier, ?members) =
            MemberDefnInterfaceNode(
                SingleTextNode.make "interface",
                Type.Anon(SingleTextNode.make identifier),
                (if members.IsSome then
                     Some(SingleTextNode.make "with")
                 else
                     None),
                members |> Option.defaultValue [],
                Range.Zero
            )

    type MemberDefnInheritNode with
        static member Create(identifier, ?withConstructor) =
            let withConstructor = defaultArg withConstructor false
            let identifier = if withConstructor then $"{identifier}()" else identifier
            MemberDefnInheritNode(SingleTextNode.make "inherit", Type.Anon(SingleTextNode.make identifier), Range.Zero)

    type TypeDefnRegularNode with
        static member Create(typeNameNode, members) =
            TypeDefnRegularNode(typeNameNode, members, Range.Zero)

        static member Create
            (
                identifier: string,
                members: MemberDefn list,
                ?constructor,
                ?docs,
                ?attributes,
                ?inheritString: string,
                ?interfaceStrings: string list,
                ?withWith
            ) =
            let members =
                if
                    inheritString.IsSome
                    && (members
                        |> List.exists (function
                            | MemberDefn.Inherit _ -> true
                            | _ -> false))
                then
                    failwith
                        "Tried to Create a TypeDefnRegularNode with an input inherit string, \
when there is already an existing inherit members"

                match inheritString, interfaceStrings with
                | Some inherits, Some interfaces ->
                    (inherits |> (MemberDefnInheritNode.Create >> MemberDefn.Inherit))
                    :: (interfaces |> List.map (MemberDefnInterfaceNode.Create >> MemberDefn.Interface))
                    @ members
                | Some inherits, _ -> (inherits |> (MemberDefnInheritNode.Create >> MemberDefn.Inherit)) :: members
                | _, Some interfaces ->
                    (interfaces |> List.map (MemberDefnInterfaceNode.Create >> MemberDefn.Interface))
                    @ members
                | _, _ -> members

            let typeNameNode =
                TypeNameNode.Create(
                    identifier,
                    ?docs = docs,
                    ?attributes = attributes,
                    ?withWith = withWith,
                    ?constructor = constructor
                )

            TypeDefnRegularNode.Create(typeNameNode, members)

    type TypeDefnDelegateNode with
        static member Create(identifier: string, types, ?docs, ?attributes) =
            TypeDefnDelegateNode(
                TypeNameNode.Create(identifier, ?docs = docs, ?attributes = attributes),
                SingleTextNode.make "delegate",
                types,
                Range.Zero
            )

        static member Create(identifier: string, types, returnValue, ?docs, ?attributes) =
            TypeDefnDelegateNode.Create(
                identifier,
                TypeFunsNode.Create(types, returnValue, curried = false),
                ?docs = docs,
                ?attributes = attributes
            )

    type OpenModuleOrNamespaceNode with
        static member Create(identifier: string) =
            OpenModuleOrNamespaceNode(IdentListNode.make identifier, Range.Zero)

        static member Create(identifier: string list) =
            OpenModuleOrNamespaceNode(IdentListNode.make identifier, Range.Zero)

    type OpenListNode with
        static member Create(openIdentifiers: string list) =
            openIdentifiers
            |> List.map (OpenModuleOrNamespaceNode.Create >> Open.ModuleOrNamespace)
            |> OpenListNode

    type NestedModuleNode with
        static member Create(identifier, members, ?docs, ?attributes, ?isPrivate, ?isInternal, ?isRecursive) =
            let isPrivate = defaultArg isPrivate false
            let isInternal = defaultArg isInternal false
            let isRecursive = defaultArg isRecursive false

            let accessibility =
                if isInternal then SingleTextNode.make "internal" |> Some
                elif isPrivate then SingleTextNode.make "private" |> Some
                else None

            NestedModuleNode(
                docs,
                attributes,
                SingleTextNode.make "module",
                accessibility,
                isRecursive,
                IdentListNode.make identifier,
                SingleTextNode.make "=",
                members,
                Range.Zero
            )

    type ModuleOrNamespaceHeaderNode with
        static member Create(identifier, ?docs, ?attributes, ?isRecursive, ?isModule) =
            let isModule = defaultArg isModule false
            let isRecursive = defaultArg isRecursive false

            ModuleOrNamespaceHeaderNode(
                docs,
                attributes,
                MultipleTextsNode.make (if isModule then "module" else "namespace"),
                None,
                isRecursive,
                Some(IdentListNode.make identifier),
                Range.Zero
            )

        static member CreateModule(identifier, ?docs, ?attributes, ?isRecursive) =
            ModuleOrNamespaceHeaderNode.Create(
                identifier,
                ?docs = docs,
                ?attributes = attributes,
                ?isRecursive = isRecursive,
                isModule = true
            )

        static member CreateNamespace(identifier, ?docs, ?attributes, ?isRecursive) =
            ModuleOrNamespaceHeaderNode.Create(
                identifier,
                ?docs = docs,
                ?attributes = attributes,
                ?isRecursive = isRecursive
            )

    type ModuleOrNamespaceNode with
        static member Create(decls, ?header) =
            ModuleOrNamespaceNode(header, decls, Range.Zero)

        static member Create(identifier, decls, ?docs, ?attributes, ?isRecursive, ?isModule) =
            let isModule = defaultArg isModule false

            ModuleOrNamespaceNode.Create(
                decls,
                ModuleOrNamespaceHeaderNode.Create(
                    identifier,
                    ?docs = docs,
                    ?attributes = attributes,
                    isModule = isModule,
                    ?isRecursive = isRecursive
                )
            )

    type UnionCaseNode with
        static member Create(identifier, ?fields, ?docs, ?attributes) =
            UnionCaseNode(
                docs,
                attributes,
                Some(SingleTextNode.make "|"),
                SingleTextNode.make identifier,
                fields |> Option.defaultValue [],
                Range.Zero
            )

    type TypeDefnUnionNode with
        static member Create(typeNameNode, cases, ?members) =
            TypeDefnUnionNode(typeNameNode, None, cases, members |> Option.defaultValue [], Range.Zero)

        static member Create(typeName, cases, ?members, ?docs, ?attributes) =
            TypeDefnUnionNode(
                TypeNameNode.Create(typeName, ?docs = docs, ?attributes = attributes),
                None,
                cases,
                members |> Option.defaultValue [],
                Range.Zero
            )

        static member Create(typeName, cases: Name list, ?members, ?docs, ?attributes) =
            cases
            |> List.map (fun name ->
                UnionCaseNode.Create(
                    (name.ValueOrModified |> toPascalCase),
                    attributes = MultipleAttributeListNode.make [ $"CompiledName(\"{name.ValueOrSource}\")" ]
                ))
            |> fun cases ->
                TypeDefnUnionNode.Create(typeName, cases, ?members = members, ?docs = docs, ?attributes = attributes)
