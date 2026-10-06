# AD-025: A Suite Runs In A Process Of Its Own

A host that runs a suite's scenarios (the Presenter, and the IDEs' language server for Step through
and Run, AD-022) does not load the suite. It starts a **runner process** from the suite's output and
drives it with messages.

| Decision | What holds |
| --- | --- |
| **The runner is the suite's own `Brinell.Uat.dll`** | `Brinell.Uat` has an entry point (`BddRunnerProgram`). A host starts it with `dotnet exec`, the suite's `runtimeconfig.json` and `deps.json`, from the suite's output (`BddRunnerStart`), as a test host is started. `AppContext.BaseDirectory`, native libraries, shared frameworks and test data resolve as under `dotnet test`. Every suite has the file after a build; a host copies nothing. |
| **The runner is the gate's process; the host is its remote control** | Everything that holds a suite type stays in the runner: the fixture, the bound scenario, the step session, the pacer and the screen recorder. The host asks for one step at a time and holds text and numbers. |
| **Messages carry no engine type** | One JSON object per line over a named pipe (`Brinell.Bdd.Runner`). The runner's first message has the protocol version; a mismatch says which side to rebuild. The runner's console is its log, never the protocol. |
| **A warm runner per suite, from a copy** (amended) | A host keeps one runner ready per suite (`BddWarmRunners`), started from a copy of the suite's output (`BddShadowCopy`: unchanged files hard-linked to the copy before), so the runner holds none of the output's files and a build goes through while it waits. A host takes it for a run and gives it back once the scenario is closed; a changed output replaces it. The language server starts it when a feature file of the suite opens and after each build, and ends it when the suite's last feature file has been closed for the idle minutes (settings in both IDEs); the Presenter when it loads a built workspace. A runner a debugger was put on ends with its run. |
| **A runner reads its scenario files from the source** | The host tells it the suite's source folder (`BRINELL_SUITE_SOURCE`); the config, feature and step files are read there (AD-022). |
| **Runners end with their host** | They are in a Windows job that ends them, the app under test and a mock server when the host ends, however it ends. A canceled step that does not stop within a named grace has its runner ended. |
| **What is said aloud is played by the host** | The runner says which line it waits for; the host has the voice's player. The recorders a suite does not reference are the host's (`BRINELL_RECORDERS`). |
| **A fixture owns its scenario lifecycle** | `IUatScenarioLifecycle` on the fixture: `BeforeFile` (the file's `TestData`) and `BeforeScenario` (a clean start). `UatScenarioTestBase` calls it and so does the runner, so a scenario starts the same way in both. |
| **The app is kept only when a scenario starts clean without a launch** | A fixture says so (`StartsCleanWithoutLaunch`, false by default): its `BeforeScenario` then gives a clean start on the app as it is, and a warm runner keeps the fixture and its app between runs. Otherwise the warm runner makes the next fixture, and so starts the app, as soon as a run ends (`BddRunnerPrepare`): a run never waits for a launch, and never starts on what the run before left. Todo's MAUI fixture says no: its clean start keeps the app's local database. |
| **A suite finds its files from its own assembly** | `AssemblyFile.FolderOf(typeof(MyFixture).Assembly)` (`Brinell.Core.Composition`): right under `dotnet test` and in a runner, a runner from a copy included (it answers with the output the copy was made from, `BRINELL_BUILT_OUTPUT`). Its scenario files: `BddScenarioSource.SuiteDirectory(...)`, the source folder when a host runs it from there. |

**Why:** a suite loaded into Presenter's own process was never freed. A suite with SQLite or
ASP.NET Core leaves timers, `AppDomain` handlers and event sources behind that hold a collectible
load context: every Todo run added 102 assemblies and about 4 MB of private memory, and the
suite's DLLs stayed locked. Each earlier fix was for a run that passed under `dotnet test` and
failed from Presenter for where or how the suite was loaded. In a runner, eight Todo runs leave
the host at the same 81 assemblies. Evidence (at f20deb4b):
[the RCA](https://github.com/exact-mobile-apps/test-tooling/blob/f20deb4b/.my/rca/rca-001-presenter-memory-grows-per-run.md),
[the plan as built](https://github.com/exact-mobile-apps/test-tooling/blob/f20deb4b/.my/rca/rca-001-plan-runner-process.md).
A runner kept ready makes a Run or Step through start in under a second; from a copy, it no longer
holds the suite's files. Evidence (at 6b201bcc):
[the debugger plan as built](https://github.com/exact-mobile-apps/test-tooling/blob/6b201bcc/.my/debugger/plan.md), steps 2.3 and 2.4.

**Ruled out:**
- loading the suite into the host, in a collectible load context or read from memory;
- reusing one load context per build of the suite: a leak per rebuild is left, and the host still resolves the suite's dependencies for it;
- chasing every static that holds a collectible context, in libraries Brinell does not own;
- a runner kept per workspace in the suite's output: it holds the suite's files while nothing
  runs, so a build from an IDE fails (a runner from a copy of the output holds nothing);
- keeping every fixture and its app between runs: a scenario then starts on what the one before
  left in the app's own storage (Todo's TOD.01.1 failed so);
- a runner project of its own, or a file the host copies into the suite's output;
- standard input and output as the protocol: a suite, its app host and a mock server write there.

**Broken when:**
- a host loads a suite's assembly to run a scenario;
- a message carries an engine or suite type rather than JSON;
- a runner, the app it started or its mock server outlives the run, or the host;
- the suite cannot be built while a runner waits, or while nothing runs;
- a waiting runner reads a file of the suite's output instead of its copy, or a copy a runner
  runs from is removed;
- a fixture is kept between runs that does not say it starts clean without a launch;
- a scenario passes under `dotnet test` and fails from Presenter for where or how the suite runs.
