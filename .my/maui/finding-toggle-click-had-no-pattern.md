# Finding: Click on a toggle had no pattern to use

## Symptom

Seven Toggle tests failed — five `Switch`, two `CheckBox` — all of them the ones that *change*
state, while every read-only test passed. Each failed on the app's status label, not on the
click:

```
AssertionException : Expected TextContains to be 'on'. Locator: AutomationId:SwitchStatusLabel
```

So the click reported success and the control never flipped.

## Cause

The tests call `Click()`, which walks `ClickableControlBase`'s activation ladder —
`SelectionItem`, then `Invoke`. Probing what the controls actually expose:

```
[CLK] AutomationId:TestSwitch   sel=False inv=False tog=True
[CLK] AutomationId:TestCheckBox sel=False inv=False tog=True
[CLK] AutomationId:Open_Toggle  sel=False inv=True  tog=False
```

MAUI's `Switch` and `CheckBox` expose **only** `TogglePattern`. Neither rung matched, so every
click fell through to `element.Click()` — FlaUI's physical mouse click, which needs the window in
front and does not reliably reach a XAML toggle. Nothing threw, so the failure surfaced later and
somewhere else, as an unchanged status label.

Note this was invisible from the toggle side: `ToggleCore` has its own ladder that tries
`TogglePattern` first, and it works. Only `Click()` was affected, and `Click()` is what a test
naturally writes for a switch.

## Fix

`ToggleControlBase` now overrides `TryActivateByPattern` to add the toggle command to the
inherited ladder. Toggle knowledge lives in the base class for toggles — not in an element, which
must not know what a MAUI view means, and not in a shared click helper that would decide for
every control.

The rung is deliberately **last**. `RadioButton` shares this base and activates through
`SelectionItem`, which carries the "one of a group" meaning a bare toggle does not; letting toggle
win first would flip a radio as though it were independent. Ordering it after `base` leaves every
RadioButton test on exactly the path it already passed on.

## Result

Toggle: **16 / 16**, up from 9. Buttons + Text + Display: **53 / 54**, the one failure being
`ProgressBar_Reset_ReturnsToInitialState`, which also failed at baseline and is unrelated.

The suite also got faster — Toggle went from 43s to 8s, because the failing path was waiting on
pointer clicks that were never going to work.

## Worth noting

This is the third defect traced to the same shape: a control reaching for a capability it does not
have and silently falling back to physical input. The others were the Windows-only `ToBy()`
default and Android's always-true `SupportsSelectionItemPattern`. The common tell is a fallback
that cannot fail loudly — `element.Click()` bypasses `RequirePointerInput`, so a pointer click
happens where the policy would have refused one. Routing that call through
`FlaUIMauiDriver.PointerClick` would have turned all three into an immediate, named error.
