module Build.ci.Build

open Fake.Api
open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.Globbing.Operators
open Spec
open Fake.Tools
open Fake.DotNet

initializeContext()

let mutable _release = None

Target.create Ops.clean <| fun _ ->
    !! "**/**/bin"
    -- "bin"
    ++ "temp/"
    |> Shell.cleanDirs
Target.create Ops.fableClean <| fun _ ->
    dotnet
        [ "fable"; "clean"; "-e"; ".js" ]
        Root.src.``Fable.Electron``.``.``

Target.create Ops.restoreTools <| fun _ ->
    dotnet [ "tool"; "restore" ] Root.``.``

Target.create Ops.configGitBot <| fun _ ->
    [
        $"config --local user.email \"{githubEmail}\""
        $"config --local user.name \"{githubUsername}\""
    ] |> List.iter (Git.CommandHelper.directRunGitCommandAndFail Root.``.``)

Target.create Ops.listReleases <| fun _ ->
    Electron.getReleases()
    |> if Args.detailed
        then List.map _.tagName >> printfn "%A"
        else printfn "%A"

Target.create Ops.downloadLatestApi <| fun _ ->
    Electron.getReleases()
    |> List.tryFind _.isLatest
    |> function
        | Some release ->
            _release <- Some release
            release
        | None -> failwith "Unable to find a release on the Electron GH that is the 'latest'."
    |> Electron.downloadElectronApi VirtualRoot.temp.``electron-api.json``
    |> Async.RunSynchronously
    
Target.create Ops.downloadApi <| fun p ->
    if not Args.releaseVersion.IsSome then failwith $"To use the 'download-api' command, please specify the version to download with --release <VERSION>"
    Electron.getReleases()
    |> List.tryFind (_.tagName.Trim('v') >> (=) Args.releaseVersion.Value)
    |> function
        | Some release ->
            _release <- Some release
            release
        | None ->
            Target.runSimple Ops.listReleases
            |> ignore
            failwith $"Unable to find a release on the Electron GH that matches {Args.releaseVersion.Value}"
    |> Electron.downloadElectronApi VirtualRoot.temp.``electron-api.json``
    |> Async.RunSynchronously

Target.create Ops.fableBuild <| fun _ ->
    dotnet [ "fable"; "-e"; ".js" ] Root.src.``Fable.Electron``.``.``

Target.create Ops.build <| fun _ ->
    [
        Projects.Electron
        Projects.Remoting
    ]
    |> List.iter (
        DotNet.build (fun p -> {
            p with
                Configuration = DotNet.BuildConfiguration.Release
                DotNet.BuildOptions.MSBuildParams.DisableInternalBinLog = true
        })
        )
Target.create Ops.pack <| fun _ ->
    [
        Projects.Electron
        Projects.Remoting
    ]
    |> List.iter (DotNet.pack (fun p -> {
        p with
            NoRestore = true
            OutputPath = Some "bin"
            DotNet.PackOptions.MSBuildParams.DisableInternalBinLog = true
    }))

Target.create Ops.push <| fun _ ->
    let key =
        Args.apiKey
        |> Option.orElse (Environment.environVarOrNone "NUGET_KEY")
        |> function
            | Some key -> key
            | None -> failwith "Cannot push to NuGet without either the 'NUGET_KEY' env var being set, or by using the flag --nuget-api-key <APIKEY>"
    if Args.apiKey.IsNone then failwith $""
    !! "bin/*.nupkg"
    |> Seq.iter(DotNet.nugetPush (fun p -> {
        p with
            DotNet
                .NuGetPushOptions
                .PushParams
                .Source = Some "https://api.nuget.org/v3/index.json"
            DotNet
                .NuGetPushOptions
                .Common
                .CustomParams = Some "--skip-duplicate"
            DotNet
                .NuGetPushOptions
                .PushParams
                .ApiKey = Some key
    }))
let files = [
    Root.src.``Fable.Electron``.``Fable.Electron.fsproj``
    Root.src.``Fable.Electron``.``Types.fs``
    Root.src.``Fable.Electron``.``Program.fs``
    
    Root.src.``Fable.Electron.Remoting``.``Fable.Electron.Remoting.fsproj``
    Root.src.``Fable.Electron.Remoting``.``Main.fs``
    Root.src.``Fable.Electron.Remoting``.``Renderer.fs``
    Root.src.``Fable.Electron.Remoting``.``Preload.fs``
    
    Root.src.``Fable.Electron.Forge``.``Fable.Electron.Forge.fsproj``
    Root.src.``Fable.Electron.Forge``.``Program.fs``
]
Target.create Ops.gitPull <| fun _ ->
    let token =
        Args.gitClientToken
        |> Option.defaultWith (fun () -> Environment.environVarOrFail "GITHUB_CLIENT")
    files
    |> List.iter (Git.Staging.stageFile Root.``.`` >> ignore)
    Git.Commit.exec Root.``.`` $"ci: Generated bindings release for {_release.Value.tagName}"
    Git.Information.getCurrentShortSHA1 Root.``.``
    |> Git.Branches.createBranch Root.``.`` _release.Value.tagName
    Git.Branches.pushBranch Root.``.`` "origin" _release.Value.tagName
    Git.Information.getBranchName Root.``.``
    |> fun branch ->
        let pull = Octokit.NewPullRequest($"Electron update: {_release.Value.tagName}", _release.Value.tagName, branch)
        GitHub.createClientWithToken token
        |> GitHub.createPullRequest "fable-hub" "fable-electron" pull
        |> Async.RunSynchronously
        |> Async.RunSynchronously
        |> ignore

Target.create Ops.gitCommit <| fun _ ->
    files
    |> List.iter (Git.Staging.stageFile Root.``.`` >> ignore)
    if _release.IsSome
    then Git.Commit.execExtended Root.``.`` "[skip ci]" $"ci: Generated bindings release for {_release.Value.tagName}."
    else Git.Commit.execExtended Root.``.`` "[skip ci]" "ci"

open ElectronApi.Json.Parser.Generator
Target.create Ops.generateBinding <| fun _ ->
    Transpiler.generateFromApiFile
        VirtualRoot.temp.``electron-api.json``
        Root.src.``Fable.Electron``.``Program.fs``
    
open Fake.Core.TargetOperators
let dependencyMapping = [
    Ops.clean ==> Ops.downloadLatestApi
    Ops.clean ==> Ops.downloadApi
    
    Ops.downloadLatestApi
    =?> (Ops.generateBinding, Args.releaseVersion.IsNone)
    
    Ops.downloadApi
    =?> (Ops.generateBinding, Args.releaseVersion.IsSome)
    
    Ops.restoreTools
    ==> Ops.generateBinding
    ==> Ops.fableBuild
    
    Ops.fableBuild
    ==> Ops.fableClean
    ==> Ops.build
    ==> Ops.pack
    ==> Ops.push
    
    Ops.clean
    ==> Ops.fableClean
    ==> Ops.gitPull
]

[<EntryPoint>]
let main argsv =
    argsv |> Args.setArgs
    argsv[0] |> Target.runOrDefaultWithArguments
    0
