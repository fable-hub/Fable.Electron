module ElectronApi.Json.Parser.SourceMapper

open System.Collections.Generic
open System.ComponentModel
open ElectronApi.Json.Parser.Prelude
open ElectronApi.Json.Parser.Types.Name
open Fantomas.Core
open Fantomas.Core.SyntaxOak
open Fantomas.FCS.Text
open Fantomas.Utils

module List =
    let tryTail =
        function
        | [] -> []
        | _ :: tail -> tail


module XmlDocs =
    let makeClosedSeeAlso (link: string) = $"<seealso href=\"{link}\"/>"

    let makeSeeAlso (link: string) (description: string) =
        $"<seealso href=\"{link}\">{description}</seealso>"

    let makeParam (name: string) (description: string) =
        $"<param name=\"{name.Trim('`')}\">{description}</param>"

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

    [<Literal>]
    let br = "<br/>"

    let inline wrapWith (openBoundary: string) (closeBoundary: string) (contents: string list) =
        [ openBoundary; yield! contents; closeBoundary ]

    let wrapInFSharpCode = wrapWith Boundaries.openFSharpCode Boundaries.closeCode
    let wrapInCode = wrapWith Boundaries.openCode Boundaries.closeCode
    let wrapInExample = wrapWith Boundaries.openExample Boundaries.closeExample
    let wrapInSummary = wrapWith Boundaries.openSummary Boundaries.closeSummary
    let wrapInRemarks = wrapWith Boundaries.openRemarks Boundaries.closeRemarks
    let wrapInPara = wrapWith Boundaries.openPara Boundaries.closePara

    module Helpers =
        let makeCompatibilityLine (compat: Compatibility) =
            let makeEmoji =
                function
                | true -> "✔"
                | false -> "❌"

            match compat with
            | Unspecific -> ValueNone
            | Specified compatability ->
                [ "⚠ OS Compatibility: "
                  "WIN " + makeEmoji compatability.Windows + " | "
                  "MAC " + makeEmoji compatability.Mac + " | "
                  "LIN " + makeEmoji compatability.Linux + " | "
                  "MAS " + makeEmoji compatability.Mas ]
                |> ValueSome

        let makeProcessLine (processBlock: Decoder.ProcessBlock) =
            let makeEmoji =
                function
                | true -> "✔"
                | false -> "❌"

            [ "⚠ Process Availability: "
              "Main " + makeEmoji processBlock.Main + " | "
              "Renderer " + makeEmoji processBlock.Renderer + " | "
              "Utility " + makeEmoji processBlock.Utility + " | "
              "Exported " + makeEmoji processBlock.Exported ]

        let chunkDocStringBySize size (docString: string) =
            docString.ReplaceLineEndings(br).Split(' ')
            |> Array.chunkBySize size
            |> Array.map (String.concat " ")
            |> Array.map (sprintf "/// %s")

        let makeDocs (docList: string list) =
            docList |> List.toArray |> Array.collect (chunkDocStringBySize 20)

