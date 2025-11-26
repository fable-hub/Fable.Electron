module Build.ci.Build

open System.Collections.Generic
open System.IO
open System.Xml.Linq
open Partas.Tools.SepochSemver
open Workers
open System.Text.Json
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
open Partas.GitNet
open Partas.GitNet.RepoCracker
open Partas.GitNet.BuildHelpers
open GitNet

initializeContext ()


// Laundry
Target.create Ops.clean (ignore >> Laundry.clean)
Target.create Ops.fableClean (ignore >> Laundry.fableClean)
Target.create Ops.listReleases (fun _ -> Electron.listReleases true)
Target.create Ops.listDetailedReleases (fun _ -> Electron.listReleases false)

Target.create Ops.downloadApi
<| fun _ ->
    match Args.release with
    | Some value ->
        Electron.tryGetReleaseFromString value
        |> Option.orElseWith (fun () -> failwith $"Could not download a release matching the input '{value}'")
        |> Option.iter (fun releaseInfo ->
            Status.setRelease releaseInfo
            Electron.downloadRelease releaseInfo)
    | None -> failwithf $"Target %s{Ops.downloadApi} requires the argument '--release <RELEASE>' to be set"

Target.create Ops.downloadInput
<| fun _ ->
    let getUserInput () =
        let isQuit: string -> bool =
            _.ToLowerInvariant()
            >> function
                | "q"
                | "quit" -> true
                | _ -> false

        match UserInput.getUserInput "Choose a release or (q)uit:\n" with
        | text when isQuit text -> failwith "User quit"
        | text -> text

    let rec run value =
        match Electron.tryGetReleaseFromString value with
        | None ->
            Electron.listReleases true
            getUserInput () |> run
        | Some value ->
            Status.setRelease value
            Electron.downloadRelease value

    Electron.listReleases true
    getUserInput () |> run

Target.create Ops.downloadLatest
<| function
| _ when Args.downloadMinorOnly || Args.downloadPatchOnly ->
    Target.runSimple Ops.loadCache []
    |> ignore
    let currentElectronVersion =
        let tagName = Status.getCache().tagName.TrimStart('v')
        tagName
        |> tryParseSepochSemver
        |> Option.map _.SemVer
        |> function
            | Some ver -> ver
            | None ->
                failwith $"The `--only-minor` and `--only-patch` flags require the cache \
                            to have a compatible semver. Found {tagName} instead."
    let parseTagName =
        _.tagName.TrimStart('v')
        >> tryParseSepochSemver
        >> Option.map _.SemVer
    Electron.getReleases()
    |> List.filter (_.isPrerelease >> not)
    |> List.filter (parseTagName >> function
        | Some value when Args.downloadMinorOnly && Args.downloadPatchOnly ->
            currentElectronVersion.Major = value.Major
            && (
                currentElectronVersion.Minor < value.Minor
                || (
                    currentElectronVersion.Minor = value.Minor
                    && currentElectronVersion.Patch < value.Patch
                )
            )
        | Some value when Args.downloadMinorOnly ->
            currentElectronVersion.Major = value.Major
            && currentElectronVersion.Minor < value.Minor
        | Some value when Args.downloadPatchOnly ->
            currentElectronVersion.Major = value.Major
            && currentElectronVersion.Minor = value.Minor
            && currentElectronVersion.Patch < value.Patch
        | _ -> false
        )
    |> function
        | [] ->
            Electron.tryGetReleaseFromString (Status.getCache().tagName)
        | releases ->
            releases
            |> List.maxBy _.createdAt
            |> _.tagName
            |> Electron.tryGetReleaseFromString
    |> function
        | Some release ->
            Status.setRelease release
            Electron.downloadRelease release
        | None -> failwith "Was not able to identify the latest release using the 'gh' cli."
