# Build

This build project utilises Fake and EasyBuild to provide CI for Fable.Electron.

We use the `gh` cli tool to list and download release asset files for electron.

By default we target 'latest' releases. You can use the build process to create bindings
for a different release if required.

## Organisation

The `Build.fsproj` is the root project to allow top level `dotnet run` commands
to run the project without conflict.

To prevent obfuscating the root file system, we nest the files within a ci folder.
