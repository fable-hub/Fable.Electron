[<AutoOpen>]
module ElectronApi.Json.Parser.Generator

open System
open System.IO
open ElectronApi.Json.Parser.Decoder
open ElectronApi.Json.Parser.Prelude
open ElectronApi.Json.Parser.SourceMapper
open Fantomas.Core
open Fantomas.Core.SyntaxOak
open Fantomas.FCS.Text
open Thoth.Json.Net

let private makeModuleGrouper text =
    GeneratorGrouper.create (Path.PathKey.CreateModule(Source text))

type private NamePath = Name list
let private getNamePath: Path.PathKey -> NamePath = Path.tracePathOfEntry

/// <summary>
/// Provides the head of the list, only in the case that doing so does not leave the list empty.
/// This ensures the tail is always populated.
/// </summary>
let private pathHead: NamePath -> Name option =
    function
    | [ _ ] -> None
    | head :: _ -> Some head
    | [] -> failwith "Should never reach this point"

/// <summary>
/// Used in conjunction with <c>pathHead</c>; provides the tail of a name path, asserting
/// that it contains at least one item.
/// </summary>
let private pathTail: NamePath -> NamePath =
    function
    | [] -> failwith "Should never reach this point"
    | [ _ ] as tail -> tail
    | _ :: tail -> tail

/// <summary>
/// Checks whether a <c>GeneratorGrouper</c> contains a nested grouper with the given name.
/// Name equality is checked by the source value, with pascal casing applied.
/// </summary>
/// <param name="name"></param>
/// <param name="grouper"></param>
let private groupContainsNestedModuleName (name: Name) (grouper: GeneratorGrouper) =
    grouper.Children
    |> List.exists (
        function
        | Nested { PathKey = ValueSome path } ->
            name.ValueOrSource |> toPascalCase = (path.Name.ValueOrSource |> toPascalCase)
        | _ -> false
    )

/// <summary>
/// Applies the given <c>GeneratorGrouper</c> mapping function recursively along a name path,
/// adding new <c>GeneratorGrouper</c>'s as it goes, until it reaches the tail of the name path (which
/// would indicate the name of the child itself).
/// </summary>
/// <param name="func"></param>
/// <param name="path"></param>
/// <param name="grouper"></param>
let rec private mapGroupForPath
    (func: GeneratorGrouper -> GeneratorGrouper)
    (path: NamePath)
    (grouper: GeneratorGrouper)
    =
    match pathHead path with
    | Some name ->
        let path = pathTail path

        if groupContainsNestedModuleName name grouper then
            { grouper with
                Children =
                    grouper.Children
                    |> List.map (
                        function
                        | Nested({ PathKey = ValueSome nestedPath } as group) when
                            (nestedPath.Name.ValueOrSource |> toPascalCase) = (name.ValueOrSource |> toPascalCase)
                            ->
                            mapGroupForPath func path group |> Nested
                        | child -> child
                    ) }
        else
            grouper
            |> GeneratorGrouper.addNestedGroup (
                makeModuleGrouper (name.ValueOrModified |> toPascalCase)
                |> mapGroupForPath func path
            )
    | None -> func grouper

/// <summary>
/// Adds a given child to a generator group, creating nested generator groups until the final
/// part of its name path is reached (indicating the name of the child itself).
/// </summary>
/// <param name="child"></param>
/// <param name="group"></param>
let private addToGroup (child: GeneratorGroupChild) (group: GeneratorGrouper) =
    let add =
        fun genGroup ->
            { genGroup with
                Children = child :: genGroup.Children }

    match child with
    | Nested generatorGrouper -> generatorGrouper.PathKey.Value
    | Child generatorContainer -> generatorContainer.PathKey
    | StringEnumType stringEnum -> stringEnum.PathKey
    | Delegate funcOrMethod -> funcOrMethod.PathKey
    | EventConstant pathKey
    | EventInterface(pathKey, _) -> pathKey
    |> getNamePath
    |> fun namePath -> mapGroupForPath add namePath group

/// <summary>
/// A map/bind-like pattern against a GeneratorGrouper which is a nested child, found by the
/// keyname given.
/// </summary>
/// <param name="keyName"></param>
/// <param name="func"></param>
/// <param name="generatorGrouper"></param>
let private mapNestedGroup
    (keyName: string)
    (func: GeneratorGrouper -> GeneratorGrouper)
    (generatorGrouper: GeneratorGrouper)
    =
    let result =
        { generatorGrouper with
            Children =
                generatorGrouper.Children
                |> List.map (
                    function
                    | Nested({ PathKey = ValueSome path } as group) when path.Name.ValueOrModified = keyName ->
                        Nested(func group)
                    | child -> child
                ) }

    result

