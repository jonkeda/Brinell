---
title: Physical Input Inventory
description: What the MAUI UI suite actually reaches for real mouse, keyboard and clipboard — measured, not guessed
status: measurement
---

# Physical Input Inventory

The deliverable of [step 3](steps.md). Nothing was fixed to produce it; it is a measurement of
which physical-input paths the suite actually reaches, so the build order for the semantic verbs
comes from evidence rather than from reading the source and guessing.

**Read the scope before the numbers.** Everything below measures
`Brinell.Samples.Maui.App` — an app written to be automated, with an `AutomationId` on
everything, the Brinell automation handlers registered, and controls chosen because they are
reachable. It is the best case, not a typical one, and it is the only app measured here.

Within that best case the framework comes out well: every control-object click goes through a UI
Automation pattern and not one falls back to the mouse. **That result does not transfer to a real
app**, and nothing in this document should be used to re-prioritise work until the same audit has
been run against one. The instrument is the deliverable; these numbers are a calibration run.

## How it was measured

`PhysicalInput.Used(callSite, replacement)` guards all 24 raw `Mouse`, `Keyboard`, `Clipboard`
and `SetForeground` calls in `Brinell.Maui.FlaUI`, grouped into 15 named entry points.

```powershell
# from the Brinell root
$env:BRINELL_BACKGROUND_MODE = "audit"
$env:BRINELL_PHYSICAL_INPUT_LOG = "physical-input.log"
dotnet test testsnew\Brinell.Maui.UITests -v:minimal /nr:false
```

`audit` records and lets the input proceed, so the suite behaves exactly as normal and one pass
yields complete data. This is why the mode has two settings rather than the one the plan
specified: `1`/`strict` throws, which aborts each test at its *first* offending call and reports
one finding per test instead of all of them. Refusing is the enforcement step 15 needs; auditing
is the instrument step 3 needs.

Full-suite audit: **183 passed, 24 failed, 209 total** — identical to the run without background
mode, confirming the instrument does not disturb what it measures. The 24 are the known baseline
(11 Stepper, 13 Shell).

## What the suite reaches

400 uses across 209 tests, across four call sites.

| Call site | Uses | Replacement |
|---|---:|---|
| `FlaUIMauiDriver.EnsureRootWindowFocused` | 201 | the Focus verb (step 13) |
| `FlaUIMauiElement.Click` | 194 | an activation pattern |
| `FlaUIMauiElement.Swipe(wheel)` | 3 | the ScrollTo verb (step 21) |
| `FlaUIMauiElement.SendKeys(Keys)` | 2 | the SetText verb (step 14) |

## What the suite never reaches

Eleven of the fifteen instrumented entry points were not reached once:

`SendKeys(Paste)` · `SendKeys(SetValue fallback)` · `Clear(Ctrl+A,Delete)` · `Submit(Enter)` ·
`DoubleClick` · `RightClick` · `Hover` · `PointerLongPress` · `PointerDrag` ·
`NavigateBack(Alt+Left)` · `Refresh(F5)`

Two of those are worth calling out.

**The clipboard is never touched.** `SendKeys(Paste)` writes the machine-wide clipboard and sends
Ctrl+V, and it was reported as one of the strongest reasons for this whole programme. The suite
does not use it. The hazard is real but latent — worth removing eventually, worth nothing now.

**`SendKeys(SetValue fallback)` is never reached either**, which means `TrySetTextValue` — the
`ValuePattern` rung — succeeds every time. Text entry is already semantic wherever the suite
exercises it.

## Attribution: all 194 clicks are one line

The three headline numbers are suspiciously close together, and they should be: they are the same
event counted three times. `MauiFixture.ReturnToHub` calls `IMauiElement.Click`, which calls
`EnsureRootWindowFocused`.

Measured directly by giving `ReturnToHub` its own marker and running the Buttons area — eleven
tests that click buttons constantly:

```
10  MauiFixture.ReturnToHub
10  FlaUIMauiElement.Click
10  FlaUIMauiDriver.EnsureRootWindowFocused
```

Ten of each, for eleven tests: one back-navigation per test, and **zero physical clicks from
control objects**. Repeated across three runs of a wider filter: 41 / 41 / 41, perfectly
correlated.

So `ClickableControlBase.ClickCore` → `ActivationHelper.TryActivateByPattern` → SelectionItem,
then Invoke — works, on every control the suite drives. The comment in `FlaUIMauiElement.Click`
calling itself "the last resort: UI Automation patterns handle every click the suite performs"
is accurate. It just was not true of the fixture line beside it.

