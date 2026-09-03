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

## Cause: the tap is issued while the page is still moving

`UiScrollable.scrollIntoView` flings the container and returns while it is still coasting. A tap
issued at that moment is delivered at coordinates the element has already left, so it lands on
nothing — or on whatever slid into that spot. Nothing throws; the click simply does not happen,
and the failure surfaces two steps later as an unchanged status label.

## Fix applied

`ReResolveAfterScrolling` now waits for the element to stop moving before returning it:
`WaitUntilPositionSettles` reads the element's rectangle until two consecutive reads agree, up to
500 ms. Not a fixed sleep — it returns as soon as the element is still, and gives up rather than
blocking if it never is.

**Result: `Switch_Reset` fixed.** The Toggle suite went from two or three failures a run to
consistently one.

## What is left, and why it is probably the same shape

Three consecutive Toggle runs after the fix:

| run | result | failing test |
|---|---|---|
| 1 | 15 / 16 | `CheckBox_Reset` |
| 2 | 15 / 16 | `CheckBox_Reset` |
| 3 | 15 / 16 | `RadioButton_Reset` |

Exactly one Reset test fails per run, and **which one moves**. That rules out a per-control
defect: all three click the same button by the same path. Run on its own,
`CheckBox_Reset_ClearsState` **passes** in 10 s, which rules out the test itself.

The remaining suspect is where `scrollIntoView` leaves the element. It stops as soon as the
element is on screen, which puts it hard against the bottom edge — and a tap aimed at the
geometric centre of a partially clipped element can land outside the visible area. That would be
intermittent in exactly this way, depending on where each scroll happens to stop.

### Next step

After scrolling to an element, do not act on it at the viewport edge: scroll far enough that it
sits fully inside the container, then act. That needs the container's bounds alongside the
element's, both of which are already available where the sweep happens.

Worth doing before any further performance work — a suite that fails one test a run for reasons
unrelated to the code under test is a worse problem than a slow one.
