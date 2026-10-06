# AD-025: A Suite Runs In A Process Of Its Own

A host that runs a suite's scenarios (Presenter) does not load the suite. It starts a **runner
process** in the suite's output folder and drives it with messages.

| Decision | What holds |
| --- | --- |
| **The runner is the suite's own `Brinell.Uat.dll`** | `Brinell.Uat` has an entry point (`BddRunnerProgram`). A host starts it with `dotnet exec`, the suite's `runtimeconfig.json` and `deps.json`, from the suite's output (`BddRunnerStart`), as a test host is started. `AppContext.BaseDirectory`, native libraries, shared frameworks and test data resolve as under `dotnet test`. Every suite has the file after a build; a host copies nothing. |
| **The runner is the gate's process; the host is its remote control** | Everything that holds a suite type stays in the runner: the fixture, the bound scenario, the step session, the pacer and the screen recorder. The host asks for one step at a time and holds text and numbers. |
| **Messages carry no engine type** | One JSON object per line over a named pipe (`Brinell.Bdd.Runner`). The runner's first message has the protocol version; a mismatch says which side to rebuild. The runner's console is its log, never the protocol. |
| **One runner per run** | A run of several scenarios (`IBddExecutionService.BeginRun`) keeps one runner, with a new fixture per scenario; it ends with the run. The suite's files are held only while something runs, as under `dotnet test`. A build waits for ended runners first. |
| **Runners end with their host** | They are in a Windows job that ends them, the app under test and a mock server when the host ends, however it ends. A canceled step that does not stop within a named grace has its runner ended. |
| **What is said aloud is played by the host** | The runner says which line it waits for; the host has the voice's player. The recorders a suite does not reference are the host's (`BRINELL_RECORDERS`). |
| **A fixture owns its scenario lifecycle** | `IUatScenarioLifecycle` on the fixture: `BeforeFile` (the file's `TestData`) and `BeforeScenario` (a clean start). `UatScenarioTestBase` calls it and so does the runner, so a scenario starts the same way in both. |
| **A suite finds its files from its own assembly** | `AssemblyFile.FolderOf(typeof(MyFixture).Assembly)` (`Brinell.Core.Composition`): right under `dotnet test` and in a runner. |

**Why:** a suite loaded into Presenter's own process was never freed. A suite with SQLite or
ASP.NET Core leaves timers, `AppDomain` handlers and event sources behind that hold a collectible
load context: every Todo run added 102 assemblies and about 4 MB of private memory, and the
suite's DLLs stayed locked. Each earlier fix was for a run that passed under `dotnet test` and
failed from Presenter for where or how the suite was loaded. In a runner, eight Todo runs leave
the host at the same 81 assemblies. Evidence (at f20deb4b):
[the RCA](https://github.com/exact-mobile-apps/test-tooling/blob/f20deb4b/.my/rca/rca-001-presenter-memory-grows-per-run.md),
[the plan as built](https://github.com/exact-mobile-apps/test-tooling/blob/f20deb4b/.my/rca/rca-001-plan-runner-process.md).

**Ruled out:**
- loading the suite into the host, in a collectible load context or read from memory;
- reusing one load context per build of the suite: a leak per rebuild is left, and the host still resolves the suite's dependencies for it;
- chasing every static that holds a collectible context, in libraries Brinell does not own;
- a runner kept per workspace: it holds the suite's files while nothing runs, so a build from an IDE fails;
- a runner project of its own, or a file the host copies into the suite's output;
- standard input and output as the protocol: a suite, its app host and a mock server write there.

**Broken when:**
- a host loads a suite's assembly to run a scenario;
- a message carries an engine or suite type rather than JSON;
- a runner, the app it started or its mock server outlives the run, or the host;
- the suite cannot be built while the host is open and nothing runs;
- a scenario passes under `dotnet test` and fails from Presenter for where or how the suite runs.
