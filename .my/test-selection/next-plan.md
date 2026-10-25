# What next for test speed: the proposals by value for their cost

A plan, 2026-10-25, after [speed-plan.md](speed-plan.md)'s five iterations took Tomorrow's studio suite from 11 m 15 s to
1 m 9 s on bUnit and from 9 to 17 min to 2 m 41 s in the browser. It orders what is left by what it saves against what it
costs, so the cheap and valuable come first. Each item is measured the way the speed plan works: a small set, the timings,
one change, the same set again.

## Brinell's suites first

The owner's rule from 2026-10-25: what is left is built and measured on Brinell's own sample apps and test suites first
(`testsnew/Brinell.Blazor.UITests`, `testsnew/Brinell.Html.UITests`, `samples/Todo/tests`), then carried to Tomorrow's as a
step of its own. A change tried on Tomorrow first, the World lane, failed scenarios there that its one green run had not shown.

| Item | Brinell's proving ground |
| --- | --- |
| 1, deal by duration | The Todo suites' collections, and a dealer in `Brinell.Uat` any suite can use |
| 3, selection | `brinell touch-map` and `brinell affected` on the Blazor and Html suites and their sample |
| 5, a warm host | The Todo UAT suite, whose warm runner keeps the app already |
| 7, the phrase index | The large-suite benchmark in `Brinell.Bdd.Tests` |
| 8, browser calls | The Blazor and Html suites, measured per call kind |

## The order

Cost: S is an afternoon, M a day or two, L a week or a design. Value is measured where there are numbers, estimated (~)
where not.

| # | Proposal | Value | Cost | Who decides |
| --- | --- | --- | --- | --- |
| 1 | Deal scenarios to collections by their last duration (done) | ~5 s bUnit, ~20 to 30 s browser, every full run | S | done by Claude |
| 2 | Report a skipped scenario as skipped, not Passed (done) | True counts in every run and gate | S | done by Claude |
| 3 | Selection: the touch map and a script that picks what a diff reaches | A change's run in seconds, not 1 to 3 minutes | M | the owner: the widening rules |
| 4 | Fewer fights in PLT.35.4 (done: 10) | 15 to 20 s off the slowest collection | S | the owner: the scenario |
| 5 | A warm host between runs | ~10 to 20 s off every small run | M | done by Claude |
| 6 | Measure `Tomorrow.Data.Tests`' database setup | Unknown until measured; the template may apply | S, then M | done by Claude |
| 7 | A phrase index and a parse cache, with a large-suite benchmark | ~1 to 3 s now; keeps a very large suite linear | M | done by Claude |
| 8 | Cheaper browser calls (the 2026-10-21 review's Html items left) | ~20 to 30 % of the browser's 337 s of calls | M to L | done by Claude |
| 9 | Squash the six migrations into one `Initial` | 1 to 2 s, fresh builds only | S | the owner: the dev database's history |
| 10 | Scenario-scoped settings, so `@own-database` can run in parallel | ~33 s bUnit, ~55 s browser | L | the owner: a game design change |
| 11 | Generated phrase tables (a source generator, `[GeneratedRegex]`) | Only if item 7's benchmark shows the index is not enough | L | not now |

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

- `Brinell.Uat`'s scenario theory reports the row as skipped with its reason (xUnit's dynamic skip).
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
  clicked. To be made to stand alone, in Tomorrow.
- Brinell's own suites have no parallel collections of their own to deal yet; the dealer moves to `Brinell.Uat` when one has.

## 3. Selection

The biggest saving during development: most changes reach a few pages and a handful of scenarios. The timings already
record test, scenario, step, page and control for every call: the touch map the [paper](paper.md) and the
[workflow](workflow.md) need.

- `brinell touch-map`: from a run's `timings-*.csv`, which scenarios used which page objects; kept as
  `TestData/touch-map.json`, written by a full run.
- `tools/Select-Tests.ps1` in Tomorrow: `git diff` to files, files to pages (a page's `.razor` and its test-id prefix to its
  page object), page objects to scenarios through the touch map, plus the smoke set; prints one `--filter` and why each
  scenario is in.
- The widening rules: shared code (layout, `Tomorrow.Web`, the fixture, migrations, `Directory.Packages.props`) runs the
  suite whole. The owner checks the list.
- Done when a change to one bench page runs that bench's scenarios and nothing else, with the reasons printed.

## 4. Fewer fights in PLT.35.4: done

The owner asked for way fewer (2026-10-25). The bench offers 10 beside 100 and 500 (`FightRunPanel.Counts`, still opening on
100), and PLT.35.4 runs 10. Its run step went from 21.6 s to 2.7 s on bUnit and 3.0 s in the browser; the whole of PLT.35 from
28 to 32 s to 5 s on bUnit, 16 s in the browser, 16 of 16 on both.

## 5. A warm host between runs

For a small set the fixed costs now lead: the test process, discovery, and the studio's start (4 s on bUnit, 10 s in the
browser). Brinell's warm runners (`BddWarmRunners`) keep a runner and its fixture ready per suite between runs; applied to the
studio suite, a small set would skip the start. Done when S1 runs a second time in about its tests' own 17 s.

## 6. `Tomorrow.Data.Tests`' databases

If it migrates and seeds a fresh database per test class, it pays 7.5 s each time, the cost the template removed from the UI
suites. First a timing of one run; then, if it holds, the same template.

## 7. A phrase index, a parse cache and a benchmark

Matching tries each step against every phrase of its keyword, about 1 ms a step here and growing with the catalog; each
feature file is parsed again for every scenario in it. Both are small for Tomorrow and would grow for a suite many times its
size.

- First a benchmark in `Brinell.Bdd.Tests`: a synthetic catalog of 5,000 phrases and 10,000 steps, timed.
- Then the index (phrases by their first literal word) and the parse cache (one parse per file per run), timed again.

## 8. Cheaper browser calls

In the browser the calls are 337 s of 393 s of test time. The [2026-10-21 review](../reviews/2026-10-21-review.md)'s plan has
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
