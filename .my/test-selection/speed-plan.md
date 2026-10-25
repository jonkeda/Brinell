# Faster studio tests: timings first, then one change per small set

Agent document, 2026-10-25. Builds [paper.md](paper.md) (the touch map) and [workflow.md](workflow.md) (selection from the
diff). Status: T1 built; iterations 1 to 4 done; the full studio suite measured on both heads (below): 1 m 29 s on bUnit, 3 m 15 s in the browser. Next: the serial collection, then selection on the touch map.

## What the last full run showed

`Tomorrow.Studio.UITests` on the bUnit head, `game` e2a97b7a plus the marks of `wip/bunit-marks`, 2026-10-25 14:08:

| Measure | Value |
| --- | --- |
| Outcome | 167 passed, 4 not run (`UiFact(Browser)`), 1 failed (PLT.40.4); 18 scenarios skipped as `@browser` |
| Wall | 11 m 15 s |
| xUnit time summed | 3,794 s |
| The scenarios' own time summed (`__result.json` `durationMs`) | 112 s, median 0.5 s |
| A scenario's xUnit test, parallel collections A to J | median 20 to 33 s |
| A scenario's xUnit test, the serial collection | median 6 s |

About 97 % of the run is outside the scenarios' steps. Each scenario costs about 6 s around its steps when it runs alone
(serial), and four to five times that with ten collections on the machine: something CPU-bound runs per scenario. The
call log cannot say what, because it logs only control calls and names no test.

## The loop

The full suite runs twice: once at the start (done, above) and once at the end, on both heads. Between them:

1. **Measure** a small set with the timings (T1): one collection's scenarios, or the few a change touches.
2. **Find** the biggest span the report names.
3. **Change** one thing.
4. **Run the same small set** and compare its report with the last one. Keep the change if it is faster and green.
5. Record the row under "Iterations" below, then the next span.

| Set | Filter | Why |
| --- | --- | --- |
| S1, one parallel collection | `FullyQualifiedName~TomorrowUatScenariosA` | Ten scenarios under one collection's own overhead |
| S2, two collections | `FullyQualifiedName~TomorrowUatScenariosA\|FullyQualifiedName~TomorrowUatScenariosB` | Whether a cost grows with parallel collections |
| S3, the serial collection | `FullyQualifiedName~TomorrowUatScenariosSerial` | The settings and quests resets, run alone |
| S4, the xUnit tests | `FullyQualifiedName~Tomorrow.App.UITests.Tests.` | Sign-in and the old-link pages, 24 tests |

All sets run with `BRINELL_HEAD=bunit` while looking, then the change once in the browser head on the same set.

## T1: timings that name the test, the step and the time between calls

Brinell (`srcnew/Brinell.Core`, `Brinell.Uat`):

| Piece | Where | What |
| --- | --- | --- |
| `TestScope` | `Brinell.Core/Testing/TestScope.cs` | `AsyncLocal` run id, head, test (xUnit display name), scenario, step; `TestScope.Current` |
| xUnit hook | `UiFact`'s runner and `UatScenarioTestBase` | Sets the test; the scenario runner sets scenario and step around each |
| `Timing.Span(kind, name)` | `Brinell.Core/Logging/Timing.cs` | `IDisposable`; writes one row on dispose: kind (`host`, `fixture`, `lifecycle`, `arrange`, `navigate`, `bind`, `step`), name, start, duration |
| Call phases | `ControlCall.Run`, `ControlCalls` | On exit: readiness wait, lookup, action, confirmation in ms, and attempts, from the attempt log |
| One file per run | `BRINELL_TIMINGS=<folder>` | `timings-<run>.csv`, one row per call or span, columns `Run;Head;Test;Scenario;Step;Kind;Name;Page;Control;Action;StartMs;DurationMs;ReadyMs;LookupMs;ActMs;ConfirmMs;Attempts;Result` |
| Spans Brinell owns | the UAT runner | `bind` (parse and bind a scenario), `lifecycle` (`BeforeScenario`, `AfterScenario`), each `step` |
| Spans the contexts own | `PlaywrightTestContext`, `BunitTestContext` | `navigate` (`NavigateTo`, `Refresh`, `ResetAppState`) |
| Report | `tools/Scripts/Timings.ps1` (or `brinell timings`) | Per test: total, in steps, in calls, outside calls; per span kind: sum and top 10; per step: slowest 20 |

