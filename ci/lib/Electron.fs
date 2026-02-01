module Fake.Tools.Electron

open Fake.Tools

let private repo =
    { Owner = "electron"
      Name = "electron" }

let getReleases () = Gh.releaseList repo

/// Will not overwrite
let downloadElectronApi outputFile release =
    Gh.downloadReleaseAsset
        (fun args ->
            { args with
                Repository = repo
                Pattern = [ "electron-api.json" ]
                Release = Some release
                Flags.Output = Some outputFile
                Flags.SkipExisting = true
                Flags.Overwrite = false }
        )
        "."

/// Will overwrite
let downloadAndOverwriteElectronApi outputFile release =
    Gh.downloadReleaseAsset
        (fun args ->
            { args with
                Repository = repo
                Pattern = [ "electron-api.json" ]
                Release = Some release
                Flags.Output = Some outputFile
                Flags.SkipExisting = false
                Flags.Overwrite = true }
        )
        "."