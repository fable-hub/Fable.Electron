---
---

# Extension Helper

Where a documented structure, class or module is an extension of a term,
we try to embed/lift the properties/methods et al of the extended type for
the source generation.

To do so, we cache the names and parsed documentation for each of the Module,
Class, Structure and Element documentation types as they are decoded.

```fsharp
module ExtensionAssistant =
    /// <summary>
    /// Key is name of the type. The value is the type info,
    /// with an optional second type which represents the parent
    /// in the case of classes exported from modules.
    /// </summary>
    let private cache =
        Dictionary<string, ParsedDocumentation * ModuleDocumentationContainer voption>()
```

The user of the decoder is responsible for utilising the values as they see fit.

In our implementation, this is delegated to the `Prelude` of the
Source Generation.

:::warning
Where we do not find the name of the Extension, we will error unless the
name is specifically accounted for, as an exception to ignore, in the
`Prelude` step.

This ensures that we account for any dependencies we might have to include
in the source generation to account for these types before we add a `inherit` or
`interface`.
:::
