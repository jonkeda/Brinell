# What the pointer layer in `FlaUIMauiDriver` still does by hand

## Why this exists

`FlaUIMauiDriver` carries thirteen `internal` pointer/keyboard methods that no other Brinell FlaUI
driver has. They arrived together in `5afd617` as the enforcement layer for the Windows
interaction policy — every one opened with `RequirePointerInput(action)` or
`RequireGlobalKeyboardInput(action)`, and the `action` string was the argument that named the
refusal.

[The policy was removed](../maui/decision-remove-windows-interaction-policy.md), deliberately, and
that decision closes with: *"The pointer and keyboard methods themselves stay and simply do the
input."* This audit is the follow-up. With the gates gone, most of them do nothing FlaUI does not
already do, and **every one of the thirteen still takes an `action` parameter that no body reads.**

Verdicts below are measured against the FlaUI 5.0.0 assembly, by reflection and IL, not from
memory.

**Nine of thirteen can go. Two are real and stay. Two are behaviour questions rather than wrapper
questions — `PointerClick`, and `EnsureRootWindowFocused` itself.**

## The shape to aim for already exists in this repo

[FlaUIWpfElement](../../srcnew/Brinell.Wpf/FlaUI/FlaUIWpfElement.cs) does the same job without any
of this. The element adapter calls FlaUI directly, and the driver exposes exactly one helper:

```csharp
public void DoubleClick() => _element.DoubleClick();

public void LongPress(int durationMs = 1000)
{
    var rect = _element.BoundingRectangle;
    Mouse.Position = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    Mouse.Down(MouseButton.Left);
    WaitHelper.Pause(durationMs);
    Mouse.Up(MouseButton.Left);
}
```

`FlaUIWpfDriver.EnsureRootWindowFocused()` — no parameter — is the whole surface. That is the
target.

## Verdicts

### Delete — pure pass-throughs (4)

The entire body is one FlaUI static call. The `action` parameter is discarded.

| Method | Body | Replace with |
|---|---|---|
| `GlobalType(string, string)` [:245](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L245) | `Keyboard.Type(text)` | `Keyboard.Type(text)` |
| `GlobalType(VirtualKeyShort, string)` [:250](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L250) | `Keyboard.Type(key)` | `Keyboard.Type(key)` |
| `GlobalTypeSimultaneously(string, params VirtualKeyShort[])` [:255](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L255) | `Keyboard.TypeSimultaneously(keys)` | same |
| `SetClipboardTextForInput(string, string)` [:262](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L262) | `Clipboard.SetText(text)` | same, at the call site |

`SetClipboardTextForInput` is the one with no FlaUI equivalent — FlaUI has no clipboard API at
all. It is still a static call that touches no driver state, so it does not belong on the driver.
`FlaUIWpfElement.SendKeys` inlines `System.Windows.Forms.Clipboard.SetText(text)` and reads fine.

### Delete — focus plus one FlaUI call (5)

Each is `EnsureRootWindowFocused(action)` followed by a single FlaUI call. Keeping a bare
`EnsureRootWindowFocused()` at the call site preserves the behaviour exactly and drops the
indirection.

| Method | Body after the focus call |
|---|---|
| `PointerDoubleClick(AutomationElement, string)` [:169](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L169) | `element.DoubleClick()` |
| `PointerRightClick(AutomationElement, string)` [:175](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L175) | `element.RightClick()` |
| `PointerHover(Point, string)` [:181](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L181) | `Mouse.MoveTo(point)` |
| `PointerScroll(Point, int, string)` [:202](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L202) | `Mouse.MoveTo(point); Mouse.Scroll(clicks)` |
| `FocusForGlobalKeyboardInput(AutomationElement, string)` [:239](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L239) | `element.Focus()` |

The first two are worse than neutral: they take an `AutomationElement` the caller already holds
and hand it straight back to FlaUI, so the driver is a courier for its own caller's field.

### Delete — dead (1)

