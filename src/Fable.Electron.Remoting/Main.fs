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
    let init =
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
    static member buildReceiverProxy(config: RemotingConfig, impl, resolvedType: Type) =
        let schemaType = createTypeInfo resolvedType

        match schemaType with
        | TypeInfo.Record getFields ->
            let fields, recordType = getFields ()
            let makeChannelName = config.ChannelNameMap

            for field in fields do
                let returnType =
                    Proxy.getReturnType field.PropertyInfo.PropertyType |> createTypeInfo
                // Check if we need to await the implementation call
                let isPromiseOrAsyncReturn =
                    match returnType with
                    | TypeInfo.Async _ -> true
                    | TypeInfo.Promise _ -> true
                    | _ -> false
                // Check if we pass the first natural argument of the ipc arguments (the IpcMainEvent)
                let handlesIpcMainEvent =
                    field.FieldType
                    |> function
                        | TypeInfo.Func _ ->
                            field.PropertyInfo.PropertyType
                            |> FSharpType.GetFunctionElements
                            |> fst
                            |> _.Name
                            |> (=) typeof<IpcMainEvent>.Name
                        | _ -> false

                let channelName = makeChannelName recordType.Name field.FieldName

                match isPromiseOrAsyncReturn, handlesIpcMainEvent with
                | true, true ->
                    // If async and handles the IpcEvent, then we pass the ipc event
                    // (index 0) and then spread the args and await the promise
                    ipcMain.handle (
                        channelName,
                        emitJsExpr
                            (impl.Item(field.FieldName))
                            // The first arg passed by the renderer will be the nonsensical filler value.
                            "async (...args) => { return await $0(args[0], ...(args[1].slice(1))) }"
                    )
                | false, true ->
                    // If not async and handles the IpcEvent, then we pass the ipc event
                    // (index 0) and then spread the args, but there is no need to await the promise
                    ipcMain.handle (
                        channelName,
                        emitJsExpr
                            (impl.Item(field.FieldName))
                            // The first arg passed by the renderer will be the nonsensical filler value.
                            "async (...args) => { return $0(args[0], ...(args[1].slice(1))) }"
                    )
                | true, false ->
                    // If we dont handle the IpcMainEvent, then we do not pass it to the proxy (index 0).
                    ipcMain.handle (
                        channelName,
                        emitJsExpr (impl.Item(field.FieldName)) "async (...args) => { return await $0(...(args[1])) }"
                    )
                | false, false ->
                    ipcMain.handle (
                        channelName,
                        emitJsExpr (impl.Item(field.FieldName)) "async (...args) => { return $0(...(args[1])) }"
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
    /// Builds the receiver for the two way <c>Main &lt;-> Renderer</c> IPC proxy router.
    /// </summary>
    /// <param name="implementation">The record of functions which respond to received messages.</param>
    /// <param name="config"></param>
    static member inline buildHandler<'t> (implementation: 't) (config: RemotingConfig) : unit =
        Remoting.buildReceiverProxy (config, implementation, typeof<'t>)

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
