# MauiUnit: where it stands

MauiUnit (named so on 2026-10-26; MUnit until then) is a headless component-testing framework for .NET MAUI, a bUnit for
MAUI. `Bruinet.Maui.MauiUnit` is a Bruinet head on top of it, a third MAUI driver beside FlaUI and
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
| Bruinet head | `srcnew/Bruinet.Maui.MauiUnit` | Driver, element, state, app sinks, refusals, `MauiUnitHead` |
| Head's own tests | `testsnew/Bruinet.Maui.MauiUnit.Tests` | 3 of 3 green |
| The device suite on both heads | `testsnew/Bruinet.Maui.UITests`, both sample apps compiled headless | `BRUINET_HEAD=mauiunit`: **325 pass, 35 skip with their reason, 0 fail, in 12 s**. Unset: Windows (FlaUI), as before |
| The Todo sample on both heads | `samples/Todo/tests/Bruinet.Samples.Todo.Uat`, `.UITests`, `samples/Todo/src/Bruinet.Samples.Todo.App.Headless` | `BRUINET_HEAD=mauiunit`: UAT **42 of 42 scenarios** (1 `@service` skips) in about 30 s; UI tests **44 of 44** (3 live-API skips) in about 18 s; green in 5 runs running. Unset: Windows, smoke and two scenarios checked |
| Proposal, design, count | `.my/mUnit` | Written |

How the head is chosen, as the Html suites choose bUnit:
- `BRUINET_HEAD=mauiunit` is read by `MauiHeads` in Bruinet.Core; anything else is the device.
- `MauiTestFixtureBase` calls the fixture's `CreateInMemoryOptions()` on MauiUnit and `CreateTestContextOptions()` on a device; `MauiFixture` and `ShellFixture` name their app's first page.
- A test that needs a device is `[DeviceFact("why")]`, or `[WindowsOnlyFact(Device = "why")]` where it is Windows only too; a scenario is tagged `@device`. They are skipped on MauiUnit with the reason.

Changes outside the new projects:
- `MauiHeads` and `DeviceFact` in Bruinet.Core, `@device` in Bruinet.Bdd;
- the head choice in `MauiTestFixtureBase`;
- the device suite references the head and both headless apps, and marks 33 tests;
- `net10.0` added to `Bruinet.Maui.AppSupport`'s targets, which the apps' XAML needs.

The work is pushed (2026-10-26, e64b2cc7): `move` on origin and jonkeda/bruinet (its `main` too), and
Tomorrow's submodule pointer on `game`.

## Next

1. A run of the three suites on a real Mac (`dotnet workload install maui`, then `BRUINET_HEAD=mauiunit`).
2. A CI step on `windows-latest` with `BRUINET_HEAD=mauiunit`; a Linux job after the Mac run.
3. Snapshot files (Phase 5) and fake leaf handlers for layout (Phase 6), each only once a test needs them.

Done 2026-10-26: the decision record (`.docs/decisions/ad-032-a-maui-suite-runs-without-a-device.md`)
and the maui-ui-test skill's "Two heads" section (`DeviceFact`, `@device`, what needs a device).

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
- **Bruinet head green.** No change to Bruinet.Maui was needed. Visible elements report nominal 1x1 bounds.
- **The device suite linked unchanged.** The whole sample app compiled headless at the first try. The suite needed `net10.0-windows` like the original, and only `FlaUIErrorsTests` is left out.
- **First count: 10 s per test.** The cause was a head bug: `CurrentRoute()` named the page by type, not by `AutomationId`, so `WaitArrived` timed out on every `Open`. After the fix, `ButtonTests` took 0.1 s instead of 60 s.
- **Second count: 220 of 354 pass.** The run took 3 min, almost all of it the failing tests waiting out their timeouts.
- **Brinell became Bruinet.** The rename (c9d220b6, another session) carried every MauiUnit project over; all green after it.
- **Head gaps closed, run 3: 278.**
  - State is answered as the Windows bridge answers it.
  - Refusals are Bruinet's exceptions.
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
  - `BRUINET_HEAD=mauiunit` replaces the static seam, and the device suite runs on either head itself, so the linked project `Bruinet.Maui.UITests.Munit` is gone.
  - The 33 device-only tests are marked and skip with their reason: 325 pass, 35 skip, 0 fail, in 7 s. `FlaUIErrorsTests` now runs too.
  - Unset, the Windows head runs as before: `ButtonTests`, and two FlaUI-only tests, passed on the built Windows app.
