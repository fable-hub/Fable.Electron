module Build.ci.Build

open System.IO
open EasyBuild.Tools
open EasyBuild.Tools.Git
open Fake.Api
open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.Globbing.Operators
open Fake.Tools.Git
open Spec
open Fake.Tools
open Fake.DotNet
open EasyBuild.Tools.ChangelogGen
open Fake.JavaScript

initializeContext ()

/// <summary>
/// Will be occupied by the release of the latest api (or the api searched for)
/// after the release info is checked.
/// </summary>
let mutable _release = None
let mutable _fableElectronNewVersion = None

// ========================================================
// Laundry

Target.create Ops.apiDocs
<| fun _ ->
    DirectoryInfo(VirtualRoot.fsdocs.``.``)
    |> DirectoryInfo.exists
    |> function
        | false -> Directory.create VirtualRoot.fsdocs.``.``
        | _ -> ()

    Fsdocs.build (fun p ->
        { p with
            SourceRepository = Some "https://github.com/fable-hub/fable-electron"
            SaveImages = Some true
            Input = Some VirtualRoot.fsdocs.``.``
            MdComments = Some true
            Projects = Some [ Projects.Electron; Projects.Forge; Projects.Remoting ]
            Properties = Some "Configuration=Release" })

Target.create Ops.docs
<| fun _ ->
    if Args.npmCi then
        Npm.cleanInstall (fun p ->
            { p with
                WorkingDirectory = Root.docs.``.`` })
    else
        Npm.install (fun p ->
            { p with
                WorkingDirectory = Root.docs.``.`` })

    Npm.run "start" (fun p ->
        { p with
            WorkingDirectory = Root.docs.``.`` })

// Generate changelog for FABLE.ELECTRON. Make major and minor match the release we are
// generating for. Internal patches can only bump patch number.
// Make sure this runs before we commit any files so the current hash is not updated
Target.create Ops.checkChangeLogGen
<| fun _ ->
    let fableElectronChanged =
        Information.getCurrentSHA1 Root.``.``
        |> FileStatus.getChangedFilesInWorkingCopy Root.``.``
        |> Seq.tryFind (
            snd
            >> fun file ->
                file.EndsWith("Fable.Electron/Program.fs")
                || file.EndsWith("Fable.Electron\\Program.fs")
        )
        |> function
            | Some _ -> true
            | _ -> false
    // Terminate early
    if not fableElectronChanged then
        ()
    else
        let file = System.IO.FileInfo(Root.``RELEASE_NOTES.md``)
        let currentVersion = Changelog.Changelog.findLastVersion file |> SemVer.parse

        let releaseVersion =
            match _release with
            | None -> failwith "Cannot run 'changelog-gen' without first downloading and running the api generator"
            | Some { tagName = rawTag } ->
                let tag = rawTag.TrimStart 'v'

                if SemVer.isValid tag |> not then
                    failwith $"The downloaded electron-api comes from a release with an invalid semver: {rawTag}"

                SemVer.parse tag

        let newVersion =
            match releaseVersion, currentVersion with
            | { Patch = releasePatch }, { Patch = currentPatch } when
                releaseVersion.Major = currentVersion.Major
                && releaseVersion.Minor = currentVersion.Minor
                ->
                { releaseVersion with
                    Patch =
                        if currentPatch >= releasePatch then
                            currentPatch + 1u
                        else
                            releasePatch }
            | _ when
                releaseVersion.Major > currentVersion.Major
                || releaseVersion.Minor > currentVersion.Minor
                ->
                releaseVersion
            | _ ->
                failwith
                    $"Unexpected version collision where the current version is higher than the generated version:\n \
                        Current: {currentVersion}\n Generated: {releaseVersion}"

        _fableElectronNewVersion <- Some newVersion
        printfn $"Fable.Electron bindings have been updated. New version after commit should be: %A{newVersion}"

// Generates and commits the changelog if required
Target.create Ops.changeLogGen
<| fun _ ->
    match _fableElectronNewVersion with
    | Some newVersion ->
        ChangelogGen.tryRun (
            System.IO.FileInfo(Root.``RELEASE_NOTES.md``),
            forceVersion = newVersion.AsString,
            skipInvalidCommit = true
        )
        |> function
            | ChangelogGenResult.Error e -> printfn $"Failed to generate changelog for %A{newVersion}: %s{e}"
            | ChangelogGenResult.NewVersion version ->
                Root.``RELEASE_NOTES.md`` |> Git.Staging.stageFile Root.``.`` |> ignore
                Commit.exec Root.``.`` $"chore: release %s{version}"
                Branches.tag Root.``.`` version
            | ChangelogGenResult.NoVersionBump ->
                printfn
                    $"Failed to generate changelog for %A{newVersion} because it was not determined to be a \
                        version bump by EasyBuild."
    | _ -> ()

// Clean misc and temp directories
Target.create Ops.clean
<| fun _ -> !!"**/**/bin" -- "bin" ++ "temp/" |> Shell.cleanDirs

