# Test selection across Bruinet's stacks: what the samples and suites tell us

The second paper on test selection, 2026-10-25. The first, [paper.md](paper.md), proposed a
touch map from Bruinet's call log, widening rules and the full suite at gates, with Tomorrow's
Blazor suite in mind. This one reads Bruinet's own suites and the Todo sample, on every stack,
to test that proposal and to see whether it carries over to MAUI, Android, WPF, WinForms and
Stride.

## Outcome

It carries over, and it pays more off the web than on it. A web test costs about a second; a
MAUI test on Android costs four to five, and a new app session twenty. Three findings change the
first paper's plan:

- **The cost is app starts, not tests.** On every desktop and mobile stack the app starts once
  per collection or data set, so selection must count starts. Picking three tests from three
  data sets can cost more than running one whole set.
- **Record on the cheap head, select on the expensive one.** MAUI's Android suite compiles the
  Windows suite's sources, and Todo's heads share page objects. A touch map recorded on Windows
  or the web head selects the same tests on Android.
- **The call log must name the xUnit test.** Today it names the test context, which is one per
  test only on Html. On MAUI, Android, WPF, WinForms and Stride the context is shared by a
  collection, so the log cannot say which test touched what.

The Todo sample already holds most of the parts: journey traits on every tier, `Gate=Smoke` and
`Gate=Review`, a catalog that reads the traits by reflection, and an orderer that groups
scenarios by data set. The framework's own suites have none of that.

## What the suites cost

