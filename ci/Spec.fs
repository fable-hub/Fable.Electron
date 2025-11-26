module Spec

open System.IO
open EasyBuild.FileSystemProvider
open Fake.Core
open Fake.Core.Context
open Fake.IO
open Partas.GitNet

[<Literal>]
let _rootPath = __SOURCE_DIRECTORY__ + "/.."
//%FileProvider%START%
type Root = AbsoluteFileSystem<_rootPath>

type VirtualRoot =
    VirtualFileSystem<
        _rootPath,
        """
fsdocs/
temp
    electron-api.json
"""
     >
//%FileProvider%END% //%PredefinedFileProvider%START%
module Projects =
    module Folders =
        type Remoting = Root.src.``Fable.Electron.Remoting``
        type Generator = Root.src.``ElectronApi.Json.Parser``
        type Build = Root.ci
        type Electron = Root.src.``Fable.Electron``
        type Forge = Root.src.``Fable.Electron.Forge``
        type Tests = Root.tests

    let Remoting = Folders.Remoting.``Fable.Electron.Remoting.fsproj``
    let Generator = Folders.Generator.``ElectronApi.Json.Parser.fsproj``
    let Build = Root.``Build.fsproj``
    let Electron = Folders.Electron.``Fable.Electron.fsproj``
    let Forge = Folders.Forge.``Fable.Electron.Forge.fsproj``

    let Test =
        Folders.Tests.``Fable.Electron.Remoting.Tests``.``Fable.Electron.Remoting.Tests.fsproj``

    let Docs = Root.docs.``Docs.fsproj``

module Solutions =
    let Electron = Root.``Fable.Electron.sln``

module Files =
    let Api = VirtualRoot.temp.``electron-api.json``
    let Cache = Root.ci.``cache.json``
//%PredefinedFileProvider%END% //%TargetsExample%START%
module Ops =
    /// Clean directories from build material, and temporary files downloaded such as electron-api.json
    [<Literal>]
    let clean = "clean"

    /// Clean directories from fable generated files
    [<Literal>]
    let fableClean = "fable-clean"

    /// List releases from electron
    [<Literal>]
    let listReleases = "list-releases" //%TargetsExample%END%

    /// List releases from electron with details
    [<Literal>]
    let listDetailedReleases = "list-detailed-releases"
    //%DownloadTargets%START%
    /// Download a specified release
    [<Literal>]
    let downloadApi = "download-api"

    [<Literal>]
    let downloadLatest = "download-latest"

    /// Combines list releases and download api interactively
    [<Literal>]
    let downloadInput = "download-input" //%DownloadTargets%END%

    /// Post download cleanup
    [<Literal>]
    let postDownload = "post-download-clean"

    /// Generate the Fable.Electron bindings
    [<Literal>]
    let generate = "generate"

    [<Literal>]
    let activateGitnet = "activate-gitnet"

    /// Setup docs via npm i or npm ci
    [<Literal>]
    let setupDocs = "setup-docs"

    /// Run docs in watch mode
    [<Literal>]
    let docs = "docs"

    /// Build projects
    [<Literal>]
    let build = "build"

    /// Pack projects
    [<Literal>]
    let pack = "pack"

    /// Push to nuget
    [<Literal>]
    let push = "push"

    /// Generate the API Docs (only to be run in an external repo)
    [<Literal>]
    let generateApiDocs = "generate-api-docs"

    /// Does setup for tests by downloading deps with npm i or npm ci
    [<Literal>]
    let setupTest = "setup-test"

    /// Run tests
    [<Literal>]
    let test = "test"

    /// Do post test cleanup
    [<Literal>]
    let postTest = "post-test"

    /// Restores tools in repo
    [<Literal>]
    let restore = "restore"

    /// Formats files with fantomas
    [<Literal>]
    let format = "format"

    /// Cron job for use by CI
    [<Literal>]
    let cron = "cron"

    [<Literal>]
    let loadCache = "load-cache"

    [<Literal>]
    let gitnet = "gitnet"
    //%ExampleArgsDef%START%
    module Args =
        [<Literal>]
        let help = "--help"

        [<Literal>]
        let detailed = "--detailed"

        [<Literal>]
        let quick = "--quick"

        [<Literal>]
        let dry = "--dry-run" //%ExampleArgsDef%END%

        [<Literal>]
        let release = "--release"

        [<Literal>]
        let npmCi = "--npm-ci"

        [<Literal>]
        let skipTest = "--skip-test"

        [<Literal>]
        let nugetApi = "--nuget-key"

        [<Literal>]
        let ghKey = "--gh-key"

        [<Literal>]
        let target = "--target"

        [<Literal>]
        let open' = "--open"

        [<Literal>]
        let watch = "--watch"

        [<Literal>]
        let debug = "--debug" //%ExampleCommandsDef%START%

