---
title: Setup
---

# Setup

Once you have a `Fable.Electron` base setup, you can install `wdio`.

```bash
npm init wdio@latest ./
```

:::tip
You can follow the instructions on
the [electron documentation page](https://www.electronjs.org/docs/latest/tutorial/automated-testing#using-the-webdriver-interface).
:::

## Configuration

For capabilities, you should have:

```js title="wdio.conf.js"
{
    // ...
    capabilities: [
        {
            browserName: 'electron',
            'wdio:electronServiceOptions': {
                appArgs: []
            }
        }
    ]
}
```

You then need to ensure that you package the application before running the test-runner.

You can combine this with your Transpilation step within a `package.json`.

```json title="package.json"
{
  "scripts": {
    "pretest": "dotnet fable -e .js --noCache --run electron-forge package",
    "test": "wdio run ./wdio.conf.js"
  }
}
```

<details>
<summary>Explanation</summary>

```bash
dotnet fable -e .js --noCache --run electron-forge package
```

* `dotnet fable` - activating the transpiler
* `-e .js` - using the `.js` extension (can make this whatever you want so long as your `wdio.conf.js` file is setup
  correctly to pick up your extension).
* `--noCache` - force retranspilation of code and dependencies.
* `--run electron-forge package` - tells Fable to run everything after the `--run` when the compilation is complete.

Since this is set as a `pre` hook in the `package.json`, if we were to run `npm run test` then it would compile and
package the application before running the test suites.

</details>
