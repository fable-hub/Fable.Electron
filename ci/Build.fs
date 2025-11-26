module Build.ci.Build

open System.Collections.Generic
open System.IO
open System.Xml.Linq
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
//%StatusModule%START%
module Status =
    //highlight-next-line
    let mutable private _cache = None
    let setCache (releaseInfo: ReleaseInfo) = _cache <- Some releaseInfo
    let hasCache () = _cache.IsSome
    let tryGetCache () = _cache
    let getCache = tryGetCache >> Option.get
    //highlight-next-line
    let mutable private _release = None
    let setRelease (release: ReleaseInfo) = _release <- Some release
    let hasRelease () = _release.IsSome
    let tryGetRelease () = _release
    let getRelease = tryGetRelease >> Option.get
    let getSemver = getRelease >> _.tagName.TrimStart('v') >> SemVer.parse //%StatusModule%END%

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
<| fun _ ->
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
// Target.create Ops.push (ignore >> Project.push)
Target.create Ops.push ignore
Target.create Ops.generateApiDocs (ignore >> ApiDocs.validateDir >> ApiDocs.build)
Target.create Ops.setupTest (fun _ -> Electron.installTests Args.npmCi)
Target.create Ops.test (ignore >> Electron.test)
Target.create Ops.postTest (ignore >> Laundry.fableClean)
Target.create Ops.restore (ignore >> Laundry.restoreTools)
Target.create Ops.format (ignore >> Laundry.format)

// sign post
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
    let projects = runtime.CrackRepo

    let getProjectOrFail leaf =
        try
            Seq.find _.ProjectFileName.EndsWith(leaf + ".fsproj") projects
        with e ->
            Trace.traceError $"A required project was not found. Could not find a project file ending with '{leaf}'"
            raise e

    let project =
        {| electron = getProjectOrFail "Electron"
           forge = getProjectOrFail "Forge"
           remoting = getProjectOrFail "Remoting" |} //%gitnet1%END% //%gitnet2%START%
    // ========== Current version/status
    let currentElectronVersion =
        let mutable maybeVersion: string option = None

        project.electron
        //highlight-next-line
        |> CrackedProject.withFsProj (
            CrackedProject.Document.withProperty
                "ElectronVersion"
                (_.Value
                 >> Option.ofObj
                 >> function
                     | Some value -> maybeVersion <- Some value
                     | None -> ())
            >> Error
        )
        |> fun _ -> maybeVersion |> Option.bind tryParseSepochSemver |> Option.map _.SemVer
        |> Option.orElseWith (fun () ->
            Status.tryGetCache ()
            |> Option.bind (_.tagName.TrimStart('v') >> tryParseSepochSemver >> Option.map _.SemVer))
        |> Option.defaultValue (Semver.SemVersion(0, 1, 0))

    let downloadedVersion =
        Status.tryGetRelease ()
        |> Option.bind (_.tagName.TrimStart('v') >> tryParseSepochSemver)
        |> Option.map _.SemVer
        //highlight-next-line
        |> Option.get

    let currentPackageVersion =
        //highlight-next-line
        getInitVersionElectron
        |> ValueOption.bind (function
            | GitNetTag.SepochTag(sepochSemver = { SemVer = semver })
            | GitNetTag.SemVerTag(semver = semver) -> ValueSome semver
            | _ -> ValueNone)
        |> ValueOption.defaultValue (Semver.SemVersion(0, 1, 0)) //%gitnet2%END%
    //%gitnet3%START%
    // ============ Deltas
    let deltaVersion =
        {| major = int (downloadedVersion.Major - currentElectronVersion.Major)
           minor = int (downloadedVersion.Minor - currentElectronVersion.Minor)
           patch = int (downloadedVersion.Patch - currentElectronVersion.Patch) |}

    let isMajorChange = deltaVersion.major > 0
    let isMinorChange = deltaVersion.minor > 0 || isMajorChange
    let isPatchChange = deltaVersion.patch > 0 || isMajorChange
    let isElectronVersionDifferent = isMajorChange || isMinorChange || isPatchChange
    //highlight-next-line
    let isLocalBindingDirty = Electron.isDirty ()

    let cachedVersion =
        Status.tryGetCache ()
        |> Option.bind (_.tagName.TrimStart('v') >> tryParseSepochSemver >> Option.map _.SemVer)
        |> Option.defaultValue (Semver.SemVersion(0, 1, 0))

    let isProbablyPulled =
        isMajorChange && (currentElectronVersion.ComparePrecedenceTo cachedVersion < 0)
    //%gitnet3%END% //%gitnet4%START%
    // ============= Next
    let electronScope = "Electron"

    let makeElectronSepochSemver =
        fun semver ->
            { SemVer = semver
              Sepoch = Sepoch.Scope electronScope }

    let nextVersion =
        match isElectronVersionDifferent, isLocalBindingDirty, isMajorChange || isMinorChange with
        | false, false, _ -> currentPackageVersion |> makeElectronSepochSemver
        | true, _, false
        | false, true, _ -> currentPackageVersion |> makeElectronSepochSemver |> SepochSemver.bumpPatch
        | true, _, _ -> downloadedVersion |> makeElectronSepochSemver

    let electronPackageUpdated = isElectronVersionDifferent || isLocalBindingDirty

    let anyPackageUpdated =
        electronPackageUpdated || getInitBumpRemoting.IsSome || getInitBumpForge.IsSome

    let packageRequiresPull =
        (isMajorChange && not isProbablyPulled) || para.Context.HasError
    //%gitnet4%END%
    // ====== Debug msg
    printfn
        $"
