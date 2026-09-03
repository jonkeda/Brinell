# Finding: the controls exist — their accessibility nodes do not

## Question

Is the Android problem that a control is not created when it is not visible?

## Answer: no. The views are created, laid out, and marked visible.

The scroll test page, opened on the emulator, dumped from the app's own view hierarchy
(`adb shell dumpsys activity top`):

```
crc...MauiScrollView    { 0,0-1080,2190 }
crc...MauiMaterialButton{ VFED..C..  53,306-1028,422   }   TopButton
crc...MauiMaterialButton{ VFED..C..  53,461-1028,577   }   ResetButton
crc...MauiMaterialButton{ VFED..C..  53,3099-1028,3215 }   MiddleButton   off-screen
crc...MauiMaterialButton{ VFED..C..  53,5824-1028,5940 }   BottomButton   far off-screen
```

All four exist, all four carry real laid-out bounds — the content runs to y≈5940 inside a 2190-high
viewport — and all four are flagged `V`, meaning `View.VISIBLE`. MAUI creates every child of a
`VerticalStackLayout`; nothing here is virtualized or recycled.

What is missing is their **accessibility node**. The same moment, via `uiautomator dump`:

```
app ids on screen: ScrollTestPage, ScrollStatusLabel, ScrollTopButton, ScrollResetButton
ScrollMiddleButton present? False
ScrollBottomButton present? False
```

Android publishes `AccessibilityNodeInfo` only for content inside the scrollable viewport. So the
boundary is one of *reporting*, not of *lifecycle*: the control is there, the app knows where it
is, and automation simply cannot see it.

## `allowInvisibleElements` does not lift the boundary

The obvious hope was that these nodes exist but are flagged invisible, in which case
UiAutomator2's `allowInvisibleElements` setting would expose them. Tested: the setting reached
the driver —

```
Applying the initial values to Appium settings parsed from W3C caps: {"allowInvisibleElements":true}
Proxying [POST /appium/settings] ... {"settings":{"allowInvisibleElements":true}}
```

— and changed nothing (still 6/7, same failure). That setting governs the `isVisibleToUser`
flag, not viewport clipping. Reverted.

**So scrolling is not one way to reach off-screen content on Android; it is the only way.** No
capability or setting substitutes for it.

## Caveat: virtualized containers are a different case

This page is a `ScrollView` over a `VerticalStackLayout`, where every child is realized. A
`CollectionView` backed by a `RecyclerView` genuinely does not create off-screen rows, so there
the control really is absent, not merely unreported. The two look identical through Appium and
need distinguishing before generalizing anything here to collections.

## Consequence for `IsExists`

Should `IsExists` mirror the two-check split we gave visibility — `IsVisible` /
`IsVisibleAfterScroll`?

**Recommendation: no.** The two cases are not symmetric.

- Both visibility questions are meaningful to a test author. "Is it on screen now" and "can the
  user see it at all" are different things a test might genuinely want to assert, and neither is
  a platform artifact.
- Only one existence question is meaningful: "is this control on the page". "Is it in the
  accessibility tree at this instant" is not something anyone writes a UI test about — as shown
  above, it is a by-product of where the page happens to be scrolled, and it is the direct cause
  of `BottomButton_Exists_WithoutScrolling` passing on Windows and failing on Android. A second
  method would give that artifact a name and invite tests to depend on it.

So `IsExists` should mean "exists on the page" and resolve by scrolling, the way the action path
already does. The price is that asserting genuine absence must exhaust a scroll of the container
before it can answer — which is the honest cost of the question "is it really not there?", and is
paid only when the element is not found.

That is a semantic change to a core method, so it was recorded here before being made.

## Implemented

`IsExists`, `WaitExists` and `AssertExists` now resolve through `TryFindElementAfterScroll`, which
asks the scope for `TryFindElementAfterScroll(locator)`. The name pairs with the lookup it
qualifies, the way the visibility pair already does:

| Strict — here, now | Scrolls to look |
|---|---|
| `TryFindElement()` | `TryFindElementAfterScroll()` |
| `IsVisible()` | `IsVisibleAfterScroll()` | `MauiTestContext` implements that as a plain lookup followed —
**on Android only** — by the `UiScrollable` scroll. Windows needs nothing extra: its tree keeps
off-screen elements, so a lookup that found nothing means the element is genuinely absent and no
scroll could change that. The platform test lives in the context, not in a control object.

It deliberately **does not poll**. The first version routed through `FindElement`, which re-runs
the full `ElementFind` timeout before its own scroll fallback — that cost 3 s on every genuine
absence and took the Windows suite from 42 s to **4 m 27 s**. Skipping the redundant poll brought
it back to 59 s.

`IsVisible` is untouched and still strict, by construction rather than by exception: the Exists
family passes its own resolver to the shared helpers, so nothing else changed behaviour.

### Result

| | Before | After |
|---|---|---|
| Android scroll tests | 6 / 7 | **7 / 7** |
| Android Buttons | 11 / 11 | 11 / 11 |
| Android Toggle | 13 / 16 | 14 / 16 |
| Windows (Buttons, Text, Display, Toggle, Scroll) | 76 / 77 | 76 / 77 |

Windows runtime unchanged at 59 s.

### The cost is real, and it is on Android

Scroll + Buttons + Toggle, 34 tests, took **10 m 34 s**. For comparison, Buttons + Text + Toggle
(55 tests) took about 4 m 35 s before this change. Every lookup that finds nothing now drives a
`UiScrollable` sweep of the container before it can answer, and readiness polls call these methods
repeatedly.

That is the price of the semantics, but it is steep enough to be worth reducing:

- a search-swipe budget (`setMaxSearchSwipes`) would bound each sweep;
- the sweep could be skipped while a scope is still waiting to become ready, since a not-yet-loaded
  page will answer "absent" for reasons scrolling cannot fix;
- an absence that has already been established once within a poll need not be re-established on
  every iteration.

A separate hazard surfaced during the same runs: the UiAutomator2 instrumentation process crashed
mid-suite, producing 35 cascading `instrumentation process is not running` failures. It recovered
after restarting the emulator and Appium. Whether the extra scrolling makes that more likely is
not established.
