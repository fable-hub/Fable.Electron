---
sidebar_position: 1
title: Installation
---

# Installation

![dotnet add package Fable.Electron](@site/static/img/fable-electron-install.png)

There are 3 packages available on nuget. All packages compile with Fable, so there's no need to have separate projects/dependencies (as is the case with other stacks using Fable.Remoting) unless you want to!

# Required

### Fable.Electron

These are the bindings to `electron`.
 
 ```shell
 dotnet add package Fable.Electron
 ```

:::tip Semver
The downloaded _Major_ and _Minor_ version mirror the `electron` version they were generated from!

The patch is increased not just by a new `electron` patch, but also with our own changes.

> If you have `Fable.Electron` version 39.2.x, then you should install the corresponding `electron` version.
:::

<br/>

# Ancillary Packages

### Fable.Electron.Remoting

```console
dotnet add package Fable.Electron.Remoting
```

A library to empower you to use the familiar `Fable.Remoting` style for `electron` IPC!

Supports:
* Two way IPC (Renderer -> Main -> Renderer)
* One way IPC (Main -> Renderer)

Setup in your `main` process, `prerender` script and `render` process and you're set!

### Fable.Electron.Forge

```shell
dotnet add package Fable.Electron.Forge
```

These are light bindings for `electron-forge` with environment constants that are available in the `vite` based templates.



