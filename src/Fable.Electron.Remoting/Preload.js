
import { Record } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Types.js";
import { fullName, makeRecord, name, record_type, lambda_type, string_type } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Reflection.js";
import { toFail, toText } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/String.js";
import { take, item, equalsWith } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Array.js";
import { curry2, defaultOf, equals } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Util.js";
import { contextBridge, ipcRenderer } from "electron";
import { createTypeInfo } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/Fable.SimpleJson.3.24.0/TypeInfo.Converter.fs.js";
import { singleton, collect, delay, toArray } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Seq.js";

export class RemotingConfig extends Record {
    constructor(ApiNameBase, ApiNameMap, ChannelNameMap) {
        super();
        this.ApiNameBase = ApiNameBase;
        this.ApiNameMap = ApiNameMap;
        this.ChannelNameMap = ChannelNameMap;
    }
}

export function RemotingConfig_$reflection() {
    return record_type("Fable.Electron.Remoting.Preload.RemotingConfig", [], RemotingConfig, () => [["ApiNameBase", string_type], ["ApiNameMap", lambda_type(string_type, lambda_type(string_type, string_type))], ["ChannelNameMap", lambda_type(string_type, lambda_type(string_type, string_type))]]);
}

export const RemotingModule_init = new RemotingConfig("FABLE_REMOTING", (baseName, typeName) => toText(`${baseName}_${typeName}`), (typeName_1, fieldName) => toText(`${typeName_1}:${fieldName}`));

export function RemotingModule_withApiNameBase(apiName, config) {
    return new RemotingConfig(apiName, config.ApiNameMap, config.ChannelNameMap);
}

export function RemotingModule_withApiNameMap(func, config) {
    return new RemotingConfig(config.ApiNameBase, func, config.ChannelNameMap);
}

export function RemotingModule_withChannelNameMap(func, config) {
    return new RemotingConfig(config.ApiNameBase, config.ApiNameMap, func);
}

function Proxy_proxyFetch(typeName, func, config) {
    let funcArgs;
    const matchValue = func.FieldType;
    switch (matchValue.tag) {
        case 25: {
            funcArgs = [func.FieldType];
            break;
        }
        case 26: {
            funcArgs = [func.FieldType];
            break;
        }
        case 37: {
            const getArgs = matchValue.fields[0];
            funcArgs = getArgs();
            break;
        }
        default:
            funcArgs = toFail(`Field ${func.FieldName} does not have a valid definition`);
    }
    const makeChannelName = config.ChannelNameMap;
    const argumentCount = (funcArgs.length - 1) | 0;
    const channelName = makeChannelName(typeName, func.FieldName);
    const funcNeedParameters = (!equalsWith(equals, funcArgs, defaultOf()) && (funcArgs.length === 1)) ? ((item(0, funcArgs).tag === 25) ? false : (!(item(0, funcArgs).tag === 26))) : ((!equalsWith(equals, funcArgs, defaultOf()) && (funcArgs.length === 2)) ? ((item(0, funcArgs).tag === 0) ? (!(item(1, funcArgs).tag === 25)) : true) : true);
    return (arg0) => ((arg1) => ((arg2) => ((arg3) => ((arg4) => ((arg5) => ((arg6) => ((arg7) => {
        const inputArguments = funcNeedParameters ? take(argumentCount, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7]) : [];
        const value = ipcRenderer.invoke(channelName, inputArguments);
        return value;
    })))))));
}

