module Fable.Electron.Remoting.Main

open System
open System.ComponentModel
open Browser
open FSharp.Core
open Fable.Core
open Fable.Core.DynamicExtensions
open Fable.Core.JsInterop
open FSharp.Reflection
open Fable.Electron
open Fable.Electron.Main
open Fable.SimpleJson
//%REMOTING_TYPE%START%
/// <summary>
/// Configuration for a Remoting proxy.
/// </summary>
type RemotingConfig =
    {
        /// <summary>
        /// No effect for Main Process. Kept for uniformity.
        /// </summary>
        ApiNameBase: string
        /// <summary>
        /// No effect for Main Process. Kept for uniformity.
        /// </summary>
        ApiNameMap: string -> string -> string
        /// <summary>
        /// A function that creates the name of the channel that messages are sent over/received from.
        /// The first parameter is the name of the type, while the second is the name of the field.
        /// </summary>
        /// <remarks>Defaults to <code>fun typeName fieldName -> sprintf "%s{typeName}:%s{fieldName}</code></remarks>
        ChannelNameMap: string -> string -> string
        /// <summary>
        /// Required when building a <c>Main -> Renderer</c> Proxy router. The array of windows that the
        /// messages are sent to.
        /// </summary>
        Windows: BrowserWindow array
    }
//%REMOTING_TYPE%END%
//%REMOTING_MODULE%START%
[<Erase>]
module Remoting =
    let createHandler () =
        { ApiNameBase = "FABLE_REMOTING"
          ApiNameMap = fun baseName typeName -> sprintf $"%s{baseName}_{typeName}"
          ChannelNameMap = fun typeName fieldName -> sprintf $"%s{typeName}:%s{fieldName}"
          Windows = [||] }

    let withApiNameBase apiName config = { config with ApiNameBase = apiName }
    let withApiNameMap func config = { config with ApiNameMap = func }
    let withChannelNameMap func config = { config with ChannelNameMap = func }

    /// <summary>
    /// Adds a window to the array of windows for a config.
    /// </summary>
    /// <param name="window"><c>BrowserWindow</c></param>
    /// <param name="config"></param>
    let withWindow window config =
        { config with
            Windows = config.Windows |> Array.insertAt 0 window }

    let setWindows windows config = { config with Windows = windows }
//%REMOTING_MODULE%END%

[<EditorBrowsable(EditorBrowsableState.Never)>]
module internal Proxy =
    let rec getReturnType typ =
        if Reflection.FSharpType.IsFunction typ then
            let _, res = Reflection.FSharpType.GetFunctionElements typ
            getReturnType res
        else
            typ

