
import { Record } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Types.js";
import { fullName, makeRecord, name, record_type, lambda_type, string_type } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Reflection.js";
import { toFail, toText } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/String.js";
import { createTypeInfo } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/Fable.SimpleJson.3.24.0/TypeInfo.Converter.fs.js";
import { map, delay, toArray } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Seq.js";
import { item } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Array.js";

export class RemotingConfig extends Record {
    constructor(ApiNameBase, ApiNameMap, ChannelNameMap) {
        super();
        this.ApiNameBase = ApiNameBase;
        this.ApiNameMap = ApiNameMap;
        this.ChannelNameMap = ChannelNameMap;
    }
}

export function RemotingConfig_$reflection() {
    return record_type("Fable.Electron.Remoting.Renderer.RemotingConfig", [], RemotingConfig, () => [["ApiNameBase", string_type], ["ApiNameMap", lambda_type(string_type, lambda_type(string_type, string_type))], ["ChannelNameMap", lambda_type(string_type, lambda_type(string_type, string_type))]]);
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

export function Remoting_buildProxySender_69894920(config, resolvedType) {
    const schemaType = createTypeInfo(resolvedType);
    if (schemaType.tag === 39) {
        const getFields = schemaType.fields[0];
        const patternInput = getFields();
        const recordType = patternInput[1];
        const fields = patternInput[0];
        const bridgeName = config.ApiNameMap(config.ApiNameBase, name(resolvedType));
        const recordFields = toArray(delay(() => map((field) => ((window[bridgeName])[field.FieldName]), fields)));
        const proxy = makeRecord(recordType, recordFields);
        return proxy;
    }
    else {
        return toFail(`Cannot build proxy. Expected type ${fullName(resolvedType)} to be a valid protocol definition which is a record of functions`);
    }
}

export function Remoting_buildProxyReceiver_69F94225(impl, config, resolvedType) {
    const schemaType = createTypeInfo(resolvedType);
    const bridgeName = config.ApiNameMap(config.ApiNameBase, name(resolvedType));
    if (schemaType.tag === 39) {
        const getFields = schemaType.fields[0];
        const patternInput = getFields();
        const recordType = patternInput[1];
        const fields = patternInput[0];
        for (let idx = 0; idx <= (fields.length - 1); idx++) {
            const field = item(idx, fields);
            const callSite = (window[bridgeName])[field.FieldName];
            const fieldTarget = impl[field.FieldName];
            const func = callSite((...args) => { return fieldTarget(...args) });
        }
    }
    else {
        toFail(`Cannot build proxy. Expected type ${fullName(resolvedType)} to be a valid protocol definition which is a record of functions`);
    }
}