module Type =
    type internal ApiType = Prelude.Type
    type internal FcsType = Fantomas.Core.SyntaxOak.Type
    let inline internal makeSimple text = FcsType.Anon(SingleTextNode.make text)

    type internal Fantomas.Core.SyntaxOak.Type with
        static member boolean = makeSimple "bool"
        static member integer = makeSimple "int"
        static member float = makeSimple "float"
        static member double = makeSimple "double"
        static member string = makeSimple "string"
        static member obj = makeSimple "obj"
        static member option = makeSimple "option"
        static member unit = makeSimple "unit"
        static member Option = makeSimple "Option"
        static member Promise = makeSimple "Promise"

    type FantomasFactory =
        static member makeUnion(fcsTypes: ApiType list) : FcsType =
            // MAPPING KNOWN EXCEPTIONS
            match fcsTypes with
            | typs when typs |> List.forall _.IsPromise ->
                typs
                |> List.map (
                    function
                    | Promise innerType -> innerType
                    | _ -> failwith "UNREACHABLE"
                )
                |> ApiType.OneOf
                |> List.singleton
                |> FantomasFactory.makePromise
            | [ typ; ApiType.Unit | ApiType.Undefined ] -> [ typ ] |> FantomasFactory.makeOption
            | [ typ; ApiType.Unknown | ApiType.Any ] ->
                TypeAppPrefixNode.Create(makeSimple "U2", [ FantomasFactory.mapToFantomas typ; FcsType.obj ])
                |> FcsType.AppPrefix
            | typs ->
                let length = typs.Length

                match length with
                | _ when length < 8 ->
                    TypeAppPrefixNode.Create(makeSimple $"U{length}", typs |> List.map FantomasFactory.mapToFantomas)
                    |> FcsType.AppPrefix
                | _ -> failwith $"Cannot create a U# type union with a length of {length}"

        static member makeCollection(fcsType: FcsType) : FcsType =
            TypeArrayNode(fcsType, 1, Range.Zero) |> FcsType.Array

        static member makeLambda(funcOrMethod: FuncOrMethod) : FcsType =
            let maybeIsDelegate = Type.Cache.GetFuncOrMethod funcOrMethod.PathKey

            match funcOrMethod.Parameters.Length > 1, maybeIsDelegate with
            | true, ValueSome delegatedFunc ->
                delegatedFunc.PathKey
                |> Path.tracePathOfEntry
                |> List.map (_.ValueOrModified >> toPascalCase)
                |> String.concat "."
                |> makeSimple
            | _, _ ->
                let normalizedParameters =
                    match funcOrMethod.Parameters with
                    | [] ->
                        [ Parameter.Positional
                              { ParameterInfo.Description = ValueNone
                                Required = true
                                Type = ApiType.Unit } ]
                    | parameters -> parameters

                let parameters =
                    normalizedParameters
                    |> List.map (
                        function
                        | Named(_, info)
                        | Positional info ->
                            let wrap: FcsType -> _ = if info.Required then id else FantomasFactory.makeOption
                            info.Type |> FantomasFactory.mapToFantomas |> wrap
                        | InlinedObjectProp _ -> failwith "Lambdas should not have inlined object props"
                    )

                let names =
                    normalizedParameters
                    |> List.mapi (fun i _ ->
                        if i = normalizedParameters.Length - 1 then
                            SingleTextNode.make "->"
                        else
                            SingleTextNode.make "*"
                    )

                let parameterNamePairs = names |> List.zip parameters

                TypeFunsNode(
                    parameterNamePairs,
                    funcOrMethod.Returns |> FantomasFactory.mapToFantomas,
                    range = Range.Zero
                )
                |> FcsType.Funs

        static member makePromise(apiTypes: ApiType list) : FcsType =
            TypeAppPrefixNode.Create(FcsType.Promise, apiTypes |> List.map FantomasFactory.mapToFantomas)
            |> FcsType.AppPrefix

        static member makeOption(apiTypes: ApiType list) : FcsType =
            TypeAppPrefixNode.Create(FcsType.Option, List.map FantomasFactory.mapToFantomas apiTypes)
            |> FcsType.AppPrefix

        static member makeOption(fcsType: FcsType) : FcsType =
            TypeAppPrefixNode.Create(FcsType.Option, [ fcsType ]) |> FcsType.AppPrefix

        static member makeTuple(fcsTypes: FcsType list) : FcsType =
            TypeTupleNode(
                fcsTypes
                |> List.collect (fun t -> [ Choice1Of2 t; Choice2Of2(SingleTextNode.make "*") ])
                |> List.cutOffLast,
                Range.Zero
            )
            |> FcsType.Tuple

        static member makeTuple(apiTypes: ApiType list) : FcsType =
            apiTypes |> List.map FantomasFactory.mapToFantomas |> FantomasFactory.makeTuple
        //%mapToFantomas%START%
        static member mapToFantomas(apiType: ApiType) : FcsType =
            match apiType with
            | ApiType.Any
            | Prelude.Type.Undefined
            | ApiType.Unknown -> FcsType.obj
            | ApiType.Unit -> FcsType.unit
            | Prelude.Type.Boolean -> FcsType.boolean
            | Prelude.Type.Date -> FcsType.LongIdent(IdentListNode.make [ "System"; "DateTime" ])
            | Prelude.Type.Double -> FcsType.double
            | Prelude.Type.Float
            | Prelude.Type.Number -> FcsType.float
            | Prelude.Type.Integer -> FcsType.integer
            | Prelude.Type.String -> FcsType.string
            | Prelude.Type.StructureRef value ->
                //highlight-start
                match value with
                | "UserDefaultTypes[Type]" -> FcsType.string
                | "ClientRequestConstructorOptions" -> makeSimple "ClientRequest.Options"
                | "MenuItemConstructorOptions" -> makeSimple "MenuItem.Options"
                | "SaveDialogOptions" -> FcsType.obj
                | "PopupOptions" -> makeSimple "Menu.Popup.Options"
                | "GlobalRequest" -> makeSimple "Request"
                | "GlobalResponse" -> makeSimple "Response"
                | "ReadableStream" -> makeSimple "Readable<obj>"
                | "Type" -> makeSimple "UserType" // self implemented string DU
                | value when value.StartsWith "NodeJS." ->
                    value.Substring "NodeJS.".Length
                    |> Prelude.Type.StructureRef
                    |> FantomasFactory.mapToFantomas
                | value when value.StartsWith "Electron." ->
                    value.Substring "Electron.".Length
                    |> Prelude.Type.StructureRef
                    |> FantomasFactory.mapToFantomas
                //highlight-end //%mapToFantomas%END%
                | _ ->
                    Path.Cache.retrievePath value
                    |> function
                        | Path.Cache.OkProcessMap { Main = ValueSome value }
                        | Path.Cache.OkProcessMap { Renderer = ValueSome value }
                        | Path.Cache.OkProcessMap { Utility = ValueSome value }
                        | Path.Cache.OkSimple value ->
                            value
                            |> Path.tracePathOfEntry
                            |> List.rev
                            |> function
                                | [] -> failwith "unreachable"
                                | head :: tail ->
                                    head.ValueOrModified :: (tail |> List.map (_.ValueOrModified >> toPascalCase))
                            |> List.rev
                            |> String.concat "."
                            |> makeSimple
                        | Path.Cache.NoResult -> makeSimple (value |> toPascalCase)
                        | Path.Cache.OkProcessMap _ -> failwith "Ok Process Map must have at least one field filled"
            | Function funcOrMethod -> FantomasFactory.makeLambda funcOrMethod
            | Event { PathKey = pathKey } when pathKey.Name.ValueOrModified = "Event" -> makeSimple "Event"
            | Object { Properties = [] } -> FcsType.obj
            | Event { PathKey = pathKey }
            | Object { PathKey = pathKey }
            | StringEnum { PathKey = pathKey } ->
                Path.tracePathOfEntry pathKey
                |> List.map (_.ValueOrModified >> _.Trim(''') >> toPascalCase)
                |> String.concat "."
                |> makeSimple
            | Promise innerType -> FantomasFactory.makePromise [ innerType ]
            | Record(key, value) ->
                TypeAppPrefixNode.Create(makeSimple "Record", [ key; value ] |> List.map FantomasFactory.mapToFantomas)
                |> Type.AppPrefix
            | EventRef ``type`` -> FantomasFactory.mapToFantomas ``type``
            | Partial(``type``, _) ->
                // TODO - implementation that accounts for string args
                FantomasFactory.mapToFantomas ``type``
            | Omit(``type``, l) ->
                // TODO - implementation that accounts for string args
                FantomasFactory.mapToFantomas ``type``
            | Pick(``type``, l) ->
                // TODO - implementation that accounts for string args
                FantomasFactory.mapToFantomas ``type``
            | Collection ``type`` -> FantomasFactory.mapToFantomas ``type`` |> FantomasFactory.makeCollection
            | Array ``type`` -> FantomasFactory.mapToFantomas ``type`` |> FantomasFactory.makeCollection
            //%TouchBarItemsMap%START%
            | OneOf [ Prelude.Type.StructureRef "TouchBarButton"
                      Prelude.Type.StructureRef "TouchBarColorPicker"
                      Prelude.Type.StructureRef "TouchBarGroup"
                      Prelude.Type.StructureRef "TouchBarLabel"
                      Prelude.Type.StructureRef "TouchBarPopover"
                      Prelude.Type.StructureRef "TouchBarScrubber"
                      Prelude.Type.StructureRef "TouchBarSegmentedControl"
                      Prelude.Type.StructureRef "TouchBarSlider"
                      Prelude.Type.StructureRef "TouchBarSpacer" ] ->
                FantomasFactory.mapToFantomas (Prelude.Type.StructureRef Spec.touchBarItemsName)
            | OneOf types when
                types
                |> List.truncate 9
                |> (=)
                    [ Prelude.Type.StructureRef "TouchBarButton"
                      Prelude.Type.StructureRef "TouchBarColorPicker"
                      Prelude.Type.StructureRef "TouchBarGroup"
                      Prelude.Type.StructureRef "TouchBarLabel"
                      Prelude.Type.StructureRef "TouchBarPopover"
                      Prelude.Type.StructureRef "TouchBarScrubber"
                      Prelude.Type.StructureRef "TouchBarSegmentedControl"
                      Prelude.Type.StructureRef "TouchBarSlider"
                      Prelude.Type.StructureRef "TouchBarSpacer" ]
                ->
                Prelude.Type.OneOf
                    [ yield Prelude.Type.StructureRef Spec.touchBarItemsName
                      yield! types |> List.skip 9 ]
                |> FantomasFactory.mapToFantomas
            //%TouchBarItemsMap%END%
            | OneOf types -> FantomasFactory.makeUnion types
            | Constant literalType ->
                // TODO - where this is encountered, we actually have
                // to emit the expression so we can add the constant arg.
                FcsType.string
            | Tuple types -> FantomasFactory.makeTuple types
            | Join(structureRef, props) ->
                // TODO - for the ONE case that this occurs =,=
                makeSimple structureRef

/// <summary>
/// Module for functions types and caches relating to creating interfaces for Event parameters to provide
/// a simplified DX when working with multi argument events in Fable.
/// </summary>
module EventInterfaces =
    // We will provide two means for using event handlers. One is
    // curried, the other is accessing the args via an interface for
    // named and documented details
    let private cache = HashSet<Event>()
    let private rootModuleName = "EventInterfaces"

    let private getInterfaceName: Event -> string =
        _.PathKey.Name.ValueOrModified >> sprintf "IOn%s"

    let private getInterfaceModuleRoot: Event -> _ =
        _.PathKey.ParentName
        >> _.ValueOrModified
        >> toPascalCase
        >> sprintf "%s.%s" rootModuleName

    /// <summary>
    /// Indicates why an event failed to get added to the interface cache
    /// </summary>
    type CacheRejectionReason =
        | AlreadyExists of Path.PathKey
        | HasLessThanTwoParameters

    /// <summary>
    /// A simple DU returned when adding an event which provides the interface name and module path
    /// to reference as the type parameter for the overloaded handler that uses the interface.
    /// </summary>
    let eventInterfaceFullPath: Path.PathKey -> _ =
        Path.tracePathOfEntry
        >> List.mapWithLast (_.ValueOrModified >> toPascalCase) (_.ValueOrModified >> sprintf "IOn%s")
        >> String.concat "."

    /// <summary>
    /// Try to add an Event to be lifted into an interface. If the Event is a candidate, then it will return
    /// the intended path to access the interface when it is generated later.
    /// </summary>
    /// <remarks>
    /// It will fail and indicate the reason as either being: because it had one or less parameters; because
    /// it already existed in the cache (with the details attached to access that event).
    /// </remarks>
    let addEvent: Event -> Result<Path.PathKey, CacheRejectionReason> =
        function
        // We don't need to create an interface for events with one or less parameters
        | { Parameters = [] | [ _ ] } -> Error CacheRejectionReason.HasLessThanTwoParameters
        | event ->
            event.PathKey
            |> match cache.Add event with
               | true -> Ok
               | false -> CacheRejectionReason.AlreadyExists >> Error

    let makeInterfaces () =
        cache
        |> Seq.toList
        |> List.choose (fun event ->
            let name = getInterfaceName event

            let typeAttributes =
                MultipleAttributeListNode.make
                    [ if StabilityStatus.IsExperimental event then
                          "Experimental(\"Indicated to be Experimental by Electron\")"
                      if StabilityStatus.IsDeprecated event then
                          "System.Obsolete()"
                      "System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)"
                      "AllowNullLiteral"
                      "Interface" ]

            let parameterMembers =
                event.Parameters
                |> List.mapi (fun idx ->
                    function
                    | Parameter.InlinedObjectProp prop ->
                        // TODO - there are no cases of this, but this is not handled
                        // correctly because the attributes should emit different
                        let name = prop.PathKey.Name.ValueOrModified

                        let xmlDocs =
                            [ match XmlDocs.Helpers.makeCompatibilityLine prop.Compatibility with
                              | ValueSome docs -> docs |> XmlDocs.wrapInPara |> String.concat ""
                              | ValueNone -> ()
                              match prop.Description with
                              | ValueSome docs -> docs
                              | ValueNone -> () ]
                            |> function
                                | [] -> None
                                | docs ->
                                    docs
                                    |> XmlDocs.wrapInSummary
                                    |> XmlDocs.Helpers.makeDocs
                                    |> fun docs -> XmlDocNode(docs, Range.Zero)
                                    |> Some

                        let attributes =
                            MultipleAttributeListNode.make
                                [ $"Emit(\"$0[{idx}]\")"
                                  if StabilityStatus.IsExperimental event then
                                      "Experimental(\"Indicated to be Experimental by Electron\")"
                                  if StabilityStatus.IsDeprecated event then
                                      "System.Obsolete()" ]

                        MemberDefnAbstractSlotNode(
                            xmlDocs,
                            Some attributes,
                            MultipleTextsNode.make "abstract member",
                            SingleTextNode.make name,
                            None,
                            prop.Type |> Type.FantomasFactory.mapToFantomas,
                            Some(MultipleTextsNode.make "with get, set"),
                            Range.Zero
                        )
                        |> MemberDefn.AbstractSlot
                    | p ->
                        match p with
                        | Named(name, paramInfo) -> name.Name.ValueOrModified, paramInfo
                        | Positional paramInfo -> $"arg{idx}", paramInfo
                        | _ -> failwith "UNREACHABLE"
                        |> fun (name, paramInfo) ->
                            let name, attributes =
                                match name with
                                | "``...args``"
                                | "...args" -> "args", MultipleAttributeListNode.make $"Emit(\"$0.slice({idx})\")"
                                | _ -> name, MultipleAttributeListNode.make $"Emit(\"$0[{idx}]\")"

                            let xmlDocs =
                                paramInfo.Description
                                |> ValueOption.map (
                                    List.singleton
                                    >> XmlDocs.wrapInSummary
                                    >> XmlDocs.Helpers.makeDocs
                                    >> fun docs -> XmlDocNode(docs, Range.Zero)
                                )
                                |> ValueOption.toOption

                            MemberDefnAbstractSlotNode(
                                xmlDocs,
                                Some attributes,
                                MultipleTextsNode.make "abstract member",
                                SingleTextNode.make name,
                                None,
                                paramInfo.Type |> Type.FantomasFactory.mapToFantomas,
                                Some(MultipleTextsNode.make "with get, set"),
                                Range.Zero
                            )
                            |> MemberDefn.AbstractSlot
                )

            let typeName =
                let docs =
                    [ match XmlDocs.Helpers.makeCompatibilityLine event.Compatibility with
                      | ValueSome docs -> docs |> XmlDocs.wrapInPara |> String.concat ""
                      | ValueNone -> ()
                      match event.Description with
                      | ValueSome docs -> docs
                      | ValueNone -> () ]
                    |> function
                        | [] -> None
                        | docs ->
                            docs
                            |> XmlDocs.wrapInSummary
                            |> XmlDocs.Helpers.makeDocs
                            |> fun docs -> XmlDocNode(docs, Range.Zero)
                            |> Some

                TypeNameNode(
                    docs,
                    Some typeAttributes,
                    SingleTextNode.make "type",
                    None,
                    IdentListNode.make name,
                    None,
                    [],
                    None,
                    Some(SingleTextNode.make "="),
                    None,
                    Range.Zero
                )
            // This should not be possible anyway. We filter out no member parameters
            // when adding the events.
            match parameterMembers with
            | [] -> None
            | parameterMembers ->
                Some(
                    event.PathKey,
                    TypeDefnRegularNode(typeName, parameterMembers, Range.Zero)
                    |> Compatibility.wrapInCompatibilityDirective event.Compatibility
                    |> TypeDefn.Regular
                )
        )

type GeneratorContainer =
    { PathKey: Path.PathKey
      Constructor: Parameter list option
      InstanceProperties: Property list
      InstanceMethods: Method list
      InstanceEvents: Event list
      StaticProperties: Property list
      StaticMethods: Method list
      StaticEvents: Event list
      TypeAttributes: string list
      Description: string voption
      Process: Decoder.ProcessBlock option
      Compatibility: Compatibility option
      Extends: string voption }

module GeneratorContainer =
    let create path =
        { PathKey = path
          Constructor = None
          InstanceProperties = []
          InstanceMethods = []
          InstanceEvents = []
          StaticProperties = []
          StaticMethods = []
          StaticEvents = []
          TypeAttributes = []
          Description = ValueNone
          Process = None
          Compatibility = None
          Extends = ValueNone }

    let withConstructor (props: Parameter list) container =
        { container with
            GeneratorContainer.Constructor = Some props }

    let withInstanceProperties (props: Property list) container =
        { container with
            InstanceProperties = props }

    let withInstanceMethods (methods: Method list) container =
        { container with
            InstanceMethods = methods }

    let withInstanceEvents (events: Event list) container =
        { container with
            InstanceEvents = events }

    let withStaticProperties (props: Property list) container =
        { container with
            GeneratorContainer.StaticProperties = props }

    let withStaticMethods (methods: Method list) container =
        { container with
            GeneratorContainer.StaticMethods = methods }

    let withStaticEvents (events: Event list) container =
        { container with
            GeneratorContainer.StaticEvents = events }

    let withAttribute (attr: string) container =
        { container with
            TypeAttributes = attr :: container.TypeAttributes }

    let withAttributes (attr: string list) container =
        { container with TypeAttributes = attr }

    let withExtends (extension: string) container =
        { container with
            GeneratorContainer.Extends = ValueSome extension }

    let makeImported container =
        withAttribute $"Import(\"{container.PathKey.Name.ValueOrSource}\", \"electron\")" container

    let inline mergeCompatability (object: ^T when ^T: (member Compatibility: Compatibility)) container =
        { container with
            GeneratorContainer.Compatibility = Some object.Compatibility }

    let inline mergeDescription (object: ^T when ^T: (member Description: string voption)) container =
        { container with
            GeneratorContainer.Description = object.Description }

    let inline mergeProcess (object: ^T when ^T: (member Process: Decoder.ProcessBlock)) container =
        { container with
            GeneratorContainer.Process = Some object.Process }

    let inline mergeExtension (object: ^T when ^T: (member Extends: string voption)) container =
        { container with
            GeneratorContainer.Extends = object.Extends }

    let private makeSimpleInterfaceNode name =
        MemberDefnInterfaceNode(SingleTextNode.make "interface", Type.makeSimple name, None, [], Range.Zero)

    let private makeSimpleInheritNode name =
        MemberDefnInheritNode(SingleTextNode.make "inherit", Type.makeSimple name, Range.Zero)

    let inline private makeEventEmitterInterface
        (object: ^T when ^T: (member Events: Event list) and ^T: (member Description: string voption))
        =
        [ if
              object.Events.Length > 0
              || object.Description
                 |> ValueOption.exists _.Contains("is an EventEmitter", System.StringComparison.Ordinal)
          then
              makeSimpleInterfaceNode Spec.eventEmitterName ]

    let private makeParamObjectParameterAttribute (parameters: Parameter list) =
        parameters
        |> List.tryFindIndex _.IsInlinedObjectProp
        |> Option.map (
            fun idx -> $"ParamObject({idx})"
            >> List.singleton
        )
        |> Option.defaultValue []

    let private makeNamedNode (attributes: string list option) required typ name =
        PatParameterNode(
            attributes |> Option.map MultipleAttributeListNode.make,
            Pattern.Named(
                PatNamedNode(
                    None,
                    (match required with
                     | true -> SingleTextNode.make name
                     | false -> SingleTextNode.makeOptional name),
                    Range.Zero
                )
            ),
            Some typ,
            Range.Zero
        )

    let private makeMappedNamedNode (attributes: string list option) required typ name =
        match name with
        | "...args" ->
            struct {| Attributes =
                       attributes
                       |> function
                           | Some attrs -> Some("System.ParamArray" :: attrs)
                           | None -> Some [ "System.ParamArray" ]
                      Type = typ
                      Required = required
                      Name = "args" |}
        | _ ->
            struct {| Attributes = attributes
                      Type = typ
                      Required = required
                      Name = name |}
        |> fun data -> makeNamedNode data.Attributes data.Required data.Type data.Name

    let private makePropParameterDocLine (prop: Property) =
        [ match prop.Compatibility |> XmlDocs.Helpers.makeCompatibilityLine with
          | ValueSome docs -> docs |> String.concat ""
          | _ -> ()
          match prop.Description with
          | ValueSome docs -> docs
          | _ -> () ]
        |> function
            | [] -> ""
            | docs -> docs |> String.concat " || "
        |> XmlDocs.makeParam prop.PathKey.Name.ValueOrModified

    let private makeParameterDocLine (parameter: ParameterInfo) (name: string) =
        parameter.Description |> ValueOption.defaultValue "" |> XmlDocs.makeParam name

    let private makePropMemberDocsLines (prop: Property) =
        [ match prop.Compatibility |> XmlDocs.Helpers.makeCompatibilityLine with
          | ValueSome docs -> docs |> XmlDocs.wrapInPara |> String.concat ""
          | ValueNone -> ()
          match prop.Description with
          | ValueSome docs -> docs
          | _ -> () ]
        |> function
            | [] -> None
            | docs -> docs |> Some

    let makeParametersXmlDocs (parameter: Parameter list) =
        parameter
        |> List.mapi (fun idx ->
            function
            | Positional parameterInfo -> makeParameterDocLine parameterInfo $"arg{idx}"
            | Named(name, info) -> makeParameterDocLine info name.Name.ValueOrModified
            | InlinedObjectProp prop -> makePropParameterDocLine prop
        )

    let private makeDocNode (wrapperFunction: string list -> string list) (lines: string list) =
        lines
        |> function
            | [] -> None
            | docs ->
                docs
                |> wrapperFunction
                |> XmlDocs.Helpers.makeDocs
                |> fun docs -> XmlDocNode(docs, Range.Zero) |> Some

    let private makePropParameter (prop: Property) =
        let typ = prop.Type |> Type.FantomasFactory.mapToFantomas
        let required = prop.Required
        let name = prop.PathKey.Name.ValueOrModified
        let attributes = None
        makeMappedNamedNode attributes required typ name

    let private makePropParameterWithDirectives (prop: Property) =
        [ SingleTextNode.make ","
          |> Compatibility.wrapInCompatibilityDirectiveBefore prop.Compatibility
          |> Choice2Of2
          makePropParameter prop
          |> Compatibility.wrapInCompatibilityDirectiveAfter prop.Compatibility
          |> Pattern.Parameter
          |> Choice1Of2 ]

    let makeReturnInfoNode typ =
        BindingReturnInfoNode(SingleTextNode.make ":", Type.FantomasFactory.mapToFantomas typ, Range.Zero)

    let unitReturnInfo = makeReturnInfoNode Type.Unit

    let private cutOffFirstTextNodeWithDirectiveMaybe
        (compatibility: Compatibility)
        (parameters: Choice<Pattern, SingleTextNode> list)
        =
        match compatibility, parameters with
        | Specified _, []
        | Unspecific, _ -> parameters |> List.tryTail
        | Specified _, _ ->
            parameters
            |> List.tryTail
            |> function
                | head :: tail ->
                    match head with
                    | Choice1Of2(Pattern.Parameter para) ->
                        (para
                         |> Compatibility.wrapInCompatibilityDirectiveBefore compatibility
                         |> Pattern.Parameter
                         |> Choice1Of2)
                        :: tail
                    | _ -> failwith "UNREACHABLE"
                | _ -> failwith "UNREACHABLE"

    let private makeParameter (parameter: ParameterInfo) (name: string) =
        let typ = parameter.Type |> Type.FantomasFactory.mapToFantomas
        let required = parameter.Required
        let attributes = None
        makeMappedNamedNode attributes required typ name

    let private makePositionalParameter (idx: int) (parameterInfo: ParameterInfo) =
        [ SingleTextNode.make "," |> Choice2Of2
          $"arg%i{idx}" |> makeParameter parameterInfo |> Pattern.Parameter |> Choice1Of2 ]

    let private makeNamedParameter (name: Path.PathKey) (parameterInfo: ParameterInfo) =
        [ SingleTextNode.make "," |> Choice2Of2
          name.Name.ValueOrModified
          |> makeParameter parameterInfo
          |> Pattern.Parameter
          |> Choice1Of2 ]

    let private makeFunsPattern (typ: Type.ApiType) =
        typ |> Type.FantomasFactory.mapToFantomas, SingleTextNode.make "->"

    let private makeFunsTypeNode returnInfo (types: Type.ApiType list) =
        types
        |> List.map makeFunsPattern
        |> function
            | [] -> (Type.makeSimple "unit", SingleTextNode.make "->") |> List.singleton
            | typs -> typs
        |> fun typs -> TypeFunsNode(typs, returnInfo, Range.Zero) |> Type.Funs

    let private makeFunsUnitTypeNode (types: Type.ApiType list) =
        types |> makeFunsTypeNode (Type.makeSimple "unit")

    let private makeFunsParameter (parameterInfo: Parameter) =
        parameterInfo
        |> function
            | Positional { Type = typ }
            | InlinedObjectProp { Type = typ }
            | Named(_, { Type = typ }) -> makeFunsPattern typ

    let private makeFunsTypeNodeFromParameters returnInfo (parameters: Parameter list) =
        parameters
        |> List.map makeFunsParameter
        |> function
            | [] -> makeFunsPattern Type.ApiType.Unit |> List.singleton
            | patts -> patts
        |> fun typeFunsPat -> TypeFunsNode(typeFunsPat, returnInfo, Range.Zero) |> Type.Funs

    let private makeUnitFunsTypeNode parameters =
        parameters |> makeFunsTypeNodeFromParameters (Type.makeSimple "unit")

    let private makeUnitFunsParameterNode required name parameters =
        (makeUnitFunsTypeNode parameters, name) ||> makeNamedNode None required

    let private wrapParametersIntoParenNode (parameters: Choice<Pattern, SingleTextNode> list) =
        PatTupleNode(parameters, Range.Zero)
        |> Pattern.Tuple
        |> PatParenNode.make
        |> Pattern.Paren

    let private makeAttributesNode (attributes: string list) =
        attributes
        |> function
            | [] -> None
            | attrs -> MultipleAttributeListNode.make attrs |> Some

    let inline private makeAttributesForStability object =
        [ if StabilityStatus.IsExperimental object then
              "Experimental(\"Experimental according to Electron\")"
          if StabilityStatus.IsDeprecated object then
              "System.Obsolete" ]

    let private makeAttributesForStaticProperty (prop: Property) =
        let isStropped: string -> bool =
            fun str -> str.StartsWith("``") && str.EndsWith("``")

        let isSourceName =
            prop.PathKey.Name
            |> function
                | Modified(_, text) -> isStropped text
                | Source _ -> true

        [ if not isSourceName then
              $"Emit(\"{prop.PathKey.ParentName.ValueOrSource}.{prop.PathKey.Name.ValueOrSource}{{{{ = $0 }}}}\")"
          yield! makeAttributesForStability prop ]

    let private makeAttributesForInstanceProperty (prop: Property) =
        let isStropped: string -> bool =
            fun str -> str.StartsWith("``") && str.EndsWith("``")

        let isSourceName =
            prop.PathKey.Name
            |> function
                | Modified(_, text) -> isStropped text
                | Source _ -> true

        [ if not isSourceName then
              $"Emit(\"$0.{prop.PathKey.Name.ValueOrSource}{{{{ = $1 }}}}\")"
          yield! makeAttributesForStability prop ]

    let private makePropertyMemberDefn (isStatic: bool) (prop: Property) =
        let isStropped: string -> bool =
            fun str -> str.StartsWith("``") && str.EndsWith("``")

        let isSourceName =
            prop.PathKey.Name
            |> function
                | Modified(_, text) -> isStropped text
                | Source _ -> true

        let docs =
            makePropMemberDocsLines prop |> Option.bind (makeDocNode XmlDocs.wrapInSummary)

        let attributes =
            if isStatic then
                makeAttributesForStaticProperty prop
            else
                makeAttributesForInstanceProperty prop
            |> List.append [ "Erase" ]
            |> makeAttributesNode

        match isSourceName with
        | true ->
            MemberDefnAutoPropertyNode(
                docs,
                attributes,
                MultipleTextsNode.make
                    [ if isStatic then
                          "static"
                      "member"
                      "val" ],
                None,
                SingleTextNode.make prop.PathKey.Name.ValueOrModified,
                Some(prop.Type |> Type.FantomasFactory.mapToFantomas),
                SingleTextNode.make "=",
                Expr.Ident(SingleTextNode.make "Unchecked.defaultof<_>"),
                Some(MultipleTextsNode.make (if prop.ReadOnly then "with get" else "with get, set")),
                Range.Zero
            )
            |> Choice1Of2
        | false ->
            MemberDefnPropertyGetSetNode(
                docs,
                attributes,
                MultipleTextsNode.make
                    [ if isStatic then
                          "static"
                      "member" ],
                None,
                None,
                IdentListNode.make
                    [ if not isStatic then
                          "_"
                      prop.PathKey.Name.ValueOrModified ],
                SingleTextNode.make "with",
                PropertyGetSetBindingNode(
                    None,
                    None,
                    None,
                    SingleTextNode.make "get",
                    [ PatTupleNode([], Range.Zero)
                      |> Pattern.Tuple
                      |> PatParenNode.make
                      |> Pattern.Paren ],
                    Some(makeReturnInfoNode prop.Type),
                    SingleTextNode.make "=",
                    Expr.Ident(SingleTextNode.make "Unchecked.defaultof<_>"),
                    Range.Zero
                ),
                (if prop.ReadOnly then
                     None
                 else
                     SingleTextNode.make "and" |> Some),
                (if prop.ReadOnly then
                     None
                 else
                     PropertyGetSetBindingNode(
                         None,
                         None,
                         None,
                         SingleTextNode.make "set",
                         [ makeNamedNode None true (Type.FantomasFactory.mapToFantomas prop.Type) "value"
                           |> Pattern.Parameter
                           |> PatParenNode.make
                           |> Pattern.Paren ],
                         None,
                         SingleTextNode.make "=",
                         Expr.Ident(SingleTextNode.make "()"),
                         Range.Zero
                     )
                     |> Some),
                Range.Zero
            )
            |> Choice2Of2

    let private makePropertyMemberDefnWithDirectives (isStatic: bool) (prop: Property) =
        prop
        |> makePropertyMemberDefn isStatic
        |> function
            | Choice1Of2 node ->
                node
                |> Compatibility.wrapInCompatibilityDirective prop.Compatibility
                |> MemberDefn.AutoProperty
            | Choice2Of2 node ->
                node
                |> Compatibility.wrapInCompatibilityDirective prop.Compatibility
                |> MemberDefn.PropertyGetSet

    let private makeStaticPropertyMemberDefnWithDirectives =
        makePropertyMemberDefnWithDirectives true

    let private makeInstancePropertyMemberDefnWithDirectives =
        makePropertyMemberDefnWithDirectives false

    let private makeStaticBindingNode inlined xmlDocs attributes (name: string) parameters returnInfo =
        BindingNode(
            xmlDocs,
            attributes,
            MultipleTextsNode.make [ "static"; "member" ],
            false,
            (if inlined then Some(SingleTextNode.make "inline") else None),
            None,
            Choice1Of2(IdentListNode.make name),
            None,
            parameters,
            Some returnInfo,
            SingleTextNode.make "=",
            Expr.Ident(SingleTextNode.make "Unchecked.defaultof<_>"),
            Range.Zero
        )

    let private makeStaticBindingNodeWithDirectives
        compatibility
        inlined
        xmlDocs
        attributes
        (name: string)
        parameters
        returnInfo
        =
        makeStaticBindingNode inlined xmlDocs attributes name parameters returnInfo
        |> Compatibility.wrapInCompatibilityDirective compatibility

    let private makeInstanceBindingNode
        inlined
        xmlDocs
        attributes
        (name: string)
        parameters
        (returnInfo: BindingReturnInfoNode)
        =
        let selfIdentifier, returnInfo, exprText =
            match returnInfo.Type with
            | Type.Anon text when text.Text = "This" || text.Text = "this" -> "this", None, "this"
            | _ -> "_", Some returnInfo, "Unchecked.defaultof<_>"

        BindingNode(
            xmlDocs,
            attributes,
            MultipleTextsNode.make [ "member" ],
            false,
            (if inlined then Some(SingleTextNode.make "inline") else None),
            None,
            Choice1Of2(IdentListNode.make [ selfIdentifier; name ]),
            None,
            parameters,
            returnInfo,
            SingleTextNode.make "=",
            Expr.Ident(SingleTextNode.make exprText),
            Range.Zero
        )

    let private makeInstanceBindingNodeWithDirectives
        compatibility
        inlined
        xmlDocs
        attributes
        (name: string)
        parameters
        returnInfo
        =
        makeInstanceBindingNode inlined xmlDocs attributes name parameters returnInfo
        |> Compatibility.wrapInCompatibilityDirective compatibility

    let makeInterfaces (this: GeneratorContainer) =
        [ if
              this.InstanceEvents.Length > 0
              || this.StaticEvents.Length > 0
              || this.Description
                 |> ValueOption.exists _.Contains("is an EventEmitter", System.StringComparison.Ordinal)
          then
              makeSimpleInterfaceNode Spec.eventEmitterName |> MemberDefn.Interface ]

    let makeInheritance (this: GeneratorContainer) =
        [ match this.Extends with
          | ValueSome name -> makeSimpleInheritNode name |> MemberDefn.Inherit
          | ValueNone -> () ]

    let makeAbstractPropertyNode xmlDocs attributes name typ isWritable =
        MemberDefnAbstractSlotNode(
            xmlDocs,
            attributes,
            MultipleTextsNode.make [ "abstract"; "member" ],
            SingleTextNode.make name,
            None,
            typ,
            (if isWritable then
                 Some(MultipleTextsNode.make [ "with"; "get,"; "set" ])
             else
                 None),
            Range.Zero
        )

    let makeParameterNodesForParameters (parameters: Parameter list) =
        let mutable idx = 0

        parameters
        |> List.collect (
            function
            | Parameter.InlinedObjectProp prop -> (prop.Type |> Type.FantomasFactory.mapToFantomas, prop) |> Choice1Of2
            | Parameter.Positional info ->
                let name = Name.createCamel $"arg{idx}"
                idx <- idx + 1
                (info.Type |> Type.FantomasFactory.mapToFantomas, name, info) |> Choice2Of2
            | Parameter.Named(name, info) ->
                (info.Type |> Type.FantomasFactory.mapToFantomas, name.Name, info) |> Choice2Of2
            >> function
                | Choice1Of2(typ, prop) ->
                    [ makeNamedNode None prop.Required typ prop.PathKey.Name.ValueOrModified
                      |> Pattern.Parameter ]
                | Choice2Of2(typ, name, paramInfo) ->
                    [ makeNamedNode None paramInfo.Required typ name.ValueOrModified
                      |> Pattern.Parameter ]
        )

    let makeConstructor (this: GeneratorContainer) =
        let attributes (parameters: Parameter list) =
            if this.TypeAttributes |> List.exists ((=) "JS.Pojo") then
                None
            else
                makeParamObjectParameterAttribute parameters
                |> function
                    | [] -> None
                    | attrs -> MultipleAttributeListNode.make attrs |> Some

        let mutable idx = 0

        this.Constructor
        |> function
            | None ->
                // Since we use `member val` properties to be compact where we can,
                // we are required to have a primary constructor. To satisfy this requirement, we make a private primary
                // constructor when there would otherwise not be one.
                if
                    this.InstanceProperties |> List.isNotEmpty
                    || this.StaticProperties |> List.isNotEmpty
                then
                    ImplicitConstructorNode(
                        None,
                        None,
                        Some(SingleTextNode.make "private"),
                        Pattern.Paren(PatParenNode.make (Pattern.Null(SingleTextNode.make ""))),
                        None,
                        Range.Zero
                    )
                    |> Some
                else
                    None
            | Some parameters ->
                parameters
                |> List.sortByDescending _.Required
                |> List.collect (
                    function
                    | Parameter.InlinedObjectProp prop -> makePropParameterWithDirectives prop
                    | Parameter.Positional info ->
                        idx <- idx + 1
                        makePositionalParameter idx info
                    | Parameter.Named(name, info) -> makeNamedParameter name info
                )
                |> cutOffFirstTextNodeWithDirectiveMaybe (
                    parameters
                    |> List.tryHead
                    |> Option.map _.Compatibility
                    |> Option.defaultValue Unspecific
                )
                |> wrapParametersIntoParenNode
                |> fun pat ->
                    ImplicitConstructorNode(
                        makeParametersXmlDocs parameters |> makeDocNode id,
                        attributes parameters,
                        None,
                        pat,
                        None,
                        Range.Zero
                    )
                |> Some

    let makeMethodAttributes (method: Method) =
        [ match method.PathKey.Name with
          | Modified(source, _) -> $"CompiledName(\"{source}\")"
          | Source _ -> ()

          yield! method.Parameters |> makeParamObjectParameterAttribute

          yield! method |> makeAttributesForStability ]

    let inline makeCompatibilityXmlDocs
        (wrapperFunction: string list -> string list)
        (object: ^T when ^T: (member Compatibility: Compatibility))
        =
        XmlDocs.Helpers.makeCompatibilityLine object.Compatibility
        |> ValueOption.map wrapperFunction
        |> ValueOption.defaultValue []

    let inline makeDescriptionXmlDocs (object: ^T when ^T: (member Description: string voption)) =
        object.Description
        |> ValueOption.map List.singleton
        |> ValueOption.defaultValue []

    let inline makeProcessXmlDocs (object: ^T when ^T: (member Process: Decoder.ProcessBlock)) =
        object.Process |> XmlDocs.Helpers.makeProcessLine

    let inline makeParameterXmlDocs (object: ^T when ^T: (member Parameters: Parameter list)) =
        object.Parameters |> makeParametersXmlDocs

    let makeMethodBindingNode attributes isInline (isStatic: bool) (method: Method) =
        let xmlDocs =
            method
            |> makeParameterXmlDocs
            |> List.append (
                [ yield!
                      method
                      |> makeCompatibilityXmlDocs (String.concat "" >> List.singleton >> XmlDocs.wrapInPara)
                  yield! method |> makeDescriptionXmlDocs ]
                |> XmlDocs.wrapInSummary
            )
            |> makeDocNode id

        let attributes =
            makeMethodAttributes method |> List.append attributes |> makeAttributesNode

        let firstOptionalIdx =
            method.Parameters
            |> List.tryFindIndex (_.Required >> not)
            |> Option.defaultValue method.Parameters.Length

        (if isStatic then
             makeStaticBindingNodeWithDirectives
         else
             makeInstanceBindingNodeWithDirectives)
            method.Compatibility
            isInline
            xmlDocs
            attributes
            method.PathKey.Name.ValueOrModified
            (method.Parameters
             |> List.mapi (fun idx ->
                 function
                 | Positional info ->
                     let info =
                         if idx > firstOptionalIdx then
                             { info with Required = false }
                         else
                             info

                     makePositionalParameter idx info
                 | Named(name, info) ->
                     let info =
                         if idx > firstOptionalIdx then
                             { info with Required = false }
                         else
                             info

                     makeNamedParameter name info
                 | InlinedObjectProp prop ->
                     let prop =
                         if idx > firstOptionalIdx then
                             { prop with Required = false }
                         else
                             prop

                     [ Choice2Of2(SingleTextNode.make ",")
                       makePropParameter prop |> Pattern.Parameter |> Choice1Of2 ]
             )
             |> List.collect id
             |> cutOffFirstTextNodeWithDirectiveMaybe Unspecific
             |> wrapParametersIntoParenNode
             |> List.singleton)
            (makeReturnInfoNode method.ReturnType)

    let makeEventAttributes (isStatic: bool) (functionPrefix: string) (event: Event) =
        [ $"Emit(\"$0.{functionPrefix}('{event.PathKey.Name.ValueOrSource}', $1)\")"
          if isStatic then
              $"Import(\"{event.PathKey.ParentName.ValueOrSource}\", \"electron\")" ]

    let makeEventBindingNode (isInline: bool) (isStatic: bool) (event: Event) =
        let prefixes = [ "on"; "once"; "off" ]

        let docs =
            [ yield!
                  event
                  |> makeCompatibilityXmlDocs (String.concat "" >> List.singleton >> XmlDocs.wrapInPara)
              yield! event |> makeDescriptionXmlDocs ]
            |> makeDocNode XmlDocs.wrapInSummary

        let cachedEventInterfaceResult = EventInterfaces.addEvent event

        prefixes
        |> List.collect (fun functionPrefix ->
            let attributes =
                makeEventAttributes isStatic functionPrefix event |> makeAttributesNode
            (* Alternative to the above; this would hide the off/once prefix events since they
                    serve little purpose in intellisense. *)
            // |> match functionPrefix with
            //     | "on" -> makeAttributesNode
            //     | _ ->
            //         fun attrs ->
            //             "System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)" :: attrs
            //             |> makeAttributesNode

            let parameter =
                makeUnitFunsParameterNode true "handler" event.Parameters
                |> Pattern.Parameter
                |> Choice1Of2
                |> List.singleton
                |> wrapParametersIntoParenNode
                |> List.singleton

            [ (if isStatic then
                   makeStaticBindingNodeWithDirectives
               else
                   makeInstanceBindingNodeWithDirectives)
                  event.Compatibility
                  isInline
                  docs
                  attributes
                  $"{functionPrefix}{event.PathKey.Name.ValueOrModified}"
                  parameter
                  unitReturnInfo
              match cachedEventInterfaceResult with
              | Error(EventInterfaces.CacheRejectionReason.AlreadyExists resultValue)
              | Ok resultValue ->
                  makeFunsTypeNode
                      (Type.makeSimple "unit")
                      [ resultValue
                        |> EventInterfaces.eventInterfaceFullPath
                        |> Type.ApiType.StructureRef ]
                  |> makeNamedNode None true
                  |> fun f -> f "handler"
                  |> Pattern.Parameter
                  |> Choice1Of2
                  |> List.singleton
                  |> wrapParametersIntoParenNode
                  |> List.singleton
                  |> fun parameter ->
                      (if isStatic then
                           makeStaticBindingNodeWithDirectives
                       else
                           makeInstanceBindingNodeWithDirectives)
                          event.Compatibility
                          isInline
                          docs
                          attributes
                          $"{functionPrefix}{event.PathKey.Name.ValueOrModified}"
                          parameter
                          unitReturnInfo
              | _ -> () ]
        )

    let makeXmlDocs (this: GeneratorContainer) =
        [ match this.Process with
          | Some text ->
              text
              |> XmlDocs.Helpers.makeProcessLine
              |> XmlDocs.wrapInPara
              |> String.concat ""
          | None -> ()
          match this.Compatibility with
          | Some compat ->
              match
                  compat
                  |> XmlDocs.Helpers.makeCompatibilityLine
                  |> ValueOption.map (XmlDocs.wrapInPara >> String.concat "")
              with
              | ValueSome text -> text
              | ValueNone -> ()
          | None -> ()
          match this.Description with
          | ValueSome text -> text
          | ValueNone -> () ]
        |> function
            | [] -> None
            | docs -> Some docs
        |> Option.map (
            XmlDocs.wrapInSummary
            >> XmlDocs.Helpers.makeDocs
            >> fun docs -> XmlDocNode(docs, Range.Zero)
        )

    let makeTypeNameNodeWithNameMap (mapper: Path.PathKey -> string) (this: GeneratorContainer) =
        let docs = makeXmlDocs this

        let attributes =
            match this.TypeAttributes with
            | [] -> None
            | attrs -> MultipleAttributeListNode.make attrs |> Some

        let name = IdentListNode.make (mapper this.PathKey)
        let constructor = this |> makeConstructor

        TypeNameNode(
            docs,
            attributes,
            SingleTextNode.make "type",
            None,
            name,
            None,
            [],
            constructor,
            Some(SingleTextNode.make "="),
            None,
            Range.Zero
        )

    let makeTypeNameNode =
        makeTypeNameNodeWithNameMap (
            function
            | Path.PathKey.Module path -> path.Name.ValueOrSource |> toStroppedCamelCase |> _.ValueOrModified
            | path -> path.Name.ValueOrModified |> toPascalCase
        )

    let private makeSignatureParameterTypeNode
        isRequired
        (attributes: string list)
        (name: string)
        (type': Type.FcsType)
        =
        let name, additives =
            match name with
            | "``...args``"
            | "...args" -> "args", [ "System.ParamArray" ]
            | name -> name, []

        let attributes = additives @ attributes

        TypeSignatureParameterNode(
            makeAttributesNode attributes,
            (if isRequired then
                 SingleTextNode.make
             else
                 SingleTextNode.makeOptional)
                name
            |> Some,
            type',
            Range.Zero
        )

    let private makeDelegateParameterTupleNode
        isRequired
        (attributes: string list)
        (name: string)
        (type': Type.FcsType)
        =
        [ makeSignatureParameterTypeNode isRequired attributes name type'
          |> Type.SignatureParameter
          |> Choice1Of2
          SingleTextNode.make "*" |> Choice2Of2 ]

    let private makeDelegateFunsTypeNodeFromParameters returnInfo (parameters: Parameter list) =
        let parenthesiseLambdas =
            function
            | Type.FcsType.Funs _ as typ ->
                TypeParenNode(PatParenNode.makeOpening, typ, PatParenNode.makeClosing, Range.Zero)
                |> Type.Paren
            | typ -> typ

        parameters
        |> List.mapi (fun idx ->
            function
            | Positional info ->
                info.Type
                |> Type.FantomasFactory.mapToFantomas
                |> parenthesiseLambdas
                |> makeDelegateParameterTupleNode info.Required [] $"arg{idx}"
            | Named(name, info) ->
                info.Type
                |> Type.FantomasFactory.mapToFantomas
                |> parenthesiseLambdas
                |> makeDelegateParameterTupleNode info.Required [] name.Name.ValueOrModified
            | InlinedObjectProp(prop) ->
                prop.Type
                |> Type.FantomasFactory.mapToFantomas
                |> parenthesiseLambdas
                |> makeDelegateParameterTupleNode prop.Required [] prop.PathKey.Name.ValueOrModified
        )
        |> List.collect id
        |> List.cutOffLast
        |> fun typs -> TypeTupleNode(typs, Range.Zero) |> Type.Tuple, SingleTextNode.make "->"
        |> List.singleton
        |> fun typs -> TypeFunsNode(typs, returnInfo, Range.Zero)

    let makeStaticMethods (this: GeneratorContainer) =
        this.StaticMethods
        |> List.map (makeMethodBindingNode [ "Erase" ] true true >> MemberDefn.Member)

    let makeInstanceMethods (this: GeneratorContainer) =
        this.InstanceMethods
        |> List.map (makeMethodBindingNode [ "Erase" ] true false >> MemberDefn.Member)

    let makeStaticProperties (this: GeneratorContainer) =
        this.StaticProperties |> List.map makeStaticPropertyMemberDefnWithDirectives

    let makeInstanceProperties (this: GeneratorContainer) =
        this.InstanceProperties |> List.map makeInstancePropertyMemberDefnWithDirectives

    let makeInstanceEvents (this: GeneratorContainer) =
        this.InstanceEvents
        |> List.collect (makeEventBindingNode true false)
        |> List.map MemberDefn.Member

    let makeStaticEvents (this: GeneratorContainer) =
        this.StaticEvents
        |> List.collect (makeEventBindingNode true true)
        |> List.map MemberDefn.Member

    /// <summary>
    /// The advantage of using this over the regular type definitions in Fantomas is that
    /// we avoid any issues if all the members of the class are erased by conditional compiler
    /// directives.
    /// </summary>
    /// <param name="this"></param>
    let makeExplicitTypeDefn (this: GeneratorContainer) =
        TypeDefnExplicitNode(
            makeTypeNameNode this,
            TypeDefnExplicitBodyNode(SingleTextNode.make "class", [], SingleTextNode.make "end", Range.Zero),
            [ yield! makeInheritance this
              yield! makeInterfaces this
              yield! makeInstanceEvents this
              yield! makeInstanceMethods this
              yield! makeInstanceProperties this
              yield! makeStaticEvents this
              yield! makeStaticMethods this
              yield! makeStaticProperties this ],
            Range.Zero
        )

    let makeDefaultTypeDefn (this: GeneratorContainer) =
        TypeDefnRegularNode(
            makeTypeNameNode this,
            [ yield! makeInheritance this
              yield! makeInterfaces this
              yield! makeInstanceEvents this
              yield! makeInstanceMethods this
              yield! makeInstanceProperties this
              yield! makeStaticEvents this
              yield! makeStaticMethods this
              yield! makeStaticProperties this ],
            Range.Zero
        )
    // let makeDefaultTypeDecl = makeDefaultTypeDefn >> TypeDefn.Regular >> ModuleDecl.TypeDefn
    /// <summary>
    /// The advantage of using the ExplicitTypeDefn node is the initial 'class end' statement.
    /// This prevents compiler conditional directives causing issue where all the members may be erased on a condition.
    /// </summary>
    let makeDefaultTypeDecl =
        makeExplicitTypeDefn >> TypeDefn.Explicit >> ModuleDecl.TypeDefn

    let private makeDefaultDelegateDefn (funcOrMethod: FuncOrMethod) =
        let returns = funcOrMethod.Returns |> Type.FantomasFactory.mapToFantomas

        let attributes =
            makeParamObjectParameterAttribute funcOrMethod.Parameters |> makeAttributesNode

        let typNode = makeDelegateFunsTypeNodeFromParameters returns funcOrMethod.Parameters
        let docs = makeParametersXmlDocs funcOrMethod.Parameters |> makeDocNode id

        let typeNameNode =
            TypeNameNode(
                docs,
                attributes,
                SingleTextNode.make "type",
                None,
                IdentListNode.make funcOrMethod.PathKey.Name.ValueOrModified,
                None,
                [],
                None,
                SingleTextNode.make "=" |> Some,
                None,
                Range.Zero
            )
        // TypeNameNode.makeSimple(funcOrMethod.Name.Name.ValueOrModified, docs=docs, attributes = attributes)
        TypeDefnDelegateNode(typeNameNode, SingleTextNode.make "delegate", typNode, Range.Zero)

    let makeDefaultDelegateTypeDecl =
        makeDefaultDelegateDefn >> TypeDefn.Delegate >> ModuleDecl.TypeDefn

    let makeDefaultEventInfoDefn (eventInfo: GeneratorContainer) =
        let members =
            [ yield! makeInheritance eventInfo
              yield!
                  eventInfo.InstanceProperties
                  |> List.map (fun prop ->
                      let xmlDocs =
                          makePropMemberDocsLines prop |> Option.bind (makeDocNode XmlDocs.wrapInSummary)

                      let attributes = makeAttributesForInstanceProperty prop |> makeAttributesNode

                      makeAbstractPropertyNode
                          xmlDocs
                          attributes
                          prop.PathKey.Name.ValueOrModified
                          (prop.Type |> Type.FantomasFactory.mapToFantomas)
                          true
                      |> MemberDefn.AbstractSlot
                  ) ]

        let attributes = eventInfo.TypeAttributes |> makeAttributesNode

        let typeNameNode =
            TypeNameNode.Create(eventInfo.PathKey.Name.ValueOrModified, ?attributes = attributes)

        TypeDefnRegularNode.Create(typeNameNode, members)

    let makeDefaultEventInfoTypeDecl =
        makeDefaultEventInfoDefn >> TypeDefn.Regular >> ModuleDecl.TypeDefn

    let makeDefaultEventStringConstant (pathKey: Path.PathKey) =
        BindingNode(
            None,
            (MultipleAttributeListNode.make [ "Literal"; "Erase" ] |> Some),
            MultipleTextsNode.make "let",
            false,
            None,
            None,
            Choice1Of2(IdentListNode.make pathKey.Name.ValueOrModified),
            None,
            [],
            None,
            SingleTextNode.make "=",
            Expr.makeString pathKey.Name.ValueOrSource,
            Range.Zero
        )
        |> ModuleDecl.TopLevelBinding


    let map (func: GeneratorContainer -> 'a) = func

    let mapPathKey (func: Path.PathKey -> Path.PathKey) group =
        { group with
            GeneratorContainer.PathKey = group.PathKey |> func }


[<Struct>]
type OpenModule = private OpenModule of string list

module OpenModule =
    let create: string -> _ =
        // make sure that there are no spaces.
        _.Split()
        >> Array.exactlyOne
        // Split the dots
        >> _.Split('.')
        >> Array.toList
        // Create
        >> OpenModule

    let value (OpenModule value) = value

    let toOpenNode =
        value
        >> IdentListNode.make
        >> fun identNode -> OpenModuleOrNamespaceNode(identNode, Range.Zero) |> Open.ModuleOrNamespace

type GeneratorGroupChild =
    | Nested of GeneratorGrouper
    | Child of GeneratorContainer
    | StringEnumType of StringEnum
    | Delegate of FuncOrMethod
    | EventInterface of pathKey: Path.PathKey * node: TypeDefn
    | EventConstant of pathKey: Path.PathKey

and GeneratorGrouper =
    { PathKey: Path.PathKey voption
      Description: string list voption
      Opens: OpenModule list
      Children: GeneratorGroupChild list
      Attributes: string list }

module GeneratorGrouper =
    let createRoot () =
        { PathKey = ValueNone
          Description = ValueNone
          Opens = []
          Children = []
          Attributes = [] }

    let create pathKey =
        { PathKey = ValueSome pathKey
          Description = ValueNone
          Opens = []
          Children = []
          Attributes = [] }

    /// <summary>
    /// Modules to open added will be opened in the order given
    /// </summary>
    /// <param name="targetModule"></param>
    /// <param name="grouper"></param>
    let addOpen targetModule grouper =
        { grouper with
            Opens = OpenModule.create targetModule :: grouper.Opens }

    let addAttribute attr grouper =
        { grouper with
            Attributes = attr :: grouper.Attributes }

    let withAttributes attributes grouper =
        { grouper with Attributes = attributes }

    let makeMultipleAttributeListNode =
        _.Attributes
        >> function
            | [] -> None
            | attrs -> attrs |> MultipleAttributeListNode.make |> Some

    let makeOpenListNode =
        _.Opens >> List.rev >> List.map OpenModule.toOpenNode >> OpenListNode

    let makeXmlDocNode =
        _.Description
        >> function
            | ValueSome []
            | ValueNone -> None
            | ValueSome docs ->
                docs
                |> XmlDocs.Helpers.makeDocs
                |> fun docs -> XmlDocNode(docs, Range.Zero) |> Some

    let withOpens opens grouper = { grouper with Opens = opens }

    let withDescription desc grouper =
        { grouper with
            GeneratorGrouper.Description = desc }

    let withPathKey path grouper =
        { grouper with
            GeneratorGrouper.PathKey = path }

    let withChildren children grouper = { grouper with Children = children }

    let private addChild child grouper =
        { grouper with
            Children = child :: grouper.Children }

    let addNestedGroup group =
        if group.PathKey.IsNone then
            failwith "A nested GeneratorGrouper must have a pathkey to represent a module"

        addChild (GeneratorGroupChild.Nested group)

    let addTypeChild typ =
        addChild (GeneratorGroupChild.Child typ)

    let addDelegateChild deleg =
        addChild (GeneratorGroupChild.Delegate deleg)

    let addStringEnumChild stringEnum =
        addChild (GeneratorGroupChild.StringEnumType stringEnum)

    let isNested =
        _.PathKey
        >> function
            | ValueSome path -> path.Path.IsRoot |> not
            | ValueNone -> false

    let makeNestedModule grouper =
        if grouper.PathKey.IsNone then
            failwith "A nested module must have a pathkey"

        fun decls ->
            NestedModuleNode(
                makeXmlDocNode grouper,
                makeMultipleAttributeListNode grouper,
                SingleTextNode.make "module",
                None,
                false,
                IdentListNode.make grouper.PathKey.Value.Name.ValueOrModified,
                SingleTextNode.make "=",
                decls,
                Range.Zero
            )
            |> ModuleDecl.NestedModule

    let private makeModuleOrNamespace addRootNamespace isNamespace isRecursive grouper =
        let leader = if isNamespace then "namespace" else "module"

        let pathName =
            IdentListNode.make
                [ if addRootNamespace then
                      Spec.rootNamespace
                  match grouper.PathKey with
                  | ValueNone -> ()
                  | ValueSome path -> path.Name.ValueOrModified ]

        let header =
            ModuleOrNamespaceHeaderNode(
                makeXmlDocNode grouper,
                makeMultipleAttributeListNode grouper,
                MultipleTextsNode.make leader,
                None,
                isRecursive,
                Some pathName,
                Range.Zero
            )

        fun decls -> ModuleOrNamespaceNode(Some header, decls, Range.Zero)

    let makeNamespace isRecursive grouper =
        makeModuleOrNamespace true true isRecursive grouper

    let makeModule isRecursive grouper =
        makeModuleOrNamespace true false isRecursive grouper

    let makeModuleOrNamespaceNode grouper =
        if isNested grouper then
            makeNestedModule grouper |> Choice1Of2
        else
            match grouper.PathKey with
            | ValueNone -> makeNamespace false grouper
            | _ -> makeModule false grouper
            |> Choice2Of2

    let makeChildren func : GeneratorGrouper -> ModuleDecl list = _.Children >> List.map func

    let makeDefaultDelegateType =
        function
        | GeneratorGroupChild.Delegate funcOrMethod ->
            GeneratorContainer.makeDefaultDelegateTypeDecl funcOrMethod |> Some
        | _ -> None

    let makeDefaultEventInterfaceAndTypeAlias =
        function
        | GeneratorGroupChild.EventInterface(_, typeDefn) -> typeDefn |> ModuleDecl.TypeDefn |> Some
        | _ -> None

    let makeDefaultEventStringConstant =
        function
        | GeneratorGroupChild.EventConstant path -> GeneratorContainer.makeDefaultEventStringConstant path |> Some
        | _ -> None

    let makeDefaultStringEnumType =
        function
        | GeneratorGroupChild.StringEnumType stringEnum ->
            TypeNameNode.makeSimple (
                stringEnum.PathKey.Name.ValueOrModified,
                attributes = [ "StringEnum(CaseRules.None)"; "RequireQualifiedAccess" ]
            )
            |> fun typeNameNode ->
                stringEnum.Cases
                |> List.map (fun case ->
                    case.Description
                    |> ValueOption.map (
                        List.singleton
                        >> XmlDocs.wrapInSummary
                        >> XmlDocs.Helpers.makeDocs
                        >> fun docs -> XmlDocNode(docs, Range.Zero)
                    )
                    |> ValueOption.toOption
                    |> fun docs ->
                        UnionCaseNode(
                            docs,
                            Some(MultipleAttributeListNode.make $"CompiledName(\"{case.Value.ValueOrSource}\")"),
                            Some(SingleTextNode.make "|"),
                            SingleTextNode.make case.Value.ValueOrModified,
                            [],
                            Range.Zero
                        )
                )
                |> fun cases ->
                    TypeDefnUnionNode(typeNameNode, None, cases, [], Range.Zero)
                    |> TypeDefn.Union
                    |> ModuleDecl.TypeDefn
                |> Some
        | _ -> None

    let mapPathKey (func: Path.PathKey voption -> Path.PathKey voption) group =
        { group with
            GeneratorGrouper.PathKey = group.PathKey |> func }


type Structure with
    member this.ToGeneratorContainer() =
        Path.PathKey.Type(Path.Type(Path.ModulePath.Root, this.Name))
        |> GeneratorContainer.create
        |> GeneratorContainer.mergeDescription this
        |> GeneratorContainer.withAttribute "JS.Pojo"
        |> GeneratorContainer.withInstanceProperties this.Properties
        |> GeneratorContainer.withConstructor (this.Properties |> List.map Parameter.InlinedObjectProp)

type StringEnum with
    static member tryDebugTypeGen(stringEnums: StringEnum list) =
        GeneratorGrouper.create <| Path.PathKey.CreateModule(Source "Enums")
        |> GeneratorGrouper.withChildren (stringEnums |> List.map GeneratorGroupChild.StringEnumType)
        |> _.Children
        |> List.choose GeneratorGrouper.makeDefaultStringEnumType
        |> fun decls ->
            Oak([], [ ModuleOrNamespaceNode(None, decls, Range.Zero) ], Range.Zero)
            |> CodeFormatter.FormatOakAsync
            |> Async.RunSynchronously
            |> printfn "%s"

type Class with
    member this.ToGeneratorContainer() =
        GeneratorContainer.create this.PathKey
        |> match this.PathKey.Name.ValueOrSource with
           | "TouchBarSpacer"
           | "TouchBarButton"
           | "TouchBarColorPicker"
           | "TouchBarGroup"
           | "TouchBarLabel"
           | "TouchBarPopover"
           | "TouchBarScrubber"
           | "TouchBarSegmentedControl"
           | "TouchBarSlider"
           | "TouchBarSpacer" as name ->
               GeneratorContainer.withAttribute $"Import(\"TouchBar.{name}\", \"electron\")"
               >> GeneratorContainer.withStaticProperties this.StaticProperties
           | "TouchBar" ->
               GeneratorContainer.makeImported
               >> GeneratorContainer.withStaticProperties (
                   this.StaticProperties
                   |> List.filter (
                       _.PathKey.Name.ValueOrSource
                       >> function
                           | "TouchBarButton"
                           | "TouchBarColorPicker"
                           | "TouchBarGroup"
                           | "TouchBarLabel"
                           | "TouchBarPopover"
                           | "TouchBarScrubber"
                           | "TouchBarSegmentedControl"
                           | "TouchBarSlider"
                           | "TouchBarSpacer"
                           | "TouchBarOtherItemsProxy" -> false
                           | _ -> true
                   )
               )
           | _ ->
               GeneratorContainer.makeImported
               >> GeneratorContainer.withStaticProperties this.StaticProperties
        |> GeneratorContainer.mergeDescription this
        |> GeneratorContainer.mergeProcess this
        |> GeneratorContainer.withInstanceEvents this.Events
        |> GeneratorContainer.withInstanceMethods this.Methods
        |> GeneratorContainer.withInstanceProperties this.Properties
        |> GeneratorContainer.withStaticMethods this.StaticMethods
        |> fun gen ->
            this.Constructor
            |> Option.map (fun parameters -> GeneratorContainer.withConstructor parameters gen)
            |> Option.defaultValue gen

type Module with
    member this.ToGeneratorContainer() =
        GeneratorContainer.create this.PathKey
        |> GeneratorContainer.makeImported
        |> GeneratorContainer.mergeDescription this
        |> GeneratorContainer.mergeProcess this
        |> GeneratorContainer.withStaticMethods this.Methods
        |> GeneratorContainer.withStaticEvents this.Events
        |> GeneratorContainer.withStaticProperties this.Properties

    member this.ToGeneratorGrouper() =
        if this.ExportedClasses.Length = 0 then
            None
        else
            let grouper = GeneratorGrouper.create this.PathKey

            this.ExportedClasses
            |> List.fold (fun state item -> GeneratorGrouper.addTypeChild (item.ToGeneratorContainer()) state) grouper
            |> GeneratorGrouper.addAttribute "Erase"
            |> Some

type Element with
    member this.ToGeneratorContainer() =
        GeneratorContainer.create this.PathKey
        |> GeneratorContainer.makeImported
        |> GeneratorContainer.mergeDescription this
        |> GeneratorContainer.mergeProcess this
        |> GeneratorContainer.withInstanceEvents this.Events
        |> GeneratorContainer.withInstanceMethods this.Methods
        |> GeneratorContainer.withInstanceProperties this.Properties