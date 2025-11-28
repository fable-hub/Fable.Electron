---
title: Types
---

# Type Design

## Aggregation

Where there is the unification of different type fields in the schema, we instead
embed those fields as a separate type.

For example:

```fsharp
type MethodParameterDocumentation =
    { Name: string
      Description: string
      Required: bool
      // highlight-start
      // Embedded
      TypeInformation: TypeInformation }
        // highlight-end
```

The type above would be an interface that inherits/extends the properties of the
`TypeInformation` shape. In this case, it is easier to embed this as a separate
type, rather than spread its shape within the record.

## Optional Fields vs Nullable Values

In cases where there is the distinction between a property that is always present
but may contain a null/undefined value, and a property that may not be present,
we annotate the field with `// Optional Field` to distinguish the latter.

```fsharp
type MethodDocumentationBlock =
    {
      // Embedded
      DocumentationBlock: DocumentationBlock
      // highlight-start
      // Optional field
      RawGenerics: string option
      // highlight-end
      Signature: string
      Parameters: MethodParameterDocumentation[]
      // highlight-next-line
      Returns: TypeInformation option }
```

This is reflected in the `Decoder`.

```fsharp
module MethodDocumentationBlock =
    let decode: Decoder<MethodDocumentationBlock> =
        Decode.object (fun get ->
            { DocumentationBlock = get.Required.Raw DocumentationBlock.decode
              // highlight-next-line
              RawGenerics = get.Optional.Field "rawGenerics" Decode.string
              Signature = get.Required.Field "signature" Decode.string
              Parameters =
                Decode.array MethodParameterDocumentation.decode
                |> get.Required.Field "parameters"
              // highlight-next-line
              Returns = Decode.option TypeInformation.decode |> get.Required.Field "returns" })
```

> Notice that the `Returns` field uses the `.Required` field access; however,
> the `RawGenerics` field uses the `.Optional` field access for thoth since it
> may not be present.
>
> Still, both have the same `Option` type.

## Unions

Where we have a possibility of one of several types distinguished by a field
value, we assert the condition for the field value in the decoder, and
then attempt each of the decoders in sequence, leaving the least specific to last.

```fsharp title="Example Decoder that Asserts a Field Value"
module ElementDocumentationContainer =
    let decode: Decoder<ElementDocumentationContainer> =
        Decode.object (fun get ->
            // highlight-start
            get.Required.Field "type" Decode.string
            |> function
                | "Element" ->
            // highlight-end
                    { Process = get.Required.Field "process" ProcessBlock.decode
                      Methods = Decode.array MethodDocumentationBlock.decode |> get.Required.Field "methods"
                      Events = Decode.array EventDocumentationBlock.decode |> get.Required.Field "events"
                      Properties =
                        Decode.array PropertyDocumentationBlock.decode
                        |> get.Required.Field "properties"
                      BaseDocumentationContainer = get.Required.Raw BaseDocumentationContainer.decode }
                  // highlight-next-line
                | _ -> Decode.string |> Decode.andThen Decode.fail |> get.Required.Field "type")
```

```fsharp title="Example of Parsing a Type Union Using the Above"
module ParsedDocumentation =
    let decode: Decoder<ParsedDocumentation> =
        Decode.oneOf
            [ ModuleDocumentationContainer.decode
              |> Decode.map (Module >> ExtensionAssistant.add)

              ClassDocumentationContainer.decode
              |> Decode.map (Class >> ExtensionAssistant.add)

              StructureDocumentationContainer.decode
              |> Decode.map (Structure >> ExtensionAssistant.add)

              ElementDocumentationContainer.decode
              |> Decode.map (Element >> ExtensionAssistant.add) ]
            // This example has no *catch all* at the end, because
            // we want an exception to be raised if our schema doesn't
            // fit.
```


