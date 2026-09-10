---
title: Beyond Gestures — What Else the UIA Bridge Should Carry
description: MAUI functionality that today needs physical keys or mouse, and would be better served by the same custom-pattern bridge
status: analysis
---

# Beyond Gestures — What Else the UIA Bridge Should Carry

Gestures were the obvious first use of the bridge described in
[design-uia-gesture-pattern.md](design-uia-gesture-pattern.md) because they are the most
visibly blocked. They are not the most valuable.

This document catalogues the rest: every place Brinell currently reaches for physical keys,
a real mouse, or the system clipboard, plus the state it cannot read at all. Each entry is
grounded in a specific line of this repo, not in speculation about what might be useful.

---

## The finding that should change Phase 1

**The gesture pattern's method table is `Invoke(int, int, int)`. That cannot carry text.**

Per D-2 in the design, a registered pattern's method table is fixed at registration and both
processes must agree on it exactly. Appending a method later means a new pattern GUID, which
means every app built against the old contract stops working with the new test assembly.

Almost everything in this document needs a **string** in, a **string** out, or both:
`SetText`, `ScrollTo(itemId)`, `Navigate(route)`, `ReadState(property)`. If the contract ships
as int-only in Phase 1, adding them later is a breaking protocol migration across the app
under test and the test assembly simultaneously.

**Recommended amendment to Phase 1** — cheap now, expensive later:

```csharp
// Method table, frozen at registration. Index order IS the contract.
//   0: Invoke   (int verb, int arg1, int arg2)            — gestures, today
//   1: Exchange (int verb, string arg, out string result) — everything in this document
HRESULT Invoke(int verb, int arg1, int arg2);
HRESULT Exchange(int verb, [in] string arg, [out] out string result);
```

`Exchange` costs one extra `UIAutomationMethodInfo` entry and one more branch in the
handler's `Dispatch`. It stays unused until the first capability below is built. The
alternative is registering a second pattern later and maintaining two GUIDs, two
registrations and two client wrappers forever.

`UIAutomationType.String` is a supported marshalling type, so this is a shape decision, not a
feasibility one. **Verify it in spike S2** — round-trip a string, not just an int.

---

## What belongs on the bridge, and what does not

The design states a constraint worth repeating, because this document is exactly the pressure
that would erode it:

> The bridge must never become the app's primary automation surface.

Three tests before anything is added:

1. **Is it blocked?** Does a real UIA pattern already answer this? `InvokePattern` clicks
   buttons, `TogglePattern` toggles, `ValuePattern` reads text, `SelectionItemPattern` selects,
   `ScrollPattern` reports position. Those stay where they are. The bridge is for what UIA
   genuinely cannot express.
2. **Is the physical path the thing under test?** A test asserting that typing into an `Entry`
   fires `TextChanged` must actually type. The bridge is for **arranging** state and for
   actions with no semantic route — not for skipping the behaviour under test.
3. **Does it stay a UI test?** Reading `BindingContext` off an element turns a UI test into a
   unit test with extra steps. Read what the user can see; if the assertion needs the view
   model, the coverage belongs in `Brinell.Maui.Tests`.

Entries below are marked **arrange** (setup, safe to shortcut) or **assert** (the observation
itself) — the distinction decides whether a shortcut is a convenience or a lie.

---

## A. Focus and window activation — the highest-value item

**Today.** Every physical action first calls `EnsureRootWindowFocused`
([FlaUIMauiDriver.cs:123](srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L123)), whose own
remarks state the problem plainly:

> `Keyboard.Type` reaches whichever window holds focus. Without this, keystrokes meant for the
> app land in whatever the user is doing.

It tries `SetWindowVisualState`, then `SetForeground`, then `Focus`, swallowing each failure in
turn — three fallbacks deep, and it still cannot guarantee the app is frontmost.

**Cost.** This is the root cause of the rule that only one UI test process may run at a time.
Two concurrent runs fight over a single desktop-wide resource — the foreground window — and
produce failures that describe nothing real.

**Bridge.** `Focus(elementId)` calls `VisualElement.Focus()` on the UI thread. No foreground
window, no `SetForeground`, no z-order. A focused element is a property of the app, not of the
desktop.

