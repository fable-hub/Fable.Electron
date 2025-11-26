module GitNet

open Partas.GitNet
open Spec
open System.IO

let private gitnetConfig =
    { GitNetConfig.initFSharp with
        //%IgnoreProjects%START%
        Projects =
            { ProjectConfig.init with
                IgnoredProjects =
                    [ Projects.Build
                      Projects.Generator
                      Projects.Test
                      Projects.Docs
                      Projects.Folders.Tests.``Tests.Common``.``Tests.Common.fsproj`` ]
                    |> List.map Path.GetFileNameWithoutExtension } //%IgnoreProjects%END%
        AssemblyFiles = AssemblyFileManagement.None
        WriteVersionToProjects = true
        Bump =
            { BumpConfig.init with
                DefaultBumpStrategy = ForceBumpStrategy.All
                Mapping =
                    { CommitBumpTypeMapping.init with
                        Major = [ BumpMatcher.Type "breaking"; BumpMatcher.Type "remove" ]
                        Minor = [ BumpMatcher.Type "feat"; BumpMatcher.Type "new"; BumpMatcher.Type "add" ]
                        Patch =
                            [ BumpMatcher.Type "fix"
                              BumpMatcher.Type "perf"
                              BumpMatcher.Type "update"
                              BumpMatcher.Type "change" ] } }
        Output =
            { OutputConfig.init with
                AllowUnconventional = false
                DefaultUnmatchedGroup = CommitGroup.Defaults.other
                Ignore =
                    [ IgnoreCommit.FooterKeyValue("changelog", "true")
                      IgnoreCommit.FooterKeyValue("gitnet", "ignore")
                      IgnoreCommit.SkipCi ]
                AllowUnmatched = true
                GroupMatcher =
                    [ GroupMatcher(CommitGroup.Defaults.breaking, [ BumpMatcher.Type "breaking" ])
                      GroupMatcher(
                          CommitGroup.Defaults.changed,
                          [ BumpMatcher.Type "update"; BumpMatcher.Type "change" ]
                      )
                      GroupMatcher(
                          CommitGroup.Defaults.deprecated,
                          [ BumpMatcher.Type "depr"
                            BumpMatcher.Type "deprecated"
                            BumpMatcher.Type "deprecate" ]
                      )
                      GroupMatcher(
                          CommitGroup.Defaults.feat,
                          [ BumpMatcher.Type "feat"
                            BumpMatcher.Type "enhancement"
                            BumpMatcher.Type "new"
                            BumpMatcher.Type "added" ]
                      )
                      GroupMatcher(CommitGroup.Defaults.fix, [ BumpMatcher.Type "fix"; BumpMatcher.Type "fixed" ])
                      GroupMatcher(
                          CommitGroup.Defaults.revert,
                          [ BumpMatcher.Type "rollback"
                            BumpMatcher.Type "revert"
                            BumpMatcher.Type "rev" ]
                      )
                      GroupMatcher(
                          CommitGroup.Defaults.changed,
                          [ BumpMatcher.Type "updated"
                            BumpMatcher.Type "update"
                            BumpMatcher.Type "change" ]
                      ) ] } }

let runtime = new GitNetRuntime(gitnetConfig)
let initialCompute = runtime.DryRun()

let private getInitialVersion scope =
    match initialCompute.Versions.TryGetValue(scope) with
    | true, semver -> semver |> ValueOption.bind GitNetTag.chooseSemverCompatible
    | _ -> ValueNone

let private getInitialBump scope =
    match initialCompute.Bumps.TryGetValue(scope) with
    | true, semver -> ValueSome semver
    | _ -> ValueNone

let getInitVersionForge = getInitialVersion "Forge"
let getInitVersionElectron = getInitialVersion "Electron"
let getInitVersionRemoting = getInitialVersion "Remoting"
let getInitBumpForge = getInitialBump "Forge"
let getInitBumpElectron = getInitialBump "Electron"
let getInitBumpRemoting = getInitialBump "Remoting"

let createRuntime () = new GitNetRuntime(gitnetConfig)