Tomorrow (`tests/Tomorrow.App.UITests`, shared with the studio's suite):

| Span | Where |
| --- | --- |
| `host` | `TomorrowApp.StartAsync`: database prepared, host started (process or in-process), geo import, `GenerateArea` |
| `fixture` | `TomorrowAppFixture.InitializeAsync` |
| `arrange` | each `Put*`, `SignInArranged*`, `InProcessArrangement` call |
| `lifecycle` | `ResetSettings`, `ClearTownQuests` |

T1 is done when S1 on bUnit gives a report whose per-test "outside calls" adds up to the xUnit duration within 5 %.

The same rows are the touch map: test, scenario and step to page and control. `brinell touch-map` (selection, the
paper's step 3) reads this file; T1 builds the writer once for both.

## Leads to look at first, with T1's report

| Lead | Why it is suspected | Check |
| --- | --- | --- |
| Binding per scenario | The scenario runner binds each scenario's steps to phrases by reflection over the pages and controls assemblies | `bind` span on S1 |
| The bUnit context's reset per scenario | `StartTest` signs out, which restarts the context: a new `BunitContext`, a scope, the studio's `Routes` rendered | `navigate` span named `ResetAppState` |
| Composition per scenario | `TestComposition.ForFixture` scans `[TestModuleScan]` assemblies | a `fixture` or `bind` span |
| The in-process studio's workers | The call queue and runners share the test process's CPU with ten collections | S1 against S2: a cost that grows with collections |
| `GenerateArea` and the host start | 100+ s in an earlier full run's first tests | `host` span |

## Candidates to try after T1, each on its set

They hold whatever T1 finds; each is one iteration, kept only if its set is faster and green.

| Candidate | Set | Expected | How |
| --- | --- | --- | --- |
| Fewer parallel collections | S2, then the full run | Each scenario stretches from about 6 s alone to about 25 s with ten collections; six may finish sooner | `maxParallelThreads` in the suites' `xunit.runner.json` (10 now) |
| Rebalance the serial collection | S3 | 48 scenarios run alone at the end, about 5 min | Move the ones that change no shared setting to the parallel collections (`UatCollections.ChangesSettings`) |
| The start without generating the area | `host` span | Part of the host start is the geo import and `GenerateArea` | A snapshot of the generated test database, restored at the start |
| A warm host between runs | any small set | A small set skips the start | Brinell's warm runners (`BddWarmRunners`) for the studio suite |

## Fixes already known

| Fix | Where | Note |
| --- | --- | --- |
| A skipped scenario reported as Passed in the TRX | `Brinell.Uat` scenario theory | Report it as skipped (`@browser`, the head skip rules), so a run's counts are true |
| PLT.40.4 fails on bUnit: "UnauthorizedAccess" on Publish | Tomorrow, the canon folder of the in-process studio | Probably the content root: the in-process host writes where the built app does not; look at `World:CanonFolder` |

## Iterations

| # | Set | Head | Change | Before | After | Kept |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | full | bUnit | none (the run above) | | 11 m 15 s | |
| T1 | S1 | bUnit | the timings | | 1 m 35 s; tests 96.0 s, of it binding 87.7 s (8 to 9 s a test), steps 7.9 s, outside 0 s | yes |
| 1 | S1 | bUnit | a test binds only its own scenario, not the file's others (`BddBinder.Bind(..., only)`) | 1 m 35 s | 1 m 21 s; binding still 5 to 9 s a test | yes |
| 2 | S1 | bUnit | the runtime and its catalogs kept per fixture (`UatScenarioTestBase.Catalogs`): a fresh catalog's first match warmed up for 5 to 8 s in every test | 1 m 21 s | **17 s**; tests 17.5 s, binding 8.7 s once (the first test), steps 8.4 s | yes |
| 2 | S1 | browser | the same, checked | not measured | 37 s, 10 of 10; tests 38.0 s, steps 28.5 s, binding 8.3 s once | yes |
| 3 | S1 | bUnit | the prepared database kept as a template (a SQL Server backup, by a fingerprint of the build's `Tomorrow*.dll` and the geo and OSM test data), restored by the next run: no migration, seed, geo import or area (`TestDatabase.TryRestore`, `KeepAsTemplate`; `TOMORROW_UI_TEMPLATE=0` builds afresh) | start 18.2 s, wall 38 s | start 7.2 s (restore 0.8 s, the studio's own start 4.6 s), wall 28 s | yes |
| S2 | S2 | bUnit | measured, no change | | 20 of 20, 38 s, wall 48 s; collection A 18.2 s as alone, B 39.0 s: 21.6 s of it is PLT.35.4 waiting for 100 fights the studio plays | |
| 4 | S2 | bUnit | command patterns interpreted, not `RegexOptions.Compiled` (`BddCommandCatalog`): every step is tried against every pattern, so a catalog's first walk compiled and JIT-ed hundreds of regexes | 38 s, wall 48 s; binding 8.8 and 8.6 s | **15 s**, wall 30 s; binding 0.13 s a collection, a first walk 12 ms; A 4.6 s, B 15.1 s | yes |
| 4 | S1 | browser | the same, with the template | 37 s, wall 73 s; start 31.2 s, tests 38.0 s | **17 s**, wall 31 s; start 9.2 s, tests 17.7 s (steps 17.0 s) | yes |

What T1 showed: the binding, not the app, was 91 % of a scenario's time on bUnit, and it is Brinell's runner, so the
browser head paid it too. Next, by the S1 reports: the host's start (20 s on bUnit, 31 s in the browser: the host 12 s,
the geo import 4 to 9 s, the area 3 to 5 s); then S2, to see what parallel collections cost now; then the one warm-up
left (6.8 s in a collection's first test).

What iteration 3 and S2 showed:

- A fresh database costs 3.8 s of migrations (six of them) and 3.7 s of seed; the template takes both out of every run but
  the first after a change. Squashing the migrations into one `Initial` would save part of the 3.8 s, and only on a fresh
  build: worth it when they pile up, or for `Tomorrow.Data.Tests` if it migrates a database per test class.
- Two collections side by side cost nothing visible; S2's slow collection is one scenario's own work (PLT.35.4, 100 fights).
- Left, by size: the catalog's warm-up (about 8.7 s in each collection's first test, ten collections), the studio's own
  start (4.6 s), and PLT.35.4's 100 fights (the owner's scenario: fewer fights, or a faster fight simulation).

