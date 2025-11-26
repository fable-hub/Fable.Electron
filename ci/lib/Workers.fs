module Workers

open System.IO
open System.Text.Json
open EasyBuild.Tools.Changelog
open EasyBuild.Tools.ChangelogGen
open ElectronApi.Json.Parser
open Fake.Api
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.Tools
open Fake.JavaScript
open Fake.Tools.Git
open GitNet
open Octokit
open Partas.GitNet
open Partas.Tools.SepochSemver
open Spec

//%StatusModule%START%
module Status =
    /// Cached release info from the last generation of bindings
    //highlight-next-line
    let mutable private _cache = None
    let setCache (releaseInfo: ReleaseInfo) = _cache <- Some releaseInfo
    let hasCache () = _cache.IsSome
    let tryGetCache () = _cache
    let getCache = tryGetCache >> Option.get
    /// Release info from any downloaded releases in this run.
    //highlight-next-line
    let mutable private _release = None
    let setRelease (release: ReleaseInfo) = _release <- Some release
    let hasRelease () = _release.IsSome
    let tryGetRelease () = _release
    let getRelease = tryGetRelease >> Option.get
    let getReleaseSemver = getRelease >> _.tagName.TrimStart('v') >> SemVer.parse //%StatusModule%END%

module Npm =
    let private setDir dir =
        fun p ->
            { p with
                Npm.NpmParams.WorkingDirectory = dir }

    /// Clean install the npm package.json in the given dir
    let cleanInstall = setDir >> Npm.cleanInstall
    /// Install the npm package.json in the given dir
    let install = setDir >> Npm.install
    /// Run the script 'start' for the npm package.json in the given dir
    let start = setDir >> Npm.run "start"
    let test = setDir >> Npm.runTest "test"

module ApiDocs =
    /// Ensure valid folder existence
    let validateDir () =
        if DirectoryInfo VirtualRoot.fsdocs.``.`` |> DirectoryInfo.exists |> not then
            Directory.create VirtualRoot.fsdocs.``.``

    /// Builds docs for the Electron projects using Fsdoc. To be used in a repo
    /// outside of the main source repo.
    let build () =
        Fsdocs.build (fun p ->
            { p with
                SourceRepository = Some "https://github.como/fable-hub/fable-electron"
                SaveImages = Some true
                Input = Some VirtualRoot.fsdocs.``.``
                MdComments = Some true
                Projects = Some [ Projects.Electron; Projects.Forge; Projects.Remoting ]
                Properties = Some "Configuration=Release" })

module Docs =
    let private dir = Root.docs.``.``

    /// Setup docs with install; pass true parameter to cleanInstall instead
    let setup cleanInstall =
        (if not cleanInstall then Npm.install else Npm.cleanInstall) dir

    /// Start the docs in dev mode
    let dev () = Npm.start dir

