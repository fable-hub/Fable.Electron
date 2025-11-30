---
title: Requirements
---

# Requirements

There are a variety of testing methods for `electron`. [WebdriverIO] is a supporting framework for
testing browser based node.js applications,
with [support for headless testing](https://webdriver.io/docs/headless-and-xvfb). It supports the [Mocha] framework.
This was the combination that we implemented successfully.

## Bindings

The [Fable] community has already developed bindings for [Mocha] via [Fable.Mocha].

However, there was some difficulty experienced when initially trying to get the tests running.

Whether this was because of the bindings, or user error, or something else, we are unaware.

However, we found it far easier to debug the test compiled output by creating our own lightweight bindings
around [Mocha].

:::note
There are no releases for the bindings for [Mocha]. They are quite trivial, and you can find them in the source code via
`tests/Tests.Common/Mocha.fs`.
:::

We have also made some ad-hoc bindings for the [WebdriverIO] protocols and methods for retrieving and interacting with
elements. Again, they are quite trivial as it currently stands.

Despite the above, the [Fable.Mocha] `Expect` module works fantastically for our purposes.

[WebdriverIO]: https://webdriver.io/docs/gettingstarted

[Mocha]: https://mochajs.org/

[Fable.Mocha]: https://github.com/Zaid-Ajaj/Fable.Mocha

[Fable]: https://fable.io/
