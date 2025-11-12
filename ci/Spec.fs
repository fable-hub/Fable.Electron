module Spec
open EasyBuild.FileSystemProvider
open Fake.Core
open Fake.Core.Context

let [<Literal>] _rootPath = __SOURCE_DIRECTORY__ + "/.."
type Root = AbsoluteFileSystem<_rootPath>
type VirtualRoot = VirtualFileSystem<_rootPath, """
temp
    electron-api.json
""">

module Projects =
    let Remoting = Root.src.``Fable.Electron.Remoting``.``Fable.Electron.Remoting.fsproj``
    let Generator = Root.src.``ElectronApi.Json.Parser``.``ElectronApi.Json.Parser.fsproj``
    let Build = Root.``Build.fsproj``
    let Electron = Root.src.``Fable.Electron``.``Fable.Electron.fsproj``
    let Forge = Root.src.``Fable.Electron.Forge``.``Fable.Electron.Forge.fsproj``
module Solutions =
    let Electron = Root.``Fable.Electron.sln``
module Files =
    let Api = VirtualRoot.temp.``electron-api.json``

module Ops =
    let [<Literal>] clean = "clean"
    let [<Literal>] restoreTools = "restore-tools"
    let [<Literal>] listReleases = "list-releases"
    let [<Literal>] downloadLatestApi = "download-latest-api"
    let [<Literal>] generateBinding = "generate-binding"
    let [<Literal>] fableBuild = "fable-build"
    let [<Literal>] fableClean = "fable-clean"
    let [<Literal>] configGitBot = "configure-git-bot"
    let [<Literal>] gitPull = "git-pull"
    let [<Literal>] gitCommit = "git-commit"
    let [<Literal>] fantomas = "fantomas"
    let [<Literal>] downloadApi = "download-api"
    let [<Literal>] build = "build"
    let [<Literal>] pack = "pack"
    let [<Literal>] push = "push"
    let [<Literal>] test = "test"
    let [<Literal>] checkChangeLogGen = "check-changelog-gen"
    let [<Literal>] changeLogGen = "changelog-gen"

let [<Literal>] githubUsername = "GitHub Action"
let [<Literal>] githubEmail = "41898282+github-actions[bot]@users.noreply.github.com"

module Args =
    let mutable detailed = false
    let mutable apiKey: string Option = None
    let mutable releaseVersion: string option = None
    let mutable gitClientToken: string Option = None
    let setArgs args =
        let containsArgs arg =
            args |> Array.contains arg
        let getArgValue arg =
            args
            |> Array.tryFindIndex ((=) arg)
            |> Option.map ((+) 1)
            |> Option.bind(fun idx -> Array.tryItem idx args)
        detailed <- containsArgs "--detail"
        apiKey <- getArgValue "--nuget-api-key"
        releaseVersion <- getArgValue "--release"
        gitClientToken <- getArgValue "--github-client-token"

open Fake.IO.Globbing.Operators
let sourceFiles =
    !! "**/*.fs"
    -- "**/obj/**/*.*"
    -- "**/AssemblyInfo.fs"
    // Fantomas will most assuredly choke on this
    -- "**/Fable.Electron/Program.fs"

// Credit SAFE STACK
let initializeContext() =
    let execContext = FakeExecutionContext.Create false "build.fsx" []
    setExecutionContext (RuntimeContext.Fake execContext)

let createProcess exe args dir =
    CreateProcess.fromRawCommand exe args
    |> CreateProcess.withWorkingDirectory dir
    |> CreateProcess.ensureExitCode
let dotnet args dir =
    createProcess "dotnet" args dir
    |> Proc.run
    |> ignore
