# Component convenience operations - proposal

Goal: let a component such as `MediaElement` offer convenience members (`Play`, `Pause`,
`IsPlaying`, `AssertPlaying`, `WaitProgressGreaterThan`) with no nested units of work, and
have the generator write them.

Status: accepted, revision 2 (decisions in section 10). No code changed. Complements [plan.md](plan.md), section 3.5
(component rules), which links here once this is decided.

## 1. The problem, briefly

Every public control member is a complete unit of work: page readiness, lookup, scroll into
view, a poll with its own timeout, a log entry. `MediaElement`'s Core methods call its parts'
public members *inside* its own generated wrappers (`RunAssertWithElement` around
`PlayPauseButton.GetAttribute(...)`), so all of that happens twice per tick: nested polls
multiply the timeout, `WaitHelper.WaitFor` swallows the part's real error, and one `Play()`
writes about 25 log pairs.

## 2. The principle

**A part's behaviour lives in the part's control. The component only forwards.**

- A part that needs more than its base offers gets its own control class, with ordinary
  element-first Core methods (`IsPlayingCore(IMauiElement? element)` reads `element.Name`).
  The generator already handles those, and they read their own element directly, so nothing
  nests.
- The component declares one-line **shortcuts** to part members. The generator turns each
  into public members that call the part and do nothing else: no `Run*` wrapper, no second
  poll, no second log entry. The part's call is the one unit of work.
- A component Core method never calls another control's public member. When the only way to
  write a member is across two parts, it is hand-written (section 5), and that is the
  exception.

Because a part is scoped to the component (`new(this, id)`), the part's members already return
the component. `player.Play().AssertPlaying().ProgressSlider...` chains without extra code, and
`SetPlaying` stops returning the page: the return-type inconsistency goes away without a rule
of its own.

## 3. Part controls

```csharp
namespace Brinell.Maui.CommunityToolkit.Controls.Media;

/// The transport's play/pause button: one button whose name says what it will do next.
/// Windows names it "Pause" while media plays, "Play" otherwise (localized; English only).
public partial class MediaPlayPauseButton<TScope> : Button<TScope>
    where TScope : IMauiScope<TScope>
{
    public MediaPlayPauseButton(IMauiScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public MediaPlayPauseButton(IMauiScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }

    protected virtual bool? IsPlayingCore(IMauiElement? element)
        => element?.Name is { } name
            ? name.StartsWith("Pause", StringComparison.OrdinalIgnoreCase)
            : null;

    protected virtual void PlayCore(IMauiElement element, int? timeoutMs = null)
        => SetPlayingCore(element, true, timeoutMs);

    protected virtual void PauseCore(IMauiElement element, int? timeoutMs = null)
        => SetPlayingCore(element, false, timeoutMs);

    /// Presses the button when the state differs, then confirms the name changed.
    protected virtual void SetPlayingCore(IMauiElement element, bool? playing, int? timeoutMs = null)
    {
        if (playing == null || IsPlayingCore(element) == playing) return;

        ClickCore(element, timeoutMs);                 // the base Core, not the public Click

        if (!WaitHelper.WaitFor(() => IsPlayingCore(element), actual => actual == playing,
                timeoutMs ?? DefaultTimeoutMs, PollingIntervalMs))
        {
            throw new TimeoutException(
                $"'{Locator.Value}' did not {(playing.Value ? "start playing" : "pause")}.");
        }
    }
}
```

The generator gives it `IsPlaying / WaitPlaying / AssertPlaying`, `Play`, `Pause` and
`SetPlaying`, plus everything a `Button` has. Inside it, only its own element and its own
Core methods are used.

Where a component has a separate button per action, no part class is needed: the shortcut
forwards straight to `Click`, as in `PlayShortcut() => PlayButton.Click(timeoutMs)`.

## 4. Shortcuts: the generator rule

### Naming

A suffix, the way `Core` marks generated behaviour. Candidates:

