# Remoting Tests

```bash
cd tests/Fable.Electron.Remoting.Tests
npm i
npm run test
```

Simple framework independent app that implements a counter to test
the TWO-way and ONE-way remoting.

The TWO-way remoting allows the renderer to communicate clicks and
actions to the main process, and log changes.

The ONE-way remoting commands the renderer to change its elements according
to the internal state on the main process.
