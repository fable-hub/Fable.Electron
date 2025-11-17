---
sidebar_position: 1
title: Introduction
---

# Fable.Electron.Remoting

:::note Install Package
This package is not included by default.
```bash
dotnet add package Fable.Electron.Remoting
```
:::

[Inter-Process Communication (IPC) in `electron-js`](https://www.electronjs.org/docs/latest/tutorial/ipc) -
inbetween maintaining and developing the code on the main process, preload script, and renderer - can
be burdensome to develop and maintain as the codebase grows.

`Fable.Remoting` is a common pattern for RPC between _dotnet_ servers and _fable_ apps, where we define
a shared API via a record of functions, and build a proxy that abstracts the
RPC layer between the client and server.

`Fable.Electron.Remoting` is an extremely minimal version of this that utilises
the `electron-js` serialization instead of our own json.

Two patterns are supported out of the box:
* Two-Way IPC between `Main` and `Renderer` Processes
* One-Way IPC from `Main` to `Renderer` Processes

:::warning
Be aware that each of `Main`, `Renderer` and `Preload` have their own `Fable.Electron.Remoting` module.
:::

## Build Names

The Remoting build names follow a common pattern:
* When a router is initiating communication, it is the client, and the build command will be `Remoting.buildClient`
* When a router is receiving communication, it is the handler, and the build command will be `Remoting.buildHandler`
* When a router acts as a bridge (the preload script), the command is a derivative of `Remoting.buildBridge` (`Remoting.buildTwoWayBridge` in the case of the two way IPC)
