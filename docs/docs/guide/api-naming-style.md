---
sidebar_position: 2
title: API Style
---
# API Style

A combined approach is used for the API styling, to allow minimal cognitive effort
translating JS examples to F#, while maintaining common F# idioms.

## JS Modules

Within Electron, there are distinctions between static 'module' like classes
(where an instance exists by default), to 'classes' which are created when
required.

The 'modules' are cased in `camelCase` similar to the source material.
__All__ other __TYPE__ definitions utilise `PascalCasing`.

All other methods and their parameters follow their original naming pattern of `camelCase`, with the
use of backtick-stropping where required for reserved keywords.

## StringEnums

StringEnums are named by the _access path_ taken to navigate to their definition.
The only addendum to this is that they are all contained in a root module `Enums`.

For example, the string enum for a parameter named `policy` for method `setPolicy`
of module `someModule` in the `Main` process will be located at `Enums.Main.SomeModule.SetPolicy`,
with the type name being `Policy`.

```fsharp title='Example access path breakdown'
Enums           // <- Container for all Enums
    .Main       // <- Process module
    .SomeModule // <- Some type (class/module)
    .SetPolicy  // <- Some method
    .Policy     // <- Parameter name, and name of Enum type
```

You can see that the path is styled by typical F# module `PascalCase` and type idioms.

You can feel free to create abbreviations and other patterns to make access to common
enums easier.

:::warning
Sometimes the path includes a route that is non-congruent to your usage. This is likely
because we have abstracted away an options object parameter, meaning your access
path is made easier than the real access path.

For the above example, if the method took only an `options` object with the named property
being `policy`, then the final access path would be `Enums.Main.SomeModule.SetPolicy.Options`
with the type name `Policy`.
:::

## POJOs

We generate a POJO class definition where a parameter or other API surface uses 
an options object that we are unable to inline.

The POJOs are located by their _access path_ similar to [`StringEnums`](#stringenums).

## Structures

In the Electron API, named POJOs are called `structures` and are available globally.

These structures are located within an F# `AutoOpen` module `Types`.

## Events

Where an `Event` handler/listener has a type signature that is longer than TWO (including the return value),
then we generate both a curried handler signature, and a TWO length typed lambda where the
first type is an interface where you can access the parameters by name, with full
documentation.

These are defined by their _access path_ similar to [`StringEnums`](#stringenums) et al.

:::warning
Unlike `POJO`'s, `StringEnum`'s and `Structure`'s, you typically will not ever
need to access the type definition itself, as you access the members from an
inline function parameter:

```fsharp
fun parameters -> parameters.senderId
```
:::

## Delegates

Where a method parameter, or other api surface, uses a callback/lambda with more than
ONE parameter, we create both curried and delegate overloads.

The Delegates are defined by their _access path_, similar to [`StringEnums`](#stringenums)
and [`POJO`s](#pojos).

The exception is in the case of `Event`'s as [described above](#events).
