
import { Record } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Types.js";
import { makeRecord, fullName, name, getFunctionElements, isFunction, record_type, array_type, class_type, lambda_type, string_type } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Reflection.js";
import { ipcMain, BrowserWindow } from "electron";
import { concat, toFail, toText } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/String.js";
import { item, insertAt } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Array.js";
import { createTypeInfo } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/Fable.SimpleJson.3.24.0/TypeInfo.Converter.fs.js";
import { singleton, empty, append, collect, delay, toArray } from "../../tests/Fable.Electron.Remoting.Tests/fable_modules/fable-library-js.5.0.0-alpha.14/Seq.js";

export class RemotingConfig extends Record {
    constructor(ApiNameBase, ApiNameMap, ChannelNameMap, Windows) {
        super();
        this.ApiNameBase = ApiNameBase;
        this.ApiNameMap = ApiNameMap;
        this.ChannelNameMap = ChannelNameMap;
        this.Windows = Windows;
    }
}

export function RemotingConfig_$reflection() {
    return record_type("Fable.Electron.Remoting.Main.RemotingConfig", [], RemotingConfig, () => [["ApiNameBase", string_type], ["ApiNameMap", lambda_type(string_type, lambda_type(string_type, string_type))], ["ChannelNameMap", lambda_type(string_type, lambda_type(string_type, string_type))], ["Windows", array_type(class_type("Fable.Electron.Main.BrowserWindow", undefined, BrowserWindow))]]);
}

export const RemotingModule_init = new RemotingConfig("FABLE_REMOTING", (baseName, typeName) => toText(`${baseName}_${typeName}`), (typeName_1, fieldName) => toText(`${typeName_1}:${fieldName}`), []);

export function RemotingModule_withApiNameBase(apiName, config) {
    return new RemotingConfig(apiName, config.ApiNameMap, config.ChannelNameMap, config.Windows);
}

export function RemotingModule_withApiNameMap(func, config) {
    return new RemotingConfig(config.ApiNameBase, func, config.ChannelNameMap, config.Windows);
}

export function RemotingModule_withChannelNameMap(func, config) {
    return new RemotingConfig(config.ApiNameBase, config.ApiNameMap, func, config.Windows);
}

/**
 * Adds a window to the array of windows for a config.
 */
export function RemotingModule_withWindow(window$, config) {
    return new RemotingConfig(config.ApiNameBase, config.ApiNameMap, config.ChannelNameMap, insertAt(0, window$, config.Windows));
}

export function RemotingModule_setWindows(windows, config) {
    return new RemotingConfig(config.ApiNameBase, config.ApiNameMap, config.ChannelNameMap, windows);
}

export function Proxy_getReturnType(typ_mut) {
    Proxy_getReturnType:
    while (true) {
        const typ = typ_mut;
        if (isFunction(typ)) {
            const res = getFunctionElements(typ)[1];
            typ_mut = res;
            continue Proxy_getReturnType;
        }
        else {
            return typ;
        }
        break;
    }
}

export function Remoting_buildReceiverProxy_Z5BEA2CED(config, impl, resolvedType) {
    const schemaType = createTypeInfo(resolvedType);
    if (schemaType.tag === 39) {
        const getFields = schemaType.fields[0];
        const patternInput = getFields();
        const recordType = patternInput[1];
        const fields = patternInput[0];
        const makeChannelName = config.ChannelNameMap;
        for (let idx = 0; idx <= (fields.length - 1); idx++) {
            const field = item(idx, fields);
            const returnType = createTypeInfo(Proxy_getReturnType(field.PropertyInfo[1]));
            const isPromiseOrAsyncReturn = (returnType.tag === 25) ? true : (returnType.tag === 26);
            const handlesIpcMainEvent = (field.FieldType.tag === 37) && ("IpcMainEvent" === name(getFunctionElements(field.PropertyInfo[1])[0]));
            const channelName = makeChannelName(name(recordType), field.FieldName);
            if (isPromiseOrAsyncReturn) {
                if (handlesIpcMainEvent) {
                    ipcMain.handle(channelName, async (...args) => { return await (impl[field.FieldName])(args[0], ...(args[1])) });
                }
                else {
                    ipcMain.handle(channelName, async (...args) => { return await (impl[field.FieldName])(...(args[1])) });
                }
            }
            else if (handlesIpcMainEvent) {
                ipcMain.handle(channelName, async (...args) => { return (impl[field.FieldName])(args[0], ...(args[1])) });
            }
            else {
                ipcMain.handle(channelName, async (...args) => { return (impl[field.FieldName])(...(args[1])) });
            }
        }
    }
    else {
        toFail(`Cannot build proxy. Expected type ${fullName(resolvedType)} to be a valid protocol definition which is a record of functions`);
    }
}

export function Remoting_buildSenderProxy_65A0F576(config, resolvedType) {
    const schemaType = createTypeInfo(resolvedType);
    if (schemaType.tag === 39) {
        const getFields = schemaType.fields[0];
        const patternInput = getFields();
        const recordType = patternInput[1];
        const fields = patternInput[0];
        const makeChannelName = config.ChannelNameMap;
        const windows = config.Windows;
        const recordFields = toArray(delay(() => collect((field) => {
            const returnType = Proxy_getReturnType(field.PropertyInfo[1]);
            return append((createTypeInfo(returnType).tag === 0) ? (empty()) : (((() => {
                throw new Error(concat("Cannot build proxy. Expected type ", fullName(resolvedType), ..." to be a valid protocol definition which is a record of callback-functions."));
            })(), empty())), delay(() => append((field.FieldType.tag === 37) ? (empty()) : (((() => {
                throw new Error(concat("Cannot build proxy. Expected type ", fullName(resolvedType), ..." to be a valid protocol definition which is a record of functions."));
            })(), empty())), delay(() => {
                const channelName = makeChannelName(name(recordType), field.FieldName);
                const func = (...args) => { return windows.forEach((window) => window.webContents.send(channelName, ...args)) };
                return singleton(func);
            }))));
        }, fields)));
        const proxy = makeRecord(recordType, recordFields);
        return proxy;
    }
    else {
        return toFail(`Cannot build proxy. Expected type ${fullName(resolvedType)} to be a valid protocol definition which is a record of functions`);
    }
}

