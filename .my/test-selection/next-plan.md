# What next for test speed: the proposals by value for their cost

A plan, 2026-10-25, after [speed-plan.md](speed-plan.md)'s five iterations took Tomorrow's studio suite from 11 m 15 s to
1 m 9 s on bUnit and from 9 to 17 min to 2 m 41 s in the browser. It orders what is left by what it saves against what it
costs, so the cheap and valuable come first. Each item is measured the way the speed plan works: a small set, the timings,
one change, the same set again.

## Bruinet's suites first

The owner's rule from 2026-10-25: what is left is built and measured on Bruinet's own sample apps and test suites first
(`tests/Bruinet.Blazor.UITests`, `tests/Bruinet.Html.UITests`, `samples/Todo/tests`), then carried to Tomorrow's as a
step of its own. A change tried on Tomorrow first, the World lane, failed scenarios there that its one green run had not shown.

| Item | Bruinet's proving ground |
| --- | --- |
| 1, deal by duration | The Todo suites' collections, and a dealer in `Bruinet.Uat` any suite can use |
| 3, selection | `bruinet touch-map` and `bruinet affected` on the Blazor and Html suites and their sample |
| 5, a warm host | The Todo UAT suite, whose warm runner keeps the app already |
| 7, the phrase index | The large-suite benchmark in `Bruinet.Bdd.Tests` |
| 8, browser calls | The Blazor and Html suites, measured per call kind |

## The order

Cost: S is an afternoon, M a day or two, L a week or a design. Value is measured where there are numbers, estimated (~)
where not.