| _ ->
    Electron.tryGetRelease _.isLatest
    |> function
        | Some release ->
            Status.setRelease release
            Electron.downloadRelease release
        | None -> failwith "Was not able to identify the latest release using the 'gh' cli."

Target.create Ops.postDownload (ignore >> Laundry.clean)

Target.create Ops.generate
<| fun _ ->
    Electron.generate ()
    |> Result.mapError (fun _ ->
        failwith
            "Attempted to generate from an electron-api.json, but none were downloaded.\n \
                  Either run target 'generate-release' or place the electron-api.json in the \
                  '/temp' folder at the root of the repository directory.")
    |> ignore

Target.create Ops.setupDocs <| fun _ -> Docs.setup Args.npmCi
Target.create Ops.docs (ignore >> Docs.dev)
Target.create Ops.build (fun _ -> Project.build Project.Targets.All)
Target.create Ops.pack (fun _ -> Project.pack true Project.Targets.All)
Target.create Ops.push (fun _ ->
    Project.push()
    Target.deactivateFinal Ops.gitnet)
Target.create Ops.generateApiDocs (ignore >> ApiDocs.validateDir >> ApiDocs.build)
Target.create Ops.setupTest (fun _ -> Electron.installTests Args.npmCi)
Target.create Ops.test (ignore >> Electron.test)
Target.create Ops.postTest (ignore >> Laundry.fableClean)
Target.create Ops.restore (ignore >> Laundry.restoreTools)
Target.create Ops.format (ignore >> Laundry.format)

// This target doesn't necessarily need to run anything itself. It acts to a sign post
// to target with a specific dependency list
Target.create Ops.cron ignore

Target.create Ops.loadCache
<| fun _ ->
    if File.exists Files.Cache then
        File.readAsString Files.Cache
        |> JsonSerializer.Deserialize<ReleaseInfo>
        |> Status.setCache

open Partas.Tools.SepochSemver
//%gitnet1%START%
Target.create Ops.gitnet
<| fun para ->
    if para.Context.IsRunningFinalTargets then
        match para.Context.FinalTarget with
        | Ops.gitnet -> Target.deactivateFinal Ops.gitnet
        | _ -> ()
    let projects, electronDeltaInfo = Versions.ElectronDelta.CreateFromContext()
    let project =
        {| electron = Project.Cracked.getProjectOrFail "Electron" projects
           forge = Project.Cracked.getProjectOrFail "Forge" projects
           remoting = Project.Cracked.getProjectOrFail "Remoting" projects |} //%gitnet1%END% //%gitnet2%START%

    let anyPackageUpdated =
        electronDeltaInfo.IsElectronBump || getInitBumpRemoting.IsSome || getInitBumpForge.IsSome

    let packageRequiresPull =
        (electronDeltaInfo.DeltaKind.IsMajor && not electronDeltaInfo.IsProbablyPulled)
        || para.Context.HasError
    // ====== Debug msg
    printfn
        $"
Summary of current status for GitNet:

Electron Cached Version: {electronDeltaInfo.Versions.CachedElectron.ToString()}
    This is the electron release
    information that is stored in
    ci/cache.json

Is Probably Pulled: {electronDeltaInfo.IsProbablyPulled}
    We can assume the repository
    is being merged from a pull when
    the electron cached version is higher
    than the project files electron version

Current Electron Version: {electronDeltaInfo.Versions.FableElectronElectron.ToString()}
    This is the project file electron version.
    This is not updated except when being merged to main.

Current Package Version: {electronDeltaInfo.Versions.FableElectronPackage.ToString()}
    This is the package version for Fable.Electron

Downloaded Version: {electronDeltaInfo.Versions.DownloadedElectron.ToString()}
    This is the version of Electron that was
    downloaded in this run.

If the major is updated, then we will submit a pull:
    {electronDeltaInfo.DeltaKind}

Next version: {electronDeltaInfo.NextElectronVersion.ToString()}
    This is the next calculated version
    of Fable.Electron

