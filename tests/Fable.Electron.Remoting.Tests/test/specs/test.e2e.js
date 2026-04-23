
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../../fable_modules/Fable.Promise.3.2.0/Promise.fs.js";
import { promise } from "../../fable_modules/Fable.Promise.3.2.0/PromiseImpl.fs.js";
import { browser } from "@wdio/globals";
import { head } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Array.js";
import { parse } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Int32.js";
import { int32ToString, structuralHash, assertEqual } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Util.js";
import { ofArray, contains } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/List.js";
import { equals, class_type, decimal_type, string_type, float64_type, bool_type, int32_type } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Reflection.js";
import { concat, printf, toText } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/String.js";
import { Expect_notEqual, Expect_isTrue, Expect_isFalse } from "../../fable_modules/Fable.Mocha.2.17.0/Mocha.fs.js";
import { isMatch } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/RegExp.js";
import { some } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Option.js";

describe("App loads correctly", () => {
    it("Switch window if required", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-decrement").then((_arg_2) => {
        const dec_1 = _arg_2;
        return dec_1.error ? (browser.getWindowHandles().then((_arg_3) => (browser.getWindowHandle().then((_arg_4) => {
            let arg;
            return ((arg = head(_arg_3.filter((y) => (_arg_4 !== y))), browser.switchToWindow(arg))).then(() => (browser.$("#counter-button-decrement").then((_arg_6) => (expect(_arg_6).toBeExisting().then(() => (Promise.resolve(undefined)))))));
        })))) : (expect(dec_1).toBeExisting().then(() => (Promise.resolve(undefined))));
    })))));
    it("Buttons and label exist", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-decrement").then((_arg_10) => (expect(_arg_10).toBeExisting().then(() => (browser.$("#counter-button-increment").then((_arg_12) => (expect(_arg_12).toBeExisting().then(() => (browser.$("#counter-button-disable").then((_arg_14) => (expect(_arg_14).toBeExisting().then(() => (browser.$("#counter-set-42").then((_arg_16) => (expect(_arg_16).toBeExisting().then(() => (browser.$("#counter-button-enable").then((_arg_18) => (expect(_arg_18).toBeExisting().then(() => (browser.$("#counter-text").then((_arg_20) => (expect(_arg_20).toBeExisting().then(() => (browser.$("#window-logger-button").then((_arg_22) => (expect(_arg_22).toBeExisting().then(() => (browser.$("#window-logger-output").then((_arg_24) => (expect(_arg_24).toBeExisting().then(() => (Promise.resolve(undefined)))))))))))))))))))))))))))))))))))));
    describe("Initial state is correct", () => {
        it("Label is zero", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
            let ele;
            return ((ele = browser.$("#counter-text"), PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (ele.then((_arg_28) => (_arg_28.getText().then((_arg_1_1) => (Promise.resolve(parse(_arg_1_1, 511, false, 32))))))))))).then((_arg_29) => {
                let copyOfStruct, arg_1, arg_1_1;
                const actual = _arg_29 | 0;
                if ((actual === 0) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                    assertEqual(actual, 0, "Label should be 0");
                }
                else {
                    throw new Error(contains((copyOfStruct = actual, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                        Equals: equals,
                        GetHashCode: (x) => (structuralHash(x) | 0),
                    }) ? ((arg_1 = int32ToString(0), (arg_1_1 = int32ToString(actual), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_1)(arg_1_1)("Label should be 0")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(0)(actual)("Label should be 0"));
                }
                return Promise.resolve();
            });
        })));
        it("Enable is disabled", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-enable").then((_arg_31) => (_arg_31.isEnabled().then((_arg_32) => {
            Expect_isFalse(_arg_32)("Enable should be disabled");
            return Promise.resolve();
        })))))));
        it("Disable is enabled", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-disable").then((_arg_34) => (_arg_34.isEnabled().then((_arg_35) => {
            Expect_isTrue(_arg_35)("Disable should be enabled");
            return Promise.resolve();
        })))))));
        it("Window Logger output is empty", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#window-logger-output").then((_arg_37) => (_arg_37.getText().then((_arg_38) => {
            let copyOfStruct_1;
            const actual_3 = _arg_38;
            if ((actual_3 === "Placeholder") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                assertEqual(actual_3, "Placeholder", "Window Logger output should be Placeholder");
            }
            else {
                throw new Error(contains((copyOfStruct_1 = actual_3, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                    Equals: equals,
                    GetHashCode: (x_1) => (structuralHash(x_1) | 0),
                }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Placeholder")(actual_3)("Window Logger output should be Placeholder") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Placeholder")(actual_3)("Window Logger output should be Placeholder"));
            }
            return Promise.resolve();
        })))))));
    });
});

