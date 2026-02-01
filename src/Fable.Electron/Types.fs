namespace Fable.Electron
// A good sign for any project is starting a file with directives: /s
#nowarn 1182

open System.ComponentModel
open System.Runtime.CompilerServices
open Fable.Core
open Browser.Types

// Code for accelerators taken from previous work by F# Fable Community members on Fable.Electron;
// specifically Christer van der Meeren @cmeeren
[<StringEnum; RequireQualifiedAccess>]
type Modifier =
    /// macOS only. Use CmdOrCtrl instead.
    | [<CompiledName("Command")>] Command
    /// Alias for Command
    | [<CompiledName("Cmd")>] Cmd
    | [<CompiledName("Control")>] Control
    /// Alias for Control
    | [<CompiledName("Ctrl")>] Ctrl
    | [<CompiledName("CommandOrControl")>] CommandOrControl
    /// Alias for CommandOrControl
    | [<CompiledName("CmdOrCtrl")>] CmdOrCtrl
    | [<CompiledName("Alt")>] Alt
    /// macOS only. Use Alt instead.
    | [<CompiledName("Option")>] Option
    | [<CompiledName("AltGr")>] AltGr
    | [<CompiledName("Shift")>] Shift
    /// Mapped to the Windows key on Windows and Linux and the Cmd key on macOS.
    | [<CompiledName("Super")>] Super