| Suffix | Reads as | Note |
| --- | --- | --- |
| `Shortcut` | `IsPlayingShortcut() => PlayPauseButton.IsPlaying()` | **Recommended.** Short, and it says what it is: the component's shortcut to a part's member. |
| `Convenience` | `IsPlayingConvenience() => ...` | Your term. Clear, but long, and "convenience" also describes the hand-written aliases (`TurnOn`, `Check`) that are not forwards. |
| `Via` | `IsPlayingVia() => ...` | Shortest, reads oddly for actions (`PlayVia`). |

The rest of this plan uses `Shortcut`.

### What the generator matches

A method is a shortcut when all of these hold. A near-miss is reported by
`ControlObjectAnalyzer`, like a Core method that is not `protected virtual`:

1. The name ends in `Shortcut`.
2. It is `protected` (not virtual: it is a declaration, not behaviour to override).
3. It is expression-bodied, and the body is one call on a member of the class:
   `<Part>.<Member>(<arguments>)`, where `<Part>` is a property or field of the class.

The generator reads everything else from the syntax. It needs no knowledge of the part's
type: if the part lacks the member, the generated code does not compile, and that error
names the part and the member.

### What it emits

The public name is the shortcut's name with `Shortcut` removed. The prefix decides the kind,
with the same rules as Core methods, and the part member's name gives the names to forward
to (`IsPlaying` -> `WaitPlaying`, `AssertPlaying`; `GetValue` -> `WaitValue`, `AssertValue`).

| Shortcut | Emits |
| --- | --- |
| `protected bool? IsPlayingShortcut() => PlayPauseButton.IsPlaying();` | `public bool? IsPlaying() => PlayPauseButton.IsPlaying();`<br>`public bool WaitPlaying(bool? expected = true, int? timeoutMs = null) => PlayPauseButton.WaitPlaying(expected, timeoutMs);`<br>`public MediaElement<TScope> AssertPlaying(bool? expected = true, string? message = null, int? timeoutMs = null) => PlayPauseButton.AssertPlaying(expected, message, timeoutMs);` |
| `protected double? GetProgressShortcut() => ProgressSlider.GetValue();` | `GetProgress(int? timeoutMs = null)`, `WaitProgress(double? expected, ...)`, `AssertProgress(double? expected, ...)`, forwarding to `GetValue`, `WaitValue`, `AssertValue` |
| `[GenerateComparisons(Comparison.GreaterThan)]` on a `Get` shortcut | also `WaitProgressGreaterThan` / `AssertProgressGreaterThan`, forwarding to `WaitValueGreaterThan` / `AssertValueGreaterThan` |
| `protected MediaElement<TScope> PlayShortcut(int? timeoutMs = null) => PlayPauseButton.Play(timeoutMs);` | `public MediaElement<TScope> Play(int? timeoutMs = null) => PlayPauseButton.Play(timeoutMs);` |
| `protected MediaElement<TScope> SetPlayingShortcut(bool? playing, int? timeoutMs = null) => PlayPauseButton.SetPlaying(playing, timeoutMs);` | `public MediaElement<TScope> SetPlaying(...)`, the same call |

For actions and setters the emitted member is the shortcut made public under its new name.
For `Is` and `Get` the generator also writes the `Wait` and `Assert` forms, which it can do
because every part member was itself generated with the same fixed signatures.

The `Assert*` return type is the component. It compiles only when the part is scoped to the
component (`new(this, id)`), which is the component rule anyway, so a part scoped elsewhere
is caught by the compiler.

## 5. Across two parts: the exception

Some values need two parts. `MediaElement`'s duration is elapsed plus remaining, which are two
labels. Options, in order of preference:

1. **Find one part that answers it.** Here the remaining label alone answers "has the media
   opened" (it shows a clock once the length is known), which is what the old `Play` waited
   for. A `MediaTimeLabel` part with `GetSecondsCore(element)` parses the clock, and
   `IsOpenedShortcut() => TimeRemainingLabel.IsOpened()` forwards to it.