Is Electron Package Updated: {electronDeltaInfo.IsElectronBump}
    Whether or not the Electron package
    is changed, regardless of whether the
    'electron' version has changed.
    This is caused by changes in the generator.

Is Any Package Updated: {anyPackageUpdated}
    Whether any of our packages have
    changed.

Next versions:
    Remoting: {getInitBumpRemoting}
    Forge: {getInitBumpForge}

Package Requires Pull: {packageRequiresPull}
    Whether this run will result in a pull.
"
    //%gitnet5%START%
    // ============= Action
    match anyPackageUpdated, electronDeltaInfo.DeltaKind, packageRequiresPull with
    | false, _, _ ->
        // nothing to do
        Trace.log "No changes during CI."
    | true, Versions.Equal, true ->
        // Electron package didnt update, but our other dependent packages failed
        // which means we will not push this update at all.
        failwith $"%A{para.Context.ErrorTargets}"
    | _, (Versions.Major | Versions.Minor | Versions.Patch as deltaKind), requiresPull ->
        // The message for the commit should still abide by ConventionalCommits.
        let commitMessage =
            Versions.makeCommitMessage
                ("Electron binding update to match " + Status.getRelease().tagName)
                deltaKind
        let runOrDryLog message (fn: Lazy<_>) =
            if Args.dryRun
            then fn.Value
            else Trace.log $"[ACTION] "
        let runOrDryLogItems messages (fn: Lazy<_>) =
            if Args.dryRun
            then fn.Value
            else Trace.logItems "[ACTION] " messages
        lazy
            Branches.getRemoteBranches Root.``.``
            |> List.exists ((=) $"ci/electron/{Status.getRelease().tagName}")
            |> function
                | true when not Args.dryRun -> failwith "A pull already exists for this release."
                | false -> Laundry.createBranch $"ci/electron/{Status.getRelease().tagName}"
                | _ -> ()
        |> runOrDryLog $"[ACTION] Create branch: ci/electron/{Status.getRelease().tagName}"
        lazy (
            // If we don't have to make a pull, then we'll change the versions in the project files
            // Otherwise, this change should be delegated to when we actually merge.
            // Exception for this is the cache release info. We'll use that as our guide post
            // for the merge version.

            // If the electron version is different, we also update the property in the project file
            // to match this.
            if electronDeltaInfo.Versions.DownloadedElectron.Value
               <> electronDeltaInfo.Versions.FableElectronElectron.Value then
                project.electron
                |> CrackedProject.withFsProj (
                    CrackedProject.Document.withProperty "ElectronVersion" _.SetValue(electronDeltaInfo.Versions.DownloadedElectron.Value.ToString())
                    // Return Ok to overwrite the project file
                    // Return Error to prevent overwriting project file
                    >> ignore
                    >> Ok
                )
                |> ignore
            let nextVersion = electronDeltaInfo.NextElectronVersion
            [ project.electron, nextVersion.SemVer
              match getInitBumpForge with
              | ValueSome { SemVer = version } -> project.forge, version
              | _ -> ()
              match getInitBumpRemoting with
              | ValueSome { SemVer = version } -> project.remoting, version
              | _ -> () ]
            |> List.iter (fun (proj, version) ->
                let versionString = version.ToString()
                proj
                |> CrackedProject.withFsProj (
                    CrackedProject.Document.withPackageVersion _.SetValue(versionString)
                    >> CrackedProject.Document.withVersion _.SetValue(versionString)
                    >> ignore
                    >> Ok
                )
                |> ignore)
        )
        |> runOrDryLogItems
            [ electronDeltaInfo.Versions.DownloadedElectron.ToString() |> sprintf "Set Fable.Electron ElectronVersion: %s"
              electronDeltaInfo.NextElectronVersion.SemVer.ToString() |> sprintf "Set Fable.Electron Version: %s"

              match getInitBumpForge with
              | ValueSome { SemVer = version } ->
                  version.ToString() |> sprintf "Set Fable.Electron.Forge Version: %s"
              | _ -> ()
              match getInitBumpRemoting with
              | ValueSome { SemVer = version } ->
                  version.ToString() |> sprintf "Set Fable.Electron.Remoting Version: %s"
              | _ -> () ]

        // Write the version/release info to the cache that this generation was based off
        lazy
            Status.getRelease () |> Electron.writeToCache
        |> runOrDryLog $"Write to cache: {Status.getRelease ()}"

        [ project.electron; project.forge; project.remoting ] // We collect all the compiled files for each project, the project files
        |> List.collect (fun proj ->
            CrackedProject.getCompiledFilePaths proj
            |> List.map (Path.combine proj.ProjectDirectory)
            |> List.append [ CrackedProject.projectFileName proj ])
        // We also add the cache file
        |> List.append [ Path.combine "ci" "cache.json" ]
        // We stage the files and then commit
        |> function
            | files when Args.dryRun ->
                Trace.log $"[ACTION] Stage files: {files}"
                Trace.log $"[ACTION] Commit with Message: {commitMessage}"
            | files ->
                runtime.StageFiles files
                runtime.CommitChanges(message = commitMessage, appendCommit = false)

        // If we don't need to make a pull, then we can commit the tags.
        // Otherwise, we'll leave that for when the pull is merged.
        if not requiresPull then
            let tags =
                [ electronDeltaInfo.NextElectronVersion
                  if getInitBumpForge.IsSome then
                      getInitBumpForge.Value
                  if getInitBumpRemoting.IsSome then
                      getInitBumpRemoting.Value ]
            lazy
                runtime.CommitTags tags
            |> runOrDryLogItems (tags |> List.map (_.ToString() >> sprintf "Git Tag with: %s"))

        lazy
            // Once we have committed above, the markdown output will include the
            // tags/commits, and we can generate the release notes
            runtime.DryRun()
            |> _.Markdown
            // Instead of using WriteToOutputAndCommit, which automatically appends
            // the message if a commit has been made - but also overwrites the commit
            // message, we use WriteToOutputAndStage and then commit the changes
            |> runtime.WriteToOutputAndStage

            runtime.CommitChanges(appendCommit = false)
        |> runOrDryLogItems [ "GENERATE RELEASE_NOTES"; "Stage release notes"; "Commit changes" ]

        if not requiresPull then
            // Before we do any pushing, we'll make sure the packages have no issues getting
            // pushed to nuget if we're not doing a pull
            Target.WithContext.run 1 (if Args.dryRun then Ops.pack else Ops.push) []
            |> Target.raiseIfError
        lazy
            // This will push to main or push to the created branch
            Laundry.pushCurrentBranch ()
        |> runOrDryLog "Push to branch"
        // If we have to make a pull, we'll generate the pull using GH CLI
        if requiresPull then
            let title =
                if para.Context.HasError then "[GEN ERROR] For " else ""
                + "Electron "
                + Status.getRelease().tagName
            let body =
                if para.Context.HasError then
                    let rec addDetails (errors: (exn * Target) list) : string list =
                        match errors with
                        | [] -> []
                        | (e, target) :: rest ->
                            [ $"Error during '{target.Name}':"
                              ""
                              "<details>"
                              "<summary>Error</summary>"
                              ""
                              "```"
                              $"{e}"
                              "```"
                              "</details>"
                              "" ]
                            @ addDetails rest
                    addDetails para.Context.ErrorTargets
                    |> String.concat "\n"
                    |> sprintf
                        """During the build process, I came across some errors.

Once these are corrected, please consider merging this to `develop`

%s"""
                else
                    let release = Status.getRelease()
                    $"""Bindings for electron {release.tagName} were generated successfully and passed tests.

This electron release was created on {release.createdAt}.

This pull must be merged to `main` for publishing to occur.

It is recommended to merge to `develop` for major electron versions first.
"""

            lazy // Pulls are made to Devel rather than Main
                Laundry.sendPullForDevel title body
            |> runOrDryLog "Send pull to devel:\n{title}\n\n{body}"
    | true, Versions.Equal, false when not Args.dryRun ->
        // If electron package is the same, then we can just do a normal run
        // and let everything fall into place
        use runtime = createRuntime ()
        runtime.Run() |> ignore
        Target.WithContext.run 1 Ops.push [] |> Target.raiseIfError
    | true, Versions.Equal, false ->
        Trace.log $"[ACTION] Update Forge?: {getInitBumpForge}"
        Trace.log $"[ACTION] Update Remoting?: {getInitBumpRemoting}"
        Trace.log "[ACTION] Pushing to nuget"
