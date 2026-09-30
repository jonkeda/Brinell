# Plan: a suite runs in a process of its own (RCA-001, option 2)

Status: **built** (steps 1 to 7), 2026-09-30. The decisions are in AD-025. Decided: option 2 of
[RCA-001](rca-001-presenter-memory-grows-per-run.md), and Q1 to Q4 below.

## Goal

Presenter no longer loads a suite into its own process to run it. It starts a **runner process**
that runs the scenario the way `dotnet test` does, and drives it with messages.

| What this gives | How |
| --- | --- |
| No leak | The runner exits; Windows frees everything it loaded. Presenter itself loads no suite code for a run |
| The same run as the gate | The runner starts with the suite's own `deps.json` and `runtimeconfig.json`, from the suite's output folder: `AppContext.BaseDirectory`, native libraries, ASP.NET Core and test data resolve as under `dotnet test` |
| AD-025's workarounds go | No session load context, no probing of folders and shared frameworks, no native-library resolver, no assemblies read into memory for a run |
| Presenter's own state stays clean | The app path, the window variables and the working directory are the runner's environment, not changes to Presenter's |

Not a goal: changing how Presenter *checks* files. That keeps loading the suite's catalogs
through `Brinell.Bdd.Workspace` from a shadow copy (AD-022, L1), which does unload.

## The shape

```text
Presenter (MAUI)                               Runner (one per run)
  PresenterShellViewModel                        dotnet exec
    |                                              --runtimeconfig <Suite>.runtimeconfig.json
  IBddExecutionService                             --depsfile      <Suite>.deps.json
    RunnerExecutionService                         <suite output>\Brinell.Uat.dll        
      starts the process, in a job object            |
      named pipe, JSON lines  <------------------>  BddRunnerHost (in Brinell.Uat)
  PresenterUatExecutionSession (a proxy)              fixture, UatRuntime, BddScenarioRunner,
    RunNextAsync, SkipNext, reports as text           the pacer and the screen recorder
```

**The rule that keeps it simple: the runner is the gate's process, Presenter is its remote
control.** Everything that holds a suite type stays in the runner: the fixture, the bound
scenario, the step session, the pacer and the recorder. Presenter holds text and numbers.

### What crosses, and what does not

| Today, in Presenter's process | After |
| --- | --- |
| `CreateSessionAsync` builds the fixture, binds, returns a session | `open` message: config, feature file, scenario name. Reply: the discovery and catalog reports, the step count, or the failure text |
| `session.RunNextAsync` / `SkipNext` | `next` / `skip` messages. Reply: the step's index, status, message and the exception as text |
| `session.StepSession.Results`, `Runner.Context.Diagnostics` (the details pane) | Sent with each step result and at `close`, as lines |
| `session.Runner.Context.Pacer = ...` set by `PresenterRecorder` | `record` and `rehearse` messages. The runner makes the `BddSceneRecording` or `BddNarrationPacer` itself, as a gate run with `BRINELL_RECORD` does |
| `session.RecordingTarget` (a window handle, or a Playwright page object) | Never crosses. The recorder runs in the runner, beside the target. This is what makes a browser suite recordable at all across a process boundary |
| Speech clips (`BddSceneClips.SpeakAsync`, Windows voices) | Stay in Presenter: made before the run, sent as file paths in the `record` message |
| `FinishSceneAsync` returns the scene's timeline | `finishRecording` message. The runner writes the timeline and the video into the scene's folder as today; the reply carries the timeline |
| Stitching, Try voice, kept-scene state | Unchanged, in Presenter: they need no suite |
| Placing the app's window | Unchanged: it finds the newest top-level window by snapshot, whoever started it |
| Speed delays, pause, run from/until here, breakpoints in the step list | Unchanged: Presenter asks for one step at a time |
| Environment scopes and `Directory.SetCurrentDirectory` on Presenter | The runner's start environment and working directory |

### Where the code lives

