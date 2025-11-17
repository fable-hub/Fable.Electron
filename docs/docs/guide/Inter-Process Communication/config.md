---
title: Remoting Config
---

# Configuration

There are few changes that can be done to change the default behaviour of the
proxy builds.

You should also be aware that the `Fable.Electron.Remoting.Main` module has
an extra config compared to the others.

---

When the proxies are created on the `Renderer` side, they are exposed via the `Preload` step on the `window` object as a property.

The name of this property is created based on the combination of a 'base' name
in the config and the name of your Record type.

By default, the prefix is `FABLE_REMOTING` and it is mapped with the type name
as follows: 

```fsharp
$"{baseName}_{typeName}"
```

Similarly, the `Main` and `Preload` step share a named communication - `channel-name`.

The `channel-name` is unique for each record field, and is a combination of
the type name and the field name, which is mapped by default as follows:

```fsharp
$"{typeName}:{fieldName}"
```

## Common

```fsharp title="Api Name Base"
Remoting.init
|> Remoting.withApiNameBase "FABLE_REMOTING"
```

```fsharp title="Api Name Mapping"
Remoting.init
|> Remoting.withApiNameMap (fun baseName typeName -> $"{baseName}_{typeName}")
```

```fsharp title="Channel Name Mapping"
Remoting.init
|> Remoting.withChannelNameMap (fun typeName fieldName -> $"{typeName}:{fieldName}")
```

## `Main` Specific

When using `Remoting.buildClient` on the `Main` process, you will be required to
pass all the windows that you wish to send the messages to.

```fsharp
Remoting.init
|> Remoting.withWindow mainWindow // repeat this as many times as required

// alternatively, create your array of windows and feed it in
let windows = [| ... |]
Remoting.init
|> Remoting.setWindows windows
```