2. **Hand-write a `Get` only**, as two plain part calls in sequence
   (`TimeElapsedLabel.GetSeconds() + TimeRemainingLabel.GetSeconds()`). There is still no
   wrapper, so there is no nesting. Do not add a generated `Wait`/`Assert` around it.
3. If a test needs to wait on a cross-part value, that is a signal the value belongs to one part
   or to the app. Ask before wrapping part calls in `RunWait`.

Option 2 is allowed (decision 2). Each hand-written cross-part member carries a comment naming
the parts it reads and why no single part answers.

### Where the "media opened" wait goes

The old `SetPlayingCore` waited for a duration before pressing, because a press before the
media opens is dropped (it failed once in the full suite). With forwarding, the play button
cannot see the time labels. Before implementing, **probe** whether the button is disabled
until the media opens:

- If it is, nothing is needed: `ClickableControlBase` already waits for enabled
  (`EnsureReadyForActionCore`).
- If it is not, the test waits for it explicitly (`player.WaitOpened()` from option 1), and
  the component's remarks say so.

## 6. MediaElement after the change

```csharp
public MediaPlayPauseButton<MediaElement<TScope>> PlayPauseButton => new(this, PlayPauseButtonId);
public Slider<MediaElement<TScope>> ProgressSlider => new(this, ProgressSliderId);
public MediaTimeLabel<MediaElement<TScope>> TimeElapsedLabel => new(this, TimeElapsedId);
public MediaTimeLabel<MediaElement<TScope>> TimeRemainingLabel => new(this, TimeRemainingId);

protected bool? IsPlayingShortcut() => PlayPauseButton.IsPlaying();
protected bool? IsOpenedShortcut() => TimeRemainingLabel.IsOpened();

[GenerateComparisons(Comparison.Equals | Comparison.GreaterThan)]
protected double? GetProgressShortcut() => ProgressSlider.GetValue();

protected MediaElement<TScope> PlayShortcut(int? timeoutMs = null) => PlayPauseButton.Play(timeoutMs);
protected MediaElement<TScope> PauseShortcut(int? timeoutMs = null) => PlayPauseButton.Pause(timeoutMs);
protected MediaElement<TScope> SetPlayingShortcut(bool? playing, int? timeoutMs = null)
    => PlayPauseButton.SetPlaying(playing, timeoutMs);

/// Elapsed plus remaining, in seconds. Hand-written: two parts (section 5).
public double? GetDuration()
    => TimeElapsedLabel.GetSeconds() + TimeRemainingLabel.GetSeconds();
```

The component has no Core methods left. A test reads:

```csharp
page.Player.WaitOpened();
page.Player.Play()
    .AssertPlaying()
    .WaitProgressGreaterThan(5);
```

`Stop` is left out until probed: WinUI's transport shows a stop button only when
`IsStopButtonVisible` is set. If the sample app sets it, `StopShortcut() => StopButton.Click()`
is all it takes, which is exactly the `StartButton.Click()` shape.

## 7. Generator work

1. **Shortcuts** (section 4): a `ShortcutGenerator` registered before the others, the
   near-miss check in `ControlObjectAnalyzer`, and `Brinell.Generator.Tests` cases for each
   kind, for `[GenerateComparisons]`, and for a body that is not a single part call (reported,
   not emitted).
2. **Ordered comparisons**: add `GreaterThan`, `AtLeast`, `LessThan`, `AtMost` to
   `Comparison`, for `IComparable` values. Then mark `RangeControlBase.GetValueCore` with them
   so `Slider` has `WaitValueGreaterThan` to forward to. This also removes the hand-written
   `WaitProgressPasses` and `WaitDurationKnown`.

No `Run*` helper, `ViewBase` or `RootedScopeBase` change is needed.

## 8. Steps

1. **Done (2026-09-18).** Generator: shortcuts and ordered comparisons, with generator tests.
   `ShortcutGenerator` + `Analysis/ShortcutMethod`; malformed shortcuts fail generation via
   `ControlObjectAnalyzer.FindMalformedShortcuts`; `Comparison` gained `GreaterThan`,
   `AtLeast`, `LessThan`, `AtMost`; `RangeControlBase.GetValueCore` declares them.
   Regeneration changed only `RangeControlBase.gen.cs`. Generator tests 142/142 (21 new),
   `Brinell.Maui.Tests` 109 passed / 1 skipped, solution build 0 errors.
