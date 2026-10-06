# Running and debugging a scenario: the plan

Status: **in progress**: Phases T, 0 and 1 done, and 2.1-2.2 (2026-10-06). **Next: 2.3**.
Started 2026-10-04.
The design and its reasons are in [proposal.md](proposal.md); this file is the work, in order, so
a session can pick it up cold. Every open question of the proposal (Q1-Q11) is **accepted as its
leaning** (section 2).

Update the **Progress** column of section 4 after each step, with the commit.

## 0. Pick up here

**What works now.** A scenario can be *stepped through* without a debugger in both IDEs: the
preview's **⏭ Step** button (VS Code also a Test Explorer profile and *Step Through Scenario*)
builds the suite's project, the language server opens the scenario in the suite's runner process
and pauses before each step; the bar and the keys move it; the result is written as the gate's,
marked `step`. A stop shows the step's page, app and arguments as read (a debugged stop too), and a
Step-through stop offers **📷 Screenshot** (the runner takes it while idle). Debug (the C# debugger,
the engine's own breaks) works as before. A Presenter run's result shows in the IDEs' preview, and
the Presenter stops at an IDE breakpoint inside a `## Step:` body.

**How Step through flows** (read these files in this order to learn it):

1. The IDE asks `brinell/step/start {uri, line, suite}` (`tools/Brinell.Cli/Lsp/BddLanguageServer.Step.cs`).
2. The server starts the runner from `BddSuite.RunnerAssemblies` (the fixture's built assembly and
   the test project's output; `srcnew/Brinell.Bdd.Workspace/BddSuite.cs`) in a `BddRunnerJob`, with
   `BRINELL_TEST_RESULTS_DIR` set to the folder the preview reads, and sends
   `BddRunnerOpen(..., Mode: "step")`.
3. `BddRunnerStepper` (`srcnew/Brinell.Bdd/Runner/BddRunnerStepper.cs`) turns the runner's
   positions (`BddRunnerPosition`: file, line, depth, the calls around it) into stops, a call being
   a stop of its own, and makes the moves; between steps it asks `BddBreakpoints.Lists` (the same
   breakpoints file and matching as Debug).
4. The server notifies `brinell/step/paused` and `brinell/step/ended`; the IDE shows the stop as it
   shows a debugged run's (same bar, `paused` message, same editor jump) and sends moves with
   `brinell/step/move {over|into|out|continue|stop}`.
5. At the scenario's end the runner records the result itself (`UatScenarioResults`,
   `srcnew/Brinell.Uat/UatScenarioResults.cs`); the server then closes the scenario and ends the runner.

**Known limits, decided or found:**
- Visual Studio: Shift+F11 and Shift+F5 never reach the extension when nothing is debugged (the
  commands are disabled then); the bar's buttons and Ctrl+Shift+F11 stand in. VS Code has all five keys.
- A Step-through run starts a new runner (and app) each time: about 5 s from the click to the first
  stop on TOD.01.1. Phase 2's warm runner removes that.
- One Step-through run per language server at a time; a second start is refused with the reason.
- The server reads every request's `uri` through `Key` (`BddLanguageServer.cs`), which also
  accepts VS Code's escaped drive colon (`file:///e%3A/...`); keep using it for new requests.
- `Brinell.Uat.Tests.BddRunnerProcessTests.ACanceledStep_AnswersCanceled` was red before this plan; still is.

**The owner has not yet tried by hand:** Step through in the Visual Studio experimental instance
(1.4, with 1.6's details and Screenshot: manual plan B-21 to B-24) and in VS Code with the keys
(1.5); VS Code's skipped build when nothing changed (0.2); a Presenter run of TOD.01.1 showing in
the IDE preview (1.7).

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
| Step through, host side: stops and moves (`BddRunnerStepper`), the runner's job object (`BddRunnerJob`, also the Presenter's) | `srcnew/Brinell.Bdd/Runner/BddRunnerStepper.cs`, `BddRunnerJob.cs`; breakpoint matching for a host: `BddBreakpoints.Lists` |
| Step through, the server: `brinell/step/start`, `/move`, `/paused`, `/ended`; their messages | `tools/Brinell.Cli/Lsp/BddLanguageServer.Step.cs`, `Messages.cs` (`StepStartParams` ...) |
| What a runner runs from | `BddSuite.RunnerAssemblies` (`srcnew/Brinell.Bdd.Workspace/BddSuite.cs`, set in `Load`) |
| A runner run's result | `srcnew/Brinell.Uat/UatScenarioResults.cs` (the gate's and the runner's); the runner calls it in `BddRunnerHost.RecordIfFinished` |
| Visual Studio, Step through | `BddRunController.StepAsync`/`StepEnded`, `PreviewController` (`_stepping`, `OnStepPaused`, `StepMove`, `Intercept` for the keys), `BddServerClient` (`StepStartAsync`, `StepPaused` event) |
| VS Code, Step through | `src/tests.ts` (`step`, `stepAt`, `stepEnded`, `own`), `src/stepping.ts` (`onStepPaused`, `move`), `src/extension.ts` (notifications, `brinellBdd.stepThrough`, the `onStep` test API), `package.json` (keys with `brinellBdd.stepPaused && !inDebugMode`) |
| Tests, by layer | 1: `testsnew/Brinell.Bdd.Tests/BddRunnerStepperTests.cs`, `testsnew/Brinell.Uat.Tests/BddRunnerProcessTests.cs`; 2: `testsnew/Brinell.Cli.Tests/Lsp/StepTests.cs`; 3: `testsnew/Brinell.Bdd.Workspace.UITests`; 4: `tools/Brinell.Bdd.VsCode/src/test/` (`run.ts`, `suite.ts`); 5: `testsnew/Brinell.Bdd.VisualStudio.UITests` |
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
  `E:\repos\MainConstruction`. Check `Get-Process testhost,devenv` before a UI run, and build
  nothing while one runs. Server and runner tests need no app: `StepTests` uses a small counter suite
  whose fixture lives in the test project (the Todo service suite is a *head* of the MAUI suite and
  links its feature files, so it is awkward to drive alone).
- **The test layers** (section 4, Phase T), commands from the Brinell root unless said:
  - 1 and 2: `dotnet test testsnew\Brinell.Bdd.Tests`, `testsnew\Brinell.Uat.Tests`,
    `testsnew\Brinell.Cli.Tests --filter "FullyQualifiedName~LanguageServer.Tests"` (seconds).
  - 3: `dotnet test testsnew\Brinell.Bdd.Workspace.UITests` (headless Chromium, ~30 s).
  - 4: in `tools\Brinell.Bdd.VsCode`, `npm run test:ide` (opens a VS Code window and the Todo app;
    needs `tools\Brinell.Cli` built; ~15 s).
  - 5: `$env:BRINELL_IDE_TESTS=1; dotnet test testsnew\Brinell.Bdd.VisualStudio.UITests` (a test
    Visual Studio, root suffix `BrinellTest`, and the Todo app; ~2 min). Do not touch either while it runs.
  Layers 4 and 5 set the developer's step breakpoints aside and put them back.
- **Editing files from a shell**: string edits through `python -c`/heredocs mangle backslashes
  (`\u25B6`, regexes, Windows paths) and Git Bash rejects some heredocs; use the editor tool, or a
  script file.
- **Never launch the Construction app** unless asked.
- **IDE behaviour is checked by the owner** in the experimental instance (Visual Studio) and in VS
  Code; write what to check in the step's "Done when", and in the manual test plan.
- Known red test before any of this: `Brinell.Uat.Tests.BddRunnerProcessTests.ACanceledStep_AnswersCanceled`.
- Commit per step; end messages with the repo's attribution line. Steps 0.1 to 1.5 were
  each pushed once done, while the owner had asked to continue; push only when asked.

## 4. Steps

### Phase 0: faster runs today (small, independent)

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| 0.1 | **Visual Studio builds the suite's project, not the solution** (F1): `BuildAsync` uses `IVsSolutionBuildManager.StartSimpleUpdateProjectConfiguration` for the suite's project (the run target already names it). Run and Debug both. | `BddRunController.cs` | With `Brinell.sln` open, a Run builds Todo's projects only (Output pane); owner times it before/after. Automated: layer 2 (`runTarget` names the project before a build); layer 5 (the Debug test, whose run now builds only `Brinell.Samples.Todo.Uat`). | **done** 2026-10-06: the server names the suite's project before its first build; Visual Studio builds that project through DTE `BuildProject` (the solution only when no suite holds the file); the Debug test passed with it (click to stop at line 24: 42 s, the build included). |
| 0.2 | **Skip a build nothing needs** (F2), Visual Studio: no build when the suite's project is up to date (`IVsBuildableProjectCfg`/`QueryStartUpToDateCheck`, or remember the last successful build and the projects changed since). VS Code: the same with a cheap check (`dotnet build` of an up-to-date project still costs ~10 s, so track saves/builds per project). | `BddRunController.cs`; `tests.ts` | A second Run with nothing changed goes straight to `dotnet test`; a C# change still builds. Automated: not until layer 4 (VS Code in a test window, step 1.5); Visual Studio relies on its own up-to-date check of the one project. | **done** 2026-10-06: VS Code watches the workspace (not bin, obj, TestResults, .git, .vs) and skips `dotnet build` when nothing changed since the project's last clean build; Visual Studio's one-project build is incremental itself. Not yet tried by hand in VS Code. |

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
| T.3 | **Layer 5 as a test project**, `testsnew/Brinell.Bdd.VisualStudio.UITests` (net10, in `Brinell.sln`): a `VisualStudioIde` fixture doing T.2's route (install, update configuration, start, DTE, CDP), the preview reached as a Brinell.Html context over CDP with T.1's `ScenarioPreviewPage`; **opt-in** (`BRINELL_IDE_TESTS=1`, otherwise skipped with the reason, so the suite stays green with nothing set). First tests: the preview opens bound for TOD.01; then yesterday's Debug: breakpoint on TOD.01.1 line 24, Debug from the preview, wait for break mode (DTE), `bdd-paused.json` names line 24 and the active document is the feature file at line 24; F10; paused at line 25; Continue; the result is written. Would have caught 5239365c, 4116aacd and f40b01aa. | `testsnew/Brinell.Bdd.VisualStudio.UITests/` | green, with no other UI test process running (the Debug test launches the Todo app) | **done** 2026-10-05: both green (Debug 1 min 15 s). The fixture builds and deploys the VSIX like F5 (`DeployExtension=true`, `VSSDKTargetPlatformRegRootSuffix`; VSIXInstaller waits for every running Visual Studio), removes other copies of the extension in the instance, backs up and restores the developer's BDD breakpoints and the solution's `.suo`, disables C# breakpoints, time-limits every call, logs each step to `TestResults/ide-tests.log`, and always ends Visual Studio. It found three extension bugs, fixed: two pages titled "Scenario Preview" (tool window and margin); the feature file not brought back when break mode came before the paused file was read; the **build output's copy** opened instead of the source file at a stop. Run: `$env:BRINELL_IDE_TESTS=1; dotnet test testsnew\Brinell.Bdd.VisualStudio.UITests`; do not touch the test Visual Studio while it runs. |
| T.4 | Each later step's "Done when" names its layer 2/3/5 check next to the owner's. | this file | | **done** 2026-10-06 |

### Phase 1: Step through (D1-D5)

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| 1.1 | **The runner writes results** (D4): after a scenario, the runner records it as `UatScenarioTestBase.RecordScenarioResult` does (shared code, not a copy), with the failure screenshot, `"mode"` in its metadata. | `BddRunnerHost.cs`; the recording code moved where both use it | A Presenter run of a Todo scenario shows ticks in the IDE preview; Presenter and Uat tests green. Automated: layer 1 (a runner run writes `__result.json` and its manifest record); layer 2 (the preview shows a runner run). | **done** 2026-10-06: `UatScenarioResults` (in `Brinell.Uat`) writes the report and the manifest record for the gate and the runner alike, with `mode` (`test`, `runner`, or what a host passes in `BddRunnerOpen.Mode`); the runner records a scenario once its last step has run, with a failure screenshot when the config asks. Runner process tests: a finished scenario's result and mode, none for one left early. |
| 1.2 | **The runner reports calls** (Q2, Q5): each `Step` message says the step's file, line, depth and whether it is a `## Step:` call; `Next` can go into a call. | `BddRunnerMessages.cs`, `BddRunnerHost.cs`, the step session | Runner tests: stepping into and out of a callable in the Todo step file. Automated: layer 1 (runner tests). | **done** 2026-10-06: `BddRunnerOpened.Next` and `BddRunnerStep.Ran`/`Next` are `BddRunnerPosition`s (file, line, text, depth, the calls around it outermost first), plain text and numbers. A call's body is already one invocation a step, so Next goes into a call by itself; over and out are the host's, from the depths (1.3). Runner protocol 2: a suite built with protocol 1 is told to rebuild. Runner process test: a scenario that calls a `## Step:`. |
| 1.3 | **The language server as a runner host** (D1-D3): `brinell/step/start {uri, line, suite}`, `brinell/step/move {over\|into\|out\|continue\|stop}`, `brinell/step/paused` and `brinell/step/ended` notifications; the host reads the breakpoints file between steps (same matching as `BddBreakpoints.SameFile`); one run at a time; runner ended with the server (job object as the Presenter). | `tools/Brinell.Cli/Lsp/BddLanguageServer.Step.cs` (new), `Messages.cs` | Server tests on the Todo **service** suite: stop at a breakpoint, over, into a call, out, continue to the end, stop; results written. Automated: layer 2 (server tests on the Todo service suite). | **done** 2026-10-06: `BddRunnerStepper` (in `Brinell.Bdd.Runner`) pauses between the runner's steps and moves as `BddBreakpoints` does under a debugger: a call is a stop of its own, into goes inside without running, over/out by depth, every move stops at a breakpoint (`BddBreakpoints.Lists`, read each step) and after a failed step. The server's `brinell/step/start`, `brinell/step/move`, `brinell/step/paused`, `brinell/step/ended`; the runner starts from `BddSuite.RunnerAssemblies` (the fixture's built assembly, the test project's output) inside a `BddRunnerJob` (moved from the Presenter) and writes its result to the suite's results folder, marked `step`. Tests: stepper with a fake runner (layer 1); server tests on a counter suite whose fixture is in `Brinell.Cli.Tests` rather than the Todo service suite, which runs as a head of the MAUI suite (layer 2, 4 tests, ~1 s). |
| 1.4 | **Visual Studio: Step** button beside Debug; the paused bar fed by `brinell/step/paused`; the keys of D3 only while a Step-through run is paused and no debugger is in break mode; the feature file at the paused line. | `PreviewController.cs`, `BddRunController.cs`, `.vsct`, `preview-host.js` | Owner: TOD.01.1 with a breakpoint on `When I tap Sync`, F10 to the end, it **passes**. Automated: layer 3 (Step sends its message; the bar on `step/paused`); layer 5 (Step through TOD.01.1 with F10 to the end, and it passes). || **done** 2026-10-06: a **Step** button on each scenario (`data-step`, beside Run and Debug, in both IDEs' page); Visual Studio builds the suite's project, then `brinell/step/start`; the paused bar is fed by `brinell/step/paused` (a failed stop says so, with the message), the moves go to `brinell/step/move`, Cancel stops it. F10, F11 and F5 are the moves while a Step-through run is paused and no debugger is in break mode (command events, as the Debug F10 takeover). **Not as D3 asked:** Shift+F11 and Shift+F5 never reach the extension when nothing is debugged, since Visual Studio disables those commands then; the bar's buttons and Ctrl+Shift+F11 stand in. Tests: layer 3 (Step sends `step {line}`; a failed stop's bar); layer 5 (Step through TOD.01.1: paused at 18, F5 to the breakpoint at 24, F10 to the end, passed; 5 s from the click to the first pause). Owner: try it in the experimental instance. |
| 1.5 | **VS Code: Step** as a Test Explorer run profile and a preview button; keybindings with a `when` context `brinell.stepPaused`. | `tests.ts`, `stepping.ts`, `preview.ts`, `package.json` | Owner: the same check in VS Code. Automated: layer 4 (VS Code in a test window). || **done** 2026-10-06: the preview's Step button, a **Step through** Test Explorer profile and a *Step Through Scenario* command (the scenario at the caret); the run is the suite's whose project holds the file (a file linked into the service suite went there before); its test result comes from `brinell/step/ended`. F10, F11, Shift+F11, F5 and Shift+F5 are the moves while `brinellBdd.stepPaused && !inDebugMode`: unlike Visual Studio, all five of D3. Found and fixed: the server read VS Code's `file:///e%3A/...` as `/e:/...`, so no suite held the file (server test added). Tests: layer 4, `npm run test:ide` in `tools/Brinell.Bdd.VsCode`, starts the installed VS Code with a profile of its own (`BRINELL_VSCODE` to point elsewhere), sets the developer's step breakpoints aside, and steps TOD.01.1 through the extension's commands: 18, over 19, into 23, continue to the breakpoint at 24, over to the end, passed (15 s, the build included). Owner: the same by hand, with the keys. |
| 1.6 | **The stop's details and Screenshot now** (D5): the paused notification carries arguments as read, app, page; a `brinell/step/screenshot` request. | `BddLanguageServer.Step.cs`, `preview-host.js` | Server test for the payload; owner sees it in the bar. Automated: layer 2 (the payload); layer 3 (the bar shows the details). | **done** 2026-10-06: `BddRunnerPosition` and `BddRunnerStop` carry optional `Arguments`, `App`, `Page` (from `BddBreakpoints.Describe`; protocol stays 2); a call's stop has the page only (its text has its values). `brinell/step/paused` carries them; so does Debug's `bdd-paused.json`. `BddRunnerScreenshot` (answered `BddRunnerScreenshotTaken` or `Failed`) goes through the fixture's `ScreenshotService` as the failure screenshot does; `brinell/step/screenshot` holds the run still while the runner takes it. The bar has a details line and **📷 Screenshot** (only when the `paused` message says `stepping`); Visual Studio opens the file in the editor, VS Code beside. Tests: layer 1 (a stop's details, a call's page only); layer 2 (arguments at line 8 of the counter suite; a screenshot while paused, refused when none is); layer 3 (the details line and Screenshot sent; a debugged stop has no Screenshot); layer 4 asserts TOD.01.1's breakpoint stop: page `Todo List`, `control = "Sync"`. Owner: manual plan B-21 to B-24. |
| 1.7 | **The Presenter** shows its runs' results in the IDEs (from 1.1) and uses 1.2 for stepping into calls. | Presenter view models | Presenter tests; a Presenter run visible in the IDE preview. Automated: Presenter tests; layer 3 (a Presenter run in the preview). | **done** 2026-10-06: the results-folder rule is one function, `TestArtifactOptions.RootFor` (`Brinell.Core`), which the server's `BddRunResults.RootFor` now calls; the Presenter tells its runner that folder (`BRINELL_TEST_RESULTS_DIR`), so a run lands where the preview reads even from a copy of the output (Phase 2's shadow copy). Before, its results went to the repository above the **output** (its own tests wrote into Brinell's `TestResults`). The session keeps the runner's `Next` position: Play stops at an IDE breakpoint inside a `## Step:` body (`PresenterStepping.BreaksInStepFile`; the feature file's lines stay the rows'), and the Presenter's preview bar shows the stop's page, app and arguments. **Not as the notes said:** the Presenter keeps its row-based into/out (its rows already show calls and their steps, with their own tests) rather than moving to `BddRunnerStepper`. The preview showing a runner-mode run is 1.1's layer 2 test. Tests: Presenter (a finished scenario's result and manifest, mode `runner`, in the suite's results folder; `Next` inside a call; a step-file breakpoint). |

### Phase 2: Instant Run (I1-I4)

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| 2.1 | **Scenario files from source** (I1, Q8): `BddRunnerOpen` gets the source folder; the runner reads feature, step, config, apps and test-data files from there. | runner, `BddRunnerMessages.cs`, the server's run target | Edit a step's text, Run: no build, and the edited text runs. Automated: layer 2 (the runner runs the edited text with no build). | **done** 2026-10-06: the runner already opened the source feature and config, and found step files beside the feature; what read the build's copies was the **fixture** (Todo's reads `uat.config.md` beside its assembly) and an `AppsFile` only the build copies in (the Mixed suite could not be stepped through). Now a host sets `BRINELL_SUITE_SOURCE` for its runner (the server's Step run, the Presenter): `BddScenarioSource.GetConfigFilePath()` defaults to it and `BddScenarioSource.SuiteDirectory(output)` gives it to a fixture (Todo's and Todo Web's use it); `BddConfigParser.ParseFile` reads an `AppsFile` missing beside the config from the output. **Not as the step said:** an environment variable at the runner's start, not a field of `BddRunnerOpen`, since a fixture reads its config in a static initializer, before any open. Test data compiled in (Todo's embedded `TestData/*.json`) still needs a build. The IDEs skip the build before a Step-through run when only scenario files changed since the project last built (`*.feature.md`, `*.steps.md`, `*.video.md`, `uat.config.md`): VS Code from its workspace watcher, Visual Studio from `BddCodeChanges` (watches the folders of the solution's projects; an unsaved code document counts). A `dotnet test` Run still builds. Tests: layer 2 (a step file edited, and a step reading the config, which only the source has); Uat (an `AppsFile` from the output); layer 4 (after the run, the feature file rewritten, Step again: paused at 18 with no build). Layer 5 not run (a Visual Studio was open, and its VSIX install waits for every one). |
| 2.2 | **Run on the runner** (I4): the preview's Run uses the language server's runner run (1.3 without stops); **Run as test** is today's `dotnet test` path; the four buttons in both IDEs. | server, `BddRunController.cs`, `tests.ts`, `preview-host.js`, `BddPreview` | Owner: Run and Run as test both pass TOD.01.1; results marked with their mode. Automated: layer 3 (the four buttons and what each sends); layer 5 (Run and Run as test both pass TOD.01.1). | **done** 2026-10-06: the server's `brinell/run/start {uri, line?, suite}` runs the scenario at a heading (an outline's every row: they share its line) or every scenario of the file in one runner process, each opened, run to its end or first failure, recorded (mode `run`) and closed, with no stops; `brinell/run/ended` gives each scenario's status; `brinell/run/cancel`. One runner run at a time, a Step-through one included. The preview has **▶ Run** (runner), **▷ Run as test** (`dotnet test`), **● Debug**, **⏭ Step** on each scenario, and Run / Run as test / Debug on the title; the page sends `run`/`runFile` with `test` (Debug implies it). VS Code: the preview's Run, a *Run Scenario on the Runner* command and a "Run on the runner" Test Explorer profile; Test Explorer's default Run stays `dotnet test`. Visual Studio: `BddRunController.RunOnRunnerAsync`, results in the Output pane. Both skip the build as 2.1 does. Tests: layer 2 (a scenario past a breakpoint, marked `run`; the file, one failing; a Step refused meanwhile); layer 3 (the four buttons and what each sends); layer 4 (TOD.01.1 on the runner: passed, no build). Layer 5 not run (a Visual Studio was open). Owner: manual plan R-12 to R-14. |
| 2.3 | **The warm runner** (I2, Q10, Q11): per open suite, from a shadow copy of the build output; started when a feature file of the suite opens and after each build; replaced after a build; ended on last close + idle, and with the server; settings for both. The Presenter uses the same (replaces its one-runner-per-run). | server; a shared warm-runner class next to `BddRunnerClient`; `BddExecutionService.cs` | A build succeeds while a warm runner waits; a Run reaches the fixture in under a second. Automated: layer 2 (a build succeeds while a warm runner waits; a Run reaches the fixture in under a second). | |
| 2.4 | **The warm app** (I3, Q9): `IUatScenarioLifecycle` says whether a clean start needs a launch; the warm runner starts the fixture; `BeforeScenario` is the clean start; Todo's fixture first. | `IUatScenarioLifecycle.cs`, `TodoUatFixture.cs`, runner | TOD.01 run twice in a row: the second does not launch the app and passes as under Run as test. Automated: layer 1 (a second scenario does not launch the app). | |

**Notes for 2.3** (found in 2.1): the runner reads the **source** config, whose `Assemblies` rows
are relative to the source folder (`bin/Debug/...`), so the runner loads those assemblies from the
build output itself, not from a shadow copy, and holds them. The Pages assembly is loaded by name first
(`BddRunnerHost.LoadPages`); the others (Controls, Commands, TestData) need the same, or the
paths mapped into the copy, before a warm runner lets a build through.

### Phase 3: Debug from here, and the docs

| # | Step | Files | Done when | Progress |
| --- | --- | --- | --- | --- |
| 3.1 | **Debug from here** (D6): a `Break` runner message; Visual Studio and VS Code attach to the runner at a Step-through stop; the Presenter offers it too (Q7). | runner, server, both IDEs, Presenter | Owner: from a Step-through stop, F11 lands in the step definition. Automated: layer 5 (from a Step-through stop, F11 lands in the step definition). | |
| 3.2 | **ADs**: AD-022 (two modes; R2 amended; Run vs Run as test; files from source), AD-025 (IDE hosts; warm shadow-copied runner; clean start without a launch). | `.docs/decisions/` | | |
| 3.3 | **Guides and manual plan**: the four buttons, when to use which; Debug freezes a fake server in the test process; the manual test plan's section 6 for both modes. | `docs/guides/visual-studio-preview.md`, `vscode-extension.md`, `docs/platform-guides/presenter.md`, `.my/uat/manual/visual-studio-test-plan.md` | | |
| 3.4 | **Close the topic**: decisions in `.docs/`, guides in `docs/`, delete `.my/debugger/` and its row. | | | |

## 5. Per host, at the end

| Part | Visual Studio | VS Code | Presenter |
| --- | --- | --- | --- |
| F1 project build | 0.1 | already | already |
| F2 skip build | 0.2 | 0.2 | already (builds on its Build only) |
| Step through | 1.3, 1.4 | 1.3, 1.5 | already; step-file breakpoints and stop details from 1.2/1.6 (1.7) |
| Results in the preview | 1.1 | 1.1 | 1.1, 1.7 |
| Files from source | 2.1 | 2.1 | already |
| Warm runner and app | 2.3, 2.4 | 2.3, 2.4 | 2.3, 2.4 |
| Run as test | 2.2 (today's Run) | 2.2 (Test Explorer) | not added: the IDEs have it |
| Debug from here | 3.1 | 3.1 | 3.1 |

## 6. What was already done (before this plan)

Found while trying Debug on Todo's TOD.01.1, 2026-10-03/04; all pushed:


| Commit | What |
| --- | --- |
| 5239365c | Debug attaches to the testhost once (it printed its process id twice; the second attach cancelled the run). Both IDEs. |
| 8131cee5 | The scenario-failed exception is non-user code (no stop on it); no xUnit timeout under a debugger; "paused in the debugger" label in Visual Studio. |
| 4116aacd | Visual Studio: F10 at a scenario stop is the BDD step over; the feature file comes back in front after break mode. |
| f40b01aa | Visual Studio: F10 is taken over only for a pause the debugger is in (a leftover `bdd-paused.json` does not count). |

Still open from that session, not part of this plan unless it gets in the way: the failure
screenshot does not show in the preview (its file name has a `;`); the owner has not yet
confirmed "paused in the debugger" shows.
