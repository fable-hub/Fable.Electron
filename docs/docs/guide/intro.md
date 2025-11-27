---
sidebar_position: 0
title: Introduction
---

# Introduction

![Flair](@site/static/img/flair-1.png)

The `Fable.Electron` bindings are automatically generated from the same source
material as the TypeScript type files, with some features for usage from F# and Fable.

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
