
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../../fable_modules/Fable.Promise.3.2.0/Promise.fs.js";
import { promise } from "../../fable_modules/Fable.Promise.3.2.0/PromiseImpl.fs.js";
import { browser } from "@wdio/globals";
import { head } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Array.js";
import { parse } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Int32.js";
import { int32ToString, structuralHash, assertEqual } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Util.js";
import { equals, class_type, decimal_type, string_type, float64_type, bool_type, int32_type } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/Reflection.js";
import { contains, ofArray } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/List.js";
import { printf, toText } from "../../fable_modules/fable-library-js.5.0.0-alpha.14/String.js";
import { Expect_notEqual, Expect_isTrue, Expect_isFalse } from "../../fable_modules/Fable.Mocha.2.17.0/Mocha.fs.js";

describe("App loads correctly", () => {
    it("Switch window if required", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-decrement").then((_arg_2) => {
        const dec = _arg_2;
        const dec_1 = dec;
        return dec_1.error ? (browser.getWindowHandles().then((_arg_3) => {
            const handles = _arg_3;
            return browser.getWindowHandle().then((_arg_4) => {
                let arg;
                const currentHandle = _arg_4;
                return ((arg = head(handles.filter((y) => (currentHandle !== y))), browser.switchToWindow(arg))).then(() => (browser.$("#counter-button-decrement").then((_arg_6) => {
                    const dec_2 = _arg_6;
                    return expect(dec_2).toBeExisting().then(() => (Promise.resolve(undefined)));
                })));
            });
        })) : (expect(dec_1).toBeExisting().then(() => (Promise.resolve(undefined))));
    })))));
    it("Buttons and label exist", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-decrement").then((_arg_10) => {
        const dec_3 = _arg_10;
        return expect(dec_3).toBeExisting().then(() => (browser.$("#counter-button-increment").then((_arg_12) => {
            const inc = _arg_12;
            return expect(inc).toBeExisting().then(() => (browser.$("#counter-button-disable").then((_arg_14) => {
                const dis = _arg_14;
                return expect(dis).toBeExisting().then(() => (browser.$("#counter-button-enable").then((_arg_16) => {
                    const en = _arg_16;
                    return expect(en).toBeExisting().then(() => (browser.$("#counter-text").then((_arg_18) => {
                        const t = _arg_18;
                        return expect(t).toBeExisting().then(() => (Promise.resolve(undefined)));
                    })));
                })));
            })));
        })));
    })))));
    describe("Initial state is correct", () => {
        it("Label is zero", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
            let ele;
            return ((ele = browser.$("#counter-text"), PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (ele.then((_arg_22) => {
                const value = _arg_22;
                return value.getText().then((_arg_1_1) => {
                    const text = _arg_1_1;
                    return Promise.resolve(parse(text, 511, false, 32));
                });
            })))))).then((_arg_23) => {
                const t_1 = _arg_23 | 0;
                const actual = t_1 | 0;
                if ((actual === 0) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                    assertEqual(actual, 0, "Label should be 0");
                }
                else {
                    let valueType;
                    let copyOfStruct = actual;
                    valueType = int32_type;
                    const primitiveTypes = ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]);
                    let errorMsg;
                    if (contains(valueType, primitiveTypes, {
                        Equals: equals,
                        GetHashCode: (x) => (structuralHash(x) | 0),
                    })) {
                        const arg_1 = int32ToString(0);
                        const arg_1_1 = int32ToString(actual);
                        errorMsg = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_1)(arg_1_1)("Label should be 0");
                    }
                    else {
                        errorMsg = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(0)(actual)("Label should be 0");
                    }
                    throw new Error(errorMsg);
                }
                return Promise.resolve();
            });
        })));
        it("Enable is disabled", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-enable").then((_arg_25) => {
            const en_1 = _arg_25;
            return en_1.isEnabled().then((_arg_26) => {
                const actual_1 = _arg_26;
                Expect_isFalse(actual_1)("Enable should be disabled");
                return Promise.resolve();
            });
        })))));
        it("Disable is enabled", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-button-disable").then((_arg_28) => {
            const dis_1 = _arg_28;
            return dis_1.isEnabled().then((_arg_29) => {
                const actual_2 = _arg_29;
                Expect_isTrue(actual_2)("Disable should be enabled");
                return Promise.resolve();
            });
        })))));
    });
});

