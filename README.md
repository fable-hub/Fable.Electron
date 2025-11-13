# Fable.Electron

Fable bindings for [Electron](https://electronjs.org/).

Local Build/Development
--------------------------

The root solution folder contains the `Build` project file which allows us to run our `Fake` CLI from the root folder with `dotnet run -- [command] [options]`. Clone the repository and run `dotnet run -- --help` to see the CLI help menu.

## NPM ci

If you're working with the repository over time, you might come across an error like this when returning to the project and trying to run the tests or docs:

![Example image of 'Cannot find module' error running `dotnet run -- docs`](./docs/static/img/cleaninstall-example.PNG)

If this occurs, retry the command, but add the flag `--npm-ci`.
This will indicate to the CLI to use `npm ci` instead of `npm i`.


Contributions are welcome!
--------------------------

**Pull requests are more than welcome**, whether it’s bindings for new APIs, new helpers, bugfixes, or just improving typos and formatting in the documentation. If you want to create a PR with non-trivial changes, consider opening an issue first so you don’t waste time and effort on something that might not be accepted or might already be underway.

## Deployment checklist

1. Make necessary changes to the code
2. Update the changelog
3. Update the version and release notes in the package info, as well as the supported Electron versions in the `NpmPackage` node
4. Commit and tag the commit (this is what triggers deployment from  AppVeyor). For consistency, the tag should be identical to the package version number.
5. Push the changes and the tag to the repo. If AppVeyor build succeeds, the package is automatically published to NuGet.
