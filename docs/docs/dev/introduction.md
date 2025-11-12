---
title: Introduction
sidebar_position: 0
---

# Development Introduction

This series of documentation relates to the contribution and development of the
`Fable.Electron` generator and libraries.

## Technologies

The project utilises `Fantomas` and `Thoth.Net.Json`.

`Fantomas` is used for source generation, while `Thoth.Net.Json` is utilised in
the parsing of the `electron-api.json` document from which our source is generated.

## General Structure

```mermaid
flowchart 
    subgraph Parser
      ApiDecoder.fs
    end
    subgraph Common
        Types.fs
    end
    subgraph spec ["Spec & Utilities"]
        direction TB
        Spec.fs
        Utils.fs
        Fantomas.Utils.fs
    end
    subgraph source ["F# Mapping & Generation"]
      Prelude.fs
      SourceMapper.fs
      Generator.fs
    end
    Parser --> source
    Prelude.fs --> SourceMapper.fs
    SourceMapper.fs --> Generator.fs
    spec --> source
    Common --> source
    Common --> Fantomas.Utils.fs
    style source fill:#a5c0d4,stroke:#7bb8e7
    style Parser fill:#a5c0d4,stroke:#7bb8e7
```