/// <summary>
/// Abstraction which applies a list of <c>Option.bind</c>-like functions to an input,
/// with a default func applied if all return <c>None</c>
/// </summary>
/// <param name="funcs"></param>
/// <param name="orElse"></param>
/// <param name="input"></param>
let private tryWith (funcs: ('a -> 'b option) list) (orElse: 'a -> 'b) (input: 'a) =
    funcs
    |> List.fold
        (fun state func ->
            match state with
            | None -> func input
            | _ -> state
        )
        None
    |> Option.defaultWith (fun () -> orElse input)

#nowarn 40

/// <summary>
/// Converts a <c>GeneratorGrouper</c> into Fantomas module declarations recursively.
/// </summary>
let rec private finalizeGeneratorGroup: GeneratorGrouper -> ModuleDecl list =
    // Applies a list of functions that act upon the child types in turn, until one or none
    // provide a `Some` value.
    GeneratorGrouper.makeChildren (
        tryWith
            [ GeneratorGrouper.makeDefaultDelegateType
              GeneratorGrouper.makeDefaultStringEnumType
              GeneratorGrouper.makeDefaultEventInterfaceAndTypeAlias
              GeneratorGrouper.makeDefaultEventStringConstant ]
            (function
            // On failing to produce a `Some` from above, we apply the default transformer/fantomas compiler
            | Child typ -> GeneratorContainer.makeDefaultTypeDecl typ
            // Any nested groups are finalized in a similar manner
            | Nested group -> finalizeGeneratorGroup group |> (GeneratorGrouper.makeNestedModule group)
            // Principally, the other child types should be handled in the above
            | _ -> failwith "handled"
            )
    )

/// <summary>
/// The namespace Fable.Electron which wraps all subsequent child modules.
/// </summary>
let private rootGeneratorGroup =
    GeneratorGrouper.createRoot ()
    // List all dependencies here
    |> GeneratorGrouper.addOpen "Fable.Core"
    |> GeneratorGrouper.addOpen "Fable.Core.JS"
    |> GeneratorGrouper.addOpen "Browser.Types"
    |> GeneratorGrouper.addOpen "Node.Buffer"
    |> GeneratorGrouper.addOpen "Node.WorkerThreads"
    |> GeneratorGrouper.addOpen "Node.Base"
    |> GeneratorGrouper.addOpen "Node.Stream"
    |> GeneratorGrouper.addOpen "Fetch"
    // Can pre-make child modules if you want to add specific attributes
    // or documentation or other
    |> GeneratorGrouper.addNestedGroup (makeModuleGrouper "Main")
    |> GeneratorGrouper.addNestedGroup (makeModuleGrouper "Utility")
    |> GeneratorGrouper.addNestedGroup (makeModuleGrouper "Renderer")
    |> GeneratorGrouper.addNestedGroup (
        makeModuleGrouper "Enums"
        |> GeneratorGrouper.addAttribute "AutoOpen"
        |> GeneratorGrouper.addAttribute "Fable.Core.Erase"
    )
    |> GeneratorGrouper.addNestedGroup (
        makeModuleGrouper "Types"
        |> GeneratorGrouper.addAttribute "AutoOpen"
        |> GeneratorGrouper.addAttribute "Fable.Core.Erase"
    )

module Transpiler =
    let private readFile =
        // Parse the API
        File.ReadAllText
        >> Decode.fromString decode
        >> function
            | Ok values -> values
            | Error e -> failwith e
        // Preliminary run and conversion of raw info into unions and records that are more
        // idiomatic.
        >> readResults

    let private consolidateGeneratorContainers =
        // Generate the source compiler helpers, and add them to the root module, while
        // creating any nested modules that are requires as per the path of the type
        List.fold (fun state ->
            function
            | ModifiedResult.Module result ->
                let moduleContainer = result.ToGeneratorContainer()

                result.ToGeneratorGrouper()
                |> Option.map (fun g -> state |> GeneratorGrouper.addNestedGroup g)
                |> Option.defaultValue state
                |> addToGroup (moduleContainer |> Child)
            | ModifiedResult.Class result -> state |> addToGroup (result.ToGeneratorContainer() |> Child)
            | ModifiedResult.Structure result ->
                state
                |> addToGroup (
                    result.ToGeneratorContainer()
                    |> GeneratorContainer.mapPathKey
                        _.AddRootModule(Path.Module.Module(Path.ModulePath.Root, Source "Types"))
                    |> Child
                )
            | ModifiedResult.Element result -> state |> addToGroup (result.ToGeneratorContainer() |> Child)
        )

    let private fillEventInterfaceCache =
        fun group ->
            finalizeGeneratorGroup group
            |> List.append [ GeneratorGrouper.makeOpenListNode group |> ModuleDecl.OpenList ]
            |> (GeneratorGrouper.makeNamespace false group)
            |> ignore

            group

    let private getInlineObjects values =
        List.append
            (Type.Cache.GetInlineObjects()
             |> List.map (fun structOrObject -> structOrObject.PathKey, Type.Object structOrObject))
            values

    let private getDelegates values =
        List.append
            (
            // Generates delegates for functions
            Type.Cache.GetFuncOrMethods()
            |> List.map (fun funcOrMethod -> funcOrMethod.PathKey, Type.Function funcOrMethod))
            values

    let private getStringEnums values =
        List.append
            (
            // Generates string enums
            Type.Cache.GetStringEnums(true)
            |> List.map (fun stringEnum -> stringEnum.PathKey, Type.StringEnum stringEnum))
            values

    let private getEventInfo values =
        List.append
            (
            // Generates the types of Events that have additional properties available
            // in their handlers.
            Type.Cache.GetEventInfos()
            |> List.map (fun eventInfo -> eventInfo.PathKey, Type.Event eventInfo))
            values

    let private processModifiedResultList resultList =
        resultList
        |> consolidateGeneratorContainers rootGeneratorGroup
        // Holy mackarel of inefficiency Doctor! Dummy run to fill our caches
        // TODO - only the EventInterface caching system still relies on this;
        //  once refactored to the new caching system can remove dummy run
        |> fillEventInterfaceCache
        // Access the caches and add the generated records to the root namespace
        |> fun group ->
            ([]: (Path.PathKey * Type.ApiType) list)
            |> getInlineObjects
            |> getDelegates
            |> getStringEnums
            |> getEventInfo
            // Remove any duplicates that may arise TODO - log/warn these
            |> List.distinctBy fst
            // Add the above to the root namespace
            |> List.fold
                (fun state (_, item) ->
                    match item with
                    | Function funcOrMethod -> state |> addToGroup (Delegate funcOrMethod)
                    | StringEnum stringEnum -> state |> addToGroup (StringEnumType stringEnum)
                    | Event eventInfo ->
                        eventInfo.PathKey
                        |> GeneratorContainer.create
                        |> GeneratorContainer.withAttribute "Interface"
                        |> GeneratorContainer.withAttribute "AllowNullLiteral"
                        |> GeneratorContainer.withExtends "Event"
                        |> GeneratorContainer.withInstanceProperties eventInfo.Properties
                        |> GeneratorContainer.makeDefaultEventInfoDefn
                        |> fun decl ->
                            state
                            |> addToGroup ((eventInfo.PathKey, TypeDefn.Regular decl) |> EventInterface)
                    | Object structOrObject ->
                        structOrObject.PathKey
                        |> GeneratorContainer.create
                        |> GeneratorContainer.withAttributes
                            [ "JS.Pojo"
                              if structOrObject.PathKey.Name.ValueOrModified = "IpcMainEvent" then
                                  "AllowNullLiteral" ]
                        |> GeneratorContainer.withConstructor (
                            structOrObject.Properties |> List.map Parameter.InlinedObjectProp
                        )
                        |> GeneratorContainer.withInstanceProperties structOrObject.Properties
                        |> fun child -> state |> addToGroup (Child child)
                    | _ ->
                        failwith $"Unhandled type in generator: {item}"
                        state
                )
                group
        //%Injection%START%
        // Add other material, such as prebaked interfaces and constants
        // NOTE: currently the EventInterface system still uses the old method and is included here.
        |> fun group ->
            EventInterfaces.makeInterfaces ()
            |> List.fold
                (fun state eventInterface -> addToGroup (GeneratorGroupChild.EventInterface eventInterface) state)
                group
            |> addToGroup (
                (Path.PathKey
                    .Module(Path.Module.Module(Path.ModulePath.Root, Source "Main"))
                    .CreateType(Source Spec.touchBarItemsName),
                 Spec.touchBarItemsDef)
                |> GeneratorGroupChild.EventInterface
            )
        //%Injection%END%
        // Add other material, such as prebaked interfaces and constants
        |> fun group ->
            Type.Cache.GetEventStrings(true)
            |> List.fold
                (fun state item ->
                    state
                    |> addToGroup (
                        item.AddRootModule(Path.Module.Module(Path.ModulePath.Root, Source "Constants"))
                        |> GeneratorGroupChild.EventConstant
                    )
                )
                group
        // Generate the source and write to target
        |> fun group ->
            // Transcribe our objects/types into Fantomas
            finalizeGeneratorGroup group
            |> List.append
                [
                  // Add the open list to the head of the declaration list
                  GeneratorGrouper.makeOpenListNode group |> ModuleDecl.OpenList ]
            |> (GeneratorGrouper.makeNamespace true group)

    /// <summary>
    /// Parses the provided <c>electron-api.json</c> file and compiles to
    /// the given destination source.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="destination"></param>
    let generateFromApiFile (file: string) (destination: string) =
        Path.Cache.dumpCache ()
        Type.Cache.Clear()

        readFile file
        |> processModifiedResultList
        |> fun node -> Oak([], [ node ], Range.Zero)
        |> CodeFormatter.FormatOakAsync
        |> Async.RunSynchronously
        |> fun txt -> File.WriteAllText(destination, txt)

    type ProcessType =
        | Main
        | Renderer
        | Utility

    let private generateFromApiFileForProcess processType (file: string) (destination: string) =
        Path.Cache.dumpCache ()
        Type.Cache.Clear()

        let mappedReadFile func =
            File.ReadAllText
            >> Decode.fromString decode
            >> function
                | Ok values -> Array.choose func values
                | Error e -> failwith e
            // Preliminary run and conversion of raw info into unions and records that are more
            // idiomatic.
            >> readResults

        let inline processPredicate (object: ^T when ^T: (member Process: ProcessBlock)) =
            match processType with
            | ProcessType.Main when object.Process.Main -> true
            | ProcessType.Renderer when object.Process.Renderer -> true
            | ProcessType.Utility when object.Process.Utility -> true
            | _ -> false

        let remapProcess prev =
            { Main = processType.IsMain
              Renderer = processType.IsRenderer
              Utility = processType.IsRenderer
              Exported = prev.Exported }

        mappedReadFile
            (function
            | Decoder.ParsedDocumentation.Class o when processPredicate o ->
                Decoder.ParsedDocumentation.Class
                    { o with
                        ClassDocumentationContainer.Process = remapProcess o.Process }
                |> Some
            | Decoder.ParsedDocumentation.Element o when processPredicate o ->
                Decoder.ParsedDocumentation.Element
                    { o with
                        ElementDocumentationContainer.Process = remapProcess o.Process }
                |> Some
            | Decoder.ParsedDocumentation.Module o when processPredicate o ->
                Decoder.ParsedDocumentation.Module
                    { o with
                        ModuleDocumentationContainer.Process = remapProcess o.Process }
                |> Some
            | Decoder.ParsedDocumentation.Structure _ as o -> Some o
            | _ -> None)
            file
        |> processModifiedResultList
        |> fun node -> Oak([], [ node ], Range.Zero)
        |> CodeFormatter.FormatOakAsync
        |> Async.RunSynchronously
        |> fun txt -> File.WriteAllText(destination, txt)

    /// <summary>
    /// Does not work at the moment; too much interconnection between types to decouple by simple filtering on process at the beginning.
    /// </summary>
    /// <param name="apiFile"></param>
    let generateMainProcessOnlyFromApiFile apiFile =
        generateFromApiFileForProcess ProcessType.Main apiFile

    /// <summary>
    /// Does not work at the moment; too much interconnection between types to decouple by simple filtering on process at the beginning.
    /// </summary>
    /// <param name="apiFile"></param>
    let generateRendererProcessOnlyFromApiFile apiFile =
        generateFromApiFileForProcess ProcessType.Renderer apiFile

    /// <summary>
    /// Does not work at the moment; too much interconnection between types to decouple by simple filtering on process at the beginning.
    /// </summary>
    /// <param name="apiFile"></param>
    let generateUtilityProcessOnlyFromApiFile apiFile =
        generateFromApiFileForProcess ProcessType.Utility apiFile