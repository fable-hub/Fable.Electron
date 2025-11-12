---
title: Testing
---

# Testing

Testing electron apps with an isolated process, while mocking communication from other processes, is fairly trivial; you are able to test the main process via node, and the renderer via any front-end component testing tech.

Testing both processes while maintaining a headless environment requires tech
like [`WebDriverIO`](https://webdriver.io/).

## WebDriverIO

`WebDriverIO` runs our electron app headless.

It is important to follow the docs closely for usage. An example is present in
the current source repository.

It is important to note that the supported test frameworks are limited; we use mocha, which allows us to leverage any javascript compatible _assertion_ library (allowing us to use the `Expect` logic from `Fable.Mocha`).

## Frameworks

The repository is concerned with testing Electron and Remoting logic, and is pure javascript and HTML such that it is framework agnostic.

However, most/all front-end frameworks are compatible through electron-forge, vite
and webdriver. [See the docs for specific framework setups](https://webdriver.io/docs/component-testing).