From the run records in [`.my/reviews/baseline.md`](https://github.com/exact-mobile-apps/test-tooling/blob/37f180a7/.my/reviews/baseline.md), `.my/androidnative/plan.md` and the suites'
`TestResults`, on a busy development machine:

| Suite | Tests | Wall time |
| --- | --- | --- |
| Html, Blazor (Playwright, browser per test) | 26, 34 | 28-32 s, 35-41 s |
| Todo web UI tests, scenarios | 11, 34 | 15-26 s, 55-105 s |
| MAUI on Windows (FlaUI, 2 collections) | 358 | 1 m 48 s to 3 m 46 s |
| MAUI on Android (Appium, one emulator) | 303 | 22 m 36 s to 24 m 01 s |
| Native Android (Appium) | 30 | 3 m 9 s fresh; 11-15 min on an emulator up two hours |

Per test, that is about a second on the web, half a second on Windows MAUI (many small control
tests on a running app) and four to five seconds on Android, where a call's mean is 549 ms and a
lost UiAutomator2 session costs 20 s to replace. A developer who runs the Android suite twice an
hour loses most of the hour to it; selection there is worth more than anywhere else.

## How each stack starts and resets its app

| Stack | App start | Between tests | In the test process |
| --- | --- | --- | --- |
| Html, Blazor | A separate process once per assembly; a new browser per test | Navigate, `AssertIdle` | No |
| MAUI Windows | FlaUI launches the exe per collection (`Maui`, `Shell`) | `ReturnToHub`, a fresh page pushed | No |
| MAUI Android, iOS | One Appium session per collection, serial | The same | No |
| Native Android | One Appium session for the suite | A deep link to the screen | No |
| WPF, WinForms | FlaUI per collection, serial | Navigate through the shell | No |
| Stride | The game exe with `--automation`, a named pipe | none found | No |
| Todo MAUI | One app per data set (seven collections), each with its own SQLite, WireMock and pinned clock | Backend reset, back to the list; `StartOver` restores the database | No |

Two things follow. First, no Bruinet suite runs its app inside the test process (Tomorrow's does,
and Todo's integration tier does), so the first paper's coverage option is out of reach here
without collecting coverage from a child process. The touch map is the method that works on every
stack. Second, the reset between tests is already cheap everywhere except a data set change, so
a selected run's time is roughly *starts × start cost + tests × test cost*.

## The touch map on each stack

The touch map needs two halves: which page objects and controls a test used, and which source
files a page object stands for.

**The first half is the same everywhere,** because every stack's calls go through `ControlCall`
with a page name and a control id, and `CsvTestLogger` already writes them when
`BRUINET_CALL_LOG` is set. The gap is the test's name: the logger takes the context's name, so on
a collection-shared app every row says the same thing. An `AsyncLocal` current-test name, set by
a Bruinet xUnit hook around each test and read by `ControlCall`, closes it on every stack at once.
UAT scenarios have a second source already: their `__result.json` lists each step's text and
command id.

**The second half differs by stack**, and each has a static way in, by the automation ids the page
objects already use:

| Stack | Page object to source | Unknown files widen to |
| --- | --- | --- |
| Blazor | Route to `.razor` and its children | The suite |
| MAUI | `AutomationId` to the `.xaml` and its code-behind; Shell route to page | The app's suite; `App.xaml`, styles, `MauiProgram` to all |
| WPF | `AutomationProperties.AutomationId` to `.xaml` | Resources, `App.xaml` to all |
| WinForms | Control `Name` to the `.Designer.cs` | The form's tests |
| Native Android | `android:id` to the layout XML and its activity | Manifest, styles to all |
| Stride | none: scenes and scripts have no ids to read | Always the suite |

A shared view-model project, such as `Bruinet.Samples.Shared` or Todo's `Core`, is code the map
does not reach through the UI. Todo shows the answer: its rules are proved in the unit tier
(70 tests, 114 cases), and the UI tier checks one example per behaviour. A change in `Core` runs
the unit tier always and the UI tests whose journeys it serves, by the `Journey` trait.

## Journeys as the coarse map

Todo traces every test to a journey subtask (`[Trait("Journey", "TOD.02.3")]`) on every tier, and
`JourneyCoverage` reads them all by reflection. That is a selection map that needs no recording:
a change to a feature's page selects the journeys that page belongs to, and `--filter
Journey=TOD.02*` runs them on any head. Tomorrow names its tests by feature ID in the same spirit.

So the order is: journeys (or feature IDs) as the coarse map every suite can have from day one,
the touch map as the fine one that follows. Bruinet's own suites use `Pattern=` and `Method=` traits
instead, which say what a test proves about Bruinet, not which screen it uses; for them the touch
map is the only source.

## Cost-aware order

Selection is half the saving; order is the other half, and it is cheaper on the costly stacks:

- **Group by start.** `BddScenarioOrderer` already sorts Todo's scenarios by data set so the app
  restarts once per set. The same grouping belongs in the UI tests' runner: selected tests in
  collection order, one start each.
- **Last failed first, within the group.** A failing test found in the first ten seconds of an
  Android run is worth more than a selection that saves five minutes.
- **Keep the app warm.** `BddWarmRunners` keeps one runner and its fixture ready per suite between
  runs; with Todo's fixture on Windows the app stays up. A selected run on a warm app is a few
  seconds even on MAUI. Extending warm runners to the UI test projects, not only scenarios, is the
  biggest inner-loop saving on desktop.
- **Weigh by duration.** MAUI's `TestTiming` writes per-class times with a baseline, and every
  scenario's `__result.json` has `durationMs`. When a selection is too wide, the short tests of the
  most-touched pages go first.

## The gate on the expensive stacks

The full suite stays the gate, but not every gate needs every head:

- **Windows and web run the full suite at every gate,** since minutes are affordable and they
  refresh the touch map.
- **Android and iOS run the selection at a gate, and the full suite nightly or before a
  release,** on a freshly started emulator (native Android's runs went from 3 to 15 minutes on an
  emulator up two hours). This is Google's TAP pattern: select per change, catch up in batches.
- **Shard the mobile gate across emulators,** since the Android suite runs serially on one device;
  two emulators, two collections, about half the time.
- **Measure misses per head.** A test selected from a Windows-recorded map that fails on Android
  only is a miss of the map, not of selection; it widens that head's rules.

## What the samples need first

- Bruinet's own suites have no `Gate=Smoke`; give each UI suite a smoke set of a few tests, as Todo has.
- `samples/README.md` describes a WinForms suite that no longer exists, and `samples/TestRunner.ps1`
  starts a Stride exe from an old path; neither helps a developer pick tests.
- Todo's UI suite turns parallelism off and notes that turning it on is "a separate, measured
  change"; with seven data-set collections it is the natural first measurement.

## Proposed order of work

This replaces step 3 of the first paper's order; steps 1, 2 and 4 stand.

1. The current-test name in `ControlCall`, on every stack, so the call log is keyed by test.
2. A touch map writer over that log, and a page-to-source reader per stack, Blazor and MAUI first
   (Tomorrow's suite and the costly head), then native Android, WPF, WinForms.
3. `bruinet affected` with start-grouped, last-failed-first order, the widening config per suite,
   and a `--head` that applies a map recorded on another head.
4. Warm runners for UI test projects, so a selected run on desktop skips the app start.
5. A smoke set in every Bruinet UI suite; a nightly full mobile run, sharded, on a fresh emulator.

## What is unknown

- How often the Windows map misses on Android: run both heads' full suites with the map on and
  compare the sets for the same commits.
- Whether automation ids are unique enough to map back to one XAML or layout file; shared
  templates and custom controls will map to many.
- What a warm MAUI app costs in drift between runs (state left from the last run), and whether
  `StartOver` is enough to make it safe.