module Laundry =
    open Fake.IO.Globbing.Operators
    let private root = Root.``.``

    /// Clean Directories. Run before committing.
    let clean () =
        !!"**/**/bin" -- "bin" ++ "temp/" |> Shell.cleanDirs

    /// Clean temp directory. Run before committing.
    let cleanTemp () = "temp/" |> Shell.cleanDir

    /// Clean fable files. Run after tests.
    let fableClean () =
        let func = [ "fable"; "clean"; "-e"; ".js"; "--yes" ]

        [| Root.src.``Fable.Electron.Forge``.``.``
           Root.src.``Fable.Electron.Remoting``.``.``
           Root.src.``Fable.Electron``.``.``
           Root.tests.``Fable.Electron.Remoting.Tests``.src.``.``
           Root.tests.``Fable.Electron.Remoting.Tests``.test.``.`` |]
        |> Array.Parallel.iter (dotnet func)

    /// Restore tools; Run as prelude to most operations
    let restoreTools () = dotnet [ "tool"; "restore" ] root

    /// Set the local user to the git user; use for git commits within FAKE
    let configGitBot () =
        [ $"config --local user.email \"{githubEmail}\""
          $"config --local user.name \"{githubUsername}\"" ]
        |> List.iter (Git.CommandHelper.directRunGitCommandAndFail root)

    let withGithubClient =
        fun func ->
            let token =
                Args.gitClientToken
                |> Option.defaultWith (fun _ -> Environment.environVarOrFail "GITHUB_TOKEN")

            GitHub.createClientWithToken token
            |> (func >> Async.RunSynchronously >> Async.RunSynchronously)


    let pushBranch branchName =
        Git.Branches.pushBranch root "origin" branchName

    let branchName () = Git.Information.getBranchName root

    let pushCurrentBranch = branchName >> pushBranch

    let createBranch newBranchName =
        CommandHelper.directRunGitCommandAndFail root $"checkout -b {newBranchName}"

    let commitFiles msg files =
        files |> List.iter (Git.Staging.stageFile root >> ignore)
        Git.Commit.exec root msg

    [<CLIMutable>]
    type PullRequestInfo =
        { reviewers: string array
          labels: string array
          projects: string array
          assignees: string array }

    let private createNewPull targetBranch (title: string) (body: string) =
        let current = Information.getBranchName root

        let prInfo =
            try
                File.readAsString Root.ci.``pull_request.json``
                |> JsonSerializer.Deserialize<PullRequestInfo>
            with e ->
                Trace.traceError e.Message
                { reviewers = [||]
                  labels = [||]
                  projects = [||]
                  assignees = [||] }

        Gh.createPr
            (fun p ->
                { p with
                    Reviewer = prInfo.reviewers |> Array.toList
                    Label = prInfo.labels |> Array.toList
                    Project = prInfo.projects |> Array.toList
                    Assignee = prInfo.assignees |> Array.toList
                    Base = ValueSome targetBranch
                    Body = ValueSome body
                    Title = ValueSome title
                    Head = ValueSome current })
            root
        |> ignore

    let private createPullForDevel = createNewPull "develop"
    let private createPullForMain = createNewPull "main"

    let sendPullForDevel = createPullForDevel
    let sendPullForMain = createPullForMain

    let tagBranch tag = Branches.tag root tag

    let format () =
        sourceFiles
        |> Seq.map (sprintf "\"%s\"")
        |> String.concat " "
        |> DotNet.exec id "fantomas"
        |> function
            | { ExitCode = 0 } -> ()
            | result -> Trace.log $"Errors while formatting all files: %A{result.Messages}"

module Electron =
    let private apiFile = VirtualRoot.temp.``electron-api.json``

    let listReleases simple =
        Electron.getReleases ()
        |> if simple then
               List.map _.tagName >> printfn "%A"
           else
               printfn "%A"

    let getReleases () = Electron.getReleases ()

    let tryGetRelease (predicate: ReleaseInfo -> bool) =
        Electron.getReleases () |> List.tryFind predicate

    let downloadRelease = Electron.downloadElectronApi apiFile >> Async.RunSynchronously

    let tryGetReleaseFromString input =
        Electron.getReleases () |> List.tryFind (_.tagName.Trim('v') >> (=) input)

    let private cacheFile = Root.ci.``cache.json``

    /// Fallback for not being able to parse version in changelog
    let tryGetCachedRelease () =
        try
            File.readAsString cacheFile |> JsonSerializer.Deserialize<ReleaseInfo> |> Some
        with _ ->
            None

    let writeToCache (releaseInfo: ReleaseInfo) =
        releaseInfo |> JsonSerializer.Serialize |> File.writeString false cacheFile

    let generate () =
        if not <| File.exists apiFile then
            Error()
        else
            Transpiler.generateFromApiFile apiFile Root.src.``Fable.Electron``.``Program.fs``
            Ok()

    let private testDir = Root.tests.``Fable.Electron.Remoting.Tests``.``.``

    let installTests cleanInstall =
        if cleanInstall then
            Npm.cleanInstall testDir
        else
            Npm.install testDir

    let test () = Npm.test testDir

    let isDirty () =
        Information.getCurrentSHA1 Root.``.``
        |> FileStatus.getChangedFilesInWorkingCopy Root.``.``
        |> Seq.tryFind (
            snd
            >> fun file ->
                file.EndsWith("Fable.Electron/Program.fs")
                || file.EndsWith("Fable.Electron\\Program.fs")
        )
        |> Option.isSome

