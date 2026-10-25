# Testing less during development: test selection for Brinell UI suites

A research paper, 2026-10-24. The question: UI suites are slow and get run in full again and
again during development. How do we run only the tests a change can break, and keep the full set
for the gates?

## Outcome

Run the full UI suite only at gates, and let Brinell pick the rest. Brinell already knows, per
test, which page objects and controls it touched (every public call logs its page and control,
`ControlCall`). Record that once per full run as a **touch map**. Join it with the git diff and a
map from page objects to source files, and Brinell can answer "which tests does this change
reach?" as a `--filter` expression. Anything the map cannot place (shared code, a migration, a
changed fixture) widens the selection, up to the full suite. The full run at the gate is the
safety net that catches what selection missed, and it refreshes the map.

The rest of this paper is the problem, what the field has learned, the options for Brinell, and
a proposed order of work.

## The problem

Tomorrow's two UI suites are the slowest tier: the studio's takes 9 to 17 minutes, the game's
several minutes, on a machine that also runs SQL Server and the app under test. The SDLC already
says the right thing (`.sdlc/sdlc.md` §2.4): a build check runs the smoke set plus the touched
features, about 20 seconds; the full suite only at checkpoints, migrations and shared code.

The policy fails in practice for one reason: choosing "the touched features" is done by hand. An
agent or a developer has to know that a change in `QuestCard.razor` reaches QST.01 and QST.02 but
not FGT.01. When unsure, they run everything, and they are often unsure. So the fix is not a new
policy but a tool that makes the cheap choice also the safe-feeling one.

## What the field has learned

Regression test selection (RTS) is a forty-year-old field; the lessons that matter here:

- **Safe selection is possible and cheap at file granularity.** Rothermel and Harrold (1997)
  defined "safe" RTS: no test that could reveal a fault is left out. Ekstazi (Gligoric, Eloussi
  and Marinov, ISSTA 2015) showed that tracking dependencies per test at the level of *files*,
  not methods, keeps almost all of the savings at a fraction of the overhead.
- **Static selection is a good fallback.** STARTS (Legunsen et al., FSE 2016) selects from the
  class dependency graph without running anything; it selects more than dynamic tools but needs
  no recorded data.
- **Large companies select, then run everything later.** Google's TAP runs only affected targets
  per change from the build graph and catches the rest in later batched runs (Memon et al.,
  ICSE-SEIP 2017). Microsoft's Test Impact Analysis in Azure Pipelines maps tests to changed
  files from per-test coverage and falls back to all tests for files it does not know.
