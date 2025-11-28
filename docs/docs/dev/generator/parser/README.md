---
title: Parser
sidebar_position: 0
---

# Parsing

```mermaid
flowchart 
    subgraph Parser
      ApiDecoder.fs
    end
    subgraph Common
    end
    subgraph spec ["Spec & Utilities"]
    end
    subgraph source ["F# Mapping & Generation"]
    end
    Parser --> source
    spec --> source
    style Parser fill:#a5c0d4,stroke:#7bb8e7
```


The logic for the `electron-api.json` parser is contained entirely within the file
`ApiDecoder.fs`. The Schema is derived from `@electron/docs-parser`.

As the types/schema of the JSON is declared, the `Thoth` decoder is declared in a
module named after the type.