module Commands =
    [<Literal>]
    let docs = "docs"

    [<Literal>]
    let test = "test"

    [<Literal>]
    let format = "format"

    [<Literal>]
    let generateApiDocs = "generate-api-docs"

    [<Literal>]
    let generate = "generate" //%ExampleCommandsDef%END%

    [<Literal>]
    let pack = "pack"

    [<Literal>]
    let cron = "cron"

    [<Literal>]
    let run = "run"


[<Literal>]
let githubUsername = "GitHub Action"

[<Literal>]
let githubEmail = "41898282+github-actions[bot]@users.noreply.github.com"
//%ArgsType%START%
type Args =
    static let mutable args = None

    static let hasFlag value =
        args |> Option.exists (DocoptResult.hasFlag value)

    static let getFlag value =
        args |> Option.bind (DocoptResult.tryGetArgument value)

    static member setArgs argsv =
        args <- (Cli.parser: Docopt).Parse(argsv) |> Some

    static member detailed = hasFlag Ops.Args.detailed
    static member quick = hasFlag Ops.Args.quick
    static member dryRun = hasFlag Ops.Args.dry //%ArgsType%END%
    static member help = hasFlag Ops.Args.help
    static member release = getFlag Ops.Args.release
    static member npmCi = hasFlag Ops.Args.npmCi
    static member skipTest = hasFlag Ops.Args.skipTest
    static member apiKey = getFlag Ops.Args.nugetApi
    static member target = getFlag Ops.Args.target
    static member gitClientToken = getFlag Ops.Args.ghKey
    static member debug = hasFlag Ops.Args.debug
    static member open' = hasFlag Ops.Args.open'
    static member watch = hasFlag Ops.Args.watch
//%CliType%START%
and Cli =
    static member spec =
        $"""
Usage:
    Build.exe {Commands.docs} [options]
    Build.exe {Commands.generate} [options]
    Build.exe {Commands.generateApiDocs} [options]
    Build.exe {Commands.pack} [options]
    Build.exe {Commands.cron} [options]
    Build.exe {Commands.run} [run] [options]
    Build.exe {Commands.test} [test] [options]
    Build.exe {Commands.format} [options]

Test Options [test]:
    --open                  Will run the test application and open the app instead of
                            running the headless test suite.
    --watch                 Will run the test application and open the app in watch mode
                            instead of running the headless test suite.

Run Options [run]:
    --target <NAME>         The target to run

Options [options]:
    -h, --help              Show this help message.
                            Note that the `cron` job should only be performed by the CI runners
    -D, --detailed          When printing release information, show all fields.
    -Q, --quick             Skip setup steps, such as installing dependencies (for local environments).
    --dry-run               Collect actions and print them at the end instead of pushing any changes.
    --npm-ci                `npm install` commands are run using `ci` (clean install) instead. Use this
                            if you are encountering 'module missing' errors for npm dependencies.
    --release <RELEASE>     Perform the actions for the specific release tag.
    --skip-test             Skip tests
    --format                Run fantomas
    --nuget-key <API-KEY>   The key used in authentication to push packages to NuGet.
    --gh-key <PAT>          Personal access token for GitHub to use instead of the CI runner.
    --debug                 Shows the dependency list for the command and args
"""

    static member parser = Docopt(Cli.spec) //%CliType%END%

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