That line is mine. `ReturnToHub` used to click through a control object and inherit the ladder;
the Option A fix in [../fix/rca-page-readiness-gate.md](../fix/rca-page-readiness-gate.md)
re-resolved the button from the app root and called `IMauiElement.Click` directly, which skips it.

## Why it is still a real click

Both obvious repairs were tried and measured, and both fail the same way.

| Attempt | On-screen | Off-screen |
|---|---|---|
| `IMauiElement.Click` (current) | stable, ~9 s | fails — clicks empty desktop |
| `InvokePattern` alone | 4 failures in 4 | 4 of 4 pass |
| Invoke, then click if it returned false | 3 failures in 4 | — |
| `ActivationHelper.TryActivateByPattern` (the shared ladder) | 1 failure in 3, runtime 5 s → 58 s | — |

The last one is the telling result: the *same shared ladder* that works for every control object
in the suite does not work on this element. Driving a MAUI `ToolbarItem` on Windows through the
Invoke pattern **reports success and does not reliably raise the command** — navigation silently
does not happen, the retry loop burns its timeout, and the run degrades from nine seconds to
around a minute with intermittent failures.

The ladder is not at fault and neither is the fix. `ToolbarItem` renders into native chrome, and
its automation peer evidently accepts an Invoke that goes nowhere.

## What this does and does not license

**It does not license re-prioritising the plan.** An earlier draft of this document argued from
these numbers that steps 13 and 14 were worth less than claimed. That was wrong: it generalised
from the one app least likely to need them. A real app under test will have controls with no
`AutomationId`, `ValuePattern` implementations that refuse writes, context menus, hover-dependent
UI and custom controls with no Invoke — every one of which lands on a path this suite never
touches. The eleven entry points that scored zero here are exactly the ones a real app is most
likely to hit.

**It does license two narrower claims**, because both are about Brinell rather than about the app:

- The activation ladder in `ClickableControlBase` → `ActivationHelper` works. When a control
  exposes SelectionItem or Invoke, Brinell uses it. That is a property of the framework and holds
  wherever the control cooperates.
- The one physical click in this suite is `MauiFixture.ReturnToHub`, and it is there because the
  semantic route is broken at the platform, not because Brinell reached for the mouse. See below.

**And it surfaces one blocker worth tracking regardless of app:** activating a MAUI `ToolbarItem`
without the mouse. No step covers it; nearest are 18 and 26.

## Running it against a real app

The instrument is not MAUI-specific. Guards are in place across the Windows drivers, so any app a
Brinell suite already drives can be audited without touching the app itself:

| Driver | Guarded entry points |
|---|---|
| `Brinell.Maui.FlaUI` | 15 |
| `Brinell.Wpf` | 11 |
| `Brinell.WinForms` | 10 (including `DateTimePicker`, which is keyboard-driven end to end) |

```powershell
$env:BRINELL_BACKGROUND_MODE = "audit"
$env:BRINELL_PHYSICAL_INPUT_LOG = "physical-input.log"
dotnet test <the real suite> -v:minimal /nr:false
```

Audit mode does not change behaviour — verified here: 183/24 with it on, 183/24 with it off — so a
real suite can be audited on a normal run with nothing at risk. Then:

```powershell
Get-Content physical-input.log | Group-Object | Sort-Object Count -Descending
```

That output, from an app that was not written to be automated, is what should set the build order
for the verbs in steps 13, 14 and 20-26. Until then those steps keep the priority the capability
catalogue gave them.

## Caveats

- **One app, and the friendliest one available.** This is the headline caveat, not a footnote.
  `Brinell.Samples.Maui.App` exists to exercise Brinell; a real app has not been measured.
- **Windows only.** The Appium path is not instrumented; Android gestures go through a different
  driver.
- **Reached, not reachable.** Eleven entry points scored zero because this suite does not exercise
  them, not because they are dead. A suite with context menus reaches `RightClick` on its first
  test.
- **No test attribution.** The log records call sites, not test names — Brinell's logger has no
  test identity to hand. Call sites are what determine build order, so this was not pursued.
- The full-suite figures were captured before `ReturnToHub` got its own marker, so its clicks
  appear there as `FlaUIMauiElement.Click`. The narrow runs above establish the attribution.