A source-generated regex (`[GeneratedRegex]`) would cost nothing at run time, but it needs its pattern as a constant in the code;
the patterns are made at run time from the phrases discovered in a suite's pages and phrase classes, and an interpreted match
of one short step line is microseconds. Matching also runs while steps execute: collection B's calls fell by 14 s with it.

## At the end: the full studio suite, 2026-10-25

`Tomorrow.Studio.UITests`, iterations 1 to 4 in, the template restored, one run per head, one after the other.

| Head | Before (iteration 0) | Now | Tests summed | Of it, control calls |
| --- | --- | --- | --- | --- |
| bUnit | 11 m 15 s; 167 passed, 1 failed | **1 m 29 s** (wall 91 s); 168 passed, 4 skipped (`UiFact(Browser)`), 0 failed | 139 s (was 3,794 s) | 114.5 s |
| Browser | 9 to 17 min (24 min once, RW-251) | **3 m 15 s** (wall 199 s); 172 of 172 passed | 393 s | 337 s |

- The start: 4.0 s a collection on bUnit, 9.7 s in the browser, the template restored in under a second.
- Binding is gone from the report; the scenarios' time is their steps, and the steps' time is their control calls: the
  app and the head doing the work.
- The serial collection is now the largest part: 68 s of 140 s on bUnit, 126 s of 394 s in the browser. It runs alone,
  after the parallel collections; the candidate "rebalance the serial collection" is next.
- PLT.40.4, which failed on bUnit before, passes: the earlier failure was the run, not the head.

Then selection: the timings' rows already name test, scenario, step, page and control, the touch map the paper and the
workflow need; `brinell touch-map` reads this file (paper, step 3).