/// <exclude/>
/// [omit]
[<StringEnum; RequireQualifiedAccess>]
type Key =
    /// The number 0 (not NumPad).
    | [<CompiledName("0")>] N0
    /// The number 1.
    | [<CompiledName("1")>] N1
    /// The number 2.
    | [<CompiledName("2")>] N2
    /// The number 3.
    | [<CompiledName("3")>] N3
    /// The number 4.
    | [<CompiledName("4")>] N4
    /// The number 5.
    | [<CompiledName("5")>] N5
    /// The number 6.
    | [<CompiledName("6")>] N6
    /// The number 7.
    | [<CompiledName("7")>] N7
    /// The number 8.
    | [<CompiledName("8")>] N8
    /// The number 9.
    | [<CompiledName("9")>] N9
    /// The letter A.
    | [<CompiledName("A")>] A
    /// The letter B.
    | [<CompiledName("B")>] B
    /// The letter C.
    | [<CompiledName("C")>] C
    /// The letter D.
    | [<CompiledName("D")>] D
    /// The letter E.
    | [<CompiledName("E")>] E
    /// The letter F.
    | [<CompiledName("F")>] F
    /// The letter G.
    | [<CompiledName("G")>] G
    /// The letter H.
    | [<CompiledName("H")>] H
    /// The letter I.
    | [<CompiledName("I")>] I
    /// The letter J.
    | [<CompiledName("J")>] J
    /// The letter K.
    | [<CompiledName("K")>] K
    /// The letter L.
    | [<CompiledName("L")>] L
    /// The letter M.
    | [<CompiledName("M")>] M
    /// The letter N.
    | [<CompiledName("N")>] N
    /// The letter O.
    | [<CompiledName("O")>] O
    /// The letter P.
    | [<CompiledName("P")>] P
    /// The letter Q.
    | [<CompiledName("Q")>] Q
    /// The letter R.
    | [<CompiledName("R")>] R
    /// The letter S.
    | [<CompiledName("S")>] S
    /// The letter T.
    | [<CompiledName("T")>] T
    /// The letter U.
    | [<CompiledName("U")>] U
    /// The letter V.
    | [<CompiledName("V")>] V
    /// The letter W.
    | [<CompiledName("W")>] W
    /// The letter X.
    | [<CompiledName("X")>] X
    /// The letter Y.
    | [<CompiledName("Y")>] Y
    /// The letter Z.
    | [<CompiledName("Z")>] Z
    /// Function key 1.
    | [<CompiledName("F1")>] F1
    /// Function key 2.
    | [<CompiledName("F2")>] F2
    /// Function key 3.
    | [<CompiledName("F3")>] F3
    /// Function key 4.
    | [<CompiledName("F4")>] F4
    /// Function key 5.
    | [<CompiledName("F5")>] F5
    /// Function key 6.
    | [<CompiledName("F6")>] F6
    /// Function key 7.
    | [<CompiledName("F7")>] F7
    /// Function key 8.
    | [<CompiledName("F8")>] F8
    /// Function key 9.
    | [<CompiledName("F9")>] F9
    /// Function key 10.
    | [<CompiledName("F10")>] F10
    /// Function key 11.
    | [<CompiledName("F11")>] F11
    /// Function key 12.
    | [<CompiledName("F12")>] F12
    /// Function key 13.
    | [<CompiledName("F13")>] F13
    /// Function key 14.
    | [<CompiledName("F14")>] F14
    /// Function key 15.
    | [<CompiledName("F15")>] F15
    /// Function key 16.
    | [<CompiledName("F16")>] F16
    /// Function key 17.
    | [<CompiledName("F17")>] F17
    /// Function key 18.
    | [<CompiledName("F18")>] F18
    /// Function key 19.
    | [<CompiledName("F19")>] F19
    /// Function key 20.
    | [<CompiledName("F20")>] F20
    /// Function key 21.
    | [<CompiledName("F21")>] F21
    /// Function key 22.
    | [<CompiledName("F22")>] F22
    /// Function key 23.
    | [<CompiledName("F23")>] F23
    /// Function key 24.
    | [<CompiledName("F24")>] F24
    /// )
    | [<CompiledName(")")>] RParen
    /// (
    | [<CompiledName("(")>] LParen
    /// !
    | [<CompiledName("!")>] Exclamation
    /// ?
    | [<CompiledName("?")>] Question
    /// @
    | [<CompiledName("@")>] At
    /// #
    | [<CompiledName("#")>] Hash
    /// $
    | [<CompiledName("$")>] Dollar
    /// %
    | [<CompiledName("%")>] Percent
    /// ^
    | [<CompiledName("^")>] Caret
    /// &
    | [<CompiledName("&")>] Ampersand
    /// *
    | [<CompiledName("*")>] Asterisk
    /// :
    | [<CompiledName(":")>] Colon
    /// ;
    | [<CompiledName(";")>] Semicolon
    /// =
    | [<CompiledName("=")>] Equals
    | [<CompiledName("<")>] LessThan
    | [<CompiledName(">")>] GreaterThan
    /// ,
    | [<CompiledName(",")>] Comma
    /// _
    | [<CompiledName("_")>] Underscore
    /// -
    | [<CompiledName("-")>] Dash
    /// Alias for Dash
    | [<CompiledName("-")>] Hyphen
    /// .
    | [<CompiledName(".")>] Dot
    /// /
    | [<CompiledName(".")>] ForwardSlash
    /// \
    | [<CompiledName("\\")>] Backslash
    /// ~
    | [<CompiledName("~")>] Tilde
    /// `
    | [<CompiledName("`")>] Backtick
    /// {
    | [<CompiledName("{")>] LBrace
    /// }
    | [<CompiledName("}")>] RBrace
    /// [
    | [<CompiledName("[")>] LBracket
    /// ]
    | [<CompiledName("]")>] RBracket
    /// |
    | [<CompiledName("|")>] Pipe
    /// '
    | [<CompiledName("'")>] SingleQuote
    /// Alias for SingleQuote
    | [<CompiledName("'")>] Apostrophe
    /// "
    | [<CompiledName("\"")>] DoubleQuote
    | [<CompiledName("Plus")>] Plus
    | [<CompiledName("Space")>] Space
    | [<CompiledName("Tab")>] Tab
    | [<CompiledName("Capslock")>] Capslock
    | [<CompiledName("Numlock")>] Numlock
    | [<CompiledName("Scrolllock")>] ScrollLock
    | [<CompiledName("Backspace")>] Backspace
    | [<CompiledName("Delete")>] Delete
    | [<CompiledName("Insert")>] Insert
    | [<CompiledName("Return")>] Return
    /// Alias for Return
    | [<CompiledName("Enter")>] Enter
    | [<CompiledName("Up")>] Up
    | [<CompiledName("Down")>] Down
    | [<CompiledName("Left")>] Left
    | [<CompiledName("Right")>] Right
    | [<CompiledName("Home")>] Home
    | [<CompiledName("End")>] End
    | [<CompiledName("PageUp")>] PageUp
    | [<CompiledName("PageDown")>] PageDown
    | [<CompiledName("Escape")>] Escape
    /// Alias for Escape
    | [<CompiledName("Esc")>] Esc
    | [<CompiledName("VolumeUp")>] VolumeUp
    | [<CompiledName("VolumeDown")>] VolumeDown
    | [<CompiledName("VolumeMute")>] VolumeMute
    | [<CompiledName("MediaNextTrack")>] MediaNextTrack
    | [<CompiledName("MediaPreviousTrack")>] MediaPreviousTrack
    | [<CompiledName("MediaStop")>] MediaStop
    | [<CompiledName("MediaPlayPause")>] MediaPlayPause
    | [<CompiledName("PrintScreen")>] PrintScreen
    /// Numpad 0
    | [<CompiledName("num0")>] Num0
    /// Numpad 1
    | [<CompiledName("num1")>] Num1
    /// Numpad 2
    | [<CompiledName("num2")>] Num2
    /// Numpad 3
    | [<CompiledName("num3")>] Num3
    /// Numpad 4
    | [<CompiledName("num4")>] Num4
    /// Numpad 5
    | [<CompiledName("num5")>] Num5
    /// Numpad 6
    | [<CompiledName("num6")>] Num6
    /// Numpad 7
    | [<CompiledName("num7")>] Num7
    /// Numpad 8
    | [<CompiledName("num8")>] Num8
    /// Numpad 9
    | [<CompiledName("num9")>] Num9
    /// Numpad decimal
    | [<CompiledName("numdec")>] NumDec
    /// Numpad +
    | [<CompiledName("numadd")>] NumAdd
    /// Numpad -
    | [<CompiledName("numsub")>] NumSub
    /// Numpad *
    | [<CompiledName("nummult")>] NumMult
    /// Numpad /
    | [<CompiledName("numdiv")>] NumDiv

