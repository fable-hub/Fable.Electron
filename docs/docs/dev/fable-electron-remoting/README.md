# Fable.Electron.Remoting

The package that provides the `Fable.Remoting` experience for electron IPC.

This is heavily based off `Fable.Remoting`, using the same dependency for
the reflection helpers - `Fable.SimpleJson`.

## DotNet RPC vs JS IPC

Unlike `Fable.Remoting`, there is no communication between different languages/frameworks/runtimes. All processes in electron run on a javascript/node
runtime.

This means we do not have to involve the serialisation/deserialisation steps of
`Fable.Remoting`, and we are solely concerned with abstracting away the IPC
boilerplate event handling/sending.

> This also means we do not have to follow the same architecture of separating
> our API types into a separate Shared project.
> 
> The decision to do so is a matter of style/developer preference.

## Main - Preload - Renderer

:::note
Please [see the docs](https://www.electronjs.org/docs/latest/tutorial/ipc)
to understand the concepts and reasoning behind the `Main` and `Renderer` process,
and the use of the `Preload` scripts.
:::

The `Main` and `Renderer` processes have their own `Fable.Electron.Remoting`
module; the `Preload` module pertains to the bridge that exposes the relevant
API to the `Renderer` process.

You must ensure that each of the processes/scripts refer to their individual
module while building the proxies, as their internal implementations are
different and incompatible.

---

We essentially build an object of functions with the same names as the fields of
the record when we are building a client. The functions send whatever arguments
are passed through the channel determined by the field name and the type name.
The handlers create an abstraction over our API/handler implementations, which
handle the message receival, and reroute the arguments to our implementation
based on the field name.

We can rely on the safety of FCS, and dispose of the checks and validation of
argument counts etc, as these are not openly accessible APIs outside of our
application.