Target.create Ops.fantomas
<| fun _ ->
    sourceFiles
    |> Seq.map (sprintf "\"%s\"")
    |> String.concat " "
    |> DotNet.exec id "fantomas"
    |> function
        | { ExitCode = 0 } -> ()
        | result -> Trace.log $"Errors while formatting all files: %A{result.Messages}"

// Clean project directories from FABLE generated files.
Target.create Ops.fableClean
<| fun _ ->
    let clean = [ "fable"; "clean"; "-e"; ".js"; "--yes" ]
    dotnet clean Root.src.``Fable.Electron``.``.``
    dotnet clean Root.src.``Fable.Electron.Forge``.``.``
    dotnet clean Root.src.``Fable.Electron.Remoting``.``.``
    dotnet clean Root.tests.``Fable.Electron.Remoting.Tests``.src.``.``
    dotnet clean Root.tests.``Fable.Electron.Remoting.Tests``.test.``.``

// Restore local tools such as fantomas and fable
Target.create Ops.restoreTools
<| fun _ -> dotnet [ "tool"; "restore" ] Root.``.``

// Config local setup to use gitbot (when running CI)
Target.create Ops.configGitBot
<| fun _ ->
    [ $"config --local user.email \"{githubEmail}\""
      $"config --local user.name \"{githubUsername}\"" ]
    |> List.iter (Git.CommandHelper.directRunGitCommandAndFail Root.``.``)

// View releases so you can choose a release to download
Target.create Ops.listReleases
<| fun _ ->
    Electron.getReleases ()
    |> if not Args.detailed then
           List.map _.tagName >> printfn "%A"
       else
           printfn "%A"

// Download the api of the release marked 'latest'
Target.create Ops.downloadLatestApi
<| fun _ ->
    // Reroute if we're choosing the release
    if Args.releaseVersion.IsSome then
        Target.runSimple Ops.downloadApi []
        |> _.Error
        |> function
            | Some ex -> raise ex
            | None -> ()
    else
        Electron.getReleases ()
        |> List.tryFind _.isLatest
        |> function
            | Some release ->
                _release <- Some release
                release
            | None -> failwith "Unable to find a release on the Electron GH that is the 'latest'."
        |> Electron.downloadElectronApi VirtualRoot.temp.``electron-api.json``
        |> Async.RunSynchronously

// Download the api given in the cli args
Target.create Ops.downloadApi
<| fun p ->
    if not Args.releaseVersion.IsSome then
        failwith $"To use the 'download-api' command, please specify the version to download with --release <VERSION>"

    Electron.getReleases ()
    |> List.tryFind (_.tagName.Trim('v') >> (=) Args.releaseVersion.Value)
    |> function
        | Some release ->
            _release <- Some release
            release
        | None ->
            Target.runSimple Ops.listReleases |> ignore
            failwith $"Unable to find a release on the Electron GH that matches {Args.releaseVersion.Value}"
    |> Electron.downloadElectronApi VirtualRoot.temp.``electron-api.json``
    |> Async.RunSynchronously

// Build Fable.Electron
Target.create Ops.fableBuild
<| fun _ -> dotnet [ "fable"; "-e"; ".js" ] Root.src.``Fable.Electron``.``.``

// ==================================================
// Projects to pack
let buildProjects = [ Projects.Electron; Projects.Remoting; Projects.Forge ]
// Build packages
Target.create Ops.build
<| fun _ ->
    buildProjects
    |> List.iter (
        DotNet.build (fun p ->
            { p with
                Configuration = DotNet.BuildConfiguration.Release
                DotNet.BuildOptions.MSBuildParams.DisableInternalBinLog = true })
    )
// Pack packages
Target.create Ops.pack
<| fun _ ->
    buildProjects
    |> List.iter (
        DotNet.pack (fun p ->
            { p with
                NoRestore = true
                OutputPath = Some "bin"
                DotNet.PackOptions.MSBuildParams.DisableInternalBinLog = true })
    )

// Push packages
Target.create Ops.push
<| fun _ ->
    let key =
        Args.apiKey
        |> Option.orElse (Environment.environVarOrNone "NUGET_KEY")
        |> function
            | Some key -> key
            | None ->
                failwith
                    "Cannot push to NuGet without either the 'NUGET_KEY' env var being set, or by using the flag --nuget-api-key <APIKEY>"

    if Args.apiKey.IsNone then
        failwith $""

    !!"bin/*.nupkg"
    |> Seq.iter (
        DotNet.nugetPush (fun p ->
            { p with
                DotNet.NuGetPushOptions.PushParams.Source = Some "https://api.nuget.org/v3/index.json"
                DotNet.NuGetPushOptions.Common.CustomParams = Some "--skip-duplicate"
                DotNet.NuGetPushOptions.PushParams.ApiKey = Some key })
    )

// ==========================================================
// Git commit/push/create pulls

