module Spec

open EasyBuild.FileSystemProvider
open Fake.Core
open Fake.Core.Context

[<Literal>]
let _rootPath = __SOURCE_DIRECTORY__ + "/.."

type Root = AbsoluteFileSystem<_rootPath>

type VirtualRoot =
    VirtualFileSystem<
        _rootPath,
        """
temp
    electron-api.json
"""
     >

module Projects =
    let Remoting =
        Root.src.``Fable.Electron.Remoting``.``Fable.Electron.Remoting.fsproj``

    let Generator =
        Root.src.``ElectronApi.Json.Parser``.``ElectronApi.Json.Parser.fsproj``

    let Build = Root.``Build.fsproj``
    let Electron = Root.src.``Fable.Electron``.``Fable.Electron.fsproj``
    let Forge = Root.src.``Fable.Electron.Forge``.``Fable.Electron.Forge.fsproj``

module Solutions =
    let Electron = Root.``Fable.Electron.sln``

module Files =
    let Api = VirtualRoot.temp.``electron-api.json``

module Cli =
    let spec =
        """
Usage:
    build.exe list-releases [--detailed | --help]
    build.exe generate-binding [api-options]
    build.exe test [options | api-options]
    build.exe pack [options | api-options]
    build.exe push [options | api-options]
    build.exe fantomas
    build.exe docs
    build.exe [options]

Options [options]:
    -h, --help                  Print this message; Note that using this CLI
                                More often than not is initiated via `dotnet run -- <args>`
                                Replace `build.exe` with `dotnet run --`.
    --nuget-key <api-key>       For CI publishing; NUGET API key for uploading (when ENV variable not set)
    --gh-key <token>            For CI pull creation; GHA Token to create the pull
    -f, --format                Force formatting instead of failing before making commits if required.
    --git-bot                   Set the local git config to use the GitHubBot details
    --clean                     Can use to clean directories outside of the normal build commands.

Api Options [api-options]:
    --detailed                  When listing releases, will provide detailed meta-data
    -r, --release <tag>         When generating bindings, the specific release to target (latest if not set)
    --choose                    When generating bindings, list the releases and ask which release to target.
"""

    let parser = Docopt(spec)

module Ops =
    [<Literal>]
    let clean = "clean"

    [<Literal>]
    let restoreTools = "restore-tools"

    [<Literal>]
    let listReleases = "list-releases"

    [<Literal>]
    let downloadLatestApi = "download-latest-api"

    [<Literal>]
    let generateBinding = "generate-binding"

    [<Literal>]
    let fableBuild = "fable-build"

    [<Literal>]
    let fableClean = "fable-clean"

    [<Literal>]
    let configGitBot = "configure-git-bot"

    [<Literal>]
    let gitPull = "git-pull"

    [<Literal>]
    let gitCommit = "git-commit"

    [<Literal>]
    let fantomas = "fantomas"

    [<Literal>]
    let downloadApi = "download-api"

    [<Literal>]
    let build = "build"

    [<Literal>]
    let pack = "pack"

    [<Literal>]
    let push = "push"

    [<Literal>]
    let test = "test"

    [<Literal>]
    let checkChangeLogGen = "check-changelog-gen"

    [<Literal>]
    let changeLogGen = "changelog-gen"
    
    [<Literal>]
    let docs = "docs"

[<Literal>]
let githubUsername = "GitHub Action"

[<Literal>]
let githubEmail = "41898282+github-actions[bot]@users.noreply.github.com"

type Args =
    static let mutable args = None
    static let mutable _release = None

    static member hasFlag value =
        args |> Option.exists (DocoptResult.hasFlag value)

    static member getFlag value =
        args |> Option.bind (DocoptResult.tryGetArgument value)
    static member gitBot = Args.hasFlag "--git-bot"
    static member help = Args.hasFlag "--help"
    static member detailed = Args.hasFlag "--detailed"
    static member apiKey = Args.getFlag "--nuget-key"
    static member releaseVersion = Args.getFlag "--release" |> Option.orElse _release
    static member gitClientToken = Args.getFlag "--gh-key"
    static member listReleases = Args.hasFlag "--choose-release"
    static member format = Args.hasFlag "--format"
    static member clean = Args.hasFlag "--clean"
    static member setReleaseVersion value = _release <- Some value

    static member setArgs argsv =
        args <- Cli.parser.Parse(argsv) |> Some
        _release <- Args.getFlag "--release"

open Fake.IO.Globbing.Operators

let sourceFiles =
    !!"**/*.fs"
    -- "**/obj/**/*.*"
    -- "**/AssemblyInfo.fs"
    // Fantomas will most assuredly choke on this
    -- "**/Fable.Electron/Program.fs"

// Credit SAFE STACK
let initializeContext () =
    let execContext = FakeExecutionContext.Create false "build.fsx" []
    setExecutionContext (RuntimeContext.Fake execContext)

let createProcess exe args dir =
    CreateProcess.fromRawCommand exe args
    |> CreateProcess.withWorkingDirectory dir
    |> CreateProcess.ensureExitCode

let dotnet args dir =
    createProcess "dotnet" args dir |> Proc.run |> ignore