**Why it ranks first.** It is not one control's problem; it is the reason the whole suite is
serialised and the reason failures are environment-dependent. Every other entry below inherits
the fix.

| | |
|---|---|
| Mode | arrange |
| Blocked today | no — but only via a desktop-global resource |
| Verb | `Focus`, `Unfocus`, `IsFocused` |

---

## B. Text input — and the system clipboard

**Today.** [FlaUIMauiElement.cs:271-295](srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs#L271-L295):

- `TextInputMethod.Keys` → `Keyboard.Type(text)` — real keystrokes to the focused window.
- `TextInputMethod.Paste` → `System.Windows.Forms.Clipboard.SetText(text)` then Ctrl+V
  ([line 283](srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs#L283)).
- `Clear` → Ctrl+A, Delete ([lines 304-305](srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs#L304-L305)).
- `Submit` → Enter ([line 711](srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs#L711)).

**Cost.** The clipboard is a machine-wide resource shared with the human at the keyboard. A
test run silently destroys whatever they had copied, and two runs corrupt each other. `Ctrl+A`
means "select all" only if focus landed where it was supposed to.

`TrySetTextValue` already prefers `ValuePattern` where the control accepts a write — that rung
is correct and stays. The bridge covers what `ValuePattern` refuses.

**Bridge.** `Exchange(SetText, "elementIdtext")` sets `Entry.Text` / `Editor.Text` /
`SearchBar.Text` on the UI thread.

**Keep the physical path deliberately.** A test of input behaviour — `TextChanged` firing per
keystroke, `MaxLength` truncating, a numeric keyboard rejecting letters — must still type. The
bridge becomes the default for *arranging* a field's contents; `TextInputMethod.Keys` stays for
*testing* the input pipeline, and the enum already gives us the vocabulary to say which is
meant.

| | |
|---|---|
| Mode | arrange (typing stays for assert) |
| Blocked today | partially — `ValuePattern` covers some controls, refuses others |
| Verb | `SetText`, `AppendText`, `ClearText`, `Submit` |

---

## C. Navigation — Alt+Left and F5 as real keystrokes

**Today.** `NavigateBack` tries the back button, then falls back to
`Keyboard.TypeSimultaneously(ALT, LEFT)`
([FlaUIMauiDriver.cs:599](srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L599)). `Refresh` sends
`F5` ([line 613](srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L613)). Both swallow
`Win32Exception` because "some locked-down desktops deny SendInput".

**Cost.** These are desktop-wide keystrokes aimed at whatever holds focus. Alt+Left is not even
a MAUI concept — it is a browser convention that happens to work sometimes. And the code knows
it can silently do nothing.

**Bridge.** `Exchange(Navigate, route)` → `Shell.Current.GoToAsync(route)` /
`Navigation.PopAsync()`. Actual navigation, with an actual result, on the actual navigation
stack.

| | |
|---|---|
| Mode | arrange |
| Blocked today | yes — the fallback is a guess that can no-op |
| Verb | `NavigateBack`, `NavigateTo`, `CurrentRoute` |

---

## D. Date and time pickers — the ladders already written to avoid the pointer

**Today.** `DatePicker` and `TimePicker` carry the most elaborate pointer-avoidance code in the
repo. `TimePicker.SetTimeCore` opens the flyout by Invoke, sets hour, minute and period by
their `SelectionItem` patterns, and commits with Accept. `DatePicker.SetDateCore` is a
three-rung ladder — `ValuePattern` (which "WinUI advertises and refuses"), then calendar-flyout
navigation, then typed text — ending in:

> `Could not set date {date} without the pointer. Tried the Value pattern, the calendar flyout
> and typed text.`

And the root problem is stated at
[TimePicker.tpl.cs:106](srcnew/Brinell.Maui/Controls/DateTimes/TimePicker.tpl.cs#L106):

> The TimePicker root publishes no patterns at all on Windows — the value lives on its
> FlyoutButton child.

**Cost.** Both controls are in the suite's known-failing baseline. Hundreds of lines exist to
work around a platform projection, and they are fragile against WinUI's flyout internals.

**Bridge.** `DatePicker.Date = value`. One line, no flyout, no ladder. The existing ladder
stays as the fallback for uninstrumented apps.

| | |
|---|---|
| Mode | arrange |
| Blocked today | yes — documented as failing after three rungs |
| Verb | `SetDate`, `SetTime`, `OpenFlyout`, `CloseFlyout` |

---

## E. Scrolling and virtualization

**Today.** `Swipe` detects a vertical gesture and substitutes **mouse wheel clicks**
([FlaUIMauiElement.cs:545-555](srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs#L545-L555)), with
the comment "mouse drag doesn't scroll MAUI ScrollView controls". `TryScrollContent` drives
`ScrollPattern` where it can, walking up to find a scrollable ancestor, and includes a
stuck-detection loop comparing `VerticalScrollPercent` between steps.

**Cost.** Wheel clicks are an unquantified unit — five clicks scrolls an unknown distance. The
stuck-detection loop exists because the operation has no completion signal. On Android,
offscreen elements leave the tree entirely, so "scroll until it appears" is the only strategy
available.

**Bridge.** `Exchange(ScrollTo, elementId)` → `ScrollView.ScrollToAsync(element, position,
animated: false)` or `CollectionView.ScrollTo(item, position)`. Deterministic, addressed by
target rather than by distance, and it returns when the scroll is done.

This is the entry that most improves Android parity: `CollectionView.ScrollTo` is
cross-platform MAUI API, so the same bridge verb works identically on both platforms.

| | |
|---|---|
| Mode | arrange |
| Blocked today | partially — `ScrollPattern` works, wheel fallback does not quantify |
| Verb | `ScrollTo`, `ScrollToIndex`, `ScrollPosition` |

---

## F. Pickers and selection flyouts

**Today.** `Picker` opens a platform flyout. On Android the Picker tests are 0/8 before any
change — one root failure and seven cascades from it.

**Bridge.** `Picker.SelectedIndex = n`, or select by item text. The flyout never opens, so
there is nothing to dismiss and no timing window in which it might not have opened yet.

Keep a separate verb for `OpenFlyout` so a test that genuinely means "the flyout opens and
shows these items" can still say so.

| | |
|---|---|
| Mode | arrange |
| Blocked today | yes on Android, awkward on Windows |
| Verb | `SelectIndex`, `SelectByText`, `OpenFlyout` |

---

## G. Dialogs, alerts and action sheets

**Today.** `ContentDialog` is modelled, but `DisplayAlert`, `DisplayActionSheet` and
`DisplayPromptAsync` produce platform-native modals whose buttons must be found and clicked in
whatever the platform names them.

**Bridge — read first, act second.** The high-value verb is `Exchange(CurrentAlert, "")`
returning the open alert's title, message and buttons as a structured string. A test can then
assert *what was asked* rather than merely that something was dismissed.

Dismissal by result (`DismissAlert("Cancel")`) is the lower-value half, and it is the one to be
careful with: if the test is about the user confirming a destructive action, clicking the real
button is the point.

| | |
|---|---|
| Mode | assert (read) / arrange (dismiss) |
| Blocked today | partially — findable, but platform-named |
| Verb | `CurrentAlert`, `DismissAlert` |

---

## H. Menus, flyouts and context menus

**Today.** `ShellFlyout`, `Menu`, `TabMenu` and `Toolbar` are modelled. A **context menu**
needs `RightClick()` — physical, positional, and it opens a native popup.

**Bridge.** Open and close the Shell flyout directly (`Shell.FlyoutIsPresented = true`); invoke
a `MenuFlyoutItem` by id without opening the popup at all.

| | |
|---|---|
| Mode | arrange |
| Blocked today | yes for context menus |
| Verb | `OpenFlyout`, `CloseFlyout`, `InvokeMenuItem` |

---

## I. State that cannot be read — the second class

Everything above is about *actions*. This class is different: the state exists in MAUI but is
lost or distorted on its way through the platform projection, so tests assert a proxy instead.

### I-1. Image loading

[Image.tpl.cs:40](srcnew/Brinell.Maui/Controls/Display/Image.tpl.cs#L40): *"An image is
considered loaded if it occupies space."* That is a proxy, and a bad one — an image that failed
to load still occupies its requested space. `Image` is in the known-failing baseline.

**Bridge:** `Source`, `IsLoading`, and the real decoded dimensions. `IsLoading` is public MAUI
API and answers the actual question.

### I-2. ProgressBar units

[ProgressBar.tpl.cs:43](srcnew/Brinell.Maui/Controls/Display/ProgressBar.tpl.cs#L43): WinUI uses
0–100 where MAUI's `Progress` is 0–1, so the control normalises against a reported maximum.
Also in the known-failing baseline.

**Bridge:** read `ProgressBar.Progress` — the MAUI value, in MAUI units, with no normalisation
step to get wrong.

### I-3. Visibility is two questions

A recorded finding in this repo already says so: UIA's `IsOffscreen` and MAUI's `IsVisible`
answer different questions, and conflating them produces assertions that pass for the wrong
reason.

**Bridge:** report `IsVisible`, `Opacity` and `IsEnabled` as MAUI sees them, alongside the UIA
view. Where they disagree, that disagreement is often the actual bug.

### I-4. Busy and idle

Busy-state checking has been revisited at least once (commit `3784e70`). A sentinel element is
a convention the app must maintain; a real "the dispatcher queue is drained and no animation is
running" signal is a property of the framework.

**Bridge:** `IsIdle` — a genuinely better answer to the question every `WaitFor` is really
asking, and squarely inside `AD-004` (wait for concrete state, never sleep).

### I-5. Where to stop

`BindingContext`, arbitrary property reflection, and view-model state are **out**. Reading them
would make the bridge a debugger and the tests unit tests. Read what a user could perceive; if
the assertion needs the view model, it belongs in `Brinell.Maui.Tests`.

---

## Background execution — what this is all for

Individually these capabilities remove workarounds. Together they buy something bigger: **the
app under test runs behind your editor while you keep working.** That is the outcome that
justifies the priority order below, so it is worth stating precisely — including what it does
not fix.

### Why it is impossible today

[AssemblyInfo.cs](testsnew/Brinell.Maui.UITests/AssemblyInfo.cs) already names the reason:

> Two fixtures now launch two different apps. xUnit runs collections in parallel by default,
> which would put both on screen at once: **on Windows they compete for the foreground** […]
> UI tests drive one machine, so they run one at a time.

Four desktop-global resources are in play, and every one of them is shared with the human at
the keyboard:

| Resource | Where | What the user experiences |
|---|---|---|
| Foreground window | `EnsureRootWindowFocused` → `SetForeground`, before every physical action | The app steals focus mid-keystroke |
| The cursor | `Mouse.MoveTo` / `Down` / `Up` / `Scroll` | The pointer jumps across the screen |
| Keyboard focus queue | `Keyboard.Type` (SendInput) | Test keystrokes land in the user's editor |
| The clipboard | `Clipboard.SetText` + Ctrl+V | Whatever the user copied is destroyed |

The driver already tries to be polite: it captures the previous foreground window at launch and
calls `RestoreForegroundWindow`
([FlaUIMauiDriver.cs:75](srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L75)). Then
`EnsureRootWindowFocused` takes it back on the next action. The politeness cannot survive the
input model — which is the point.

### What the bridge fixes

All four. UIA property reads and pattern invokes are cross-process COM calls; they do not care
about z-order, foreground, or whether the window is visible. A gesture becomes a method call on
the app's own objects on its own UI thread. **An occluded window is fully drivable.**

This is why **A. Focus** ranks first: `VisualElement.Focus()` without `SetForeground` is the
linchpin. Without it, the window must still be frontmost to receive typing.

### What the bridge does not fix

**Screenshots, and this one is independent of the bridge entirely.** `GetScreenshot` uses
FlaUI's `Capture.Element`, which is `Graphics.CopyFromScreen` over the element's bounding
rectangle. Occlude the window and **the screenshot is of whatever is on top** — the user's
editor. That matters because `AGENTS.md` directs you to inspect screenshots first when a UI
test fails; silently capturing the wrong window is worse than capturing nothing.

The fix is `PrintWindow` with `PW_RENDERFULLCONTENT`, or `Windows.Graphics.Capture` for a
DWM-backed capture of a specific HWND. Both read a window's own content while it is occluded.
**Spike it rather than assume**: WinUI 3 composites through DirectComposition, and
hardware-accelerated surfaces sometimes capture black.

**Occluded is safe; minimized is not.** Behind other windows, DWM still composes and layout
still runs. Minimized, a WinUI window can stop producing layout and render updates, and
virtualized `CollectionView` items may never realize — so tests would fail on content that
genuinely is not there. The rule is **move it, don't minimize it**. `BRINELL_AUT_PLACE_RIGHT`
already repositions the app through the Transform pattern; the same mechanism extends to
off-screen or second-monitor placement.

**Residual physical-input paths.** The bridge covers only what is routed through it. `Hover`,
`RightClick`, `DoubleClick`, the `Swipe` wheel fallback, `NavigateBack` (Alt+Left) and
`Refresh` (F5) still use real input. Clicks are mostly safe — `Click()` is documented as "the
last resort: UI Automation patterns handle every click the suite performs."

### The enforcement mechanism

A `BRINELL_BACKGROUND_MODE` under which **every physical-input path throws** rather than
running. Without it, "you can keep working" holds until some control object quietly falls back
to `Mouse.MoveTo` and yanks the cursor — with no way to tell which test did it. A hard failure
naming the missing bridge verb turns that into a bug report instead of a mystery.

It is also useful **before** the bridge exists, as a measurement: run the suite with it on and
the failures enumerate every physical-input path the suite actually reaches. That list is the
build order for the verbs above, derived rather than guessed.

### Multiple apps at once

More plausible than today — separate processes, separate bridge HWNDs, no shared cursor,
clipboard or foreground. `DisableTestParallelization` could be relaxed **per collection, not
assembly-wide**, because the other half of that comment stays true: two Appium sessions still
share one emulator. It becomes a Windows-only relaxation.

### The alternative worth knowing about

A separate Windows session (RDP loopback) or a VM gives total isolation with none of this work
— its own cursor, its own foreground. It is the industrial answer. It is also heavyweight,
needs the app deployed there, and breaks attach-the-debugger. The bridge is the lighter path,
and it is needed anyway for gestures and the date pickers.

---

## Priority

Ranked by value × how blocked it is today, not by ease.

| # | Capability | Why here | Phase fit |
|---|---|---|---|
| 1 | **Focus** (A) | Removes the desktop-global dependency that serialises the whole suite | Right after gestures |
| 2 | **Text input** (B) | Removes the system clipboard from the test path | Right after gestures |
| 3 | **Date/time** (D) | Two baseline-failing controls; retires the largest workaround in the repo | Next |
| 4 | **Scroll** (E) | Deterministic, and the same MAUI API works on Android | Next |
| 5 | **Navigation** (C) | Removes desktop-wide Alt+Left and F5 | Next |
| 6 | **State reads** (I) | Fixes assertions that currently pass for the wrong reason | Opportunistic, per control |
| 7 | **Picker** (F) | High value on Android, moderate on Windows | After Android gesture routing |
| 8 | **Dialogs** (G) | Read half is valuable; dismiss half needs care | Later |
| 9 | **Menus** (H) | Only context menus are genuinely blocked | Later |

Items 1 and 2 are worth doing immediately after the gesture walking skeleton proves the
transport, because they benefit every existing test rather than only gesture tests — and
because item 1 is the one that could eventually let the suite stop being serialised.

---

## Consequences for the existing plan

1. **Amend Phase 1** to include `Exchange(int, string, out string)` in the frozen method table,
   and extend spike S2 to round-trip a string. Deferring this is the one decision in the plan
   that is genuinely hard to reverse.
2. **Rename the vocabulary.** `GestureKind` becomes `BrinellVerb` (or the gesture values become
   one range within it) before Phase 1 freezes the enum. Values are wire values — append-only,
   never renumbered — so the namespace should be right from the start.
3. **Keep the discipline test visible.** The three questions at the top of this document belong
   next to `AD-008` when it is written, or this catalogue becomes a shopping list and the bridge
   becomes the app's primary automation surface — which the design explicitly forbids.
4. **Nothing here changes the risk profile of Phase 0.** If the spikes fail, this document
   describes what the fallback channel would have to carry too.