module Project =
    type Targets =
        | All
        | One of string
        | Of of string list

    let private buildProjects = [ Projects.Electron; Projects.Remoting; Projects.Forge ]

    let build =
        function
        | All ->
            buildProjects
            |> List.iter (
                DotNet.build (fun p ->
                    { p with
                        Configuration = DotNet.BuildConfiguration.Release
                        DotNet.BuildOptions.MSBuildParams.DisableInternalBinLog = true })
            )
        | Of projects ->
            projects
            |> List.iter (
                DotNet.build (fun p ->
                    { p with
                        Configuration = DotNet.BuildConfiguration.Release
                        DotNet.BuildOptions.MSBuildParams.DisableInternalBinLog = true })
            )
        | One project ->
            project
            |> DotNet.build (fun p ->
                { p with
                    Configuration = DotNet.BuildConfiguration.Release
                    DotNet.BuildOptions.MSBuildParams.DisableInternalBinLog = true })

    let fableBuild () =
        dotnet [ "fable"; "-e"; ".js" ] Root.src.``Fable.Electron``.``.``

    let pack restore =
        let func =
            DotNet.pack (fun p ->
                { p with
                    NoRestore = not restore
                    OutputPath = Some "bin"
                    DotNet.PackOptions.MSBuildParams.DisableInternalBinLog = true })

        function
        | All -> buildProjects |> List.iter func
        | Of projects -> projects |> List.iter func
        | One project -> func project

    open Fake.IO.Globbing.Operators

    /// Pushes all packed projects from "bin"
    let push () =
        Args.apiKey
        |> Option.orElse (Environment.environVarOrNone "NUGET_KEY")
        |> function
            | Some key ->
                !!"bin\*.nupkg"
                |> Seq.iter (
                    DotNet.nugetPush (fun p ->
                        { p with
                            DotNet.NuGetPushOptions.PushParams.Source = Some "https://api.nuget.org/v3/index.json"
                            DotNet.NuGetPushOptions.Common.CustomParams = Some "--skip-duplicate"
                            DotNet.NuGetPushOptions.PushParams.ApiKey = Some key })
                )
            | None -> failwith "Require NuGet Key to be passed via --nuget-api-key <APIKEY> or via env var NUGET_KEY"
    
    module Cracked =
        open Partas.GitNet
        let getProjectOrFail leaf projects: CrackedProject =
            try
                Seq.find _.ProjectFileName.EndsWith(leaf + ".fsproj") projects
            with e ->
                Trace.traceError $"A required project was not found. Could \
                                not find a project file ending with '{leaf}'"
                raise e
        
        let getElectronVersionProperty project =
            let mutable maybeVersion: string voption = ValueNone
            project
            |> CrackedProject.withFsProj (
                CrackedProject.Document.withProperty "ElectronVersion" (
                    _.Value
                    >> Option.ofObj
                    >> function
                        | Some value -> maybeVersion <- ValueSome value
                        | None -> ()
                    )
                >> Error
                )
            |> fun _ ->
                maybeVersion
                |> ValueOption.bind (tryParseSepochSemver >> Option.toValueOption)
                |> ValueOption.map _.SemVer

