module Fable.Electron.Remoting.Tests.test.specs.test2_e2e_js

open Fable.Core
open Fable.Core.JsInterop
open Fable.Mocha
open WDIO
open Tests.Common.Mocha

let inline getIncButton () =
    browser.``$`` "#counter-button-increment"

let inline getDecButton () =
    browser.``$`` "#counter-button-decrement"

let inline getSet42Button () = browser.``$`` "#counter-set-42"

let inline getDisableButton () = browser.``$`` "#counter-button-disable"
let inline getEnableButton () = browser.``$`` "#counter-button-enable"
let inline getLabel () = browser.``$`` "#counter-text"

let inline getWindowLoggerButton () = browser.``$`` "#window-logger-button"

let inline getWindowLoggerOutput () = browser.``$`` "#window-logger-output"

let inline getInt (ele: JS.Promise<IWdioElement>) = promise {
    let! value = ele
    let! text = value.getText ()
    return int text
}

describe "App loads correctly"
<| fun _ ->
    it "Switch window if required"
    <| fun _ -> promise {
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

    it "Buttons and label exist"
    <| fun _ -> promise {
        let! dec = getDecButton ()
        do! expect(dec).toBeExisting ()
        let! inc = getIncButton ()
        do! expect(inc).toBeExisting ()
        let! dis = getDisableButton ()
        do! expect(dis).toBeExisting ()
        let! set42 = getSet42Button ()
        do! expect(set42).toBeExisting ()
        let! en = getEnableButton ()
        do! expect(en).toBeExisting ()
        let! t = getLabel ()
        do! expect(t).toBeExisting ()
        let! winLogBtn = getWindowLoggerButton ()
        do! expect(winLogBtn).toBeExisting ()
        let! winLogOut = getWindowLoggerOutput ()
        do! expect(winLogOut).toBeExisting ()
    }

    describe "Initial state is correct"
    <| fun _ ->
        it "Label is zero"
        <| fun _ -> promise {
            let! t = getLabel () |> getInt
            Expect.equal t 0 "Label should be 0"
        }

        it "Enable is disabled"
        <| fun _ -> promise {
            let! en = getEnableButton ()
            let! actual = en.isEnabled ()
            Expect.isFalse actual "Enable should be disabled"
        }

        it "Disable is enabled"
        <| fun _ -> promise {
            let! dis = getDisableButton ()
            let! actual = dis.isEnabled ()
            Expect.isTrue actual "Disable should be enabled"
        }

        it "Window Logger output is empty"
        <| fun _ -> promise {
            let! out = getWindowLoggerOutput ()
            let! text = out.getText ()
            Expect.equal text "Placeholder" "Window Logger output should be Placeholder"
        }

describe "Combination TWO Way and ONE Way IPC works"
<| fun _ ->
    it "Value responds to inc/dec clicks"
    <| fun _ -> promise {
        let! label = getLabel ()
        let! initialValue = label.getText().``then`` int
        let! incButton = getIncButton ()
        let! decButton = getDecButton ()
        do! incButton.click ()
        let! newValue = label.getText().``then`` int
        Expect.notEqual newValue initialValue "Value should change"
        Expect.equal newValue (initialValue + 1) "Value has increased by one"
        do! decButton.click ()
        let! newValue2 = label.getText().``then`` int
        Expect.equal newValue2 initialValue "Value has decreased back to initial"
    }

    it "Disable and Enable round trip correctly"
    <| fun _ -> promise {
        let! dis = getDisableButton ()
        let! en = getEnableButton ()
        do! expect(dis).toBeEnabled ()
        do! expect(en).toBeDisabled ()
        do! dis.click ()
        do! expect(dis).toBeDisabled ()
        do! expect(en).toBeEnabled ()
        do! en.click ()
    }

    it "Disable prevents inc/dec"
    <| fun _ -> promise {
        let! dis = getDisableButton ()
        let! en = getEnableButton ()
        let! label = getLabel ()
        do! expect(dis).toBeEnabled ()
        do! expect(en).toBeDisabled ()
        let! initialValue = label.getText().``then`` int
        do! dis.click ()

        let! inc = getIncButton ()
        do! inc.click ()
        do! inc.click ()
        let! actual = label.getText().``then`` int
        Expect.equal actual initialValue "Value should not have changed while disabled"

        do! en.click ()
        do! inc.click ()
        do! inc.click ()
        let! newActual = label.getText().``then`` int
        Expect.notEqual newActual initialValue "Value should have changed while enabled"
        Expect.equal newActual (initialValue + 2) "Value should have increased by two"
    }

    it "Set to 42 works (this tests passing values over two-way IPC)"
    <| fun _ -> promise {
        let! label = getLabel ()
        let! set42 = getSet42Button ()
        do! set42.click ()
        let! actual = label.getText().``then`` int
        Expect.equal actual 42 "Value should be set to 42"
    }

    it "Window Logger logs actions correctly (this tests two-way with IpcMainEvent)"
    <| fun _ -> promise {
        let! out = getWindowLoggerOutput ()
        let! winLogBtn = getWindowLoggerButton ()
        do! winLogBtn.click ()
        let! text = out.getText ()

        /// Should match something like: "[Window 1-639017411811680000]:Hello from Renderer!"
        /// numbers will vary.
        let regex =
            System.Text.RegularExpressions.Regex(@"^\[Window \d+-\d+\]:Hello from Renderer!$")

        let rm = regex.IsMatch(text)
        Expect.isTrue rm "Window Logger should log correct message"
    }
