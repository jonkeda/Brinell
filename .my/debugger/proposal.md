# Running and debugging a scenario: faster runs, Instant Run, Step through, Debug

Status: **decided** 2026-10-04: every question below is accepted as its leaning. The work is in [plan.md](plan.md).
Amends [AD-022](../../.docs/decisions/ad-022-the-ides-run-the-gates-engine.md) (R2, "a step
breakpoint is a file"; the preview's Run) and [AD-025](../../.docs/decisions/ad-025-a-suite-runs-in-a-process-of-its-own.md)
(the runner process: the IDEs as hosts, and a warm runner).

Three parts, in the order they pay off: **faster runs** the way Run works today (section 4),
**two modes** to stop at a step (section 3), and **Instant Run** (section 5), which puts the
runner of section 3 behind every Run.

## 1. The problem

Today the only way to stop at a scenario step is a real debugger. The IDE runs `dotnet test` with
`VSTEST_HOST_DEBUG`, attaches Visual Studio's (or VS Code's C#) debugger to the testhost, and the
engine calls `Debugger.Break()` before a step whose line has a breakpoint. Trying it on Todo's
TOD.01.1 (2026-10-03/04) showed what that costs:

| What happened | Why |
| --- | --- |
| The run never started, or was cancelled at once | The testhost prints its process id twice; the second attach failed and cancelled the run (fixed in 5239365c). |
| The stop showed Brinell's C# (`_context.Breaks.Break(stepBreak)`), not the feature file | Every BDD stop is the same `Debugger.Break` call; Visual Studio shows its own location over the file the extension opened (worked around in 4116aacd). |
| F10 stepped through Brinell's C# | F10 is the C# step over; it is taken over at a BDD stop now (4116aacd, f40b01aa). |
| The next step failed: "Sync timed out" | The debugger freezes the whole testhost, and the fake server lives in it. The app kept running and its request timed out. Any scenario with a timed server answer, a poll or a timeout fails when paused at the wrong step. |
| "Crashed", and "running..." until Continue | Visual Studio stopped on the exception that fails the test for xUnit (fixed in 8131cee5). |
| A long pause could fail the test on time | The xUnit `Timeout` counts paused time (off under a debugger since 8131cee5). |

Each is fixed or worked around, but they share one cause: a debugger pauses a **process**, and a
scenario is not only a process. It is the test's thread, a fake server, the app under test and
their timers. A developer who wants to watch a scenario go step by step does not need a C#
debugger at all; one who wants to see why a step definition misbehaves does.

## 2. What holds today

| Fact | Where |
| --- | --- |
| The engine asks a hook before each step: `IBddBreakHook.Before` gives a `BddStepBreak` or null; `Break` stops. `BddBreakpoints` is the hook: it reads the breakpoints file, writes `bdd-paused.json`, takes a one-shot move (over, into, out), and its stop defaults to `Debugger.Break`. | `srcnew/Brinell.Bdd/BddBreakpoints.cs` |
| The hook exists only under a debugger: `BddExecution.Breaks = BddBreakpoints.ForDebugger()`, null when no debugger is attached. So CI can never wait at a breakpoint. | `BddExecution.cs:40`, AD-022 R2 |
| **The Presenter already steps without a debugger.** It starts the suite's runner process (`Brinell.Uat.dll`, AD-025) and drives it over a named pipe: `Open` a scenario, then `Next` for each step, `Cancel`, `Close`. It pauses between steps itself, reads the same breakpoints file to stop Play, and sets `Breaks = null` in the runner. | `BddRunnerHost.cs`, `BddRunnerClient.cs`, `PresenterBreakpointStore.cs` |
| In the runner, only the host's next message is awaited. The app, the fake server and every timer keep running between steps. | AD-025 |
| The runner writes no `__result.json` and no manifest: a Presenter run does not show in the IDE preview. | `BddRunnerHost.cs` |
| The IDE preview already has the paused bar (Continue, Step over, Step into, Step out, Stop), the paused step marked, and the breakpoint gutter, fed by `bdd-paused.json`. | `preview-host.js`, `PreviewController.cs`, VS Code `stepping.ts` |
| The language server (`brinell lsp`) is net10 and references `Brinell.Bdd`, which has the runner client. | `tools/Brinell.Cli` |

## 3. The proposal: two modes

| | **Step through** (new) | **Debug** (today's) |
| --- | --- | --- |
| For | watching a scenario go step by step: what each step does to the app, where it fails | why a step definition, page object or control misbehaves |
| Started from | the preview's **Step** button on a scenario heading; the Debug menu | the preview's **Debug** button; Test Explorer's Debug |
| Runs in | the suite's runner process (AD-025), driven by the language server | `dotnet test` with the IDE's debugger attached |
| Pauses | the scenario only: the runner waits for the next message | the whole testhost (`Debugger.Break`) |
| App, fake server, timers | keep running | frozen with the testhost (the fake server); the app runs on |
| Moves | Continue, Step over, Step into / out of a `## Step:` call, Stop, all in the preview; F10 / F11 / Shift+F11 on the paused step | the same BDD moves, plus F11 into the step definition's C#, Locals, C# breakpoints |
| At a stop you see | the feature file at the step; the step's values as read, its page and app; the app itself, live | Brinell's C# frame with `stepBreak` in Locals |
| A failed step | stops there with the app left in its failed state, until Continue or Stop | the scenario fails; break on failure if set |
| Needs | nothing but a build | the IDE's debugger, the CoreCLR engine (VS Code: the C# extension) |
| xUnit, Test Explorer | not involved | the case runs as a test |

### D1. Step through runs in the runner, as the Presenter does

The language server becomes a runner host, like the Presenter: it starts the suite's runner with
`BddRunnerStart`, opens the scenario, and sends `Next` itself while nothing stops it. Before each
step it checks the breakpoints file (the same file, the same lines, the same `SameFile` matching
of the build output's copy); at a breakpoint, or after a move, it stops sending and tells the
client where it is paused (the same `bdd-paused.json` shape, or a `brinell/paused` notification).

**Why the runner, and not `dotnet test` with a "step mode" flag:** the runner already exists, is
tested, is the gate's own process (AD-025: "a scenario passes under `dotnet test` and fails from
Presenter" is a broken invariant), and already pauses between steps. A flag in `dotnet test` would
need a new wait inside the engine, xUnit's timeout off, and Test Explorer kept out of it.

### D2. The breakpoints file stays one file; who reads it changes

- **Step through:** the host (the language server) reads it between steps. The engine's hook stays
  null in the runner, as in the Presenter.
- **Debug:** unchanged: the engine reads it, only while a debugger is attached.

So AD-022 R2's "read only while a debugger is attached" becomes "read by the engine only while a
debugger is attached; a runner host reads it between the steps it sends". CI still never waits:
it runs neither a runner host nor a debugger.

### D3. Moves are the host's

Step over is "send `Next` once, then stop". Continue is "send `Next` until a breakpoint, a failure
with break-on-failure, or the end". Stop is `Cancel` then `Close`, which ends the runner, the app
and the fake server (AD-025's job object). Step into and out need the runner to report a
`## Step:` call's own steps (open question Q2).

The keys are the IDE's, but only on a paused Step-through run: F10 is Step over, F11 Step into
(at a call), Shift+F11 Step out, F5 Continue, Shift+F5 Stop. With no Step-through run paused, they
are the debugger's as always (the guard of f40b01aa).

### D4. A Step-through run reports like a test run

The runner writes the same `__result.json` and manifest record per scenario as
`UatScenarioTestBase` does (P5 of AD-022), so the preview's ticks, durations, messages and
failure screenshots, the Error List and the BDD Explorer's Results show a Step-through run the
same way. The run is marked as one (`"mode": "step"` in its metadata), so it can be told from a
gate run.

### D5. What a stop shows

The paused bar as today, plus, because the app is live and nothing is frozen:

- the step's arguments as read (`+2d` → `05-Oct-2026`), its app and page (what `BddStepBreak`
  holds for Locals today);
- **Screenshot now**: the app as it is at the stop, into the run's folder;
- later: the current page's controls and their state, read through its page object.

### D6. From a Step-through stop into C#: "Debug from here" (later)

At a stop, the developer may want the next step's C#. The runner is a process, so the IDE can
attach its debugger to it there and then, and the host asks the runner to break before the next
step (a `Break` message: the runner sets its hook to `Debugger.Break` for one step). Then F11 goes
into the step definition as in Debug mode. This joins the two modes instead of making the
developer choose up front. It is a later step (section 7, phase 3).

## 4. Faster runs: what a Run costs today

Measured on Todo's MAUI suite (`Brinell.Samples.Todo.Uat`), 2026-10-04, on one developer machine:

| Phase | Today | Note |
| --- | --- | --- |
| **Build**: the preview's Run and Debug build the **whole open solution** (`StartSimpleUpdateSolutionConfiguration`); with `Brinell.sln` that is 86 projects | not measured inside Visual Studio; `dotnet build` of the suite project alone, nothing changed: **10.6 s** | on every Run, even when nothing changed; the biggest cost |
| **`dotnet test` start**: vstest, the testhost, finding the cases | **4.1 s** (a filter that runs no scenario, twice) | a runner starts in about 1-2 s |
| **Fixture**: the Todo app and the fake server start | a few seconds, not measured apart | the same for any host, once per run |
| **The scenario** | 2.4 s for TOD.01.1, 2 s of it its own "the server answers after 2 seconds" | |

So the runner alone saves 2-3 s. The rest is the build and the app. In order of payoff:

### F1. Build the suite's project, not the solution

Build only the suite's project and what it references (Visual Studio:
`IVsSolutionBuildManager.StartSimpleUpdateProjectConfiguration`; VS Code: `dotnet build` of the
project). For Todo about 10 projects instead of 86. Applies to Run, Debug and Step through.

### F2. No build when nothing it needs changed

Skip the build when no project the suite depends on changed since the last successful build. The
extension already sees builds end (`brinell/buildDone`), and the up-to-date check of one project
is quick. A second Run in a row starts at once.

### F3. The runner for every Run

Section 5: the 2-3 s of test start go, and Run and Step through become one path.

### F4. The app kept between runs

Section 5 (I3): the next biggest cost after the build. It needs AD-025 changed.

## 5. Instant Run

The aim: after a scenario edit, **a Run takes the scenario's own time**. Three costs go, and the
runner of section 3 is what makes each one possible.

### I1. No build for a scenario edit

A `.feature.md`, `.steps.md`, `uat.config.md` or test-data file is not compiled; today it is only
copied into `bin`. A Run reads the scenario files **from the source folder** (the build output's
copy is for a gate run), so editing a scenario and running it needs no build. A build is needed
only when C# changed: the suite's step definitions, page objects, fixture, or Brinell; that is
decided per project (F2). This is the case a scenario writer meets most.

### I2. A warm runner, started ahead of time

The language server starts the suite's runner when a feature file of the suite is opened, and
again in the background after each build. It waits with the suite loaded; a Run only sends `Open`
and `Next`, so the test start (4 s) is gone.

The runner runs from a **shadow copy** of the build output (as `Brinell.Bdd.Workspace` loads a
suite for its catalogs, AD-022 L1), so it never holds `bin`: a build is never blocked, and after
it the old runner is replaced by a fresh one. This reverses AD-025's ruled-out "a runner kept per
workspace", whose reason was a held `bin`.

### I3. The app and the fake server already running

The warm runner also starts the fixture: the app and its fake server. Each scenario then starts
with the fixture's clean start (`IUatScenarioLifecycle.BeforeScenario`: reset the data, back to
the start page) instead of a launch. A scenario that needs a fresh launch (other launch settings,
a restart step) still gets one. A fixture says whether it can reset without a launch; one that
cannot keeps launching, and is still faster by I1 and I2.

### I4. "Run as test" stays the gate

A warm Run is the gate's engine in the gate's kind of process (AD-025), but it is not
`dotnet test`, and an app kept between scenarios is not what CI does. So the preview keeps both:

| Button | Path | For |
| --- | --- | --- |
| **Run** | the warm runner (I1-I3) | writing and fixing scenarios: instant |
| **Run as test** | `dotnet test` by the `BddScenario` address, as today (with F1, F2) | proof: what CI does; before a commit |
| **Step** | the warm runner, stopping at breakpoints (section 3) | watching a scenario step by step |
| **Debug** | `dotnet test` under the debugger (section 3) | the C# of a step |

With I2, Run and Step are one thing: a warm runner that is either let go or stepped. A **Pause**
on a running Run becomes possible.

### Costs of Instant Run

- **Stale state.** A scenario that passes only because the one before left something behind. A
  fixture whose clean start is not clean shows as a warm Run that differs from "Run as test"; the
  fix is in the fixture, and "Run as test" catches it.
- **Memory.** The runner, the app and the fake server stay up while the suite is open in the IDE.
  They end when its last feature file closes, after an idle time, and with the IDE (AD-025's job
  object).
- **An app on the desktop.** A warm MAUI app sits there while you edit. It can start minimised
  and come forward when a Run starts.
- **Two paths to one scenario** (section 9) weigh more: Run is now the runner by default.

## 6. What changes in the docs

- **AD-022**: R2 amended (D2); the table gains "two modes"; "Ruled out" keeps the DAP and adds
  "a debugger to watch a scenario: it freezes the fake server and the timers with the test".
- **AD-025**: "the IDEs are runner hosts too", next to the Presenter; a warm runner from a shadow
  copy (I2) replaces the ruled-out "runner kept per workspace"; a fixture's clean start without a
  launch (I3).
- **AD-022** also: the preview's Run is the warm runner and "Run as test" the gate (I4); a Run reads
  scenario files from the source folder (I1).
- **`docs/guides/visual-studio-preview.md`** and **`vscode-extension.md`**: Step through and
  Debug, when to use which; the warning that Debug freezes a fake server in the test process.
- **`.my/uat/manual/visual-studio-test-plan.md`**: section 6 split in two.

## 7. Phases

Each step one commit, with tests; the IDE parts checked by hand in both IDEs.

| # | Step | Done when |
| --- | --- | --- |
| 0.1 | **Build the suite's project** (F1), in both IDEs, for Run and Debug. | With `Brinell.sln` open, a Run builds only Todo's projects; timed before and after. |
| 0.2 | **Skip a build nothing needs** (F2). | A second Run with nothing changed starts the test at once; a C# change still builds. |
| 1.1 | **Runner results** (D4): the runner writes `__result.json` and the manifest record, with the screenshot on failure. | A Presenter run of TOD.01.1 shows in the IDE preview; the Presenter tests are green. |
| 1.2 | **The language server as a runner host** (D1-D3): `brinell/step/start {uri, line, suite}`, `brinell/step/move {over\|into\|out\|continue\|stop}`, `brinell/paused` notifications; one run at a time; the run ends with the server. | Server tests with the Todo service suite (no UI): stop at a breakpoint, over, continue to the end, stop; results written. |
| 1.3 | **Visual Studio**: a **Step** button beside Debug, the paused bar on the server's notifications, the keys of D3 on a paused Step-through run, the feature file in front at each stop. | Checklist: TOD.01.1 with a breakpoint on `When I tap Sync`, F10 to the end, it **passes** (the Loading state is seen). |
| 1.4 | **VS Code**: the same, through its Test Explorer's run profiles (Run, Step, Debug). | The same checklist in VS Code. |
| 2.1 | **Screenshot now** and the stop's details (D5). | |
| 2.2 | Step into and out of `## Step:` calls in a Step-through run (Q2). | |
| 3.1 | **Debug from here** (D6). | From a Step-through stop, F11 lands in the step definition. |
| 4.1 | **Run on the runner** (F3, I4): Run uses the runner, and "Run as test" is today's Run. A Run reads scenario files from the source folder (I1). | Editing a step's text and pressing Run starts with no build. |
| 4.2 | **The warm runner** (I2): started on opening a feature file and after a build, from a shadow copy; replaced after a build; ended on close and when idle. | A build succeeds while the runner waits; a Run reaches the fixture in under a second. |
| 4.3 | **The warm app** (I3): the fixture starts with the runner; `BeforeScenario` is the clean start; a fixture says whether it can reset without a launch. | TOD.01 run twice in a row: the second does not launch the app, and passes as under "Run as test". |
| 5.1 | ADs and guides (section 6); the manual test plan's section 6. | |

## 8. Open questions

| # | Question | Decided (the leaning) |
| --- | --- | --- |
| Q1 | Should Step through be the preview's default (Run, **Step**, Debug), with Debug the advanced one? | Yes: it is what most people mean by "debug a scenario", and it never freezes the app's backend. |
| Q2 | The runner's `Next` runs one top-level step. Does a `## Step:` call run as one step, or can the runner step into its steps? | Look at `BddRunnerHost` and `BddStepExecutionSession`: the session already knows calls (`BddCallFrame`); the runner needs to report them. |
| Q3 | Many apps (AD-015): a step on another app. | The runner already runs them; nothing changes for stepping. |
| Q4 | A Scenario Outline row, and `Background` steps. | Rows run as scenarios; `Background` steps are stepped like any other. Check the runner opens a row by its name with `[n]`. |
| Q5 | Is a `## Step:` callable's body a place for a breakpoint in Step-through mode? | Yes, as in Debug: the host matches the call's lines. Depends on Q2. |
| Q6 | Does Debug mode keep the F10 takeover (4116aacd)? | Yes: it makes Debug's BDD stops behave like Step through's. |
| Q7 | Keep both modes in the Presenter? | It is Step through already; "Debug from here" (D6) would give it the second. |
| Q8 | Which files does a Run read from the source folder (I1)? Feature and step files surely; `uat.config.md`, the apps file and test data too? | Every file the build only copies. A file the build transforms stays the build output's. |
| Q9 | How does a fixture say it can reset without a launch (I3)? | A property on `IUatScenarioLifecycle`, or `BeforeScenario` saying whether it relaunched. Todo's fixture first. |
| Q10 | When does a warm runner end: its suite's last feature file closed, an idle time, or only with the IDE? | Last feature file closed, plus an idle time (10 minutes); a setting. |
| Q11 | Should the warm runner start when a feature file opens, or on the first Run? | When it opens, so the first Run is instant too; a setting for machines where a waiting app is unwelcome. |

## 9. Risks

- **Two paths to one scenario.** Step through runs in the runner, a gate run in `dotnet test`.
  AD-025 already requires them to behave the same; a scenario that passes in one and fails in the
  other is a bug in the runner, and is treated as one.
- **The language server's lifetime.** A Step-through run lives in the server's process tree; a
  restarted server must end its runner, app and fake server (AD-025's job object does this for the
  Presenter; the server needs the same).
- **The keys.** F10/F11/F5 on a paused Step-through run are not the debugger's. The guard is "a
  Step-through run is paused, and no debugger is in break mode" (the reverse of f40b01aa's guard).