- **Learned selection works when history is long.** Facebook's predictive test selection
  (Machalica et al., ICSE-SEIP 2019) trains a model on change features and past failures; it
  caught over 95% of failing changes while running about a third of the tests. Microsoft
  reported similar trade-offs (Herzig et al., "The art of testing less without sacrificing
  quality", ICSE 2015). This needs thousands of runs of history; Tomorrow has not got that yet.
- **Ordering helps even without selection.** Test prioritisation (Elbaum, Malishevsky and
  Rothermel, 2002 onward) runs the likely failures first: last failed, recently changed, then the
  rest. A failure in the first minute is almost as good as running less.
- **Flaky tests poison selection.** A selected flaky test fails for no reason and teaches people
  to run everything "to be sure" (Luo et al., FSE 2014). Selection only earns trust on a suite
  that is not flaky, which is why the 2026-10-21 review's speed and flakiness fixes come first.

The tools developers know do the same at smaller scale: Jest's `--onlyChanged`, Nx and Bazel's
"affected" targets, NCrunch's impact detection for .NET, pytest-testmon.

## Why UI tests are the hard case

Unit-test RTS follows code coverage: the test calls the code. A UI test reaches the code through
a browser, a server and a database, so coverage of the test assembly says nothing. Three things
make it tractable for Brinell all the same:

- **Brinell sees the UI side.** Every call goes through `ControlCall` with a page name and a
  control id, so "test T used `QuestPage.Accept` and `HomePage.Survivor`" is already in the log.
- **Tomorrow's app runs in process.** Blazor Interactive Server, hosted by the test fixture, means
  server-side coverage per test is collectable with the .NET coverage tools, unlike a remote app.
- **The names line up.** Feature files carry IDs (`QST.01-first-quest.feature.md`), tests carry
  traits (`Gate=Smoke`, `Category=UAT`), and page objects are named after pages.

What stays hard: shared layout and CSS reach every page; a change in a game rule reaches the UI
only through data; tests in one xUnit collection share state, so selecting one may need its
setup partners.

## Options for Brinell

Five mechanisms, from cheapest to most work. They combine; none replaces the gate.

| Option | What it selects by | Cost and risk |
| --- | --- | --- |
| A. Naming and tags | Feature ID, trait, folder, by hand or by convention | Cheap; as good as the person choosing |
| B. Touch map | Page objects each test touched, joined to the diff via page-to-source | Moderate; misses non-UI paths |
| C. Coverage map | Server source files each test executed | More overhead per full run; most precise |
| D. Order and stop | Last failed first, fail fast, rerun only failed | Cheap; no selection risk |
| E. Learned selection | A model on history | Needs long history; later |

**A. Naming and tags.** Make the convention a rule: every test and scenario carries its feature
ID, and `brinell` can turn a list of IDs into a filter. This alone removes the guesswork about
filter syntax, not about which IDs.

**B. The touch map.** During a full run, a `TouchRecorder` (an `ITestLogger` beside the existing
ones) writes `test → {page object, control}` to `TestResults/touch-map.json`. A second, static
map comes from the source: page object `QuestPage` → its route → the Razor component and its
code-behind and child components. A changed file maps to page objects, page objects to tests.
This is the Brinell-shaped answer and works for every stack Brinell drives, not only Blazor.

**C. The coverage map.** For an in-process app, collect file-level coverage per test during the
full run (Ekstazi's lesson: files, not lines). Map each changed `.cs` or `.razor` to the tests
that executed it. This catches the paths B misses (a rule in `Tomorrow.Game` that changes what a
page shows). It costs a slower gate run, paid once, not in the inner loop.

**D. Order and stop.** Run last-failed first, then selected, then the rest when asked; stop at the
first failure during development; rerun only failures after a fix. These need no map and should
ship first.

**E. Learned selection.** Revisit when there are months of gate results to learn from.

## The safety rules

Selection is only worth it if people trust it. The rules that make it safe:

- **Widen on doubt.** A changed file the maps do not know runs its project's whole UI suite; a
  change in shared code (layout, skin, `Tomorrow.Web`, the fixture, a migration, `Directory.Packages.props`)
  runs the full suite. The list of "widening" paths lives in a config file per suite.
- **Always add smoke.** The smoke set runs with every selection; it is the floor.
- **The gate runs everything.** Reviews of shared code, checkpoints and the end of an unattended
  run keep the full suite, as `.sdlc` already says. Each gate run refreshes the maps.
- **Measure misses.** When a gate run fails a test the last selection left out, record it. A miss
  rate above a few percent means the maps are wrong, and the widening list grows.
- **Explain the choice.** The command prints why each test was picked ("QuestCard.razor →
  QuestPage → QST.01.1"), so a person can check it in seconds.

## The development loop, as it would look

| Moment | What runs | Time |
| --- | --- | --- |
| While editing | Selected tests from the diff, last failed first, stop on first failure | Seconds to a minute |
| Build check, per task | Smoke plus selected, widened by the rules | About a minute |
| Gate (review of shared code, checkpoint, run end) | Full suite; maps refreshed; misses recorded | As today |

For an agent this is one call: `brinell affected --since game~1` prints the filter, or the MCP
`run_test` tool takes `affected: true`. The agent no longer chooses; it runs what Brinell picks.

## Proposed order of work

1. **D first:** last-failed order, fail fast and rerun-failed in Brinell's runner and CLI. No risk,
   immediate saving.
2. **A:** feature IDs as a checked convention (an architecture test in Tomorrow), and
   `brinell filter <ids>`.
3. **B:** the `TouchRecorder`, the page-to-source map for Blazor and Html, `brinell affected`
   with the widening config and the explanation; the MCP tool option.
4. **Miss tracking** at the gate, so the next decision is made on numbers.
5. **C** only if misses show B is not enough; **E** when the history is long.

Before any of this: the 2026-10-21 review's flakiness fixes (the pause as a sleep, predicate
waits, Html readiness in one trip). A suite that fails at random cannot be trusted with fewer
runs.

## What is unknown

- How much of a full run each option saves on Tomorrow: measure the hit rate of B over the last
  twenty commits on `game` before building C.
- Whether xUnit collections that share state let a test run alone; some may need their setup
  partners selected with them.
- The overhead of per-test coverage on a parallel Blazor suite (option C).

## References

Cited from memory; titles and venues are as remembered, not re-checked.

- G. Rothermel, M. J. Harrold. *A safe, efficient regression test selection technique.* ACM TOSEM, 1997.
- M. Gligoric, L. Eloussi, D. Marinov. *Practical regression test selection with dynamic file dependencies* (Ekstazi). ISSTA 2015.
- O. Legunsen et al. *An extensive study of static regression test selection in modern software evolution* (STARTS). FSE 2016.
- A. Memon et al. *Taming Google-scale continuous testing.* ICSE-SEIP 2017.
- M. Machalica et al. *Predictive test selection.* ICSE-SEIP 2019.
- K. Herzig et al. *The art of testing less without sacrificing quality.* ICSE 2015.
- S. Elbaum, A. Malishevsky, G. Rothermel. *Test case prioritization: a family of empirical studies.* IEEE TSE, 2002.
- Q. Luo et al. *An empirical analysis of flaky tests.* FSE 2014.
- Microsoft. *Test Impact Analysis* in Azure Pipelines (the Visual Studio Test task's documentation).
