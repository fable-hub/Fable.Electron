---
sidebar_position: 1
title: Introduction
---

# Introduction

The `Fable.Electron` bindings are automatically generated from the same source
material as the TypeScript type files, with some features for usage from F# and Fable.

## Installation

There are plans to modify the package generation to also include separate process
specific packages.

For the moment, you must install the `Fable.Electron` package and access the process
specific APIs from their respective modules, and use the types from other modules
when they are provided to you by your current process.

```bash
dotnet add package Fable.Electron
```

## Fable.Electron.Remoting

The `Fable.Electron.Remoting` implements the familiar `Fable.Remoting` pattern to
Electron IPC.

This is installed as a separate package.

```bash
dotnet add package Fable.Electron.Remoting
```

## Node

We use the `Fable.Node` package for bindings to `Node`.

:::warning
Dependency on an external package that is not used as heavily as the `Fable.Browser`
packages meant that APIs in `Fable.Electron` such as `process` __do not__ inherit
the `Node` api.
:::

This means that you will need to use both `process` types from the packages for
the more comprehensive API provided.

The `Fable.Electron` `process` API provides all the _extra_ behaviour included in
`Electron`.

This may change in the future.

:::note
The exception to this is `EventEmitter`, for which we provide minimal bindings
within `Fable.Electron` itself.
:::