| Piece | Where | Why |
| --- | --- | --- |
| `BddRunnerHost`: the message loop, the session, the pacer and recorder wiring | `srcnew/Brinell.Uat/Runner/` | It versions with the suite's own `Brinell.Uat`, and every suite already has it in its output |
| The messages (records) and the pipe framing | `srcnew/Brinell.Bdd/Runner/` | Presenter and the runner host both reference `Brinell.Bdd`; no engine type is in a message (AD-022's rule for the IDEs, now for Presenter too) |
| The entry point: `BddRunnerProgram.Main`, which calls `BddRunnerHost` | `Brinell.Uat` itself, built as an exe without an app host | `dotnet exec` needs an entry point. `Brinell.Uat.dll` is in every suite's output already, so no new project and nothing to copy (spike, step 1) |
| `RunnerExecutionService`, the session proxy, the process and job object | `srcnew/Brinell.Presenter/Services/` | Replaces `BddExecutionService`'s body |
| Recorder discovery in the runner | `BRINELL_RECORDERS` pointing at Presenter's folder | The mechanism the IDEs already use for a suite that does not reference `Brinell.Narrator.Windows` |

The runner's entry assembly has to sit **in the suite's output folder** to get `AppContext.BaseDirectory`
right, as `testhost.dll` does. `Brinell.Uat` puts it there (Q1).

## Decisions (2026-09-30)

| # | Question | Decided |
| --- | --- | --- |
| Q1 | How does the runner get into the suite's output? | **`Brinell.Uat` is the runner.** `Brinell.Uat.dll` has the entry point, and every suite has it in its output after a build, as the test SDK brings `testhost`. Presenter copies nothing, and there is no shim project: the count stays at 84 |
| Q2 | How long does a runner live? | **One per run**: a scenario, a Run all, or a batch of checked scenes; it exits when the run ends. The suite's DLLs are locked only while something runs, as under `dotnet test`. With Run end set to Keep open, the runner stays until the app is closed. Cost: the first-run load (ASP.NET Core, WireMock, SQLite) on every run; step 1 measures it |
| Q3 | Does the in-process path stay as a fallback? | **No.** It is deleted in step 6, once every target has run through the runner |
| Q4 | Pipe or standard input/output? | **A named pipe.** A suite, its app host and WireMock write to the console. Stdout and stderr are captured as the run's log and shown when the runner dies |

## Steps

Each step is one commit and leaves Presenter working.

| # | Step | Done when |
| --- | --- | --- |
| 1 | **Spike, not committed.** A console exe copied into the Todo suite's output, started with the suite's `deps.json` and `runtimeconfig.json`, runs TOD.01.1 headless. Try the same on the Maui, Html and Stride UI test projects (three different target frameworks). Measure start-to-first-step against today | The launch line is known for every suite, the runner reaches a suite's output through `Brinell.Uat` alone (with or without a shim project), and the plan has the start-up cost in it |
| 2 | **The messages.** Records and JSON-lines framing in `Brinell.Bdd/Runner`: `open`, `next`, `skip`, `record`, `rehearse`, `finishRecording`, `dropRecording`, `close`, `cancel`; replies and a protocol version in the first message | Unit tests round-trip every message; an unknown version gives a message that says which side is older |
| 3 | **The runner.** `BddRunnerHost` in `Brinell.Uat` (the body of today's `TryCreateSession`, without the resolver and the environment scopes) and `BddRunnerProgram`, the entry point. `IUatScenarioLifecycle` is called as now | A test starts the runner on the Presenter test suite's sample scenario over a real pipe and reads passed steps; a second test reads a failing step's message |
| 4 | **Presenter drives it.** `RunnerExecutionService` behind `IBddExecutionService`; `PresenterUatExecutionSession` becomes a proxy with the same members the view model uses (`HasNext`, `CompletedStepCount`, `RunNextAsync`, `SkipNext`, the reports). The process starts in a job object that kills it, the app under test and the mock server when Presenter exits or Stop times out. A setting picks the old or the new service for this step and the next | Presenter's 85 tests pass on the new service. The Todo suite runs from Presenter; private memory and the assembly count are flat over eight runs (the RCA's measurement, repeated) |
| 5 | **Recording and rehearsal in the runner.** `IPresenterRecorder.StartAsync`, `StartSceneAsync` and `RehearseAsync` send a message in place of setting a pacer; clips are made in Presenter first. Finish and drop likewise | `narrator-record.feature.md` passes. The short Todo video records from Narrator mode; a scene recorded again replaces the old one; Stop drops the scene |
| 6 | **Remove the in-process path.** Delete `BddSessionLoadContext`, `BddRuntimeAssemblyResolver`, the scopes, the setting from step 4, `SessionLoadContextTests`; check whether Presenter still needs its references to `Brinell.Maui.FlaUI` and whether `AssemblyFile.Record` has a caller left | The solution builds; one run each of a Maui, WPF, WinForms, Html and Stride scenario from Presenter passes |
| 7 | **Docs.** AD-025 rewritten as "a suite runs in a process of its own" (what stays: fixtures find files from their assembly, the fixture owns its lifecycle; what goes: the rows about loading). AD-022's "as built" paragraph. The Presenter guide and the narrated-recordings guide. The working-set figures corrected or removed. RCA-001 closed and this plan deleted | Links valid; `.my/README.md` row removed |

## What the spike found (step 1)

`dotnet exec --runtimeconfig <Suite>.runtimeconfig.json --depsfile <Suite>.deps.json <output>\Brinell.Uat.dll`
with a spike `Main` in `Brinell.Uat` that made the fixture, bound one scenario and ran it to the end:

| Suite | Target framework | Result | Process start to `Main` | To the first step (app started) | Assemblies at the end |
| --- | --- | --- | --- | --- | --- |
| Todo (`Brinell.Samples.Todo.Uat`, TOD.01.1: mock server, SQLite) | `net10.0-windows` | 7 steps passed | about 70 ms | 7.0 s (the app starts, then restarts on the file's test set) | 212 |
| `Brinell.Maui.UITests` | `net10.0-windows` | 7 passed | 69 ms | 1.5 s | 45 |
| `Brinell.Html.UITests` (Playwright) | `net10.0` | 4 passed | 65 ms | 2.5 s | 63 |
| `Brinell.Stride.UITests` | `net10.0-windows` | 4 passed | 69 ms | 5.9 s | 62 |

- `AppContext.BaseDirectory` is the suite's output folder in all four, as under `dotnet test`.
- No shim is needed: `Brinell.Uat` as `OutputType` Exe with `UseAppHost` false still works as a
  library reference for every suite.
- The process itself costs about 70 ms. The rest of "to the first step" is the fixture starting
  the app, which a run pays today as well. Q2 (one runner per run) stands.

## What building it found (steps 2 to 5)

| Step | Found |
| --- | --- |
| 3 | A step method does not get the run's cancellation token, so a canceled step ends when the step does; what a cancel stops at once is the pacer's wait. As before, in the engine |
| 3 | Speak aloud: the runner only says which line it waits for (`say`); Presenter plays it. A suite need not have the Windows voice's player |
| 4 | **No temporary setting.** The plan had a switch between the old and the new service for steps 4 and 5. The session's shape had to change for the runner anyway (no engine types), so the in-process service and its load context went in step 4, and the recorder's messages with it. Step 5 is the live check and its tests |
| 4 | "One runner per run" is `IBddExecutionService.BeginRun()`: inside it, a session that ends leaves its runner for the next scenario; outside it (a single paused run), the runner ends with its session |
| 4 | A session's `Dispose` does not wait: the close request is put in line, and the runner answers it before it answers the next scenario's open |
| 4 | Runners are in a Windows job that ends them, the app and the mock server when Presenter ends, however it ends |
| 4 | Todo, TOD.01.1, eight runs through `BddExecutionService` (the RCA's loop): **81 assemblies after every run and private memory flat** (17 to 24 MB), against 277, 379, 481... and +4 MB a run before. 5 to 6 s from Run to the first step, the app's own start |
| 5 | From Presenter, Narrator mode, the short Todo script: three scenes recorded by the runner and stitched (40 s); Stop dropped a scene; recording it again replaced it; a rehearsal with Speak aloud was paced by its lines; Present mode's Record made its video. After each: no runner, no Todo app, the suite's DLL free. Presenter's private memory: 220 MB before, 229 MB after six runs |

| 6 | `AssemblyFile` is only `FolderOf` now: nothing loads a suite from memory, so there is no path to record. `FolderOf` stays what a fixture uses, since it is right in every process |
| 6 | Build waits for the runners of ended sessions (`EndRunnersAsync`): a session's `Dispose` no longer waits, so without it a build could start while the suite's files were still held |
| 6 | One scenario each of the Maui, WPF, WinForms, Html, Blazor and Stride suites passes through `BddExecutionService` |
| 6 | **Not done:** Presenter's references to `Brinell.Maui` and `Brinell.Maui.FlaUI`. Its code uses neither any more, and it built without them, but the Presenter suite was not run on that build |
| 6 | **Open:** the Todo web suite (`Brinell.Samples.Todo.Web.Uat`) does not open from Presenter: "The fixture is not started". Its fixture is started by xUnit's own lifetime, which neither the old service nor the runner calls. Not looked into further |

## What can go wrong

| Risk | Handling |
| --- | --- |
| The runner dies mid-run (a crash in the app host, an unhandled exception on another thread) | The pipe closes; the session fails the current step with the last lines of the runner's stderr. Today this would take Presenter down with it |
| Stop while a step hangs | `cancel`, then after a short named timeout the job object is closed: the runner, the app and the mock server go together. No orphaned apps |
| Presenter is newer than the suite's build of `Brinell.Uat` | The protocol version in the first reply. Presenter says "rebuild the suite" rather than failing on a missing field |
| Every run pays the first-run load | Measured in step 1: about 70 ms to start, and the app's own start dominates. Were it to grow, the fallback is a runner kept per workspace and stopped before a build, at the price of the lock; that reopens Q2 |
| The suite's output has no `runtimeconfig.json` (a plain class library as Pages assembly) | Every `*.UITests` project has one (the test SDK writes it). A workspace without one gets a message naming the file; decide in step 1 whether any existing workspace is like that |
| A debugger on the scenario | Out of scope here, but now possible: attach to the runner. Breakpoints stay Presenter's own, between `next` messages |
| Two copies of a rule: the gate's test class and the runner both start a scenario | Already true of `BddExecutionService`; `IUatScenarioLifecycle` is the shared part and stays the only place for it |

## Verification

Per AGENTS.md: the smallest tier that can falsify each step, one UI process at a time.

- Steps 2 and 3: `Brinell.Bdd.Tests`, `Brinell.Uat.Tests` and the new runner tests.
- Steps 4 to 6: `testsnew\Brinell.Presenter.Bdd.Tests` (85 today, same count minus the deleted
  load-context tests plus the new ones).
- The leak itself: the RCA's scratch loop becomes a committed test. After N runs on the Todo
  suite, Presenter's `AppDomain.GetAssemblies()` count is what it was after zero.
- By hand, once, at step 5: the manual test plan's M16 to M19 (Narrator mode).
