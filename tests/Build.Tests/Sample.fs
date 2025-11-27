module Tests

open Expecto
open Fake.Core
open Fake.Tools
open Spec
open Build.ci.Build
open System.Reflection
open FSharp.Reflection

let specModules = Assembly.GetAssembly(typeof<Args>).GetTypes() |> Array.filter FSharpType.IsModule
let findModule (name: string) = specModules |> Array.find _.Name.StartsWith(name)
let commands =
    findModule "Commands"
    |> _.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.choose (function
        | :? FieldInfo as f when not f.IsSpecialName ->
            Some(f.Name, f.GetRawConstantValue() :?> string)
        | _ -> None
        )
let commandMap =
    Map commands

let makeMap (typ: System.Type) =
    if not <| FSharpType.IsModule typ then failwith $"makeMap has to be used on modules. {typ.Name} is not a module"
    typ.GetFields(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (_.IsSpecialName >> not)
    |> Array.map(fun f -> f.Name, f.GetRawConstantValue() :?> string)
    |> Map
let makeFModule =
    findModule 
    >> _.GetNestedTypes()
    >> Array.filter FSharpType.IsModule
    >> Array.map (fun m ->
        m.Name, makeMap m
        )
    >> Map
let flagsModules = makeFModule "Flags"
let flagArgsModules = makeFModule "FlagArgs"
let ops =
    findModule "Ops"
    |> makeMap

open Workers
[<Tests>]
let fake =
    testList "Ops/Targets" [
        testList "Project Init" [
            let projects,delta = Workers.Versions.ElectronDelta.CreateFromContext()
            let getProject name =
                try
                projects |> Project.Cracked.getProjectOrFail name
                |> Ok
                with e -> Error e
            // // that's not how GitNet CrackRepo works actually. The filter is applied after cracking
            // testCase "Only 3 Projects fit filter" <| fun _ ->
            //     Expect.hasLength projects 3 "Filter out 3 projects"
            testCase "Loads electron by scope" <| fun _ ->
                getProject "Electron"
                |> Flip.Expect.isOk "Fable.Electron is found by scope"
            testCase "Loads forge by scope" <| fun _ ->
                getProject "Forge"
                |> Flip.Expect.isOk "Fable.Electron.Forge is found by scope"
            testCase "Loads remoting by scope" <| fun _ ->
                getProject "Remoting"
                |> Flip.Expect.isOk "Fable.Electron.Remoting is found by scope"
            testCase "Electron scoped project points to Fable.Electron.fsproj" <| fun _ ->
                getProject "Electron"
                |> Result.exists _.ProjectFileName.EndsWith("Fable.Electron.fsproj")
                |> Flip.Expect.isTrue "Project file for 'Electron' scope is Fable.Electron.fsproj"
            testCase "Forge scoped project points to Fable.Electron.Forge.fsproj" <| fun _ ->
                getProject "Forge"
                |> Result.exists _.ProjectFileName.EndsWith("Fable.Electron.Forge.fsproj")
                |> Flip.Expect.isTrue "Project file for 'Forge' scope is Fable.Electron.Forge.fsproj"
            testCase "Remoting scoped project points to Fable.Electron.Remoting.fsproj" <| fun _ ->
                getProject "Remoting"
                |> Result.exists _.ProjectFileName.EndsWith("Fable.Electron.Remoting.fsproj")
                |> Flip.Expect.isTrue "Project file for 'Remoting' scope is Fable.Electron.Remoting.fsproj"
            testList "Delta" [
                testCase "Delta Version for Project Props are Some" <| fun _ ->
                    (delta.Versions.FableElectronElectron.IsSome && delta.Versions.FableElectronPackage.IsSome)
                    |> Flip.Expect.isTrue "Project version props yield values for delta"
                testCase "Cache/Downloaded versions are none" <| fun _ ->
                    (delta.Versions.CachedElectron.IsNone && delta.Versions.DownloadedElectron.IsNone)
                    |> Flip.Expect.isTrue "Delta versions for downloaded/cache"
                testCase "Cache version loads as ReleaseInfo" <| fun _ ->
                    Files.Cache |> Fake.IO.File.exists
                    |> Flip.Expect.isTrue "Cache file should be present"
                    Files.Cache |> Fake.IO.File.readAsString
                    |> System.Text.Json.JsonSerializer.Deserialize<ReleaseInfo>
                    |> Status.setCache
                    Status.tryGetCache()
                    |> Flip.Expect.isSome "Release info loaded into cache"
                    Status.getCache()
                    |> _.tagName
                    |> Flip.Expect.isNotEmpty "Cached tag value has content"
            ]
        ]
    ]
open Versions
[<Tests>]
let deltas =
    let makeVers (major: int) (minor: int) (patch: int) = Semver.SemVersion(major,minor,patch)
    let makeDelta downloaded fable =
        ElectronDelta.Create(fableElectronElectronProperty = fable, downloadedVersion = downloaded, fableElectronPackage = fable)
    testList "Deltas" [
        testTheory "Electron Bumps Versions" [
            let makeTestCase (major,minor,patch) (major2,minor2,patch2) expectedBump =
                {|
                  downloadedSemver = makeVers major minor patch
                  electronSemver = makeVers major2 minor2 patch2
                  shouldBump = expectedBump
                |}
            makeTestCase (0,2,0) (0,2,0) false
            makeTestCase (0,1,0) (0,2,0) false
            makeTestCase (0,3,0) (0,2,0) true
            makeTestCase (0,0,0) (0,0,1) false
            makeTestCase (0,0,1) (0,0,1) false
            makeTestCase (0,0,2) (0,0,1) true
            makeTestCase (1,0,0) (2,0,0) false
            makeTestCase (2,0,0) (2,0,0) false
            makeTestCase (3,0,0) (2,0,0) true
            makeTestCase (1,0,0) (2,0,1) false
            makeTestCase (2,0,0) (2,0,1) false
            makeTestCase (2,0,2) (2,0,1) true
        ] (fun para ->
            makeDelta para.downloadedSemver para.electronSemver
            |> _.IsElectronBump
            |> (if para.shouldBump
                then Flip.Expect.isTrue
                else Flip.Expect.isFalse)
                "Version calculations run expectedly"
            )
        testTheory "Electron Bump Types" [
            let makeTestCase (major,minor,patch) (major2,minor2,patch2) isDirty expectedBump =
                {|
                  downloadedSemver = makeVers major minor patch
                  electronSemver = makeVers major2 minor2 patch2
                  isDirty = isDirty
                  bumpKind = expectedBump
                |}
            makeTestCase (2,0,1) (2,0,1) false DeltaKind.Equal
            makeTestCase (1,0,0) (2,0,1) false DeltaKind.Equal
            makeTestCase (1,0,0) (2,0,1) true DeltaKind.Patch
            makeTestCase (2,0,1) (2,0,1) true DeltaKind.Patch
            makeTestCase (2,0,2) (2,0,1) true DeltaKind.Patch
            makeTestCase (2,0,4) (2,0,1) true DeltaKind.Patch
            makeTestCase (2,1,0) (2,0,1) false DeltaKind.Minor
            makeTestCase (2,1,0) (2,0,1) true DeltaKind.Minor
            makeTestCase (2,0,0) (2,0,0) true DeltaKind.Patch
            makeTestCase (3,0,0) (2,0,0) false DeltaKind.Major
            makeTestCase (3,0,0) (2,0,0) true DeltaKind.Major
            makeTestCase (3,1,1) (2,0,0) false DeltaKind.Major
            makeTestCase (3,1,1) (2,0,0) true DeltaKind.Major
            
        ] (fun para ->
            let delta = {
                makeDelta para.downloadedSemver para.electronSemver with
                    Dirty = para.isDirty
            }
            delta.DeltaKind
            |> Flip.Expect.equal "Bump kind is calculated as expected" para.bumpKind
            )
        testTheory "Electron Next Versions" [
            let makeTestCase (major,minor,patch) (major2,minor2,patch2) isDirty (major3,minor3,patch3) =
                {|
                  downloadedSemver = makeVers major minor patch
                  electronSemver = makeVers major2 minor2 patch2
                  isDirty = isDirty
                  nextVersion = makeVers major3 minor3 patch3
                |}
            makeTestCase (2,0,1) (2,0,1) false (2,0,1)
            makeTestCase (1,0,0) (2,0,1) false (2,0,1)
            makeTestCase (1,0,0) (2,0,1) true (2,0,2)
            makeTestCase (2,0,1) (2,0,1) true (2,0,2)
            makeTestCase (2,0,2) (2,0,1) true (2,0,2)
            makeTestCase (2,0,4) (2,0,1) true (2,0,2)
            makeTestCase (2,1,0) (2,0,1) false (2,1,0)
            makeTestCase (2,1,0) (2,0,1) true (2,1,0)
            makeTestCase (2,0,0) (2,0,0) true (2,0,1)
            makeTestCase (3,0,0) (2,0,0) false (3,0,0)
            makeTestCase (3,0,0) (2,0,0) true (3,0,0)
            makeTestCase (3,1,1) (2,0,0) false (3,1,1)
            makeTestCase (3,1,1) (2,0,0) true (3,1,1)
        ] (fun para ->
            let delta = {
                makeDelta para.downloadedSemver para.electronSemver with
                    Dirty = para.isDirty
            }
            delta.NextElectronVersion.SemVer
            |> Flip.Expect.equal "Next version is calculated as expected" para.nextVersion
            )

        testTheory "IsPulled Calculation" [
            let makeTestCase (major,minor,patch) (major2,minor2,patch2) isDirty (major3,minor3,patch3) value =
                {|
                  downloadedSemver = makeVers major minor patch
                  electronSemver = makeVers major2 minor2 patch2
                  isDirty = isDirty
                  cachedVersion = makeVers major3 minor3 patch3
                  isPulled = value
                |}
            makeTestCase (2,0,1) (2,0,1) false (2,0,1) false
            makeTestCase (1,0,0) (2,0,1) false (2,0,1) false
            makeTestCase (1,0,0) (2,0,1) true (2,0,2) false
            makeTestCase (2,0,1) (2,0,1) true (2,0,2) false
            makeTestCase (2,0,2) (2,0,1) true (2,0,2) false
            makeTestCase (2,0,4) (2,0,1) true (2,0,2) false
            makeTestCase (2,1,0) (2,0,1) false (2,0,0) false
            makeTestCase (2,1,0) (2,0,1) true (2,2,0) false
            makeTestCase (2,0,0) (2,0,0) true (3,0,0) false
            makeTestCase (3,0,0) (2,0,0) false (3,0,0) true
            makeTestCase (3,0,0) (2,0,0) true (3,0,0) true
            makeTestCase (3,1,1) (2,0,0) false (3,1,1) true
            makeTestCase (3,1,1) (2,0,0) true (3,1,1) true

        ] (fun para ->
            let delta = {
                makeDelta para.downloadedSemver para.electronSemver with
                    ElectronDelta.Versions.CachedElectron = ValueSome para.cachedVersion
                    Dirty = para.isDirty
            }
            delta.IsProbablyPulled
            |> (if para.isPulled
                then Flip.Expect.isTrue
                else Flip.Expect.isFalse) "If cached version is higher \
                than the project electron version and bump type is major then should be maybepulled"
            )
    ]
[<Tests>]
let cli =
    testList "CLI Parser" [
        let testArgs args () =
            try
                Cli.parser.Parse(args)
                |> Ok
            with e -> Error e
            |> Flip.Expect.isOk $"Parses %A{args}"
        testList "Commands" (
                commands
                |> Array.map(fun (cmdName, cmdValue) ->
                    testCase cmdName <| testArgs [| cmdValue |]
                    )
                |> Array.toList
            )
        testList "Common Options" (
            flagsModules["Common"]
            |> Map.toList
            |> List.map (fun (flagName, flagValue) ->
                testCase flagName <| testArgs [| flagValue |]
                )
            )
        testList "Common Flags needing values fail without" (
            flagArgsModules["Common"]
            |> Map.toList
            |> List.map (fun (flagName, flagValue) ->
                testCase flagName <| fun _ ->
                    try
                        Cli.parser.Parse([| flagValue |])
                        |> Ok
                    with e -> Error e
                    |> Flip.Expect.isError $"{flagValue} requires a value"
                )
            )
        testList "Common Flags needing values succeed with" (
            flagArgsModules["Common"]
            |> Map.toList
            |> List.map (fun (flagName, flagValue) ->
                testCase flagName <| fun _ ->
                    try
                        Cli.parser.Parse([| flagValue; "somevalue" |])
                        |> Ok
                    with e -> Error e
                    |> Flip.Expect.isOk $"{flagValue} requires a value"
                )
            )
    ]