describe("Combination TWO Way and ONE Way IPC works", () => {
    it("Value responds to inc/dec clicks", () => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (browser.$("#counter-text").then((_arg_2) => {
        const label = _arg_2;
        return label.getText().then((value) => (parse(value, 511, false, 32) | 0)).then((_arg_3) => {
            const initialValue = _arg_3 | 0;
            return browser.$("#counter-button-increment").then((_arg_4) => {
                const incButton = _arg_4;
                return browser.$("#counter-button-decrement").then((_arg_5) => {
                    const decButton = _arg_5;
                    return incButton.click().then(() => (label.getText().then((value_1) => (parse(value_1, 511, false, 32) | 0)).then((_arg_7) => {
                        const newValue = _arg_7 | 0;
                        Expect_notEqual(newValue, initialValue, "Value should change");
                        const actual = newValue | 0;
                        const expected = (initialValue + 1) | 0;
                        if ((actual === expected) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                            assertEqual(actual, expected, "Value has increased by one");
                        }
                        else {
                            let valueType;
                            let copyOfStruct = actual;
                            valueType = int32_type;
                            const primitiveTypes = ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]);
                            let errorMsg;
                            if (contains(valueType, primitiveTypes, {
                                Equals: equals,
                                GetHashCode: (x) => (structuralHash(x) | 0),
                            })) {
                                const arg = int32ToString(expected);
                                const arg_1 = int32ToString(actual);
                                errorMsg = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg)(arg_1)("Value has increased by one");
                            }
                            else {
                                errorMsg = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected)(actual)("Value has increased by one");
                            }
                            throw new Error(errorMsg);
                        }
                        return decButton.click().then(() => (label.getText().then((value_2) => (parse(value_2, 511, false, 32) | 0)).then((_arg_9) => {
                            const newValue2 = _arg_9 | 0;
                            const actual_1 = newValue2 | 0;
                            const expected_1 = initialValue | 0;
                            if ((actual_1 === expected_1) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                                assertEqual(actual_1, expected_1, "Value has decreased back to initial");
                            }
                            else {
                                let valueType_1;
                                let copyOfStruct_1 = actual_1;
                                valueType_1 = int32_type;
                                const primitiveTypes_1 = ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]);
                                let errorMsg_1;
                                if (contains(valueType_1, primitiveTypes_1, {
                                    Equals: equals,
                                    GetHashCode: (x_1) => (structuralHash(x_1) | 0),
                                })) {
                                    const arg_6 = int32ToString(expected_1);
                                    const arg_1_1 = int32ToString(actual_1);
                                    errorMsg_1 = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_6)(arg_1_1)("Value has decreased back to initial");
                                }
                                else {
                                    errorMsg_1 = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_1)(actual_1)("Value has decreased back to initial");
                                }
                                throw new Error(errorMsg_1);
                            }
                            return Promise.resolve();
                        })));
                    })));
                });
            });
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
                            const actual_2 = _arg_30 | 0;
                            const actual_3 = actual_2 | 0;
                            const expected_2 = initialValue_1 | 0;
                            if ((actual_3 === expected_2) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                                assertEqual(actual_3, expected_2, "Value should not have changed while disabled");
                            }
                            else {
                                let valueType_2;
                                let copyOfStruct_2 = actual_3;
                                valueType_2 = int32_type;
                                const primitiveTypes_2 = ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]);
                                let errorMsg_2;
                                if (contains(valueType_2, primitiveTypes_2, {
                                    Equals: equals,
                                    GetHashCode: (x_2) => (structuralHash(x_2) | 0),
                                })) {
                                    const arg_7 = int32ToString(expected_2);
                                    const arg_1_2 = int32ToString(actual_3);
                                    errorMsg_2 = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_7)(arg_1_2)("Value should not have changed while disabled");
                                }
                                else {
                                    errorMsg_2 = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_2)(actual_3)("Value should not have changed while disabled");
                                }
                                throw new Error(errorMsg_2);
                            }
                            return en_1.click().then(() => (inc.click().then(() => (inc.click().then(() => (label_1.getText().then((value_5) => (parse(value_5, 511, false, 32) | 0)).then((_arg_34) => {
                                const newActual = _arg_34 | 0;
                                Expect_notEqual(newActual, initialValue_1, "Value should have changed while enabled");
                                const actual_4 = newActual | 0;
                                const expected_3 = (initialValue_1 + 2) | 0;
                                if ((actual_4 === expected_3) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
                                    assertEqual(actual_4, expected_3, "Value should have increased by two");
                                }
                                else {
                                    let valueType_3;
                                    let copyOfStruct_3 = actual_4;
                                    valueType_3 = int32_type;
                                    const primitiveTypes_3 = ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]);
                                    let errorMsg_3;
                                    if (contains(valueType_3, primitiveTypes_3, {
                                        Equals: equals,
                                        GetHashCode: (x_3) => (structuralHash(x_3) | 0),
                                    })) {
                                        const arg_8 = int32ToString(expected_3);
                                        const arg_1_3 = int32ToString(actual_4);
                                        errorMsg_3 = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_8)(arg_1_3)("Value should have increased by two");
                                    }
                                    else {
                                        errorMsg_3 = toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_3)(actual_4)("Value should have increased by two");
                                    }
                                    throw new Error(errorMsg_3);
                                }
                                return Promise.resolve();
                            })))))));
                        })))));
                    })));
                })))));
            });
        });
    })))));
});

