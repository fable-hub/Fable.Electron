# Mocha & WDIO

These are basic bindings to the limited set of API required for the test
project to function.

> [!NOTE]
> `Fable.Mocha` is not compatible with WDIO, as WDIO does not seems to
> correctly wrap the execution in a test runner context.
> 
> The functions relating to `Expect` do work correctly since they make assertions.
> But the test framework is a bit flaky.
