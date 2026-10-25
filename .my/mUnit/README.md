# MauiUnit: where it stands

MauiUnit (named so on 2026-10-26; MUnit until then) is a headless component-testing framework for .NET MAUI, a bUnit for
MAUI. `Bruinell.Maui.MauiUnit` is a Bruinell head on top of it, a third MAUI driver beside FlaUI and
Appium. This folder tracks the work while it is under way. Started 2026-10-25.

## Files here

- [README.md](README.md): this page, with the status and the log.
- [proposal.md](proposal.md): the architecture proposal, how bUnit maps onto MAUI, the feasibility and the roadmap.
- [design.md](design.md): namespaces, API, interfaces, the head's mapping, MAUI internals and planned skeletons.
- [findings.md](findings.md): what the spikes proved about MAUI without a platform.
- [suite-count.md](suite-count.md): the device suite on the MauiUnit head, what passes and why the rest cannot; the full list is in [suite-count-failures.md](suite-count-failures.md).

## Status

| Piece | Where | State |
| --- | --- | --- |
| MauiUnit core | `srcnew/MauiUnit` | Green under its tests; Shell chrome and dialogs added |
| Sample app (XAML on plain net10.0) | `samples/MauiUnit.Samples.App` | Settings, Greeting, Products, Shell, Countdown |
| xUnit, NUnit and MSTest tests | `testsnew/MauiUnit.Tests`, `.NUnit`, `.MSTest` | 41, 2 and 2 green |
| Bruinell head | `srcnew/Bruinell.Maui.MauiUnit` | Driver, element, state, app sinks, refusals, `MauiUnitHead` |
| Head's own tests | `testsnew/Bruinell.Maui.MauiUnit.Tests` | 3 of 3 green |
| The device suite on both heads | `testsnew/Bruinell.Maui.UITests`, both sample apps compiled headless | `BRUINELL_HEAD=mauiunit`: **325 pass, 35 skip with their reason, 0 fail, in 7 s**. Unset: Windows (FlaUI), as before |
| Proposal, design, count | `.my/mUnit` | Written |

How the head is chosen, as the Html suites choose bUnit:
- `BRUINELL_HEAD=mauiunit` is read by `MauiHeads` in Bruinell.Core; anything else is the device.
- `MauiTestFixtureBase` calls the fixture's `CreateInMemoryOptions()` on MauiUnit and `CreateTestContextOptions()` on a device; `MauiFixture` and `ShellFixture` name their app's first page.
- A test that needs a device is `[DeviceFact("why")]`, or `[WindowsOnlyFact(Device = "why")]` where it is Windows only too; a scenario is tagged `@device`. They are skipped on MauiUnit with the reason.

Changes outside the new projects:
- `MauiHeads` and `DeviceFact` in Bruinell.Core, `@device` in Bruinell.Bdd;
- the head choice in `MauiTestFixtureBase`;
- the device suite references the head and both headless apps, and marks 33 tests;
- `net10.0` added to `Bruinell.Maui.AppSupport`'s targets, which the apps' XAML needs.

The work is committed on `move` and not pushed: Bruinell's `move` stays local until a push is asked
for. Tomorrow's submodule pointer has not been moved.

## Next

1. A decision record once the owner has seen the counts: a third MAUI head, its fidelity rules, and the device heads as the judge at a gate.
2. The maui-ui-test skill told when to use `DeviceFact` and `@device`, as html-ui-test was for `BrowserFact`.
3. Snapshot files (Phase 5) and fake leaf handlers for layout (Phase 6), each only once a test needs them.

## Log

- **Spike.** Plain `net10.0` xUnit can make MAUI controls and run bindings, `NavigationPage`, modals and Shell with no handler. Findings in [findings.md](findings.md).
- **Host decided.** Each render goes into a `Window` opened in an in-memory `Application` (internal `AddWindow`). That is enough for MAUI to run its own page lifecycle.
- **Dispatcher decided.** One queue per context, drained at every MauiUnit call, with a virtual clock for timers and delayed work, so results are deterministic.
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
- **Second count: 220 of 354 pass.** The run took 3 min, almost all of it the failing tests waiting out their timeouts.
- **Brinell became Bruinell.** The rename (c9d220b6, another session) carried every MauiUnit project over; all green after it.
- **Head gaps closed, run 3: 278.**
  - State is answered as the Windows bridge answers it.
  - Refusals are Bruinell's exceptions.
  - The scroll routes succeed at once.
  - A Stepper publishes its two buttons.
  - The app's own verb sinks are called in process.
  - MauiUnit can swipe.
- **Shell and hidden elements, run 4: 295.**
  - The Shell sample app is compiled headless.
  - MauiUnit's tree shows Shell's tab bar and flyout.
  - The head leaves `IsVisible="False"` elements out of lookups, as every device tree does.
- **Sweeps, focus, carousel, run 5: 308.** The big one: a control that misses sweeps by asking `AppElement.TryFindByScrolling`, and the head answered that app-wide, so a container's miss became a hit outside it. Now it answers null, since nothing is off screen. Focus also works, through MAUI's own `IsFocused` key, and a carousel shows one card.
- **Dialogs, run 6: 319 in 6 s.**
  - Without a platform, MAUI keeps `DisplayAlertAsync`, `DisplayPromptAsync` and `DisplayActionSheetAsync` on the page.
  - MauiUnit takes them from there and shows a `TestDialog` over the page, whose buttons answer the app's await.
  - Off-screen carousel cards stay in lookups, as on Windows.
  - Failing tests now refuse at once, so the run went from 2 min to 6 s.
- **The head choice, 2026-10-26.**
  - `BRUINELL_HEAD=mauiunit` replaces the static seam, and the device suite runs on either head itself, so the linked project `Bruinell.Maui.UITests.Munit` is gone.
  - The 33 device-only tests are marked and skip with their reason: 325 pass, 35 skip, 0 fail, in 7 s. `FlaUIErrorsTests` now runs too.
  - Unset, the Windows head runs as before: `ButtonTests`, and two FlaUI-only tests, passed on the built Windows app.
- **Named MauiUnit, 2026-10-26.** Everything moved to the new name:
  - the framework: `srcnew/MauiUnit`, namespace and package `MauiUnit`;
  - its tests and sample: `MauiUnit.Tests`, `.NUnit`, `.MSTest`, `MauiUnit.Samples.App`;
  - the head: `Bruinell.Maui.MauiUnit` with `MauiUnitDriver`, `MauiUnitElement` and `MauiUnitHead`, and its tests;
  - the head value: `BRUINELL_HEAD=mauiunit`, with no alias for `munit`.

  This folder keeps its name, `.my/mUnit`. All suites are green after the rename, and the device suite gives 325 pass and 35 skip on MauiUnit, 7 of 7 on the Windows spot check.
