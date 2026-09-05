# Finding: the Toggle `Reset` flakiness is a scroll-then-tap race

## Symptom

`Switch_Reset_ClearsState`, `CheckBox_Reset_ClearsState` and `RadioButton_Reset_ClearsSelection`
have failed intermittently on Android across the whole session, never on Windows. The failure is
always the second assertion:

```
AssertionException : Expected TextContains to be 'off'. Locator: AutomationId:SwitchStatusLabel
```

The status label is *found* — so this is not a lookup problem — and its text has simply not
changed. The Reset click did not take effect.

## What the three tests share

All three do the same thing: set a toggle, assert the status, click **`ResetButton`**, assert the
status went back. `ResetButton` sits at the foot of the toggle page, below the fold on a phone.

The wire trace for one run:

```
find TestSwitch → CLICK              (toggle on)
find SwitchStatusLabel               (assert "on" — passes)
find ResetButton  x21                (plain lookups fail — below the fold)
SWEEP ResetButton → find → CLICK     (scrolled to it, then clicked)
find SwitchStatusLabel               (assert "off" — FAILS)
```

Windows never scrolls to reach the button, which is exactly why Windows never fails.

## Cause: the button comes to rest under the navigation bar

The first hypothesis — that the tap was issued mid-fling — was **wrong**. Instrumenting the tap
showed the button perfectly still:

```
[TAP] .../ResetButton before={X=53,Y=2169,W=975,H=116} after={same} moved=False
```

Stationary, on screen, findable, tapped once — and the app did not respond.

What matters is *where* it came to rest. The screen is 1080x2400 and the button occupies
Y=2169-2285, hard against the bottom, where `dumpsys window displays` reports
`mHasBottomNavigationBar=true`. `scrollIntoView` stops the moment an element is on screen, which
parks it under Android's navigation bar — and the navigation bar sits above the app and swallows
touches aimed at what is beneath it.

That explains every part of the pattern: Windows never scrolls to reach the button so never
fails; the failing test rotated because it depended on exactly where each scroll happened to
stop; and the test passed when run alone, because a different scroll position left the button
clear.

## Fixes applied

Two, in the order they were found:

1. **Wait for the element to stop moving** (`WaitUntilPositionSettles`). Correct in itself — a
   tap should not be issued at coordinates the element is leaving — and it fixed
   `Switch_Reset`, which is what made the fling hypothesis look right.
2. **Scroll the element clear of the bottom edge** (`NudgeClearOfBottomEdge`). After settling, if
   the element rests within an eighth of the screen height of the bottom, the container is
   scrolled slightly so the element sits in the safe area before anything acts on it.

Both live in `Brinell.Maui.Appium`, where platform behaviour belongs.

## Result

| | Before | After |
|---|---|---|
| Android Toggle | 15 / 16, rotating failure | **16 / 16** |
| Android Scroll + Buttons + Toggle | 33 / 34 | **34 / 34**, twice in a row |
| Windows (Buttons, Text, Display, Toggle, Scroll) | 76 / 77 | **77 / 77** |

Three consecutive clean Android runs, where before exactly one Reset test failed every run.

## What this cost, and the lesson

The fling hypothesis was plausible, fitted the symptoms, and was wrong. It survived one round of
"fixing" because the fix it motivated happened to repair one of the three failures — which looked
like confirmation. The diagnostic that settled it took one instrumented run and should have come
first.