export function Remoting_buildRendererToMainProxy_Z1B91F404(config, resolvedType) {
    const schemaType = createTypeInfo(resolvedType);
    if (schemaType.tag === 39) {
        const getFields = schemaType.fields[0];
        const patternInput = getFields();
        const recordType = patternInput[1];
        const fields = patternInput[0];
        const recordFields = toArray(delay(() => collect((field) => {
            const normalize = (n) => {
                const fn = Proxy_proxyFetch(name(recordType), field, config);
                switch (n) {
                    case 0:
                        return fn(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf());
                    case 1:
                        return (a) => fn(a)(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf());
                    case 2: {
                        const proxyF = (a_1) => ((b) => fn(a_1)(b)(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf()));
                        return (delegateArg, delegateArg_1) => proxyF(delegateArg)(delegateArg_1);
                    }
                    case 3: {
                        const proxyF_1 = (a_2) => ((b_1) => ((c) => fn(a_2)(b_1)(c)(defaultOf())(defaultOf())(defaultOf())(defaultOf())(defaultOf())));
                        return (delegateArg_2, delegateArg_3, delegateArg_4) => proxyF_1(delegateArg_2)(delegateArg_3)(delegateArg_4);
                    }
                    case 4: {
                        const proxyF_2 = (a_3) => ((b_2) => ((c_1) => ((d) => fn(a_3)(b_2)(c_1)(d)(defaultOf())(defaultOf())(defaultOf())(defaultOf()))));
                        return (delegateArg_5, delegateArg_6, delegateArg_7, delegateArg_8) => proxyF_2(delegateArg_5)(delegateArg_6)(delegateArg_7)(delegateArg_8);
                    }
                    case 5: {
                        const proxyF_3 = (a_4) => ((b_3) => ((c_2) => ((d_1) => ((e) => fn(a_4)(b_3)(c_2)(d_1)(e)(defaultOf())(defaultOf())(defaultOf())))));
                        return (delegateArg_9, delegateArg_10, delegateArg_11, delegateArg_12, delegateArg_13) => proxyF_3(delegateArg_9)(delegateArg_10)(delegateArg_11)(delegateArg_12)(delegateArg_13);
                    }
                    case 6: {
                        const proxyF_4 = (a_5) => ((b_4) => ((c_3) => ((d_2) => ((e_1) => ((f) => fn(a_5)(b_4)(c_3)(d_2)(e_1)(f)(defaultOf())(defaultOf()))))));
                        return (delegateArg_14, delegateArg_15, delegateArg_16, delegateArg_17, delegateArg_18, delegateArg_19) => proxyF_4(delegateArg_14)(delegateArg_15)(delegateArg_16)(delegateArg_17)(delegateArg_18)(delegateArg_19);
                    }
                    case 7: {
                        const proxyF_5 = (a_6) => ((b_5) => ((c_4) => ((d_3) => ((e_2) => ((f_1) => ((g) => fn(a_6)(b_5)(c_4)(d_3)(e_2)(f_1)(g)(defaultOf())))))));
                        return (delegateArg_20, delegateArg_21, delegateArg_22, delegateArg_23, delegateArg_24, delegateArg_25, delegateArg_26) => proxyF_5(delegateArg_20)(delegateArg_21)(delegateArg_22)(delegateArg_23)(delegateArg_24)(delegateArg_25)(delegateArg_26);
                    }
                    case 8: {
                        const proxyF_6 = (a_7) => ((b_6) => ((c_5) => ((d_4) => ((e_3) => ((f_2) => ((g_1) => ((h) => fn(a_7)(b_6)(c_5)(d_4)(e_3)(f_2)(g_1)(h))))))));
                        return (delegateArg_27, delegateArg_28, delegateArg_29, delegateArg_30, delegateArg_31, delegateArg_32, delegateArg_33, delegateArg_34) => proxyF_6(delegateArg_27)(delegateArg_28)(delegateArg_29)(delegateArg_30)(delegateArg_31)(delegateArg_32)(delegateArg_33)(delegateArg_34);
                    }
                    default:
                        return toFail(`Cannot generate proxy function for ${field.FieldName}. Only up to 8 arguments are supported. Consider using a record type as input`);
                }
            };
            let argumentCount;
            const matchValue = field.FieldType;
            switch (matchValue.tag) {
                case 25: {
                    argumentCount = 0;
                    break;
                }
                case 26: {
                    argumentCount = 0;
                    break;
                }
                case 37: {
                    const getArgs = matchValue.fields[0];
                    argumentCount = (getArgs().length - 1);
                    break;
                }
                default:
                    argumentCount = 0;
            }
            return singleton(normalize(argumentCount));
        }, fields)));
        const proxy = makeRecord(recordType, recordFields);
        contextBridge.exposeInMainWorld(config.ApiNameMap(config.ApiNameBase, name(resolvedType)), proxy);
    }
    else {
        toFail(`Cannot build proxy. Exepected type ${fullName(resolvedType)} to be a valid protocol definition which is a record of functions`);
    }
}

export function Remoting_buildMainToRendererProxy_Z1B91F404(config, resolvedType) {
    const schemaType = createTypeInfo(resolvedType);
    const bridgeName = config.ApiNameMap(config.ApiNameBase, name(resolvedType));
    const makeChannelName = curry2(config.ChannelNameMap)(name(resolvedType));
    if (schemaType.tag === 39) {
        const getFields = schemaType.fields[0];
        const patternInput = getFields();
        const recordType = patternInput[1];
        const fields = patternInput[0];
        const recordFields = toArray(delay(() => collect((field) => {
            const fieldName = field.FieldName;
            const channelName = makeChannelName(fieldName);
            const func = (callback) => ipcRenderer.on(channelName, (_event, ...args) => callback(...args));
            return singleton(func);
        }, fields)));
        const proxy = makeRecord(recordType, recordFields);
        contextBridge.exposeInMainWorld(bridgeName, proxy);
    }
    else {
        toFail(`Cannot build proxy. Exepected type ${fullName(resolvedType)} to be a valid protocol definition which is a record of functions`);
    }
}