//%gitnet5%END%

Target.create Ops.activateGitnet <| fun _ ->
    Target.runSimple Ops.loadCache []
    |> ignore
    Target.activateFinal Ops.gitnet

open Fake.Core.TargetOperators
// ==========================================================
// CI entry point
[<EntryPoint>]
let main argsv =
    argsv |> Args.setArgs
    //%TargetDeps%START%
    // ==========================================================
    // Set what operations of the CI must precede other operations
    let dependencyMapping =
        // Dependency on restore for any tool related actions
        Ops.restore
        ===> [ Ops.clean ==> Ops.fableClean
               Ops.downloadApi
               Ops.downloadInput
               Ops.downloadLatest
               Ops.listDetailedReleases
               Ops.listReleases
               Ops.generate
               Ops.generateApiDocs
               Ops.test
               Ops.format ]

        Ops.gitnet <== [ Ops.loadCache ]

        Ops.gitnet
        <==? [ Ops.postDownload; Ops.postTest; Ops.test; Ops.fableClean; Ops.format ]
        |> ignore

        [
          // define setup requirements
          Ops.setupTest =?> (Ops.test, not Args.quick) ==> Ops.postTest
          // If generate occurs, it is a soft dependency
          // for multiple targets
          Ops.generate
          ?==> [ Ops.test
                 Ops.format
                 Ops.generateApiDocs
                 Ops.build
                 Ops.pack
                 Ops.push
                 Ops.gitnet
                 Ops.postDownload ]
          // On the other hand, generate has plenty of soft dependencies itself
          Ops.generate <==? [ Ops.downloadApi; Ops.downloadInput; Ops.downloadLatest ]
          Ops.setupDocs =?> (Ops.docs, not Args.quick)

          Ops.postDownload
          <==? [ Ops.downloadApi
                 Ops.downloadInput
                 Ops.downloadLatest
                 Ops.generate
                 Ops.setupTest
                 Ops.test ] ]
    //%TargetDeps%END%
    let run =
        if Args.debug then
            Target.printDependencyGraph true
        else
            Target.runOrDefaultWithArguments

    match argsv[0] with
    | _ when Args.help -> printfn $"%s{Cli.spec}"
    | Commands.generateApiDocs -> run Ops.generateApiDocs
    | Commands.docs -> run Ops.docs
    | Commands.generate -> run Ops.generate
    | Commands.run ->
        match Args.target with
        | None -> failwith "No target supplied to '--target <NAME>'"
        | Some target -> run target
    | Commands.cron ->
        let dependencies =
            [ Ops.downloadLatest
              ==> Ops.generate
              ==> Ops.activateGitnet
              ==> Ops.build
              ==> Ops.test
              ==> Ops.postTest
              ==> Ops.postDownload
              ?==> [
                  Ops.gitnet
                  Ops.cron
              ]
              ==> Ops.cron
              Ops.pack
              ==> Ops.push ]

        run Ops.cron
    | Commands.pack -> run Ops.pack
    | Commands.test -> run (if Args.quick then Ops.test else Ops.postTest)
    | maybeTarget -> run maybeTarget

    0
