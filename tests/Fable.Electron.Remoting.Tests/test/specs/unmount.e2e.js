
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../../fable_modules/Fable.Promise.3.2.0/Promise.fs.js";
import { promise } from "../../fable_modules/Fable.Promise.3.2.0/PromiseImpl.fs.js";
import { browser } from "@wdio/globals";
import { head } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Array.js";
import { parse } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Int32.js";
import { Expect_isTrue } from "../../fable_modules/Fable.Mocha.2.17.0/Mocha.fs.js";
import { int32ToString, structuralHash, assertEqual } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Util.js";
import { ofArray, contains } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/List.js";
import { equals, class_type, decimal_type, string_type, float64_type, bool_type, int32_type } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Reflection.js";
import { printf, toText } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/String.js";

describe("Main -> Renderer unmount behavior", () => {
    it("Switch window if required", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-decrement").then((_arg_2) => {
        const dec_1 = _arg_2;
        return dec_1.error ? (browser.getWindowHandles().then((_arg_3) => (browser.getWindowHandle().then((_arg_4) => {
            let arg;
            return ((arg = head(_arg_3.filter((y) => (_arg_4 !== y))), browser.switchToWindow(arg))).then(() => (browser.$("#counter-button-decrement").then((_arg_6) => (expect(_arg_6).toBeExisting().then(() => (Promise.resolve(undefined)))))));
        })))) : (expect(dec_1).toBeExisting().then(() => (Promise.resolve(undefined))));
    })))));
    it("Unmount probe controls exist", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#main-signal-status").then((_arg_10) => (expect(_arg_10).toBeExisting().then(() => (browser.$("#main-signal-count").then((_arg_12) => (expect(_arg_12).toBeExisting().then(() => (browser.$("#main-signal-last").then((_arg_14) => (expect(_arg_14).toBeExisting().then(() => (browser.$("#main-signal-unmount").then((_arg_16) => (expect(_arg_16).toBeExisting().then(() => (Promise.resolve(undefined)))))))))))))))))))));
    it("Stops updates after unmount and allows double unmount", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#main-signal-status").then((_arg_19) => ((browser.pause(1000)).then(() => {
        let ele;
        return ((ele = browser.$("#main-signal-count"), PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (ele.then((_arg_21) => (_arg_21.getText().then((_arg_1_1) => (Promise.resolve(parse(_arg_1_1, 511, false, 32))))))))))).then((_arg_22) => {
            const countBeforeUnmount = _arg_22 | 0;
            Expect_isTrue(countBeforeUnmount > 0)("Main signal should update before unmount");
            return browser.$("#main-signal-unmount").then((_arg_23) => {
                const unmountButton_1 = _arg_23;
                return unmountButton_1.click().then(() => (unmountButton_1.click().then(() => (_arg_19.getText().then((_arg_26) => {
                    Expect_isTrue(_arg_26.indexOf("unmounted") >= 0)("Status should switch to unmounted");
                    return (browser.pause(900)).then(() => {
                        let ele_2;
                        return ((ele_2 = browser.$("#main-signal-count"), PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (ele_2.then((_arg_28) => (_arg_28.getText().then((_arg_1_2) => (Promise.resolve(parse(_arg_1_2, 511, false, 32))))))))))).then((_arg_29) => {
                            let copyOfStruct, arg_1, arg_1_1;
                            const actual = _arg_29 | 0;
                            const expected = countBeforeUnmount | 0;
                            if ((actual === expected) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                                assertEqual(actual, expected, "Update count should stay unchanged after unmount");
                            }
                            else {
                                throw new Error(contains((copyOfStruct = actual, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                                    Equals: equals,
                                    GetHashCode: (x) => (structuralHash(x) | 0),
                                }) ? ((arg_1 = int32ToString(expected), (arg_1_1 = int32ToString(actual), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_1)(arg_1_1)("Update count should stay unchanged after unmount")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected)(actual)("Update count should stay unchanged after unmount"));
                            }
                            return Promise.resolve();
                        });
                    });
                })))));
            });
        });
    })))))));
});