Summary of current status for GitNet:

Electron Cached Version: {cachedVersion.ToString()}
    This is the electron release
    information that is stored in
    ci/cache.json

Is Probably Pulled: {isProbablyPulled}
    We can assume the repository
    is being merged from a pull when
    the electron cached version is higher
    than the project files electron version

Current Electron Version: {currentElectronVersion.ToString()}
    This is the project file electron version.
    This is not updated except when being merged to main.

Current Package Version: {currentPackageVersion.ToString()}
    This is the package version for Fable.Electron

Downloaded Version: {downloadedVersion.ToString()}
    This is the version of Electron that was
    downloaded in this run.

If the major is updated, then we will submit a pull:
    Is Major Updated: {isMajorChange}
    Is Minor Updated: {isMinorChange}
    Is Patch Updated: {isPatchChange}

Next version: {nextVersion.ToString()}
    This is the next calculated version
    of Fable.Electron

Is Electron Package Updated: {electronPackageUpdated}
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
    match anyPackageUpdated, electronPackageUpdated, packageRequiresPull with
    | false, _, _ ->
        // nothing to do
        Trace.log "No changes during CI."
    | true, false, true ->
        // Electron package didnt update, but our other dependent packages failed
        // which means we will not push this update at all.
        para.Context.ErrorTargets |> Trace.traceErrorfn "%A"
    | _, true, requiresPull ->
        // The message for the commit should still abide by ConventionalCommits.
        let commitMessage =
            [
              // Major changes require ! to indicate breaking change
              if isMajorChange then
                  "feat!: Electron binding update to match " + Status.getRelease().tagName
              // Minor change
              elif isMinorChange then
                  "feat: Electron binding update to match " + Status.getRelease().tagName
              // Patch
              else
                  "fix: Electron binding update to match " + Status.getRelease().tagName
              ""
              "This commit is automatically generated by Build project."
              ""
              // Footer will allow us to filter these commits
              "generated: true" ]
            |> String.concat "\n"
        // Electron package has changed. So we'll do a run through everything.
        match requiresPull, Args.dryRun with
        | true, false ->
            // If requires a pull, then we'll do everything in a different branch
            // If the pull already exists though then we'll stop
            Git.Branches.getRemoteBranches Root.``.``
            |> List.exists ((=) $"ci/electron/{Status.getRelease().tagName}")
            |> function
                | true -> failwith "A pull already exists for this release."
                | false -> Laundry.createBranch $"ci/electron/{Status.getRelease().tagName}"
        | true, true -> Trace.log $"[ACTION] Create branch: ci/electron/{Status.getRelease().tagName}"
        | _, false ->
            // If we don't have to make a pull, then we'll change the versions in the project files
            // Otherwise, this change should be delegated to when we actually merge.
            // Exception for this is the cache release info. We'll use that as our guide post
            // for the merge version.

            // If the electron version is different, we also update the property in the project file
            // to match this.
            if isElectronVersionDifferent then
                project.electron
                |> CrackedProject.withFsProj (
                    CrackedProject.Document.withProperty "ElectronVersion" _.SetValue(downloadedVersion.ToString())
                    // Return Ok to overwrite the project file
                    // Return Error to prevent overwriting project file
                    >> ignore
                    >> Ok
                )
                |> ignore

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
        | _, true ->
            Trace.logItems
                "[ACTION]"
                [ downloadedVersion.ToString() |> sprintf "Set Fable.Electron ElectronVersion: %s"
                  nextVersion.SemVer.ToString() |> sprintf "Set Fable.Electron Version: %s"

                  match getInitBumpForge with
                  | ValueSome { SemVer = version } ->
                      version.ToString() |> sprintf "Set Fable.Electron.Forge Version: %s"
                  | _ -> ()
                  match getInitBumpRemoting with
                  | ValueSome { SemVer = version } ->
                      version.ToString() |> sprintf "Set Fable.Electron.Remoting Version: %s"
                  | _ -> () ]

        // Write the version/release info to the cache that this generation was based off
        if not Args.dryRun then
            Status.getRelease () |> Electron.writeToCache
        else
            Trace.log $"[ACTION] Write to cache: {Status.getRelease ()}"

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
                [ nextVersion
                  if getInitBumpForge.IsSome then
                      getInitBumpForge.Value
                  if getInitBumpRemoting.IsSome then
                      getInitBumpRemoting.Value ]

            if Args.dryRun then
                Trace.logItems "[ACTION]" (tags |> List.map (_.ToString() >> sprintf "Git Tag with: %s"))
            else
                runtime.CommitTags tags

        if Args.dryRun then
            Trace.logItems "[ACTION]" [ "GENERATE RELEASE_NOTES"; "Stage release notes"; "Commit changes" ]
        else
            // Once we have committed above, the markdown output will include the
            // tags/commits, and we can generate the release notes
            runtime.DryRun()
            |> _.Markdown
            // Instead of using WriteToOutputAndCommit, which automatically appends
            // the message if a commit has been made - but also overwrites the commit
            // message, we use WriteToOutputAndStage and then commit the changes
            |> runtime.WriteToOutputAndStage

            runtime.CommitChanges(appendCommit = false)

        if not requiresPull then
            // Before we do any pushing, we'll make sure the packages have no issues getting
            // pushed to nuget if we're not doing a pull
            Target.WithContext.run 1 (if Args.dryRun then Ops.pack else Ops.push) []
            |> Target.raiseIfError

        if Args.dryRun then
            Trace.log "[ACTION] Push to branch"
        else
            // This will push to main or push to the created branch
            Laundry.pushCurrentBranch ()
        // If we have to make a pull, we'll generate the pull using Octokit
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

            if Args.dryRun then
                Trace.log $"[ACTION] Send pull to devel:\n{title}\n\n{body}"
            else // Pulls are made to Devel rather than Main
                Laundry.sendPullForDevel title body
    | true, false, false when not Args.dryRun ->
        // If electron package is the same, then we can just do a normal run
        // and let everything fall into place
        use runtime = createRuntime ()
        runtime.Run() |> ignore
        Target.WithContext.run 1 Ops.push [] |> Target.raiseIfError
    | true, false, false ->
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
