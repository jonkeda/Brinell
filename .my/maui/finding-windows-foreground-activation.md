# Finding: why the Windows app comes to the foreground during a run

## Question

The sample app takes the foreground when a run starts, and appears to keep jumping forward
during the run. Is that needed?

## Answer

**No — and Brinell never asks for it.** Every deliberate foreground path funnels through
`FlaUIMauiDriver.EnsureRootWindowFocused` → `RequireForegroundActivation`, and each of its nine
call sites sits behind `RequirePointerInput` or `RequireGlobalKeyboardInput`. The shipped
policy sets `allowForegroundActivation: false`, so those paths **throw** rather than raise the
window. A probe on `BringRootWindowToForeground` never fired across a full Buttons run.

There are two real causes, both outside those paths.

### 1. Process launch (fixed)

`Process.Start(UseShellExecute = true)` — Windows grants a freshly launched process the
foreground. Unavoidable at the moment of launch, but nothing handed focus back, so the app then
kept it for the whole run.

`RestoreForegroundWindow` now captures the foreground window before launching and hands it back
once the main window is attached. Verified: `SetForegroundWindow` returns `True` and the
foreground really does return to the prior window. The app window stays *shown*, only unfocused,
so its layout and bounding rectangles remain valid for UI Automation. Skipped under interactive
mode, where physical input is allowed and genuinely needs the app in front.

### 2. `InvokePattern` on a button (not fixed — app-side)

Measured, with a 400 ms settle after each `Invoke`:

```
[RESTORE] SetForegroundWindow returned True; foreground now 3869352; wanted 3869352
[FG] Invoke on 'Open_Text'    CHANGED foreground 3869352 -> 4200138
[FG] Invoke on 'Open_Buttons' CHANGED foreground 3869352 -> 8983618
```

Invoking a WinUI button activates its window — the Invoke provider focuses the control, and
focusing a control in a background window brings that window forward. This is WinUI's behaviour,
not a Brinell call; there is no `SetForegroundWindow` anywhere on that path. Once it happens the
app stays in front, because nothing takes focus back.

## Ruled out by measurement

- **`ValuePattern.SetValue`** — 45 calls in a Text run, foreground unchanged every time. Text
  entry is not what raises the window.
- **A physical-click fallback** — `FlaUIMauiElement.Click()` calls FlaUI's real-mouse
  `_element.Click()` and does bypass the pointer policy, but a stack-trace probe showed it is
  never reached: the pattern ladder handles every click in the Buttons and Text suites. Worth
  routing through `PointerClick` on principle, since it is an unguarded hole in the policy.
- **Repeated app launches** — the `[Collection("Maui")]` fixture works; a launch counter showed
  exactly one launch per run, not one per test class.

## Remaining option, if the residual jump matters

Cause 2 cannot be fixed synchronously: the activation lands *after* `Invoke()` returns, so a
check straight after it sees nothing, and waiting 400 ms per click is far too expensive. The
mechanism that would work is a watchdog — a low-frequency poll that pushes the app back whenever
it grabs the foreground while the policy denies foreground activation. Safe by construction
(under semantic policy nothing needs focus) but it does mean continuously fighting the app for
activation, so it is a deliberate choice rather than an obvious default.

## Verification

Buttons + Text: **38 / 39**, unchanged from before the fix. The single failure is the
pre-existing `SearchBar_IsVisible_ReturnsTrue`.

---

## Follow-up: can we drop `Invoke`, or click via window messages instead?

### Do we need `InvokePattern`? Yes — it is the *least* intrusive option available

Microsoft's own `winappCli` documents the exact split we rely on:

> Everything else — `inspect`, `search`, `get-property`, `get-value`, `wait-for`, `set-value`,
> **`invoke`**, `scroll`, `screenshot` — drives the app through UIA patterns and is
> **headless/locked-session friendly**.

> Like the other input-injecting verbs, **`click` brings the target to the foreground** and
> fails fast … rather than clicking the wrong window.

So `Invoke` is the foreground-*free* path and `click` is the one that requires foreground by
design. Dropping the Invoke rung sends every click to `ClickCore`'s fallback,
`IMauiElement.Click()` → FlaUI's `AutomationElement.Click()`, which is real mouse input: it takes
over the user's cursor and needs the window in front and unobstructed. That is strictly more
intrusive, not less.

### Window messages instead of UIA? Measured, and it does not work

Tried, because posted messages do not activate a window and would have sidestepped the whole
problem.

**Per-control messages are impossible.** Every MAUI control reports no window handle of its own:

```
[HWND] id=TestButton    type=Button nativeHwnd=0 class=Button
[HWND] id=ResetButton   type=Button nativeHwnd=0 class=Button
[HWND] id=Open_Buttons  type=Button nativeHwnd=0 class=Button
[HWND] id=BackToHub     type=Button nativeHwnd=0 class=AppBarButton
```

WinUI 3 renders XAML as composition visuals inside a single top-level HWND, so there is nothing
for `BM_CLICK` or `WM_LBUTTONDOWN` to address. That is a Win32/WinForms-era assumption —
[Brinell.WinForms](../../srcnew/Brinell.WinForms/) is where it would still pay off.

**Coordinate-based messages to the top-level window do not reach the control.** Posting
`WM_MOUSEMOVE` + `WM_LBUTTONDOWN` + `WM_LBUTTONUP` at the element's centre, in client
coordinates, in place of `Invoke`:

```
[MSG] id=Open_Buttons topHwnd=8263402 client=(560,157) fgChanged=False
Failed! - Failed: 11, Passed: 0
```

The appealing half held — `fgChanged=False`, messages never activate the window — but **0 of 11
Buttons tests passed**. WinUI routes input through the ContentIsland/InputSite pointer pipeline,
so hand-posted messages are never translated into XAML pointer events. The repeated
`Open_Buttons` lines are the readiness poll retrying a click that silently did nothing.

FlaUI offers no message-based click either; its `Click()` is deliberately mouse input.

### Conclusion

Keep the Invoke rung. The ladder as it stands already picks the only foreground-free way to
activate a control, and it matches what Microsoft's own automation tooling does.

`SelectItemPattern` is a separate question and *not* answered here — being a pattern, it costs no
foreground activation, so it is a simplification question rather than an intrusion one. The
experiment that would settle it was stopped before it ran. Worth noting that Android already
hard-disables `SupportsSelectionItemPattern` with no failures, so the rung is at least not
load-bearing there.
