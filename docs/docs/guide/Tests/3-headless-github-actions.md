---
title: Headless CI
---

# Headless CI with GitHub Actions

If you're looking to setup your test suite for headless testing in a CI pipeline, then you'll want to ensure you have
this configured in your `wdio.conf.js` capabilities:

```js
{
    // ...   
    capabilities: [{
        browserName: 'electron',
        // Electron service options
        // see https://webdriver.io/docs/desktop-testing/electron/configuration/#service-options
        'wdio:electronServiceOptions': {
            //highlight-next-line
            appArgs: ['--headless', '--no-sandbox']
        }
    }]
}
```

## Workflow

Ensure you install the appropriate dependencies for your runner before running.

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout Repo
        uses: actions/checkout@v4

      #highlight-start
      - name: Install Chromium
        uses: nanasess/setup-chromedriver@v2

      - name: Install Node
        uses: actions/setup-node@v6
      #highlight-end

      - name: Install .NET
        uses: actions/setup-dotnet@v3

      - name: Restore toolchain
        run: dotnet tool restore

      - name: Navigate to your

      - name: Install npm dependencies
        run: npm i

      - name: Run test script
        run: npm run test
```
