
import { foldBack } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/List.js";

/**
 * Returns an accelerator string that can be used to register shortcuts.
 */
export function AcceleratorModule_create(modifiers, key) {
    let tupledArg;
    return [modifiers, (tupledArg = key, foldBack((m, acc) => ((m + "+") + acc), tupledArg[0], tupledArg[1]))];
}