[<Erase>]
type Accelerator =
    private
    | Accelerator of string

    [<Erase; Emit("$0")>]
    static member Create(acceleratorString: string) = Accelerator acceleratorString

    [<Erase>]
    static member inline Create(modifiers: Modifier list, key: Key) =
        (modifiers, unbox key ||> List.foldBack (fun (m: Modifier) acc -> unbox m + "+" + acc))
        |> unbox<Accelerator>

[<Erase>]
module Accelerator =
    open Fable.Core.JsInterop

    /// Returns an accelerator string that can be used to register shortcuts.
    let create (modifiers: Modifier list) (key: Key) =
        modifiers, !!key ||> List.foldBack (fun (m: Modifier) acc -> !!m + "+" + acc)

    let inline fromString acceleratorString = Accelerator.Create acceleratorString

[<StringEnum; RequireQualifiedAccess>]
type UserType =
    | String
    | Boolean
    | Integer
    | Float
    | Double
    | Url
    | Array
    | Dictionary

type Record<'Key, 'Value> =
    [<EmitIndexer>]
    abstract member Item: 'Key -> 'Value with get, set

type EventEmitter = interface end

// ========== Electron 40.0.0 + ==============
// Uses the relatively new API from 2024 for video frames.
// This is not bound by Fable.Browser
// We provide a naive implementation here.
module VideoFrameImageOptions =
    [<StringEnum; RequireQualifiedAccess>]
    type Alpha =
        | Keep
        | Discard

[<JS.Pojo>]
type VideoFrameImageOptions(
    ?duration: float,
    ?timestamp: float,
    ?alpha: VideoFrameImageOptions.Alpha,
    ?visibleRect: {| x: float; y: float; width: float; height: float |},
    ?displayWidth: float,
    ?displayHeight: float,
    ?flip: bool,
    ?rotation: float
    ) =
    [<Erase>] member val duration = duration with get, set
    [<Erase>] member val timestamp = timestamp with get, set
    [<Erase>] member val alpha = alpha with get, set
    [<Erase>] member val visibleRect = visibleRect with get, set
    [<Erase>] member val displayWidth = displayWidth with get, set
    [<Erase>] member val displayHeight = displayHeight with get, set
    [<Erase>] member val flip = flip with get, set
    [<Erase>] member val rotation = rotation with get, set
[<Erase>]
module VideoFrameDataOptions =
    [<StringEnum(CaseRules.None); RequireQualifiedAccess>]
    type Format =
        | I420
        | I420A
        | I422
        | I444
        | NV12
        | RGBA
        | RGBX
        | BGRA
        | BGRX