`IsPointInsideRootWindow(Point, int)`
[:123](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L123) has **no callers**. Not in
`Brinell.Maui.FlaUI`, not in the tests, nowhere in the solution. It was the bounds check the
pointer gate ran before allowing physical input. `FlaUIWpfDriver` carries an identical copy
([:89](../../srcnew/Brinell.Wpf/FlaUI/FlaUIWpfDriver.cs#L89)), also uncalled — delete both.

The dangling `/// <summary>Gets the Windows interaction policy for this driver session.</summary>`
above it [:113](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L113) documents a member that no
longer exists, and now attaches to `IsPointInsideRootWindow`. It goes with it.

### Keep — FlaUI genuinely has no equivalent (2)

Drop the `action` parameter; keep the body.

**`PointerLongPress(Point, int, string)`**
[:187](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L187) — FlaUI has `Mouse.Down` and
`Mouse.Up` but nothing that holds for a duration. The press lifecycle is the contract, per
[click-activation-vs-element-gestures](../maui/click-activation-vs-element-gestures.md).

**`PointerDrag(Point, Point, int, string)`**
[:209](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L209) — `Mouse.Drag` exists and looks
like a replacement. It is not. Its IL is:

```
set_Position -> Down -> set_Position -> Up
```

Two positions, no intermediate motion — the `Position` setter teleports, unlike `MoveTo`, which
runs `Interpolation.Execute`. A WinUI drag needs the pointer-move events in between, which is
exactly what the hand-rolled stepping produces. The duration parameter has nowhere to go in
`Mouse.Drag` either. **This is the clearest keep in the file, and the reason is worth a comment,
because `Mouse.Drag` will otherwise look like an obvious simplification to the next reader.**

### Is `EnsureRootWindowFocused` still needed?

**The method is. The body is three times the size of the job.**

#### Why the concept survives the policy

Real input is positional and focus-relative, and FlaUI does not handle that for you. Verified by
IL: `AutomationElement.Click()` is `GetClickablePoint -> MoveTo -> Mouse.LeftClick`, with no
foreground call anywhere in it. A `Mouse.Down` at a screen coordinate hits whichever window is on
top at that coordinate, and `Keyboard.Type` goes to whichever window holds focus.

The keyboard path is the sharp one. `FocusForGlobalKeyboardInput` is reached by `SendKeys(Keys)`,
`Clear`, `Submit`, `NavigateBack` and `Refresh`. If the app is not in front when `Keyboard.Type`
runs, **the keystrokes land in whatever the user is doing** — not a failing test, a corrupted
document. That is the failure mode worth protecting against, and it is not hypothetical here,
because `RestoreForegroundWindow`
[:336](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L336) *deliberately puts another window
in front* at launch.

`element.Focus()` does not cover it either. Its IL decodes to:

```csharp
if (ControlType == ControlType.Window) { SetForeground(); return; }
FocusNative();   // AttachThreadInput + User32.SetFocus + UIA SetFocus
```

So for a control — an `Entry`, a `Button` — it takes the `FocusNative` branch, which sets keyboard
focus without raising the window. The explicit foreground call before it is doing real work.

Microsoft's own tooling draws the same line, quoted in
[finding-windows-foreground-activation](../maui/finding-windows-foreground-activation.md): pattern
verbs are headless-friendly, while *"like the other input-injecting verbs, `click` brings the
target to the foreground and fails fast … rather than clicking the wrong window."*

#### What is not needed: two thirds of the body

`BringRootWindowToForeground` [:267](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L267) does
the same job three times:

| Layer | Raises the window | Un-minimizes |
|---|---|---|
| `SetForegroundWindow(handle)` + `ShowWindow(handle, SW_RESTORE)` + `Pause(100)` | yes | yes |
| `WindowPattern.SetWindowVisualState(Normal)` | — | yes |
| `_rootElement.SetForeground()` — itself `User32.SetForegroundWindow` + `Wait.UntilResponsive` + `SetFocus` | yes | — |

That is not a fallback chain, it is accretion. `975c6de` **prepended** the P/Invoke block onto a
body that already had the other two layers and removed nothing. `SetForegroundWindow` is called
twice on the same handle; the window is un-minimized twice; and `Pause(100)` is a fixed cost on
every gesture where `SetForeground()`'s own `Wait.UntilResponsive` is the measured version of the
same wait.

The other two FlaUI drivers are the control group. `FlaUIWpfDriver.EnsureRootWindowFocused()`
[:113](../../srcnew/Brinell.Wpf/FlaUI/FlaUIWpfDriver.cs#L113) and
`FlaUIWinFormsDriver.EnsureRootWindowFocused()`
[:56](../../srcnew/Brinell.WinForms/FlaUI/FlaUIWinFormsDriver.cs#L56) are the middle and bottom
layers only — **zero `DllImport` in either file** — and neither has needed the rest.

#### Verdict

Keep the method, drop the parameter, and collapse the body to the WPF/WinForms shape: pattern
restore, `SetForeground()`, `Focus()` as the catch fallback. That deletes `ShowWindow`, the local
`SetForegroundWindow`, `SwRestore` and `Pause(100)`, and leaves all three FlaUI drivers with an
identical helper — worth something on its own, given how much of this file is MAUI-only drift.

`GetForegroundWindow` stays: `RestoreForegroundWindow` needs it and FlaUI's `User32` does not
expose it.

Two things this audit does **not** settle, both worth their own measurement:

- **`NavigateBack` and `Refresh` pass `_rootElement`** — a Window — to
  `FocusForGlobalKeyboardInput`, so `Focus()` takes the `SetForeground()` branch immediately after
  `EnsureRootWindowFocused` already did it. Doubled, at those two sites only.
- **`RestoreForegroundWindow` and `EnsureRootWindowFocused` now pull against each other.** The
  constructor hands the foreground back to the user's window; the first gesture takes it straight
  back and nothing hands it over again. Both are individually correct. Arbitrating between them
  was the interaction policy's actual job, and it is gone. The watchdog sketched at the end of
  `finding-windows-foreground-activation` is the option that remains.

### Not a wrapper question — `PointerClick`

`PointerClick(Point, string)` [:154](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L154) is
the one method here that is not a wrapper at all. It hand-rolls the click:

```csharp
Mouse.MoveTo(point);
Mouse.Down(MouseButton.Left);
WaitHelper.Pause(120);
Mouse.Up(MouseButton.Left);
```

against FlaUI's `AutomationElement.Click()`, whose IL is
`GetClickablePoint -> MoveTo -> Mouse.LeftClick -> Wait.UntilInputIsProcessed`. Three differences,
all of them losses:

1. **Bounding-rectangle centre instead of `GetClickablePoint()`.** The caller in
   `FlaUIMauiElement.Click()` [:267](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs#L267)
   computes the centre itself. `GetClickablePoint` asks UIA where the element can actually be
   clicked, which is the whole point of the property for obscured or non-rectangular elements.
2. **`Mouse.Down`/`Up` instead of `Mouse.Click`.** `Mouse.Click` consults `GetDoubleClickTime()`
   and sleeps when the previous click landed at the same point inside that window, so two
   successive clicks are not coalesced into a double-click. The hand-rolled version has no such
   guard — and `DoubleClickCore` clicking twice is a known open question in
   [click-activation-vs-element-gestures](../maui/click-activation-vs-element-gestures.md).
3. **No `Wait.UntilInputIsProcessed`** after the click.

The 120 ms hold has no recorded justification. It landed in the policy commit alongside the gates,
not in a fix, and no doc or test refers to it.

Two facts make this low-risk to change. `click-activation-vs-element-gestures` already describes
this path as *"a direct FlaUI element click in `FlaUIMauiElement`"* — the current code diverges
from its own documentation. And
[finding-windows-foreground-activation](../maui/finding-windows-foreground-activation.md) measured,
with a stack-trace probe, that **the pointer-click fallback is never reached** across the Buttons,
Text, Display and Toggle suites: the pattern ladder handles every click. So the change is cheap to
make and cheap to be wrong about.

**Recommendation:** `FlaUIMauiElement.Click()` becomes `_driver.EnsureRootWindowFocused();`
followed by `_element.Click();` — matching WPF, and matching the doc. If a MAUI control turns out
to need the hold, that is a finding worth its own note and its own comment, not an unexplained
constant.

## Same flavour, outside the pointer layer

Noted, not proposed — these are separate passes.

| Site | Hand-rolled | FlaUI has |
|---|---|---|
| `FindElement` [:496](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L496), `FindElements` [:519](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L519) | `while (UtcNow - start < timeout)` + `Pause(100)` | `Retry.WhileNull` / `Retry.WhileEmpty`, with `RetrySettings` |
| `TryApplyRequestedWindowPlacement` [:353](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L353) | Transform pattern move + resize | `Window.Move(x, y)` — **move only**, no resize, so the pattern path still carries the resize half |

Two that look like candidates and are not:

- **`BuildAutomationTree`** [:654](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L654) — FlaUI
  5's `FlaUI.Core.Debug` exposes only `Details(element)` and `GetXPathToElement(element, root)`.
  There is no tree dump. Keep.
- **`TryFindActiveDialogRoot`** [:810](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L810) —
  `Window.Popup` exists, but its IL is `GetMainWindow().FindFirstChild(...).AsWindow()`: first
  *child*, unfiltered. A WinUI `ContentDialog` renders as a Popup *descendant*, and the existing
  code deliberately takes the last non-offscreen one that contains a button. Keep; the comment
  already carries the reasoning.

## Removal order

Each step compiles and is separately verifiable.

1. Delete `IsPointInsideRootWindow` from `FlaUIMauiDriver` and `FlaUIWpfDriver`, with the dangling
   policy summary. No call sites, so nothing else moves.
2. Drop the `action` parameter from all thirteen methods and `EnsureRootWindowFocused`. Touches 23
   call sites — 19 in `FlaUIMauiElement`, 4 in `NavigateBack`/`Refresh` — each losing a
   `nameof(...)` argument. Mechanical.
3. Inline the four pass-throughs (`GlobalType` x2, `GlobalTypeSimultaneously`,
   `SetClipboardTextForInput`) into `FlaUIMauiElement`.
4. Inline the five focus-plus-one-call methods, keeping `EnsureRootWindowFocused()` at each site.
5. Collapse `BringRootWindowToForeground` to the WPF/WinForms body — pattern restore,
   `SetForeground()`, `Focus()` fallback. Deletes the `ShowWindow` and `SetForegroundWindow`
   P/Invokes, `SwRestore` and `Pause(100)`. **A behaviour change** — it drops one of two duplicate
   activation attempts and a fixed 100 ms per gesture, so run it on its own.
6. Replace `PointerClick` with `EnsureRootWindowFocused()` + `_element.Click()` in
   `FlaUIMauiElement.Click()`. **The other behaviour change** — also on its own.

What remains on the driver: `EnsureRootWindowFocused()`, `PointerLongPress`, `PointerDrag`.

Roughly 120 lines out across the two drivers, against a line or two added at each
`FlaUIMauiElement` call site.

## Verification

All six steps were implemented and measured together.

- **Solution builds clean.** Four warnings, all pre-existing `Application.MainPage` deprecations in
  the sample app.
- **Unit, `Brinell.Maui.Tests`: 98 passed, 0 failed, 1 skipped.** Better than the baseline in
  [decision-remove-windows-interaction-policy](../maui/decision-remove-windows-interaction-policy.md),
  which recorded five failures — those were fixed by the dialog work since.
- **UI, Buttons + Text + Display + Toggle: 71 / 72**, 11 m 29 s. The one failure,
  `EntryTests.Entry_Focus_IsReported`, **also fails with these changes stashed** — checked
  explicitly, because this pass alters focus handling and the name is exactly what a regression
  here would look like. It is the defect
  [audit-what-getattribute-can-answer](../GetAttribute/audit-what-getattribute-can-answer.md)
  already measured: `IsFocused()` returns False on Windows for a demonstrably focused control,
  because `IsFocusedCore` treats "no source answered" as "not focused".

`ProgressBar_Reset_ReturnsToInitialState`, the failure at the 69/70 baseline in
[finding-windows-foreground-activation](../maui/finding-windows-foreground-activation.md), now
passes.

One question the suites do **not** answer, because the pass/fail result is the same either way:
after step 5, does the app still come to the front as reliably? The duplicate activation and the
fixed 100 ms are gone, and only watching a run — rather than reading totals — will show it.
