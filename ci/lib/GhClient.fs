namespace Fake.Tools

type ReleaseInfo = {
    createdAt: System.DateTime
    isLatest: bool
    isPrerelease: bool
    tagName: string
}

/// <summary>
/// <code>
/// { Owner = "electron"; Name = "electron" }.ToString()
/// |> (=) "electron/electron"
/// </code>
/// </summary>
type Repository = {
    Owner: string
    Name: string
} with
    override this.ToString() = $"{this.Owner}/{this.Name}"
type DownloadFlags = {
    // Overwrite existing files of the same name
    Overwrite: bool
    /// Skip downloading when files of the same name exist
    SkipExisting: bool
    /// File to write a single asset to
    Output: string option
}
type DownloadArgs = {
    /// Select repository to act on
    Repository: Repository
    /// Target a specific release
    Release: ReleaseInfo option
    /// The glob pattern of file(s) to download.
    Pattern: string list
    /// The directory to download files to (default '.')
    Directory: string option
    Flags: DownloadFlags
} with
    static member Init = {
        Repository = {
            Owner = ""
            Name = ""
        }
        Release = None
        Pattern = []
        Directory = None
        Flags = {
            Overwrite = false
            SkipExisting = true
            Output = None
        }
    }

[<RequireQualifiedAccess>]
module Gh =
    open System.Text.Json
    open Fake.Core


    let private tool =
        lazy
            match ProcessUtils.tryFindFileOnPath "gh" with
            | Some tool -> tool
            | None -> failwith "GitHub CLI 'gh' was not found on path. Please install it and make sure it is available."
            |> CreateProcess.fromRawCommand

    /// Create a process from the args for GitHub CLI
    let rawCommand args workingDir =
        tool.Value args
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.redirectOutput

    /// Create and run a process from the args for GitHub CLI. Fails on error exit code.
    let runRawCommand args workingDir=
        rawCommand args workingDir
        |> Proc.run
        |> fun result ->
            match result.ExitCode with
            | 0 -> result.Result.Output
            | _ -> failwith result.Result.Error
    /// List releases for a repository on GitHub
    let releaseList repo =
        runRawCommand [ "release"; "list"; "-R"; $"{repo.Owner}/{repo.Name}"; "--json"; "tagName,createdAt,isLatest,isPrerelease" ] "."
        |> JsonSerializer.Deserialize<ReleaseInfo[]>
        |> Array.toList



    /// Download release assets from github
    let downloadReleaseAsset (args: DownloadArgs -> DownloadArgs) workingDir =
        DownloadArgs.Init
        |> args
        |> fun args ->
            [
                "release"; "download"
                match args.Release with
                | Some { tagName = tagName } -> tagName
                | _ -> ()
                match args.Repository with
                | { Owner = ""; Name = "" } ->
                    ()
                | _ ->
                    "-R"
                    args.Repository.Owner + "/" + args.Repository.Name
                yield!
                    args.Pattern
                    |> List.collect (fun p -> [ "-p"; p ])
                match args.Directory with
                | Some dir ->
                    "--dir"; dir
                | None -> ()
                match args.Flags.Output with
                | Some o -> "--output"; o
                | None -> ()
                if args.Flags.Overwrite then
                    "--clobber"
                if args.Flags.SkipExisting then
                    "--skip-existing"
            ]
        |> fun args ->
            rawCommand args workingDir
            |> CreateProcess.ensureExitCode
            |> Proc.startAndAwait
            |> fun proc ->
                async {
                    let! result = proc
                    if result.ExitCode = 0 then
                        return ()
                    else failwith result.Result.Error
                }
        
