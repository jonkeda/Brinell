# Finding: "visible" was two questions wearing one name

## What was wrong

`SearchBar_IsVisible_ReturnsTrue` failed, and so did the visibility tests for `Image`,
`ProgressBar` and `ActivityIndicator`. None of them was a control bug.

Measured on Windows:

```
[VIS] id=TestSearchBar type=Group offscreen=True rect={X=0,Y=0,Width=0,Height=0} enabled=True
[VIS]    child type=Edit  offscreen=True rect={X=0,Y=0,Width=0,Height=0}
```

Both sample pages are a `ScrollView`, and the failing controls all sit **below the fold** -
`TestSearchBar` at TextView.xaml:80 against `TestEntry` at :36, `TestProgressBar` at
DisplayView.xaml:106 against `TestLabel` at :46. `IsOffscreen` was reporting the truth. The tests
were asserting something false.

The tell was an asymmetry: `SetText` on the very same SearchBar passed. Every action path runs
through `RunDoWithElement` -> `EnsureVisible`, which scrolls; `IsVisible` is a query, and a query
reports what is on screen rather than quietly changing it.

## Two questions, two checks

- **`IsVisible`** - is it on screen *right now*.
- **`IsVisibleAfterScroll`** - can the user see it *at all*, scrolling to it if needed.

Answering the second **requires** scrolling: UIA reports a control that is merely scrolled out of
view exactly as it reports one that is not rendered - offscreen, zero bounding rectangle - so no
property distinguishes them. The name carries the side effect rather than hiding it behind an
innocent-looking query.

Prefer `IsVisibleAfterScroll` when a test means "the page shows this control". Whether something
starts above the fold depends on window size and screen density, so it differs between Windows,
Android and iOS - exactly the accidental difference that product goal (c) says tests must not
encode. `Entry_IsVisible` passed for no better reason than sitting higher up the page.

## The fallback ladder is gone

`FlaUIMauiElement.Visible` carried four rungs - `IsOffscreen`, a bounding-rectangle check, a walk
over children, and "supports Toggle, so treat it as visible" for MAUI's Switch. All four had been
commented out for some time while the doc comment above still described them as though they ran.

Deleted, and the comment now matches the code. The Switch rung is the instructive one: that is
control knowledge living in an element that must not know what a MAUI view means. A control
genuinely needing it overrides `IsVisibleCore`, which is where control-specific behaviour belongs.
Nothing depended on any of it - no `Switch_IsVisible` test exists, and the code was dead anyway.

## The Image was a real bug - in the sample app

`TestImage` reported no bounding rectangle at *any* scroll position, while every ancestor was on
screen with a real one:

```
[VIS] id=TestImage type=Image offscreen=True rect={X=0,Y=0,Width=0,Height=0}
[VIS]   ancestor0 type=Group offscreen=False rect={X=34,Y=106,Width=1136,Height=571}
[VIS]   ancestor1 type=Pane  offscreen=False rect={X=34,Y=106,Width=1136,Height=571} scrollable=True
```

The app shipped **no images at all**: the csproj had no `MauiImage` item and
`Resources/Images/` was empty, so `Source="appicon.png"` resolved to nothing. The control
rendered at zero size, which is why no amount of scrolling helped. Fixed by adding the
`MauiImage` include, an actual asset, and pointing the demo at it - not by weakening the
visibility check to accept a control that genuinely was not there.

## Result

Buttons + Text: **39 / 39**. Display visibility tests pass; `ProgressBar_Reset_ReturnsToInitialState`
still fails, as it did at baseline, and is unrelated to visibility.
