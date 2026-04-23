module Fable.Electron.Remoting.Tests.test.specs.unmount_e2e_js

open Fable.Core
open Fable.Core.JsInterop
open Fable.Mocha
open WDIO
open Tests.Common.Mocha

let inline getDecButton () =
    browser.``$`` "#counter-button-decrement"

let inline getMainSignalStatus () =
    browser.``$`` "#main-signal-status"

let inline getMainSignalCount () =
    browser.``$`` "#main-signal-count"

let inline getMainSignalLast () =
    browser.``$`` "#main-signal-last"

let inline getMainSignalUnmountButton () =
    browser.``$`` "#main-signal-unmount"

let inline pause (milliseconds: int) : JS.Promise<unit> =
    browser?pause (milliseconds)

let inline getInt (ele: JS.Promise<IWdioElement>) =
    promise {
        let! value = ele
        let! text = value.getText ()
        return int text
    }

describe "Main -> Renderer unmount behavior"
<| fun _ ->
    it "Switch window if required"
    <| fun _ ->
        promise {
            let! dec = getDecButton ()
            let dec = dec

            if dec?error then
                let! handles = browser.getWindowHandles ()
                let! currentHandle = browser.getWindowHandle ()

                do!
                    handles
                    |> Array.filter ((<>) currentHandle)
                    |> Array.head
                    |> browser.switchToWindow

                let! dec = getDecButton ()
                do! expect(dec).toBeExisting ()
            else
                do! expect(dec).toBeExisting ()
        }

    it "Unmount probe controls exist"
    <| fun _ ->
        promise {
            let! status = getMainSignalStatus ()
            do! expect(status).toBeExisting ()
            let! count = getMainSignalCount ()
            do! expect(count).toBeExisting ()
            let! last = getMainSignalLast ()
            do! expect(last).toBeExisting ()
            let! unmountButton = getMainSignalUnmountButton ()
            do! expect(unmountButton).toBeExisting ()
        }

    it "Stops updates after unmount and allows double unmount"
    <| fun _ ->
        promise {
            let! status = getMainSignalStatus ()
            do! pause 1000

            let! countBeforeUnmount = getMainSignalCount () |> getInt
            Expect.isTrue (countBeforeUnmount > 0) "Main signal should update before unmount"

            let! unmountButton = getMainSignalUnmountButton ()
            do! unmountButton.click ()
            do! unmountButton.click ()

            let! statusAfterUnmount = status.getText ()
            Expect.isTrue (statusAfterUnmount.Contains "unmounted") "Status should switch to unmounted"

            do! pause 900

            let! countAfterUnmount = getMainSignalCount () |> getInt
            Expect.equal countAfterUnmount countBeforeUnmount "Update count should stay unchanged after unmount"
        }
