---
title: Introduction
sidebar_position: 0
---

# Development Introduction

This series of documentation relates to the contribution and development of the
`Fable.Electron` generator and libraries.

## Contribution

We welcome all contributions!

If you want to make changes to the codebase, you'll want to become familiar with the build system we have in place. It helps you simplify your workflow by performing various actions from the root repository space.

Otherwise, please be sure to make your pulls to `develop` and to utilise conventional commits.


:::important
Ensure all commits and merges into the main branch use conventional commits.

Other commits will be excluded from the release notes, and from consideration for versioning.

You can choose to squash pulls into a conventional commit if this is easier.

**Try** to keep commits for different projects separate to each other. This will allow your commit messages, and the subsequent release notes to be more informative.

If you can't decide between the type of a commit, always default to the higher one (ie, if you have a squashed commit for a fix and feature, make the type a feature).
:::

### Versioning

If you use conventional commits diligently, you can forget about versioning. This is abstracted away for you.

The only thing to keep in mind is that you must indicate a breaking change by following the type of the commit with an exclamation mark:

:::important Not breaking
`feat: Add new methods`
:::

:::danger Breaking
`feat!: Add new methods`
:::

## Development Stack

| Technology     | Description                                                                                                                                                      |
|:---------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Fantomas       | Fantomas is not just a formatter.<br/>We utilise the library module `SyntaxOak` to generate our source using a battle-tested library.                            |
| Thoth.Net.Json | Thoth is a functional approach to deserializing JSON. It is used to parse the `electron-api.json` document into F#                                               |
| FAKE           | Is the DSL/Library used for our build automations.                                                                                                               |
| Partas.GitNet  | Library utilised for versioning based on conventional commits in a mono-repo                                                                                     |
| Expecto        | Testing library/framework used for testing our build system.                                                                                                     |
| Fable.Mocha    | Provides Fable friendly `Expecto` like methods/helpers that is used in conjunction with `mocha-js` and `wdio` for testing our `electron` bindings and libraries. |
| Docusaurus     | Static site documentation framework used to build this documentation.                                                                                            |
| gh cli | Used in the build system to perform various actions such as downloading electron releases.                                                                       |


### Issues

If you have a bug, or feature to request, or just need some help, please submit an issue.

If you're unsure of how to proceed with a pull, we can discuss it before moving to a draft pull and iterating over your contribution.