describe("Combination TWO Way and ONE Way IPC works", () => {
    it("Value responds to inc/dec clicks", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-text").then((_arg_2) => {
        const label = _arg_2;
        return label.getText().then((value) => (parse(value, 511, false, 32) | 0)).then((_arg_3) => {
            const initialValue = _arg_3 | 0;
            return browser.$("#counter-button-increment").then((_arg_4) => (browser.$("#counter-button-decrement").then((_arg_5) => (_arg_4.click().then(() => (label.getText().then((value_1) => (parse(value_1, 511, false, 32) | 0)).then((_arg_7) => {
                let copyOfStruct, arg, arg_1;
                const newValue = _arg_7 | 0;
                Expect_notEqual(newValue, initialValue, "Value should change");
                const actual = newValue | 0;
                const expected = (initialValue + 1) | 0;
                if ((actual === expected) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                    assertEqual(actual, expected, "Value has increased by one");
                }
                else {
                    throw new Error(contains((copyOfStruct = actual, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                        Equals: equals,
                        GetHashCode: (x) => (structuralHash(x) | 0),
                    }) ? ((arg = int32ToString(expected), (arg_1 = int32ToString(actual), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg)(arg_1)("Value has increased by one")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected)(actual)("Value has increased by one"));
                }
                return _arg_5.click().then(() => (label.getText().then((value_2) => (parse(value_2, 511, false, 32) | 0)).then((_arg_9) => {
                    let copyOfStruct_1, arg_6, arg_1_1;
                    const actual_1 = _arg_9 | 0;
                    const expected_1 = initialValue | 0;
                    if ((actual_1 === expected_1) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                        assertEqual(actual_1, expected_1, "Value has decreased back to initial");
                    }
                    else {
                        throw new Error(contains((copyOfStruct_1 = actual_1, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                            Equals: equals,
                            GetHashCode: (x_1) => (structuralHash(x_1) | 0),
                        }) ? ((arg_6 = int32ToString(expected_1), (arg_1_1 = int32ToString(actual_1), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_6)(arg_1_1)("Value has decreased back to initial")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_1)(actual_1)("Value has decreased back to initial"));
                    }
                    return Promise.resolve();
                })));
            })))))));
        });
    })))));
    it("Disable and Enable round trip correctly", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-disable").then((_arg_11) => {
        const dis = _arg_11;
        return browser.$("#counter-button-enable").then((_arg_12) => {
            const en = _arg_12;
            return expect(dis).toBeEnabled().then(() => (expect(en).toBeDisabled().then(() => (dis.click().then(() => (expect(dis).toBeDisabled().then(() => (expect(en).toBeEnabled().then(() => (en.click().then(() => (Promise.resolve(undefined)))))))))))));
        });
    })))));
    it("Disable prevents inc/dec", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-disable").then((_arg_20) => {
        const dis_1 = _arg_20;
        return browser.$("#counter-button-enable").then((_arg_21) => {
            const en_1 = _arg_21;
            return browser.$("#counter-text").then((_arg_22) => {
                const label_1 = _arg_22;
                return expect(dis_1).toBeEnabled().then(() => (expect(en_1).toBeDisabled().then(() => (label_1.getText().then((value_3) => (parse(value_3, 511, false, 32) | 0)).then((_arg_25) => {
                    const initialValue_1 = _arg_25 | 0;
                    return dis_1.click().then(() => (browser.$("#counter-button-increment").then((_arg_27) => {
                        const inc = _arg_27;
                        return inc.click().then(() => (inc.click().then(() => (label_1.getText().then((value_4) => (parse(value_4, 511, false, 32) | 0)).then((_arg_30) => {
                            let copyOfStruct_2, arg_7, arg_1_2;
                            const actual_3 = _arg_30 | 0;
                            const expected_2 = initialValue_1 | 0;
                            if ((actual_3 === expected_2) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                                assertEqual(actual_3, expected_2, "Value should not have changed while disabled");
                            }
                            else {
                                throw new Error(contains((copyOfStruct_2 = actual_3, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                                    Equals: equals,
                                    GetHashCode: (x_2) => (structuralHash(x_2) | 0),
                                }) ? ((arg_7 = int32ToString(expected_2), (arg_1_2 = int32ToString(actual_3), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_7)(arg_1_2)("Value should not have changed while disabled")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_2)(actual_3)("Value should not have changed while disabled"));
                            }
                            return en_1.click().then(() => (inc.click().then(() => (inc.click().then(() => (label_1.getText().then((value_5) => (parse(value_5, 511, false, 32) | 0)).then((_arg_34) => {
                                let copyOfStruct_3, arg_8, arg_1_3;
                                const newActual = _arg_34 | 0;
                                Expect_notEqual(newActual, initialValue_1, "Value should have changed while enabled");
                                const actual_4 = newActual | 0;
                                const expected_3 = (initialValue_1 + 2) | 0;
                                if ((actual_4 === expected_3) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                                    assertEqual(actual_4, expected_3, "Value should have increased by two");
                                }
                                else {
                                    throw new Error(contains((copyOfStruct_3 = actual_4, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                                        Equals: equals,
                                        GetHashCode: (x_3) => (structuralHash(x_3) | 0),
                                    }) ? ((arg_8 = int32ToString(expected_3), (arg_1_3 = int32ToString(actual_4), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_8)(arg_1_3)("Value should have increased by two")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_3)(actual_4)("Value should have increased by two"));
                                }
                                return Promise.resolve();
                            })))))));
                        })))));
                    })));
                })))));
            });
        });
    })))));
    it("Set to 42 works (this tests passing values over two-way IPC)", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-text").then((_arg_36) => (browser.$("#counter-set-42").then((_arg_37) => (_arg_37.click().then(() => (_arg_36.getText().then((value_6) => (parse(value_6, 511, false, 32) | 0)).then((_arg_39) => {
        let copyOfStruct_4, arg_9, arg_1_4;
        const actual_6 = _arg_39 | 0;
        if ((actual_6 === 42) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
            assertEqual(actual_6, 42, "Value should be set to 42");
        }
        else {
            throw new Error(contains((copyOfStruct_4 = actual_6, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                Equals: equals,
                GetHashCode: (x_4) => (structuralHash(x_4) | 0),
            }) ? ((arg_9 = int32ToString(42), (arg_1_4 = int32ToString(actual_6), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_9)(arg_1_4)("Value should be set to 42")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(42)(actual_6)("Value should be set to 42"));
        }
        return Promise.resolve();
    })))))))))));
    it("Window Logger logs actions correctly (this tests two-way with IpcMainEvent)", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#window-logger-output").then((_arg_41) => (browser.$("#window-logger-button").then((_arg_42) => (_arg_42.click().then(() => (_arg_41.getText().then((_arg_44) => {
        Expect_isTrue(isMatch(/^\[Window \d+-\d+\]:Hello from Renderer!$/gu, _arg_44))("Window Logger should log correct message");
        return Promise.resolve();
    })))))))))));
    it("Window Logger logs multiple args correctly", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#window-logger-output-multiple-args").then((_arg_46) => (browser.$("#window-logger-button-multiple-args").then((_arg_47) => (_arg_47.click().then(() => (_arg_46.getText().then((_arg_49) => {
        const text_1 = _arg_49;
        console.log(some(concat("Received text from Window Logger Multiple Args Output: ", ...text_1)));
        Expect_isTrue(isMatch(/^\[Window \d+-\d+\]:Hello from Renderer!, 42, true$/gu, text_1))("Window Logger should log correct message with multiple args");
        return Promise.resolve();
    })))))))))));
});