// Relevant files
let files =
    [ Root.src.``Fable.Electron``.``Fable.Electron.fsproj``
      Root.src.``Fable.Electron``.``Types.fs``
      Root.src.``Fable.Electron``.``Program.fs``

      Root.src.``Fable.Electron.Remoting``.``Fable.Electron.Remoting.fsproj``
      Root.src.``Fable.Electron.Remoting``.``Main.fs``
      Root.src.``Fable.Electron.Remoting``.``Renderer.fs``
      Root.src.``Fable.Electron.Remoting``.``Preload.fs``

      Root.src.``Fable.Electron.Forge``.``Fable.Electron.Forge.fsproj``
      Root.src.``Fable.Electron.Forge``.``Program.fs`` ]
// Create git pull
Target.create Ops.gitPull
<| fun _ ->
    let token =
        Args.gitClientToken
        |> Option.defaultWith (fun () -> Environment.environVarOrFail "GITHUB_CLIENT")

    files |> List.iter (Git.Staging.stageFile Root.``.`` >> ignore)
    Git.Commit.exec Root.``.`` $"ci: Generated bindings release for {_release.Value.tagName}"

    Git.Information.getCurrentShortSHA1 Root.``.``
    |> Git.Branches.createBranch Root.``.`` _release.Value.tagName

    Git.Branches.pushBranch Root.``.`` "origin" _release.Value.tagName

    Git.Information.getBranchName Root.``.``
    |> fun branch ->
        let pull =
            Octokit.NewPullRequest($"Electron update: {_release.Value.tagName}", _release.Value.tagName, branch)

        GitHub.createClientWithToken token
        |> GitHub.createPullRequest "fable-hub" "fable-electron" pull
        |> Async.RunSynchronously
        |> Async.RunSynchronously
        |> ignore
// Commit files
Target.create Ops.gitCommit
<| fun _ ->
    files |> List.iter (Git.Staging.stageFile Root.``.`` >> ignore)

    if _fableElectronNewVersion.IsSome then
        Git.Commit.execExtended Root.``.`` "[skip ci]" $"ci: Generated bindings release for {_release.Value.tagName}."
    else
        Git.Commit.execExtended Root.``.`` "[skip ci]" "ci"

// ========================================================
// Test

// Note that workflow might need to download/install chromium
// try https://dev.to/slashgear_/how-to-setup-end-to-end-tests-with-webdriverio-on-github-action-f9n
Target.create Ops.test
<| fun _ ->
    let workDir = Root.tests.``Fable.Electron.Remoting.Tests``.``.``

    if Args.npmCi then
        Npm.cleanInstall (fun p -> { p with WorkingDirectory = workDir })
    else
        Npm.install (fun p -> { p with WorkingDirectory = workDir })

    Npm.runTest "test" (fun p -> { p with WorkingDirectory = workDir })

// =========================================================
// Generate bindings
open ElectronApi.Json.Parser.Generator

Target.create Ops.generateBinding
<| fun _ ->
    Transpiler.generateFromApiFile VirtualRoot.temp.``electron-api.json`` Root.src.``Fable.Electron``.``Program.fs``

// ==========================================================
// Set what operations of the CI must precede other operations
open Fake.Core.TargetOperators

// ==========================================================
// CI entry point
[<EntryPoint>]
let main argsv =
    argsv |> Args.setArgs

    let dependencyMapping =
        [ Ops.clean ==> Ops.downloadLatestApi
          Ops.clean ==> Ops.downloadApi

          Ops.configGitBot =?> (Ops.gitCommit, Args.gitBot)
          Ops.configGitBot =?> (Ops.gitPull, Args.gitBot)
          Ops.configGitBot =?> (Ops.changeLogGen, Args.gitBot)

          Ops.downloadLatestApi =?> (Ops.generateBinding, Args.releaseVersion.IsNone)

          Ops.downloadApi =?> (Ops.generateBinding, Args.releaseVersion.IsSome)

          Ops.restoreTools ==> Ops.generateBinding ?=> Ops.test

          Ops.test ?=> Ops.build
          Ops.test ?=> Ops.fableClean
          Ops.test =?> (Ops.pack, not Args.skipTest)
          Ops.test =?> (Ops.push, not Args.skipTest)

          Ops.fableClean ==> Ops.build ==> Ops.pack ==> Ops.push

          Ops.restoreTools ==> Ops.build ==> Ops.apiDocs

          Ops.clean ==> Ops.fableClean ==> Ops.gitPull ]

    if Args.help then
        printfn $"%s{Cli.spec}"
    elif Args.listReleases then
        Target.runSimple Ops.listReleases [] |> ignore

        let rec getInput () =
            let input = UserInput.getUserInput "Choose a release or quit (q):"

            match input with
            | "q" -> ()
            | version ->
                let inputVersion = version.TrimStart('v')

                if inputVersion |> SemVer.isValid then
                    inputVersion |> Args.setReleaseVersion
                    argsv[0] |> Target.runOrDefaultWithArguments
                else
                    printfn $"Input version %s{inputVersion} is not a valid SemVer; try again."
                    getInput ()

        getInput ()
    elif Args.clean && argsv[0].StartsWith("-") then
        Target.run 0 Ops.fableClean []
    else
        argsv[0] |> Target.runOrDefaultWithArguments

    0
