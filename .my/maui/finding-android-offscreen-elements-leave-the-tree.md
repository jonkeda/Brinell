# Finding: on Android, off-screen elements leave the accessibility tree

## Android run (Buttons, Text, Toggle)

| Suite | Before | After the page-root fix |
|---|---|---|
| Buttons | 11 / 11 | 11 / 11 |
| Text | 22 / 28 | 22 / 28 |
| Toggle | 14 / 16 | 13 / 16 |
| **Total** | **47 / 55** | **46 / 55** |

Windows is unaffected: Buttons + Text + Display + Toggle stay at 69 / 70, the one failure being
the pre-existing `ProgressBar_Reset_ReturnsToInitialState`.

Note the count went **down by one** while the diagnosis got much better — see "Honest accounting".

## Two blockers before any test could run

1. **`Brinell.Maui.Appium` was never referenced by the UITests project.** The factory loads it by
   assembly name, so the compile-time reference is optional — but the assembly still has to reach
   the output directory. Every Android test failed in the fixture constructor with "assembly not
   found" in 1 ms. Earlier Android runs must have worked off a stale copy in `bin`. Reference
   added.
2. **The `Bouw7_Phone` emulator was 96% full** — 248 MB free against an 85 MB APK, which needs
   several times that to install. Switched to the `Medium_Phone` AVD rather than touch an image
   with your own app installed on it.

## The real finding

Five Text failures reported `Page 'TextTestPage' is not loaded`. The Appium log gives the
sequence:

```
... 16 × find ResetAllButton (readiness poll)
UiScrollable(...).scrollIntoView(resourceIdMatches(".*ResetAllButton"))
POST /element/<id>/click
... 26 × find TestEntry  →  {"value":[]}    (empty, forever)
```

One click, exactly as the test asks. But `TextTestPage.IsLoaded()` was overridden to
`TestEntry.IsExists()`, and scrolling down to the Reset button pushes `TestEntry` off screen.

**On Android an off-screen view is gone from the accessibility tree, not merely marked
off-screen.** This is the mirror image of the Windows visibility finding: Windows keeps such
elements present with `IsOffscreen=true` and a zero rectangle, so probing a scrolled-away child
works there and fails here.

Ruled out on the way: the soft keyboard was never shown (`mInputShown=false` throughout), and
logcat shows no crash — the app dies only at teardown.

## What was fixed

No sample page had a root marker at all, so *every* page object worked around it by probing some
child: `TextTestPage` used `TestEntry`, `ToggleTestPage` and `DisplayTestPage` used `StatusLabel`.
Each root `ScrollView` now carries `AutomationId` matching the page name, and the three
`IsLoaded` overrides are gone — the base already looks for `AutomationId:{Name}`, and a scroll
container cannot scroll itself out of view.

Every `Page ... is not loaded` error disappeared.

## What remains

### Correction to the diagnosis above

"Queries do not scroll" was wrong. `RunAssertWithElement` already resolves through
`FindElement()` + `EnsureVisible`, so `AssertTextContains` was scrolling all along. A change that
made the absence-tolerant query helpers scroll too was written on that mistaken theory, measured,
found to make things *worse*, and reverted.

### The second defect: `UiScrollable` returns a node, not your element

`MauiTestContext.TryFindWithUiScrollable` scrolls with

```
new UiScrollable(new UiSelector().scrollable(true).instance(0))
    .scrollIntoView(new UiSelector().resourceIdMatches(".*ResetButton"))
```

and then returned *that query's* node to the caller, which clicked it. The trace showed exactly
one click on it, and the app never responded. It is a scrolling command that happens to return
something: the node is matched by a `resourceIdMatches` regex during the scroll, not by the
caller's own locator.

It now re-resolves the element by the caller's locator once the scroll has brought it on screen,
falling back to the scroll result if that lookup finds nothing.

## Result

| Run | Android | Note |
|---|---|---|
| Baseline | 47 / 55 | five failures masked as `Page ... is not loaded` |
| Page-root markers | 46 / 55 | misleading class gone; a coincidence stopped hiding one defect |
| Re-resolve after scrolling | **50 / 55** | |

Windows is unchanged throughout at **69 / 70** (`ProgressBar_Reset_ReturnsToInitialState`,
which also failed at baseline).

Consistent across runs: `Editor_LineBreaks_ArePreserved`, `Switch_Reset_ClearsState`,
`RadioButton_Reset_ClearsSelection`, `CheckBox_Reset_ClearsState`. The Text `Clear`/`ResetAll`
tests moved between passing and failing across two runs, so that family is genuinely flaky and
not yet understood — single runs are not enough to call it fixed.

## Honest accounting

`Switch_Reset_ClearsState` passed before any of this and fails now. It was passing by accident:
`ToggleTestPage.IsLoaded()` probed `StatusLabel`, which sits near the Reset button, so the page
read as loaded exactly when the label was reachable. Removing the override took away a
coincidence that had been masking the same defect the others hit.
