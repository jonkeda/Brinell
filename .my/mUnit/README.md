# MUnit: where it stands

MUnit (a temporary name) is a headless component-testing framework for .NET MAUI, a bUnit for
MAUI. `Bruinell.Maui.Munit` is a Bruinell head on top of it, a third MAUI driver beside FlaUI and
Appium. This folder tracks the work while it is under way. Started 2026-10-25.

## Files here

- [README.md](README.md): this page, with the status and the log.
- [proposal.md](proposal.md): the architecture proposal, how bUnit maps onto MAUI, the feasibility and the roadmap.
- [design.md](design.md): namespaces, API, interfaces, the head's mapping, MAUI internals and planned skeletons.
- [findings.md](findings.md): what the spikes proved about MAUI without a platform.
- [suite-count.md](suite-count.md): the device suite on the MUnit head, what passes, what is device-only and what breaks; the full list is in [suite-count-failures.md](suite-count-failures.md).

## Status

| Piece | Where | State |
| --- | --- | --- |
| MUnit core | `srcnew/MUnit` | Green under its tests |
| Sample app (XAML on plain net10.0) | `samples/MUnit.Samples.App` | Settings, Greeting, Products, Shell, Countdown |
| xUnit, NUnit and MSTest tests | `testsnew/MUnit.Tests`, `.NUnit`, `.MSTest` | 30, 2 and 2 green |
| Bruinell head | `srcnew/Bruinell.Maui.Munit` | Driver, element, `MunitHead.AsBruinellContext()` and `MunitHead.InMemory()` |
| Head's own tests | `testsnew/Bruinell.Maui.Munit.Tests` | 3 of 3 green |
| The device suite on the head | `testsnew/Bruinell.Maui.UITests.Munit`, `samples/Bruinell.Samples.Maui.App.Headless` | 220 of 354 pass (71% of the 308 that can run here); 20 ms per passing test |
| Proposal, design, count | `.my/mUnit` | Written |

Changes outside the new projects, both small:
- `MauiTestFixtureBase.InMemoryHead`, a static seam that makes a fixture's context in memory instead of launching the app;
- `net10.0` added to `Bruinell.Maui.AppSupport`'s targets, which the app's XAML needs.

The work is committed on `move` and not pushed: Bruinell's `move` stays local until a push is asked
for. Tomorrow's submodule pointer has not been moved.

## Next

1. Close the head gaps: read state, semantic gestures, refusal types, Stepper parts, scroll routes. They are worth about 65 tests.
2. Compile the Shell app headless, which adds 17 tests.
3. A proper head choice in the fixture base (`BRUINELL_HEAD=munit`), replacing the static seam.
4. The UAT scenarios on both heads.

## Log

- **Spike.** Plain `net10.0` xUnit can make MAUI controls and run bindings, `NavigationPage`, modals and Shell with no handler. Findings in [findings.md](findings.md).
- **Host decided.** Each render goes into a `Window` opened in an in-memory `Application` (internal `AddWindow`). That is enough for MAUI to run its own page lifecycle.
- **Dispatcher decided.** One queue per context, drained at every MUnit call, with a virtual clock for timers and delayed work, so results are deterministic.
- **Core written.** Hosting, rendering, the tree (`VisualNode`), queries, interactions, assertions and the snapshot formatter.
- **XAML confirmed.** A MAUI library with `UseMaui` on plain `net10.0` compiles XAML.
- **`Application.Current`.** It is set to the context's app while that context's work runs, under one lock, so `Shell.Current` works. This is on by default; `SetApplicationCurrent = false` turns it off.
- **xUnit suite green (30).** Fixes found by it:
  - Shell's `ContentTemplate` pages are created on request;
  - a Shell root gets a no-op `HeadlessHandler` carrying the services;
  - rendering an instance now also fixes the services.
- **NUnit and MSTest green.** Plain-exception assertions fail the test with their message in both.
- **Bruinell head green.** No change to Bruinell.Maui was needed. Visible elements report nominal 1x1 bounds.
- **The device suite linked unchanged.** The whole sample app compiled headless at the first try. The suite needed `net10.0-windows` like the original, and only `FlaUIErrorsTests` is left out.
- **First count: 10 s per test.** The cause was a head bug: `CurrentRoute()` named the page by type, not by `AutomationId`, so `WaitArrived` timed out on every `Open`. After the fix, `ButtonTests` took 0.1 s instead of 60 s.
- **Second count: 220 of 354 pass.** The groups are in [suite-count.md](suite-count.md). The run takes 3 min, almost all of it the failing tests waiting out their timeouts.