[<JS.Pojo>]
type VideoFrameDataOptions(
    format: VideoFrameDataOptions.Format,
    codedWidth: float,
    codedHeight: float,
    timestamp: int,
    colorSpace: {| primaries: string; transfer: string; matrix: string; fullRange: bool |},
    transfer: obj[],
    ?duration: int,
    ?layout: {| offset: int; stride: int |},
    ?visibleRect: {| x: float; y: float; width: float; height: float |},
    ?displayWidth: float,
    ?displayHeight: float,
    ?flip: bool,
    ?rotation: float
    ) =
    [<Erase>] member val format = format with get, set
    [<Erase>] member val codedWidth = codedWidth with get, set
    [<Erase>] member val codedHeight = codedHeight with get, set
    [<Erase>] member val timestamp = timestamp with get, set
    [<Erase>] member val colorSpace = colorSpace with get, set
    [<Erase>] member val transfer = transfer with get, set
    [<Erase>] member val duration = duration with get, set
    [<Erase>] member val layout = layout with get, set
    [<Erase>] member val visibleRect = visibleRect with get, set
    [<Erase>] member val displayWidth = displayWidth with get, set
    [<Erase>] member val displayHeight = displayHeight with get, set
    [<Erase>] member val flip = flip with get, set
    [<Erase>] member val rotation = rotation with get, set
[<Erase>]
module VideoFrame =
    [<RequireQualifiedAccess; StringEnum(CaseRules.None)>]
    type Format =
        | RGBA
        | RGBX
        | BGRA
        | BGRX
    [<RequireQualifiedAccess; StringEnum(CaseRules.KebabCase)>]
    type ColorSpace =
        | Srgb
        | DisplayP3

/// <summary>
/// Naive binding for VideoFrame to allow Electron 40.1.0 +
/// </summary>
[<Global>]
[<AllowNullLiteral>]
type VideoFrame private () =
    class end
    new(image: U5<SVGImageElement, HTMLVideoElement, HTMLCanvasElement, VideoFrame, obj>, options: VideoFrameImageOptions) = VideoFrame()
    [<ParamObject(1)>]
    new(
        image: U5<SVGImageElement, HTMLVideoElement, HTMLCanvasElement, VideoFrame, obj>,
        ?duration: float,
        ?timestamp: float,
        ?alpha: VideoFrameImageOptions.Alpha,
        ?visibleRect: {| x: float; y: float; width: float; height: float |},
        ?displayWidth: float,
        ?displayHeight: float,
        ?flip: bool,
        ?rotation: float
    ) = VideoFrame()
    new(data: obj[], options: VideoFrameDataOptions) = VideoFrame()
    [<ParamObject(1)>]
    new(
        data: obj[],
        format: VideoFrameDataOptions.Format,
        codedWidth: float,
        codedHeight: float,
        timestamp: int,
        colorSpace: {| primaries: string; transfer: string; matrix: string; fullRange: bool |},
        transfer: obj[],
        ?duration: int,
        ?layout: {| offset: int; stride: int |},
        ?visibleRect: {| x: float; y: float; width: float; height: float |},
        ?displayWidth: float,
        ?displayHeight: float,
        ?flip: bool,
        ?rotation: float
    ) = VideoFrame()
    member _.format: string = jsNative
    member _.codedWidth: float = jsNative
    member _.codedHeight: float = jsNative
    member _.codedRect: obj = jsNative
    member _.visibleRect: obj = jsNative
    member _.displayWidth: float = jsNative
    member _.displayHeight: float = jsNative
    member _.timestamp: int = jsNative
    member _.duration: float = jsNative
    member _.colorSpace: {| primaries: string; transfer: string; matrix: string; fullRange: bool |} = jsNative
    member _.flip: bool = jsNative
    member _.rotation: float = jsNative
    [<ParamObject(0)>]
    member _.allocationSize(
        ?rect: {| x: float; y: float; width: float; height: float |},
        ?format: VideoFrame.Format,
        ?colorSpace: VideoFrame.ColorSpace
        ): int = jsNative
    member _.clone(): VideoFrame = jsNative
    member _.close(): unit = jsNative