- **Named MauiUnit, 2026-10-26.** Everything moved to the new name:
  - the framework: `srcnew/MauiUnit`, namespace and package `MauiUnit`;
  - its tests and sample: `MauiUnit.Tests`, `.NUnit`, `.MSTest`, `MauiUnit.Samples.App`;
  - the head: `Bruinet.Maui.MauiUnit` with `MauiUnitDriver`, `MauiUnitElement` and `MauiUnitHead`, and its tests;
  - the head value: `BRUINET_HEAD=mauiunit`, with no alias for `munit`.

  This folder keeps its name, `.my/mUnit`. All suites are green after the rename, and the device suite gives 325 pass and 35 skip on MauiUnit, 7 of 7 on the Windows spot check.
- **The Todo sample, 2026-10-26.** The Todo app (a Shell, a backend over HTTP, SQLite, the toolkit) runs its UAT and UI suites in memory: 42 of 42 scenarios and 44 of 44 tests, with only the `@service` and live-API skips. It took, in order:
  - the app's registrations moved from `MauiProgram` into `TodoAppServices.AddTodoApp`, which `MauiProgram` and the fixture both call, so the in-memory app is composed as the device app is;
  - `TodoAppFixtureBase.CreateInMemoryOptions`: the same arrangement (database, backend, network-state and reset files), the launch settings handed to the app directly, MauiUnit's dispatcher as the main thread;
  - the toolkit's assembly loaded before the first page parses, as `UseMauiCommunityToolkit()` does on a device (Debug parses XAML at run time and only knows loaded namespaces);
  - an `IAnimationManager` in MauiUnit, whose ticker reports animations off, so a toolkit state change or a fade ends at once instead of throwing;
  - the head's timeouts back to the device fixture's (a backend answers after two seconds; the 500 ms ones were a wrong assumption);
  - `UatRuntime.CreateScope` from the fixture's composition as it is now, not as it was when the runtime was made: a restart in `BeforeFile` had left every later scenario on the old, disposed app. That one is a Bruinet.Uat bug on any head;
  - a back button and a title in MauiUnit's tree (`NavigationBackButton`, `NavigationTitle`), as the title bar shows them, found by the head under WinUI's ids;
  - an element counts as visible only while it is on the driver's window, on the shown page: a container that cached a gone page's element read stale text;
  - `IsIdle` runs the app's work until it is quiet for a moment, and the Todo fixture lets the app settle before resetting the backend, so a test's sync no longer finishes in the next one;
  - the test files (network state, reset and its answer) replaced with a short retry, as the app's in-process reads met the moves often enough to fail a run; the swallowed reset answer was the intermittent "started reset, never done".
- **`MauiPlatform.MauiUnit`, 2026-10-26.** The head reports a platform of its own instead of Windows, and so does the fixture's `Platform` on that head.
  - Where a test asked "Windows?" for a reason that holds in memory too (the Todo filter and in-place reset, the parallelism probe's window handle), it now says Windows or MauiUnit.
  - `MauiDriverFactory` refuses MauiUnit with why: the fixture renders it, there is no app to launch.
  - `Bruinet.Maui.UITests.Mobile` links the fixtures, so it references the head and both headless apps too; it had not built since the head choice. On MauiUnit it runs 267 pass, 36 skip.
  - Counts unchanged: the device suite 325 and 35, Todo UAT 42 of 42, UI tests 44 of 44.
- **Off Windows, 2026-10-26.** The device suite and both Todo suites build on a Mac or Linux as plain `net10.0`, with MauiUnit and Appium and no FlaUI.
  - `BruinetWindowsHead` in `Directory.Build.props` is true on Windows; the three projects choose `net10.0-windows` and the FlaUI reference by it, and off Windows the device suite leaves out `Tests/Diagnostics` and `Tests/Background`, as the mobile project does.
  - `Bruinet.Maui.AppSupport` lists its Windows target only on Windows, as MAUI's template does, so the headless apps restore elsewhere.
  - Checked here with `-p:BruinetWindowsHead=false`: the device suite 271 pass and 36 skip, Todo UAT 42 of 42, UI tests 44 of 44. The Windows build is unchanged (325 in memory, FlaUI's ButtonTests 17 of 17). Not yet run on a real Mac: the MAUI workload's restore there is the part this machine cannot show.
