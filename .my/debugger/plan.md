# Running and debugging a scenario: the plan

Status: **decided, not started**. Date: 2026-10-04.
The design and its reasons are in [proposal.md](proposal.md); this file is the work, in order, so
a session can pick it up cold. Every open question of the proposal (Q1-Q11) is **accepted as its
leaning** (section 2).

Update the **Progress** column of section 4 after each step, with the commit.

## 1. Where things are

| What | Where |
| --- | --- |
| The engine's step hook: `IBddBreakHook`, `BddBreakpoints` (breakpoints file, `bdd-paused.json`, moves over/into/out, `ForDebugger()`) | `srcnew/Brinell.Bdd/BddBreakpoints.cs`; set in `srcnew/Brinell.Bdd/BddExecution.cs:40` |
| The runner protocol: `BddRunnerStart`, `BddRunnerClient.StartAsync`, messages `Open`/`Next`/`Skip`/`Cancel`/`Close`/`Step`/`Failed` | `srcnew/Brinell.Bdd/Runner/BddRunnerClient.cs`, `BddRunnerMessages.cs`, `BddRunnerChannel.cs` |
| The runner itself (the suite's `Brinell.Uat.dll` entry point); sets `Breaks = null` | `srcnew/Brinell.Uat/Runner/BddRunnerProgram.cs`, `BddRunnerHost.cs` |
| The Presenter as a runner host (one runner per run, `TakeOrStartAsync`) | `srcnew/Brinell.Presenter/Services/BddExecutionService.cs`; build: `PresenterShellViewModel.cs` (`_projectBuilder`, its Build command only) |
| The gate: one case per scenario, results written | `srcnew/Brinell.Uat/UatScenarioTestBase.cs` (`RecordScenarioResult`), `UatScenarioTheory.cs` |
| A fixture's lifecycle | `srcnew/Brinell.Uat/IUatScenarioLifecycle.cs` (`BeforeFile`, `BeforeScenario`); Todo's: `samples/Todo/tests/Brinell.Samples.Todo.Uat/Runtime/TodoUatFixture.cs` |
| The language server (`brinell lsp`): preview, results lookup, run targets | `tools/Brinell.Cli/Lsp/BddLanguageServer*.cs`, `BddRunResults.cs`, `Messages.cs` |
| The preview page (both IDEs): paused bar, breakpoint gutter, "running" label | `srcnew/Brinell.Bdd.Workspace/Preview/preview-host.js`, `preview.css` |
| Visual Studio: runs, builds, debug attach, the paused bar, F10 takeover | `tools/Brinell.Bdd.VisualStudio/BddRunController.cs`, `PreviewController.cs`, `BrinellBddPackage.cs/.vsct` |
| VS Code: runs (`dotnet build <project>` then `dotnet test`), debug attach, stepping, breakpoints | `tools/Brinell.Bdd.VsCode/src/tests.ts`, `stepping.ts`, `breakpoints.ts`, `preview.ts` |
| Decisions to amend | `.docs/decisions/ad-022-the-ides-run-the-gates-engine.md`, `ad-025-a-suite-runs-in-a-process-of-its-own.md` |
| Guides to update | `docs/guides/visual-studio-preview.md`, `docs/guides/vscode-extension.md`; the Presenter's guide (`docs/platform-guides/presenter.md`) |
| Manual checks | `.my/uat/manual/visual-studio-test-plan.md` section 6 |

## 2. Decisions

From the proposal, with its questions settled as their leanings:

| # | Decision |
| --- | --- |
| D1 | **Step through** runs in the suite's runner process (AD-025), driven by a host: the language server for the IDEs, the Presenter itself. |
| D2 | One breakpoints file. In Step through the **host** reads it between steps; in Debug the **engine** reads it, only under a debugger. CI runs neither. |
| D3 | Moves are the host's: over = one `Next`; continue = `Next` until a breakpoint, a failure or the end; stop = `Cancel` + `Close`. On a paused Step-through run F10/F11/Shift+F11/F5/Shift+F5 are the BDD moves; otherwise the debugger's. |
| D4 | A runner run writes the same `__result.json` and manifest record as the gate, marked `"mode": "step"` or `"run"`. |
| D5 | A stop shows the step's values, app and page; **Screenshot now**. |
| D6 | **Debug from here**: attach the debugger to the runner at a stop; the runner breaks before the next step (a `Break` message). Later phase. |
| F1, F2 | Build only the suite's project; skip the build when nothing it depends on changed. |
| I1 | A Run reads every file the build only copies (feature, step, config, apps, test data) **from the source folder** (Q8). |
| I2 | A **warm runner** per open suite, from a **shadow copy** of the build output, started when a feature file of the suite opens (Q11) and again after each build; ended when the suite's last feature file closes plus 10 minutes idle, and with the host (Q10); both are settings. |
| I3 | The warm runner starts the fixture; each scenario begins with `BeforeScenario` as a clean start. A fixture says whether it can reset without a launch (Q9: on `IUatScenarioLifecycle`; Todo first). |
| I4 | Preview buttons: **Run** (warm runner), **Run as test** (`dotnet test`, the gate), **Step**, **Debug**. Step through is the default way to stop at a step; Debug the advanced one (Q1). |
| Q2, Q5 | The runner reports `## Step:` calls so Step into/out and breakpoints inside a callable work in Step through. |
| Q3, Q4 | Many apps and Outline rows need nothing special; check that the runner opens a row by its `[n]` name. |
| Q6 | Debug keeps the F10 takeover (4116aacd, f40b01aa). |
| Q7 | The Presenter keeps Step through and gains Debug from here (D6). |

## 3. Working rules for this plan

- **Build and test** from the Brinell root. Unit: `dotnet test testsnew\<Project>.Tests -v:minimal /nr:false`.
  The VSIX needs Visual Studio's MSBuild:
  `& "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" tools\Brinell.Bdd.VisualStudio\Brinell.Bdd.VisualStudio.slnx /restore /p:Configuration=Debug /p:DeployExtension=false /v:minimal`
  (`DeployExtension=false`: the owner runs the experimental instance; they F5 the VSIX solution to load a new build).
  VS Code: `npx tsc -p . --noEmit` in `tools\Brinell.Bdd.VsCode`.
- **One UI test process at a time**, across clones: another session may be running UI tests in
  `E:\repos\MainConstruction`. Check `Get-Process testhost` before a UI run; prefer the Todo
  **service** suite (`Brinell.Samples.Todo.IntegrationTests`, no UI) for server and runner tests.
- **Never launch the Construction app** unless asked.
- **IDE behaviour is checked by the owner** in the experimental instance (Visual Studio) and in VS
  Code; write what to check in the step's "Done when", and in the manual test plan.
- Known red test before any of this: `Brinell.Uat.Tests.BddRunnerProcessTests.ACanceledStep_AnswersCanceled`.
- Commit per step; end messages with the repo's attribution line. Push only when asked.

## 4. Steps

### Phase 0: faster runs today (small, independent)

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| 0.1 | **Visual Studio builds the suite's project, not the solution** (F1): `BuildAsync` uses `IVsSolutionBuildManager.StartSimpleUpdateProjectConfiguration` for the suite's project (the run target already names it). Run and Debug both. | `BddRunController.cs` | With `Brinell.sln` open, a Run builds Todo's projects only (Output pane); owner times it before/after. | |
| 0.2 | **Skip a build nothing needs** (F2), Visual Studio: no build when the suite's project is up to date (`IVsBuildableProjectCfg`/`QueryStartUpToDateCheck`, or remember the last successful build and the projects changed since). VS Code: the same with a cheap check (`dotnet build` of an up-to-date project still costs ~10 s, so track saves/builds per project). | `BddRunController.cs`; `tests.ts` | A second Run with nothing changed goes straight to `dotnet test`; a C# change still builds. |  |

### Phase T: tests that drive the IDE side, so steps are checked without the owner

Layers, cheapest first (the reasoning: the four bugs of section 6 were all in what only an IDE
does; layers 1 and 2 already exist and catch none of them):

| Layer | What | Where | Runs |
| --- | --- | --- | --- |
| 1 | engine and runner | `Brinell.Bdd.Tests`, `Brinell.Uat.Tests` (exist) | every step |
| 2 | language server over JSON-RPC (`ServerHarness`) | `Brinell.Cli.Tests/Lsp` (exists: preview with runs, results changed, run targets) | every step |
| 3 | **the preview page in Chromium**, through Brinell.Html page objects (dogfooding): an IDE stand-in loads `BddPreview.HostPage`, sends the IDE's messages, records what the page sends, maps the results host to a folder as WebView2 does | `testsnew/Brinell.Bdd.Workspace.UITests` (new) | every step; headless |
| 4 | VS Code extension in a real VS Code (`@vscode/test-electron`) | `tools/Brinell.Bdd.VsCode/test` | from step 1.5; opens a window |
| 5 | Visual Studio driven from outside (T.2): DTE over COM, the preview over CDP, its own root suffix so the owner's Exp instance is untouched | `testsnew/Brinell.Bdd.VisualStudio.UITests` (new; opt-in) | on demand; ~1 min a start; desktop; one UI process at a time |
| 6 | dogfood UAT: the IDE as an app under test, Visual Studio's shell through Brinell.Wpf (FlaUI) and the preview's WebView2 through Brinell.Html over CDP | later (3.5) | on demand |

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| T.1 | **Layer 3**: the project, the IDE stand-in (`PreviewIde`), the page object (`ScenarioPreviewPage`), and tests for what exists: an update renders; a paused message shows the bar, the step text and marks the step; Step into only at a call, Step out only inside one; a move button sends `debug {move}`; going on hides the bar; `running {paused: true}` marks the heading "paused"; a breakpoint click sends `toggleBreak`. In `Brinell.sln`. | `testsnew/Brinell.Bdd.Workspace.UITests/` | the tests pass headless; `SolutionTests` passes | **done**: 6 tests, 32 s headless; the paused-label test fails on the script before 8131cee5 |
| T.2 | **Layer 5 spike**: can a test drive Visual Studio 18 Insiders with this VSIX? | scratch only | **done** 2026-10-04: `[IdeFact]` (Microsoft.VisualStudio.Extensibility.Testing.Xunit) is on neither nuget.org nor the public vs-impl feed, so the fallback holds and works: install the VSIX into a root suffix of its own (`VSIXInstaller /quiet /rootSuffix:BrinellTest`), then **`devenv /rootSuffix BrinellTest /updateconfiguration` once** (without it the instance has no Brinell commands); start `devenv <slnx> /rootSuffix BrinellTest` (a solution on the command line skips the start window) with `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=<port>`; reach its DTE through the Running Object Table (`!VisualStudio.DTE.18.0:<pid>`, ready in 1-5 s); retry calls on `RPC_E_SERVERCALL_RETRYLATER`/`RPC_E_CALL_REJECTED`; `ItemOperations.OpenFile`, `ExecuteCommand("View.ScenarioPreview")` (the DTE name; `Commands.Item("{68a7da07-ac82-47a1-bd70-257d5ba3b27c}", 0x0100)` finds it by id); the preview is a CDP target titled "Scenario Preview" (a `data:` URL). `DTE.Quit()` closes it, but not while a build runs (opening the solution can start one: a modal "The build must be stopped" blocks the instance), so the fixture cancels the build (`Build.Cancel`), waits for design mode, then quits, and ends the process if it is still there. | |
| T.3 | **Layer 5 as a test project**, `testsnew/Brinell.Bdd.VisualStudio.UITests` (net10, in `Brinell.sln`): a `VisualStudioIde` fixture doing T.2's route (install, update configuration, start, DTE, CDP), the preview reached as a Brinell.Html context over CDP with T.1's `ScenarioPreviewPage`; **opt-in** (`BRINELL_IDE_TESTS=1`, otherwise skipped with the reason, so the suite stays green with nothing set). First tests: the preview opens bound for TOD.01; then yesterday's Debug: breakpoint on TOD.01.1 line 24, Debug from the preview, wait for break mode (DTE), `bdd-paused.json` names line 24 and the active document is the feature file at line 24; F10; paused at line 25; Continue; the result is written. Would have caught 5239365c, 4116aacd and f40b01aa. | `testsnew/Brinell.Bdd.VisualStudio.UITests/` | green, with no other UI test process running (the Debug test launches the Todo app) | |
| T.4 | Each later step's "Done when" names its layer 2/3/5 check next to the owner's. | this file | | |

### Phase 1: Step through (D1-D5)

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| 1.1 | **The runner writes results** (D4): after a scenario, the runner records it as `UatScenarioTestBase.RecordScenarioResult` does (shared code, not a copy), with the failure screenshot, `"mode"` in its metadata. | `BddRunnerHost.cs`; the recording code moved where both use it | A Presenter run of a Todo scenario shows ticks in the IDE preview; Presenter and Uat tests green. | |
| 1.2 | **The runner reports calls** (Q2, Q5): each `Step` message says the step's file, line, depth and whether it is a `## Step:` call; `Next` can go into a call. | `BddRunnerMessages.cs`, `BddRunnerHost.cs`, the step session | Runner tests: stepping into and out of a callable in the Todo step file. | |
| 1.3 | **The language server as a runner host** (D1-D3): `brinell/step/start {uri, line, suite}`, `brinell/step/move {over\|into\|out\|continue\|stop}`, `brinell/step/paused` and `brinell/step/ended` notifications; the host reads the breakpoints file between steps (same matching as `BddBreakpoints.SameFile`); one run at a time; runner ended with the server (job object as the Presenter). | `tools/Brinell.Cli/Lsp/BddLanguageServer.Step.cs` (new), `Messages.cs` | Server tests on the Todo **service** suite: stop at a breakpoint, over, into a call, out, continue to the end, stop; results written. | |
| 1.4 | **Visual Studio: Step** button beside Debug; the paused bar fed by `brinell/step/paused`; the keys of D3 only while a Step-through run is paused and no debugger is in break mode; the feature file at the paused line. | `PreviewController.cs`, `BddRunController.cs`, `.vsct`, `preview-host.js` | Owner: TOD.01.1 with a breakpoint on `When I tap Sync`, F10 to the end, it **passes**. | |
| 1.5 | **VS Code: Step** as a Test Explorer run profile and a preview button; keybindings with a `when` context `brinell.stepPaused`. | `tests.ts`, `stepping.ts`, `preview.ts`, `package.json` | Owner: the same check in VS Code. | |
| 1.6 | **The stop's details and Screenshot now** (D5): the paused notification carries arguments as read, app, page; a `brinell/step/screenshot` request. | `BddLanguageServer.Step.cs`, `preview-host.js` | Server test for the payload; owner sees it in the bar. | |
| 1.7 | **The Presenter** shows its runs' results in the IDEs (from 1.1) and uses 1.2 for stepping into calls. | Presenter view models | Presenter tests; a Presenter run visible in the IDE preview. | |

### Phase 2: Instant Run (I1-I4)

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| 2.1 | **Scenario files from source** (I1, Q8): `BddRunnerOpen` gets the source folder; the runner reads feature, step, config, apps and test-data files from there. | runner, `BddRunnerMessages.cs`, the server's run target | Edit a step's text, Run: no build, and the edited text runs. | |
| 2.2 | **Run on the runner** (I4): the preview's Run uses the language server's runner run (1.3 without stops); **Run as test** is today's `dotnet test` path; the four buttons in both IDEs. | server, `BddRunController.cs`, `tests.ts`, `preview-host.js`, `BddPreview` | Owner: Run and Run as test both pass TOD.01.1; results marked with their mode. | |
| 2.3 | **The warm runner** (I2, Q10, Q11): per open suite, from a shadow copy of the build output; started when a feature file of the suite opens and after each build; replaced after a build; ended on last close + idle, and with the server; settings for both. The Presenter uses the same (replaces its one-runner-per-run). | server; a shared warm-runner class next to `BddRunnerClient`; `BddExecutionService.cs` | A build succeeds while a warm runner waits; a Run reaches the fixture in under a second. | |
| 2.4 | **The warm app** (I3, Q9): `IUatScenarioLifecycle` says whether a clean start needs a launch; the warm runner starts the fixture; `BeforeScenario` is the clean start; Todo's fixture first. | `IUatScenarioLifecycle.cs`, `TodoUatFixture.cs`, runner | TOD.01 run twice in a row: the second does not launch the app and passes as under Run as test. | |

### Phase 3: Debug from here, and the docs

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| 3.1 | **Debug from here** (D6): a `Break` runner message; Visual Studio and VS Code attach to the runner at a Step-through stop; the Presenter offers it too (Q7). | runner, server, both IDEs, Presenter | Owner: from a Step-through stop, F11 lands in the step definition. | |
| 3.2 | **ADs**: AD-022 (two modes; R2 amended; Run vs Run as test; files from source), AD-025 (IDE hosts; warm shadow-copied runner; clean start without a launch). | `.docs/decisions/` | | |
| 3.3 | **Guides and manual plan**: the four buttons, when to use which; Debug freezes a fake server in the test process; the manual test plan's section 6 for both modes. | `docs/guides/visual-studio-preview.md`, `vscode-extension.md`, `docs/platform-guides/presenter.md`, `.my/uat/manual/visual-studio-test-plan.md` | | |
| 3.4 | **Close the topic**: decisions in `.docs/`, guides in `docs/`, delete `.my/debugger/` and its row. | | | |

## 5. Per host, at the end

| Part | Visual Studio | VS Code | Presenter |
| --- | --- | --- | --- |
| F1 project build | 0.1 | already | already |
| F2 skip build | 0.2 | 0.2 | already (builds on its Build only) |
| Step through | 1.3, 1.4 | 1.3, 1.5 | already; calls from 1.2 |
| Results in the preview | 1.1 | 1.1 | 1.1, 1.7 |
| Files from source | 2.1 | 2.1 | already |
| Warm runner and app | 2.3, 2.4 | 2.3, 2.4 | 2.3, 2.4 |
| Run as test | 2.2 (today's Run) | 2.2 (Test Explorer) | not added: the IDEs have it |
| Debug from here | 3.1 | 3.1 | 3.1 |

## 6. What was already done (before this plan)

Found while trying Debug on Todo's TOD.01.1, 2026-10-03/04; pushed up to 8131cee5, the last two
not pushed yet:

| Commit | What |
| --- | --- |
| 5239365c | Debug attaches to the testhost once (it printed its process id twice; the second attach cancelled the run). Both IDEs. |
| 8131cee5 | The scenario-failed exception is non-user code (no stop on it); no xUnit timeout under a debugger; "paused in the debugger" label in Visual Studio. |
| 4116aacd | Visual Studio: F10 at a scenario stop is the BDD step over; the feature file comes back in front after break mode. |
| f40b01aa | Visual Studio: F10 is taken over only for a pause the debugger is in (a leftover `bdd-paused.json` does not count). |

Still open from that session, not part of this plan unless it gets in the way: the failure
screenshot does not show in the preview (its file name has a `;`); the owner has not yet
confirmed "paused in the debugger" shows.