2. Probe on Windows: is `PlayPauseButton` disabled before the media opens; does the sample show
   a stop button.
3. Write `MediaPlayPauseButton` and `MediaTimeLabel`; rewrite `MediaElement` as in section 6.
   Delete `WaitProgressPasses` and `WaitDurationKnown` and update their callers in
   `MediaElementTests`.
4. Regenerate (`tools/Scripts/CreateMaui.Bat`) and diff every `.gen.cs`: only
   `RangeControlBase` (new comparisons) and the MediaElement files should change.
5. Build; run `MediaElementTests` at tier 1 (`--filter "Control=MediaElement"`), then the
   `Range` tests because `RangeControlBase` changed.
6. Add the principle and the shortcut rule to the `maui-control` skill's component reference.

### Steps 2-6: done (2026-09-18)

**Probe (Windows, fresh app, sampled while the page opened):**

| ms | PlayPauseButton | TimeRemainingElement |
| --- | --- | --- |
| 68 | absent | absent |
| 311 | enabled, "Play" | "Time remaining" (no clock) |
| 393 | enabled, "Play" | "Time remaining 00:00:06" |

- The button is enabled before the media opens, so `ClickableControlBase`'s enabled wait does
  not cover it. `MediaElement.Play` is hand-written across two parts: wait for the remaining
  time's clock (`WaitOpened`), then `PlayPauseButton.Play`.
- No stop button: the transport shows Mute, Repeat, Playback rate, Previous/Next track,
  Rewind, Play/Pause, Fast forward, Aspect ratio, Cast, Full window.
- The seek slider accepts `SetValue`: 50 moved playback to 3 s of 6, 0 back to the start. So
  `Stop` = `Pause` + `ProgressSlider.SetValue(0)`, hand-written across two parts.

**Code:** `MediaPlayPauseButton` (`IsPlaying`, `Play`, `Pause`, `SetPlaying`) and
`MediaTimeLabel` (`GetSeconds` with ordered comparisons) are new part controls. `MediaElement`
has no Core methods left: shortcuts for `IsPlaying`, `Pause`, `GetProgress`, `GetElapsed`,
`GetRemaining`; hand-written `Play`, `SetPlaying`, `Stop`, `WaitOpened`, `GetDuration`.
`WaitProgressPasses` and `WaitDurationKnown` are deleted. `SetPlaying` now returns the
component, not the page.

**Verification:** regeneration changed only the MediaElement files (plus `RangeControlBase`
from step 1). Solution build 0 errors. `--filter "Control=MediaElement"` 12/12, three runs
(8 existing tests adapted, 4 new: `SetPlaying`, `Stop`, remaining time, part/component
agreement). `--filter "FullyQualifiedName~Tests.Range"` 23/23. Android not run: the transport
publishes no ids there.

**Step 6:** the `maui-control` skill is not written yet; the rules are in
[plan.md](plan.md) section 3.5, which the skill is written from.

## 9. Out of scope, noted

- `ToggleControlBase.ToggleCore` calls `RunWaitWithElement` inside a Core method, a small
  version of the same nesting. Fix it separately by reading `IsCheckedCore(element)` with
  `WaitHelper.WaitFor`, as `MediaPlayPauseButton.SetPlayingCore` does.
- `WaitHelper.WaitFor` returns false and drops the last exception. Worth fixing on its own,
  so the timeout message can include the cause.

## 10. Decisions (2026-09-18)

1. Suffix: `Shortcut`.
2. Hand-written cross-part members are allowed (section 5, option 2), each with a comment
   saying which parts it reads and why no single part answers.
3. No backward compatibility: removed and renamed members are deleted outright, with no
   `[Obsolete]` forwards. This applies to `Brinell.Maui.CommunityToolkit` and to the
   `RangeControlBase` comparison additions.