[<AutoOpen; EditorBrowsable(EditorBrowsableState.Never)>]
module AutoOpenExtensions =
    type EventEmitter with
        [<Emit("$0.eventNames()")>]
        member this.eventNames() : string[] = Unchecked.defaultof<_>

        [<Emit("$0.getMaxListeners()")>]
        member this.getMaxListeners() : int = Unchecked.defaultof<_>

        [<Emit("$0.listenerCount($1{{, $2}})")>]
        member inline this.listenerCount(eventName: string, ?listener: FSharpFunc<_, _>) : int = Unchecked.defaultof<_>

        [<Emit("$0.listeners($1)")>]
        member inline this.listeners(eventName: string) : obj[] = Unchecked.defaultof<_>

        [<Emit("$0.rawListeners($1)")>]
        member inline this.rawListeners(eventName: string) : obj[] = Unchecked.defaultof<_>

    [<Erase; AutoOpen; EditorBrowsable(EditorBrowsableState.Never)>]
    type Extensions =
        [<Extension; Emit("$0.setMaxListeners($1)")>]
        static member inline setMaxListeners<'EventEmitter when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, n: int)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.emit($1, ...$2)")>]
        static member inline emit<'EventEmitter when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<System.ParamArray>] args: obj[])
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.on($1, $2)")>]
        static member inline on<'EventEmitter, 'Arg when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.on($1, $2)")>]
        static member inline on<'EventEmitter, 'Arg1, 'Arg2 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.on($1, $2)")>]
        static member inline on<'EventEmitter, 'Arg1, 'Arg2, 'Arg3 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.on($1, $2)")>]
        static member inline on<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.on($1, $2)")>]
        static member inline on<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5 when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.on($1, $2)")>]
        static member inline on<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.on($1, $2)")>]
        static member inline on<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6, 'Arg7
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> 'Arg7 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.off($1, $2)")>]
        static member inline off<'EventEmitter, 'Arg when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.off($1, $2)")>]
        static member inline off<'EventEmitter, 'Arg1, 'Arg2 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.off($1, $2)")>]
        static member inline off<'EventEmitter, 'Arg1, 'Arg2, 'Arg3 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.off($1, $2)")>]
        static member inline off<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.off($1, $2)")>]
        static member inline off<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5 when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.off($1, $2)")>]
        static member inline off<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.off($1, $2)")>]
        static member inline off<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6, 'Arg7
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> 'Arg7 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.once($1, $2)")>]
        static member inline once<'EventEmitter, 'Arg when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.once($1, $2)")>]
        static member inline once<'EventEmitter, 'Arg1, 'Arg2 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.once($1, $2)")>]
        static member inline once<'EventEmitter, 'Arg1, 'Arg2, 'Arg3 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.once($1, $2)")>]
        static member inline once<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.once($1, $2)")>]
        static member inline once<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5 when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.once($1, $2)")>]
        static member inline once<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.once($1, $2)")>]
        static member inline once<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6, 'Arg7
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> 'Arg7 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependListener($1, $2)")>]
        static member inline prependListener<'EventEmitter, 'Arg when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependListener($1, $2)")>]
        static member inline prependListener<'EventEmitter, 'Arg1, 'Arg2 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependListener($1, $2)")>]
        static member inline prependListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependListener($1, $2)")>]
        static member inline prependListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4
            when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependListener($1, $2)")>]
        static member inline prependListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependListener($1, $2)")>]
        static member inline prependListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependListener($1, $2)")>]
        static member inline prependListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6, 'Arg7
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> 'Arg7 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependOnceListener($1, $2)")>]
        static member inline prependOnceListener<'EventEmitter, 'Arg when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependOnceListener($1, $2)")>]
        static member inline prependOnceListener<'EventEmitter, 'Arg1, 'Arg2 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependOnceListener($1, $2)")>]
        static member inline prependOnceListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependOnceListener($1, $2)")>]
        static member inline prependOnceListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4
            when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependOnceListener($1, $2)")>]
        static member inline prependOnceListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependOnceListener($1, $2)")>]
        static member inline prependOnceListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.prependOnceListener($1, $2)")>]
        static member inline prependOnceListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6, 'Arg7
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> 'Arg7 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.removeListener($1, $2)")>]
        static member inline removeListener<'EventEmitter, 'Arg when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.removeListener($1, $2)")>]
        static member inline removeListener<'EventEmitter, 'Arg1, 'Arg2 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.removeListener($1, $2)")>]
        static member inline removeListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.removeListener($1, $2)")>]
        static member inline removeListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4 when 'EventEmitter :> EventEmitter>
            (this: 'EventEmitter, event: string, [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> unit)
            : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.removeListener($1, $2)")>]
        static member inline removeListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.removeListener($1, $2)")>]
        static member inline removeListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>

        [<Extension; Emit("$0.removeListener($1, $2)")>]
        static member inline removeListener<'EventEmitter, 'Arg1, 'Arg2, 'Arg3, 'Arg4, 'Arg5, 'Arg6, 'Arg7
            when 'EventEmitter :> EventEmitter>
            (
                this: 'EventEmitter,
                event: string,
                [<InlineIfLambda>] handler: 'Arg1 -> 'Arg2 -> 'Arg3 -> 'Arg4 -> 'Arg5 -> 'Arg6 -> 'Arg7 -> unit
            ) : 'EventEmitter =
            Unchecked.defaultof<_>