| # | Proposal | Value | Cost | Who decides |
| --- | --- | --- | --- | --- |
| 1 | Deal scenarios to collections by their last duration (done) | ~5 s bUnit, ~20 to 30 s browser, every full run | S | done by Claude |
| 2 | Report a skipped scenario as skipped, not Passed (done) | True counts in every run and gate | S | done by Claude |
| 3 | Selection: the touch map and a script that picks what a diff reaches (done; the game suite's map to make) | A change's run in seconds, not 1 to 3 minutes | M | the owner: the widening rules |
| 4 | Fewer fights in PLT.35.4 (done: 10) | 15 to 20 s off the slowest collection | S | the owner: the scenario |
| 5 | A warm host between runs | ~10 to 20 s off every small run | M | done by Claude |
| 6 | Measure `Tomorrow.Data.Tests`' database setup | Unknown until measured; the template may apply | S, then M | done by Claude |
| 7 | A phrase index and a parse cache, with a large-suite benchmark (done) | ~1 to 3 s now; keeps a very large suite linear | M | done by Claude |
| 8 | Cheaper browser calls (the 2026-10-21 review's Html items left) | ~20 to 30 % of the browser's 337 s of calls | M to L | done by Claude |
| 9 | Squash the six migrations into one `Initial` | 1 to 2 s, fresh builds only | S | the owner: the dev database's history |
| 10 | Scenario-scoped settings, so `@own-database` can run in parallel | ~33 s bUnit, ~55 s browser | L | the owner: a game design change |
| 11 | Generated phrase tables (a source generator, `[GeneratedRegex]`) | Only if item 7's benchmark shows the index is not enough | L | not now |
| 12 | The three slow bUnit tests: the sync, the discard dialog and delete, ~470 ms against 10 to 50 ms (done) | ~1.3 s a run now; whatever wait it is will be in every suite on bUnit | S | done by Claude |

## 1. Deal scenarios by their last duration

The run ends with its slowest collection. Today the scenarios are dealt round-robin in file order, so collection B carries
PLT.35.4 and ends last (22 s on bUnit, 65 s in the browser, against 3 to 10 s and 16 to 28 s for the others).

- `UatCollections.Of` deals the parallel scenarios by duration, longest first, each to the part with the least so far.
- The durations come from the last run's `__result.json` files (`durationMs`), kept as a small `TestData/durations.json`
  that a full run writes; a scenario without one counts as the median.
- Done when a full run's parts end within a few seconds of each other.

## 2. A skipped scenario reported as skipped

A scenario skipped by `@browser`, a head rule or a skip rule shows as Passed in the TRX: the theory row passes because
`IsAcceptableResult` accepts a skipped result. A run's counts should say what ran.

- `Bruinet.Uat`'s scenario theory reports the row as skipped with its reason (xUnit's dynamic skip).
- Done when the bUnit run shows 18 more skipped and 18 fewer passed, with their reasons.

## Items 1 and 2, done (2026-10-25)

- **2:** a scenario its config skips for its head or tags (`@browser`, a head) is reported skipped with the reason at
  discovery and never run (`UatScenarioSkip`, in `UatScenarioTestCase.Initialize`). A skip rule's variable may be set by the
  fixture as it starts (Tomorrow's `TOMORROW_OWN_DATABASE`), so rule-based skips stay the run's: the first try skipped all 20
  `@own-database` scenarios at discovery. The bUnit run now counts 22 skipped (18 `@browser`, 4 `UiFact(Browser)`).
- **1:** Tomorrow's `UatCollections.Deal` deals the parallel scenarios longest first to the part with the least time,
  from `durations.json` (`tools/Update-UiDurations.ps1` writes it from a full run's timings); without the file every
  scenario weighs the same and the deal is round-robin, as the game suite's still is.
- **The World lane is gone again:** with the new deal, World studio scenarios that keep backgrounds ran beside parts that read
  them, and PLT.37.1 (bUnit) and PLT.38.1 (browser) failed. The World studio's `@serial` scenarios are serial again; the Avatars
  lane stays.
- **The full studio suite after both:** browser 172 of 172 in 3 m 24 s (wall 207 s; 2 m 41 s with the World lane, which was
  not safe); bUnit 149 passed, 22 skipped, 1 failed in 2 m 1 s.
- **Open: PLT.39.2 on bUnit** ("Hoorn's background generated in one go") finds no run line after Generate. It fails alone, on
  a fresh database too, and passed inside the serial collection's own run: it depends on state an earlier scenario leaves,
  which bUnit's speed exposes. Its Generate returns at once (no status line), so `model` or `view` is not set when it is
  clicked. To be made to stand alone, in Tomorrow. Done with item 12: Generate was on before the town was read.
- Bruinet's own suites have no parallel collections of their own to deal yet; the dealer moves to `Bruinet.Uat` when one has.

## 3. Selection

The biggest saving during development: most changes reach a few pages and a handful of scenarios. The timings already
record test, scenario, step, page and control for every call: the touch map the [paper](paper.md) and the
[workflow](workflow.md) need.

- `bruinet touch-map`: from a run's `timings-*.csv`, which scenarios used which page objects; kept as
  `TestData/touch-map.json`, written by a full run.
- `tools/Select-Tests.ps1` in Tomorrow: `git diff` to files, files to pages (a page's `.razor` and its test-id prefix to its
  page object), page objects to scenarios through the touch map, plus the smoke set; prints one `--filter` and why each
  scenario is in.
- The widening rules: shared code (layout, `Tomorrow.Web`, the fixture, migrations, `Directory.Packages.props`) runs the
  suite whole. The owner checks the list.
- Done when a change to one bench page runs that bench's scenarios and nothing else, with the reasons printed.

## Item 3 on Bruinet's suites, done (2026-10-25)

- **`bruinet tests touch-map <timings>`** writes `touch-map.json` beside the suite: every test and scenario of the run, its
  filter (`FullyQualifiedName=` or `BddScenario=`), and the page objects and controls it reached.
- **`bruinet tests affected`** follows `git diff <base>` (or `--files`) to the tests it reaches and prints each with why and
  one `--filter`. A page reaches the tests that used its `data-testid`s or ids (an id built with `@` counts by its start); a
  page object, the tests that used it; a feature file, its scenarios; a test class, its tests.
- **The widening rules:** a file in any project the suite references (the libraries), the suite's other code (test base,
  fixture, config), the app's shared code (layout, `Program.cs`, styles) and the root build files run the suite whole. Files
  outside the suite, its app and what they reference run nothing. `--always` adds a smoke set to a pick.
- **For the rows:** `[assembly: TestScope]` names plain `[Fact]` tests in the timings; a query outside a call
  (`IsVisible`, `TryGetText`, `IsExists`) writes a `query` row. Before it, six Blazor tests that only asked `IsVisible` or
  `IsEnabled` reached no control and would have been missed. Both suites' UAT classes now run one case per scenario.
- **Misses, measured:** Counter.razor's text broken ("Current count" to "Count now"). Blazor picks 24 of 35, Html 7 of 27;
  on both heads every test that failed in the whole run was picked (5 and 5 failing on Blazor, 4 and 5 on Html).
- **Time:** small here. These suites are a few seconds of tests behind a fixed start: Blazor in the browser 36 s picked
  against 56 s whole (green), Html 27 s against 36 s; with the break both took as long, the failures' waits leading.
  The saving shows on a suite like Tomorrow's studio, 172 scenarios.
- **Two Html tests touch no page** (ResetAppState): they run only with the suite's base or libraries, which is right.
- **Next, Tomorrow:** `tools/Select-Tests.ps1` calls `bruinet tests affected` per suite (`--app src/Tomorrow.Studio`, the
  game's for the game suite, `--always "Gate=Smoke"`); `Tomorrow.Web`, `Tomorrow.Game` and the rest widen by reference. A
  full run with `BRUINET_TIMINGS` writes each suite's map. Tomorrow's pages use test-id prefixes (`bench-*`), which the
  id rule covers if the page writes them literally; to be checked on one bench page, the item's done line.

## Item 3 in Tomorrow, done (2026-10-25)

- **`tools/Select-Tests.ps1`** runs `bruinet tests affected` per suite (studio: `src/Tomorrow.Studio`; game:
  `src/Tomorrow.App`), with `Gate=Smoke` joined to any pick; `-Run` runs it, `-WriteMap` makes a suite's map from a full
  run (and the studio's `durations.json`). bruinet is built into a temp artifacts folder with `-p:BruinetRecorders=false`,
  because the agents' MCP servers keep `tools/Bruinet.Cli/bin` open.
- **What Tomorrow needed from Bruinet:** files the suite links from elsewhere are the suite's (the studio links the
  fixture, the Uat runner and shared pages from the game suite); a component's `TestId="..."` parameter is a test id; a routed
  page reaches every page object that opens its route.
- **The route rule matters:** `Fights.razor`'s own ids reached 7 of the bench's 16 scenarios; the others use only its panels'
  ids. With the rule, a change to the page picks the 16 and the one admin test that opens the bench, nothing else.
- **Misses, measured:** `BenchFights_Seed` renamed in `Fights.razor`, so the old id is gone from the page. The pick (17)
  and the whole suite (165) both failed PLT.35.1 and PLT.35.4, the two that used it; only the route rule found them.
- **Time, in the browser:** 24 s of tests for the pick against 2 m 41 s for the suite.
- **The studio's map:** 154 tests and scenarios from one full browser run (164 of 165 passed; AdminTests'
  `Settings_ShowsMinutesPerStepWithItsRange` failed in the run and passed alone: it reads settings a serial scenario changes).
- **Left:** the game suite has no map yet, so it runs whole until `-Suite game -WriteMap`; misses at the gates are not
  counted yet (a full run's failures against what the script would have picked).

## 4. Fewer fights in PLT.35.4: done

The owner asked for way fewer (2026-10-25). The bench offers 10 beside 100 and 500 (`FightRunPanel.Counts`, still opening on
100), and PLT.35.4 runs 10. Its run step went from 21.6 s to 2.7 s on bUnit and 3.0 s in the browser; the whole of PLT.35 from
28 to 32 s to 5 s on bUnit, 16 s in the browser, 16 of 16 on both.

## 5. A warm host between runs

For a small set the fixed costs now lead: the test process, discovery, and the studio's start (4 s on bUnit, 10 s in the
browser). Bruinet's warm runners (`BddWarmRunners`) keep a runner and its fixture ready per suite between runs; applied to the
studio suite, a small set would skip the start. Done when S1 runs a second time in about its tests' own 17 s.

### Item 5, measured first (2026-10-25)

On the Todo web UAT suite, `dotnet test --no-build` of one scenario (TOD.05.1): 7.8 s of wall time for 2.2 s of
scenario. A run that matches nothing takes 1.3 s (the process and discovery), so about 4.3 s is the fixture starting the
app and the browser: what a warm host would save.

- **It saves only on a run with no build.** A warm runner is made from a copy of the suite's output and is replaced when
  the output changes (AD-025). An app or page-object change rebuilds the output and the app has to start again anyway, so
  the usual loop (edit, build, run) gains nothing.
- **Where it would save:** a second run of the same build (a failure looked at again, a flake checked), and a feature-file
  edit when the runner reads the scenarios from the source (AD-022; `dotnet test` reads the output's copy, so it needs a
  build there too).
- **Proposal:** not built for now. Item 8 (cheaper browser calls) and item 6 save on every run; item 5 waits until reruns
  without a build turn out to be common.

## 6. `Tomorrow.Data.Tests`' databases

If it migrates and seeds a fresh database per test class, it pays 7.5 s each time, the cost the template removed from the UI
suites. First a timing of one run; then, if it holds, the same template.

## 7. A phrase index, a parse cache and a benchmark

Matching tries each step against every phrase of its keyword, about 1 ms a step here and growing with the catalog; each
feature file is parsed again for every scenario in it. Both are small for Tomorrow and would grow for a suite many times its
size.

- First a benchmark in `Bruinet.Bdd.Tests`: a synthetic catalog of 5,000 phrases and 10,000 steps, timed.
- Then the index (phrases by their first literal word) and the parse cache (one parse per file per run), timed again.

## 8. Cheaper browser calls

In the browser the calls are 337 s of 393 s of test time. The [2026-10-21 review](https://github.com/exact-mobile-apps/test-tooling/blob/37f180a7/.my/reviews/2026-10-21-review.md)'s plan has
Html items left (round trips per call, the readiness probe). Measured per call kind with the timings' rows.

## 9. Squash the migrations

Six migrations cost 3.8 s on a fresh database, mostly creating the schema, which one `Initial` also does. The template takes
them out of every UI run after the first; squashing saves 1 to 2 s on a fresh build. Worth it when they pile up again. On the
one dev machine: the migrations replaced by a new `Initial`, the dev database's history set to it.

## 10. Scenario-scoped settings

The 20 `@own-database` scenarios run alone because settings are global: a changed setting would reach every scenario beside
it. If a scenario's settings could be scoped to it (its account, or its circuit), most could run in parallel. That changes how
the game reads its settings: a design, not a test change.

## 11. Generated phrase tables

A source generator over the `[Bdd*]` attributes could emit the phrase patterns as constant tables with `[GeneratedRegex]`,
leaving the feature files bound at run time so the IDE's edit-and-run stays. Generating whole scenarios is ruled out: every
edit would need a build, and a second engine beside the IDE's goes against AD-022. Only if item 7's benchmark shows the run-time
index is not enough.

## 12. The three slow bUnit tests

The owner's finding (2026-10-25): on bUnit three tests take about 470 ms while the rest take 10 to 50 ms: the sync, the
discard dialog and delete. It is not the database. It looks like a fixed wait in the page objects or in the app's state
code (a poll that waits out its interval, a delay before a dialog, a settle time), and is worth chasing for bUnit speed on
its own.

- First the timings of the three (`BRUINET_TIMINGS`, `tools/Scripts/timings.ps1`): which call or span holds the 450 ms,
  and its attempts.
- Then the wait itself: a page object's fixed wait, a poll interval a bUnit render never shortens, or the app's own
  delay. Fixed where it lives, and measured on both heads.

### Item 12, done (2026-10-25)

- **Two waits in the bUnit head, not in the app's state code.** An event waited a fixed 100 ms for its handler
  (`BunitHtmlElement.Raise`): a click that opens a dialog has a handler awaiting the answer, so it always waited all of
  it, although the dialog had rendered during the dispatch. And a poll that missed by a few ms slept the browser's 100 ms.
- **The fix:** an event goes on once its handler is done, or once it has rendered and then waits with the renderer quiet
  for 5 ms (`Settle`); a handler that awaits before rendering anything is waited for as before. The bUnit head polls every
  10 ms by default (`BunitTestContextOptions.PollingIntervalMs`); a suite's own `Timeouts` replace it.
- **A first try went on whenever the renderer was quiet:** the studio's PLT.23.6 and PLT.39.2 then raced handlers still
  reading the database. Waiting for a render first fixed PLT.23.6.
- **PLT.39.2 was the app:** the Street generator's Generate was enabled before the town was read (`view` null), and a click
  then did nothing. The 100 ms waits had hidden it. Generate stays off until the town is read, and PLT.39.2 now passes alone.
- **Todo web on bUnit:** delete 475 to ~120 ms, discard 470 to ~135 ms. Sync, the first test, 615 to ~530 ms: the rest is
  the app's first render and load, not a wait.
- **Tomorrow's studio suite on bUnit:** 1 m 4 s to 47 s, 143 passed and 22 skipped both times; in the browser 165 of 165
  in 2 m 42 s, unchanged. Bruinet's Blazor, Html and Todo web suites green on both heads.

### Item 8, first findings (2026-10-25)

- **The review's Html items (H1 to H6) were done already;** what costs time now is outside the calls.
- **The sample's sign-in slept 1.5 s** (`AuthService.SimulatedDelayMs`) in seven login tests. Now 150 ms, still a wait the
  tests must make: Html in the browser 30 to 22 s, Blazor 41 to 35 s, Html on bUnit 8 to 6 s.
- **A browser per test:** both suites' test bases make a context per test, and `PlaywrightTestContext.CreateAsync` started
  Playwright and launched Chromium each time. Now one browser per process and per type and headless mode, a new browser
  context per `CreateAsync` (`HtmlTestContextOptions.ShareBrowser`, on by default; a watched window keeps its own).
- **The hang was the exit, not the sharing.** The first try closed the shared browsers from a `ProcessExit` handler, which
  blocked there, so `WebAppHost`'s own exit handler never stopped the sample: it outlived the run, held its `bin`, and the
  next build and run hung (a test host at 0 CPU). Shown by a run with the handler (the sample left running) against three
  without (none left). No handler now: Playwright's driver is the test host's child and ends with it; no browser is left.
- **After both:** Blazor in the browser 41 to 23 s, Html 30 to 23 s, Todo web 11 to 12 s; all green on both heads.
- **In Tomorrow, neutral:** the studio suite in the browser, in one worktree, two pairs: shared 2 m 59 s and 2 m 44 s,
  not shared 3 m 27 s and 2 m 44 s, 165 of 165 but for PLT.03.2 once without sharing (it passes alone: a settings scenario,
  flaky beside the others). Its fixtures make a context per collection, so there were few launches to save, and ten parts
  on one browser cost nothing measurable. `ShareBrowser` stays on.
- **A page found not ready is waited for in the browser:** `IHtmlPageProbe.ProbePageWhenReady` (no wait by default, so
  bUnit is as before), the Playwright driver's `WaitForFunctionAsync` checking every 10 ms, at most one polling interval and
  within the call's budget; a ready page is still one look. `BlazorTestContext` now passes the one-trip probe through.
  AssertIdle in the browser: Blazor 4.6 to 0.9 s over 33 calls, Html 3.3 to 0.7 s over 23, none retried. Suites: Blazor
  23 to 22 s, Html 23 to 21 s, Todo web 12 to 10 s; all green on both heads.
- **Item 8 so far, in the browser:** Blazor 41 to 22 s, Html 30 to 21 s.
- **In Tomorrow, the in-browser wait showed two races** in the studio suite's full run, both passing alone. PLT.22.2: the
  Interactions bench's person panel read its face, lines and talks one after another, each rendering, and the purse was
  typed into before the last; the panel now says `data-ready` and talking to someone waits for it (Tomorrow b3887df7). The
  old-link test (`BuildingsBench_OldLink_LandsOnTheThingBench`): the bench loaded but `<body>` had no `data-busy` for 10 s,
  once in two runs; its cause is not found. With the panel fixed, two full browser runs passed 165 of 165 (2 m 39 s and
  2 m 35 s, against about 2 m 44 s before). The in-browser wait goes to Tomorrow with the next pointer bump.
- Stopping a test run leaves the sample app it started running, holding its `bin`: the next build fails or waits.

### Item 7, done (2026-10-25)

- **The benchmark:** `BddLargeSuiteBenchmarkTests`, run with `BRUINET_BENCHMARK=1` (skipped otherwise): 5,000 phrases
  shaped as Tomorrow's, 10,000 steps, and one file of 200 scenarios read once per scenario.
- **The phrase index:** `BddCommandCatalog` keys each pattern by its keyword and its first one or two whole literal words
  (`IndexKey`); a step tries the buckets its own first one and two words name, and the phrases that begin with a
  parameter, in registration order. Two words because by the first alone "the" holds 44% of Tomorrow's 1,105 phrases and
  "I" 35%; by two the largest bucket ("I open") holds 62. `BddCommandCatalogIndexTests` checks the index finds what trying
  every pattern finds, in order, on awkward phrases and on the benchmark's catalog.
- **The parse cache:** `BddMarkdownParser.ParseFile` keeps one result per file while its last write and length stay; the
  model is immutable records, so it is shared.
- **Numbers:** matching 2.47 s to 0.84 s (247 to 84 µs a step); 200 reads of one file 300 to 64 ms; registering 5,000
  phrases 48 to 80 ms. What is left in matching is the parameter-first bucket every step tries (5% of the synthetic
  phrases, 2% of Tomorrow's). For Tomorrow's size this is the 1 to 3 s the plan named; item 11 stays not needed.
- Green: Bdd 260, Uat 233, Workspace 69, Blazor and Html suites and Todo web on both heads, Todo web UAT in the browser.
  Todo web UAT on bUnit fails as widely before the change: that suite was never made to run there.