module Versions =
    type DeltaKind =
        | Major
        | Minor
        | Patch
        | Equal
    let makeCommitMessage message =
        function
        | Major -> "feat!: " + message
        | Minor -> "feat: " + message
        | Patch -> "fix: " + message
        | Equal -> "chore: " + message
        >> fun message ->
            [
                message
                ""
                "This commit is automatically generated by the Fable.Electron CI."
                ""
                "generated: true"
            ]
            |> String.concat "\n"
    type ElectronDeltaVersions = {
        FableElectronPackage: Semver.SemVersion voption
        FableElectronElectron: Semver.SemVersion voption
        CachedElectron: Semver.SemVersion voption
        DownloadedElectron: Semver.SemVersion voption
    }
    type ElectronDelta = {
        Versions: ElectronDeltaVersions
        Dirty: bool
        Major: int
        Minor: int
        Patch: int
    } with
        static member Init = {
            Versions = {
                FableElectronPackage = ValueNone
                FableElectronElectron = ValueNone
                CachedElectron = ValueNone
                DownloadedElectron = ValueNone
            }
            Dirty = false
            Major = 0
            Minor = 0
            Patch = 0
        }
        static member Create(?fableElectronPackage: Semver.SemVersion, ?fableElectronElectronProperty: Semver.SemVersion,
                             ?cachedElectronVersion: Semver.SemVersion, ?downloadedVersion: Semver.SemVersion) =
            let electronPkgVersion = defaultArg fableElectronPackage (Semver.SemVersion(0,1,0))
            let cachedVersion = defaultArg cachedElectronVersion (Semver.SemVersion(0,1,0))
            let fableElectronPropVersion = defaultArg fableElectronElectronProperty cachedVersion
            let downloadedVers = defaultArg downloadedVersion (Semver.SemVersion(0,1,0))
            {
                Versions = {
                    FableElectronPackage = fableElectronPackage |> Option.toValueOption
                    FableElectronElectron = fableElectronElectronProperty |> Option.toValueOption
                    CachedElectron = cachedElectronVersion |> Option.toValueOption
                    DownloadedElectron = downloadedVersion |> Option.toValueOption
                }
                Dirty = Electron.isDirty()
                Major = int (downloadedVers.Major - fableElectronPropVersion.Major)
                Minor = int (downloadedVers.Minor - fableElectronPropVersion.Minor)
                Patch = int (downloadedVers.Patch - fableElectronPropVersion.Patch)
            }
        member this.DeltaKind =
            if this.Major > 0 then Major
            elif this.Minor > 0 then Minor
            elif this.Patch > 0 || this.Dirty then Patch
            else Equal
        member this.IsElectronBump = this.DeltaKind <> Equal
        static member CreateFromContext() =
            let projects = runtime.CrackRepo
            let electron = Project.Cracked.getProjectOrFail "Electron" projects
            let forge = Project.Cracked.getProjectOrFail "Forge" projects
            let remoting = Project.Cracked.getProjectOrFail "Remoting" projects
            
            let currentBindingElectronVersion =
                Project.Cracked.getElectronVersionProperty electron
                |> ValueOption.toOption
            let currentDownloadedVersion =
                Status.tryGetRelease ()
                |> Option.bind (_.tagName.TrimStart('v') >> tryParseSepochSemver)
                |> Option.map _.SemVer
            let electronPackageVersion =
                getInitVersionElectron
                |> ValueOption.bind(function
                    | GitNetTag.SepochTag(sepochSemver = { SemVer = semver })
                    | GitNetTag.SemVerTag(semver = semver) -> ValueSome semver
                    | _ -> ValueNone)
                |> ValueOption.toOption
            let cachedElectronVersion =
                Status.tryGetCache()
                |> Option.bind (
                    _.tagName.TrimStart('v')
                    >> tryParseSepochSemver
                    >> Option.map _.SemVer
                    )
            let delta =
                ElectronDelta.Create(
                    ?fableElectronElectronProperty = currentBindingElectronVersion,
                    ?cachedElectronVersion = cachedElectronVersion,
                    ?downloadedVersion = currentDownloadedVersion,
                    ?fableElectronPackage = electronPackageVersion
                    )
            projects, delta
        member this.IsProbablyPulled =
            let cachedVersion =
                this.Versions.CachedElectron
                |> ValueOption.defaultValue(Semver.SemVersion(0,1,0))
            let currentVersionLessThanCached =
                this.Versions.FableElectronPackage
                |> ValueOption.defaultValue (Semver.SemVersion(0,1,0))
                |> _.ComparePrecedenceTo(cachedVersion)
                |> (>) 0
            this.DeltaKind.IsMajor && currentVersionLessThanCached 
        
        member this.NextElectronVersion =
            let makeSepochSemver = fun semver ->
                { SemVer = ValueOption.defaultValue(Semver.SemVersion(0,1,0)) semver
                  Sepoch = Sepoch.Scope "Electron" }
                
            match this, this.DeltaKind with
            | _, Patch
            | { Dirty = true }, _ ->
                this.Versions.FableElectronPackage
                |> makeSepochSemver
                |> SepochSemver.bumpPatch
            | _, (Major | Minor) ->
                this.Versions.DownloadedElectron
                |> makeSepochSemver
            | _ ->
                this.Versions.FableElectronPackage
                |> makeSepochSemver
            
                
                
