# Foundation

`src\Fable.Electron.Remoting` creates a wrapper around electron ipc communication.
The wrapper is designed to be in the style of Fable.Remoting (https://github.com/Zaid-Ajaj/Fable.Remoting).


Currently the wrapper looks like this in usage:

```fsharp
// Shared.fs
open Fable.Core.JS // Promise type

type ExampleRouting = {
    SayHelloWorld: string -> Promise<Result<string, unit>>
}
```

```fsharp
// main.fs
let api: Shared.ExampleRouting = {
    SayHelloWorld = fun text -> promise {
        if text = "hello" then
            return Ok <| text + " world!"
        else
            return Error()
    }
}

app.whenReady().``then``(fun () ->
    //..

    Remoting.init
    |> Remoting.buildHandler(api)

    //..
|> ignore
```

```fsharp
// preload.fs

open Fable.Electron.Playground.Shared
open Fable.Electron.Remoting.Preload

Remoting.init
|> Remoting.buildTwoWayBridge<ExampleRouting>
```

```fsharp
// renderer.fs

open Fable.Core.JsInterop
open Fable.Electron.Remoting.Renderer
open Browser.Dom

importSideEffects "./index.css"

console.log "This message is being logged by 'renderer.js', included via VITE"

let api =
    Remoting.init
    |> Remoting.buildClient<Shared.ExampleRouting>

(api.SayHelloWorld "hello").``then``(function
    | Ok v -> console.log v
    | Error _ -> console.log "Didn't say hello back :(")
|> ignore
```

If we want to use the ipc main event inside the main process implementation we need to adjust it as written in docs:

```fsharp
// IPCMAINEVENT EXAMPLE
// shared.fs

type ExampleRouting = {
    SayHelloWorld: IpcMainEvent -> string -> Promise<Result<string, unit>>
}

// renderer.fs

let api =
    Remoting.init
    |> Remoting.buildClient<Shared.ExampleRouting>

(api.SayHelloWorld (unbox null) "hello").``then``(function
| Ok v -> console.log v
| Error _ -> console.log "Didn't say hello back :(")
|> ignore

// main.fs

let api: Shared.ExampleRouting = {
    SayHelloWorld = fun (event: IpcMainEvent) (text: string) -> promise {
        if text = "hello" then
            return Ok <| text + " world!"
        else
            return Error()
    }
}

```

The variant with ipc main event breaks with existing Fable.Remoting pattern. Fable.Remoting instead offers to helpers to pass the api implementation. `Remoting.fromValue`, which uses the api implementation directly, and `Remoting.fromContext`, which passes the context to the api implementation. The context can be used to create the api implementation, but it can also be used to pass the ipc main event to the api implementation.

Example server for Fable.Remoting with `fromContext`:

```fsharp
let musicStore (context: HttpContext) = {
    (* Your implementation here *)
}

let webApi =
  Remoting.createApi()
  |> Remoting.fromContext (fun ctx ->
      // create a music store from the context
      musicStore(ctx)
    )
```

# Goal

We want to align Fable.Electron.Remoting api with Fable.Remoting api. This means that we want to remove the need to pass the ipc main event to the api implementation, and instead use `fromIpcMainEvent` to pass the ipc main event to the api implementation.

The result should look like the following:

```fsharp
// IPCMAINEVENT EXAMPLE
// shared.fs

type ExampleRouting = {
    SayHelloWorld: string -> Promise<Result<string, unit>>
}

// renderer.fs

let api =
    Remoting.init
    |> Remoting.buildClient<Shared.ExampleRouting>

(api.SayHelloWorld "hello").``then``(function
| Ok v -> console.log v
| Error _ -> console.log "Didn't say hello back :(")
|> ignore

// main.fs

let api (event: IpcMainEvent): Shared.ExampleRouting = {
    // This needs a better example that actually uses the event, so we can verify correct implementation
    SayHelloWorld = fun (text: string) -> promise {
        if text = "hello" then
            return Ok <| text + " world!"
        else
            return Error()
    }
}

Remoting.createHandler() // This is a rename of `Remoting.init` to better align with Fable.Remoting
|> Remoting.fromIpcMainEvent api // alternatively `Remoting.fromValue` if we do not want to pass IpcMainEvent to the api implementation

```

# Verification

`tests\Fable.Electron.Remoting.Tests` contains tests for Fable.Electron.Remoting.

We must:
- Adjust record type in `tests\Fable.Electron.Remoting.Tests\src\shared.fs` `WindowLogger` to not include `IpcMainEvent`
- Adjust `tests\Fable.Electron.Remoting.Tests\src\main.fs` `windowLoggerApi` to use the new `fromIpcMainEvent` and pass the event as argument to the api implementation

```fsharp
let windowLoggerApi (event: IpcMainEvent) = {
    Log = fun msg ->
        promise {
            printfn $"Logging from window {event.sender.id}: {msg}"
            let windowId = event.sender.id
            let now = System.DateTime.Now.Ticks
            let logMessage = $"[Window {windowId}-{now}]:{msg}"
            Browser.Dom.console.log logMessage
            return logMessage
        }
}
```

- Adjust `tests\Fable.Electron.Remoting.Tests\src\renderer.fs` to not pass `unbox null` as argument to the api call

```fsharp
windowLoggerButton.addEventListener(
    "click",
    fun e ->
        console.log("Calling window logger API from renderer...")
        (windowLoggerApi
          .Log "Hello from Renderer!")
          .``then`` (
                fun (result: string) ->
                    windowLoggerOutput.innerText <- result
            )
        |> ignore
)
```

# Implementation Plan

## Plan: Main Remoting Context Injection Rework

- Breaking change now
- Main-side syntax shifts to createHandler + fromValue/fromIpcMainEvent
- Rename scope is Main only in this pass (Preload/Renderer keep init)
- Docs updated in the same PR
- Slight preference for createHandler() function form, unless a stronger implementation reason appears

I also saved this plan to session memory as plan.md so we can refine it quickly if needed.

**Steps**
1. Phase 1: Main API surface update (blocks later phases)
1. In Main.fs, replace module-level init with createHandler() that returns default RemotingConfig.
2. Keep config transformers unchanged (withApiNameBase, withApiNameMap, withChannelNameMap, withWindow, setWindows).
3. Remove public Main buildHandler and add public static fromValue and fromIpcMainEvent on Main Remoting type.
4. Keep pipeline ergonomics: implementation first, config second.

2. Phase 2: Receiver internals refactor (depends on Phase 1)
1. Refactor receiver registration in Main.fs into explicit paths:
- fromValue path invokes record field with payload args
- fromIpcMainEvent path creates implementation from event, then invokes record field with payload args
2. Remove first-argument IpcMainEvent reflection heuristic from runtime dispatch logic.
3. Preserve Promise/Async await behavior and sync behavior exactly as today.
4. Preserve record/function validation errors and channel mapping behavior.

3. Phase 3: Test app migration (depends on Phase 2)
1. Update shared.fs: WindowLogger.Log becomes string -> Promise<string>.
2. Update main.fs:
- windowLoggerApi becomes IpcMainEvent -> WindowLogger
- handler wiring uses createHandler() + fromValue and createHandler() + fromIpcMainEvent
3. Update renderer.fs:
- remove undefined/null filler argument from WindowLogger.Log calls
4. Keep preload.fs unchanged for naming (still init).

4. Phase 4: Docs migration (parallel with Phase 3 after Phase 2 is stable)
1. Update two-way-ipc.mdx:
- Main examples use createHandler() + fromValue/fromIpcMainEvent
- IpcMainEvent section describes explicit injection pattern
- renderer sample removes placeholder arg
2. Update one-way-ipc.mdx:
- Main snippets use createHandler()
- Preload/Renderer snippets remain init
3. Update config.md:
- explicitly distinguish Main naming from Preload/Renderer naming
4. Update architecture explanation in README.mdx to remove old implicit first-arg detection explanation.

5. Phase 5: Breaking-change communication (after Phases 3-4)
1. Add migration notes in RELEASE_NOTES.md:
- Main init renamed to createHandler
- Main buildHandler removed
- fromValue/fromIpcMainEvent introduced
- shared contracts should no longer include renderer-supplied IpcMainEvent placeholder args

6. Phase 6: Verification (final gate)
1. dotnet build Fable.Electron.sln
2. In tests/Fable.Electron.Remoting.Tests: npm run test
3. Ensure Window Logger e2e still matches [Window id-ticks]:Hello from Renderer! output pattern
4. In docs: npm run build
5. Spot-check docs consistency: Main uses createHandler, Preload/Renderer still use init

**Scope Boundaries**
- Included: Main remoting API rework, tests, docs, migration notes.
- Excluded: Renaming init in Preload/Renderer in this pass.

If you want, I can now refine this into a PR-ready checklist (commit slices and file-by-file order) before handoff.