[<Erase>]
type Remoting =
    //%TWO_WAY%START%
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    static member buildReceiverProxyFromValue(config: RemotingConfig, impl, resolvedType: Type) =
        let schemaType = createTypeInfo resolvedType

        match schemaType with
        | TypeInfo.Record getFields ->
            let fields, recordType = getFields ()
            let makeChannelName = config.ChannelNameMap

            for field in fields do
                let returnType =
                    Proxy.getReturnType field.PropertyInfo.PropertyType |> createTypeInfo

                match field.FieldType with
                | TypeInfo.Func _ -> ()
                | _ ->
                    failwith
                        $"Cannot build proxy. Expected type %s{resolvedType.FullName} to be \
                        a valid protocol definition which is a record of functions"

                // Check if we need to await the implementation call
                let isPromiseOrAsyncReturn =
                    match returnType with
                    | TypeInfo.Async _ -> true
                    | TypeInfo.Promise _ -> true
                    | _ -> false

                let channelName = makeChannelName recordType.Name field.FieldName

                match isPromiseOrAsyncReturn with
                | true ->
                    ipcMain.handle (
                        channelName,
                        fun (_: IpcMainInvokeEvent) (args) ->
                            emitJsExpr (impl.Item(field.FieldName), args) "(async (...args) => { return await $0(...args) })($1)"
                            |> U2.Case1
                    )
                | false ->
                    ipcMain.handle (
                        channelName,
                        fun (_: IpcMainInvokeEvent) (args) ->
                            emitJsExpr (impl.Item(field.FieldName), args) "(async (...args) => { return $0(...args) })($1)"
                            |> U2.Case1
                    )
        | _ ->
            failwithf
                $"Cannot build proxy. Expected type %s{resolvedType.FullName} to be \
                a valid protocol definition which is a record of functions"

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    static member buildReceiverProxyFromIpcMainEvent(config: RemotingConfig, createImpl, resolvedType: Type) =

        let schemaType = createTypeInfo resolvedType

        match schemaType with
        | TypeInfo.Record getFields ->
            let fields, recordType = getFields ()
            let makeChannelName = config.ChannelNameMap
            console.log(fields)
            for field in fields do
                let returnType =
                    Proxy.getReturnType field.PropertyInfo.PropertyType |> createTypeInfo

                match field.FieldType with
                | TypeInfo.Func _ -> ()
                | _ ->
                    failwith
                        $"Cannot build proxy. Expected type %s{resolvedType.FullName} to be \
                        a valid protocol definition which is a record of functions"

                let isPromiseOrAsyncReturn =
                    match returnType with
                    | TypeInfo.Async _ -> true
                    | TypeInfo.Promise _ -> true
                    | _ -> false

                let channelName = makeChannelName recordType.Name field.FieldName

                match isPromiseOrAsyncReturn with
                | true ->
                    ipcMain.handle (
                        channelName,
                        fun (e: IpcMainInvokeEvent) (args) ->
                            // init record type with event
                            let impl = createImpl e |> box
                            // get the function to call from the record
                            let fn = impl.Item(field.FieldName)
                            // use emitJsExpr to wire args as spread argument into the function call, and await the result if it's a promise/async
                            emitJsExpr
                                (fn, args)
                                "(async (args) => {
                                    return await $0(...args)
                                })($1)"
                            |> U2.Case1
                    )
                | false ->
                    ipcMain.handle (
                        channelName,
                        fun (e: IpcMainInvokeEvent) (args) ->
                            // init record type with event
                            let impl = createImpl e |> box
                            // get the function to call from the record
                            let fn = impl.Item(field.FieldName)
                            // use emitJsExpr to wire args as spread argument into the function call, and await the result if it's a promise/async
                            emitJsExpr
                                (fn, args)
                                "(async (args) => {
                                    return $0(...args)
                                })($1)"
                            |> U2.Case1
                    )
        | _ ->
            failwithf
                $"Cannot build proxy. Expected type %s{resolvedType.FullName} to be \
                a valid protocol definition which is a record of functions"
    //%TWO_WAY%END%
    //%IMPL%START%
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    //%CLIENT_START%START%
    static member buildSenderProxy(config: RemotingConfig, resolvedType: Type) =
        let schemaType = createTypeInfo resolvedType
        //%CLIENT_START%END%
        //%CLIENT_TWO%START%
        match schemaType with
        | TypeInfo.Record getFields ->
            let fields, recordType = getFields ()
            let makeChannelName = config.ChannelNameMap
            let windows = config.Windows
            //%CLIENT_TWO%END%
            //%CLIENT_THREE%START%
            let recordFields =
                [| for field in fields do
                       let returnType = Proxy.getReturnType field.PropertyInfo.PropertyType

                       match createTypeInfo returnType with
                       | TypeInfo.Unit -> ()
                       | _ ->
                           failwith
                               $"Cannot build proxy. Expected type %s{resolvedType.FullName} to \
                                    be a valid protocol definition which is a record of callback-functions."

                       match field.FieldType with
                       | TypeInfo.Func _ -> ()
                       | _ ->
                           failwith
                               $"Cannot build proxy. Expected type %s{resolvedType.FullName} to \
                                    be a valid protocol definition which is a record of functions."

                       let channelName = makeChannelName recordType.Name field.FieldName

                       let func =
                           emitJsExpr
                               (windows, channelName)
                               "(...args) => { return $0.forEach((window) => window.webContents.send($1, ...args)) }"
                           |> box

                       func |]
            //%CLIENT_THREE%END%

            let proxy = FSharpValue.MakeRecord(recordType, recordFields)
            unbox proxy
        | _ ->
            failwithf
                $"Cannot build proxy. Expected type %s{resolvedType.FullName} to be \
                a valid protocol definition which is a record of functions"
    //%IMPL%END%
    //%INLINE_ENTRY%START%
    /// <summary>
    /// Builds the receiver for the two way <c>Main &lt;-> Renderer</c> IPC proxy router
    /// from a direct implementation value.
    /// </summary>
    /// <param name="implementation">The record of functions which respond to received messages.</param>
    /// <param name="config"></param>
    static member inline fromValue<'t> (implementation: 't) (config: RemotingConfig) : unit =
        Remoting.buildReceiverProxyFromValue (config, implementation, typeof<'t>)

    /// <summary>
    /// Builds the receiver for the two way <c>Main &lt;-> Renderer</c> IPC proxy router
    /// from an <c>IpcMainEvent</c> factory.
    /// </summary>
    /// <param name="createImplementation">A function that receives <c>IpcMainInvokeEvent</c> and creates the implementation record.</param>
    /// <param name="config"></param>
    static member inline fromIpcMainEvent<'t> (createImplementation: IpcMainInvokeEvent -> 't) (config: RemotingConfig) : unit =
        Remoting.buildReceiverProxyFromIpcMainEvent (config, createImplementation, typeof<'t>)

    /// <summary>
    /// Builds a client for <c>Main -> Renderer</c> IPC proxy router.
    /// </summary>
    /// <param name="config"></param>
    static member inline buildClient<'T>(config: RemotingConfig) : 'T =
        if config.Windows.Length = 0 then
            console.error
                "Building a Main -> Renderer remoting client \
                        with no browser windows will do nothing or cause errors. \
                        Please add windows to the config before building the proxy."

        Remoting.buildSenderProxy (config, typeof<'T>)
//%INLINE_ENTRY%END%