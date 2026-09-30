# AD-025: A Suite Runs In A Host's Process

Presenter runs a suite's scenarios in its own process, not in a test host. What a suite and a
host owe each other for that:

| Decision | What holds |
| --- | --- |
| **A suite finds its files from its own assembly** | `AssemblyFile.FolderOf(typeof(MyFixture).Assembly)` (`Brinell.Core.Composition`) is where the config, the test data and the solution are looked for. `AppContext.BaseDirectory` is the host's folder there, and `Assembly.Location` is empty. |
| **What a suite builds is read into memory** | Presenter loads an assembly that has its `.pdb` beside it from a stream and records its path (`AssemblyFile.Record`), so the file is never locked and the suite can be rebuilt while Presenter is open. Other assemblies load from their path: a library that looks beside itself (Playwright's driver) needs it, and a build does not overwrite them. |
| **Unloading is not relied on** | A session's load context is collectible and unloaded, but a suite with SQLite or ASP.NET Core leaves timers, `AppDomain` handlers and event sources behind that hold it. Nothing that must work depends on the unload; the cost is memory until Presenter closes. |
| **Shared frameworks load into the session's context** | ASP.NET Core is probed from the shared framework folder beside the runtime's, at the running version, into the session's context. In the host it would load a second copy of the suite's assemblies by name (WireMock's server ran on one copy, its stubs were set on the other). |
| **A fixture owns its scenario lifecycle** | `IUatScenarioLifecycle` on the fixture: `BeforeFile` (the file's `TestData`) and `BeforeScenario` (a clean start). `UatScenarioTestBase` calls it by default and so does Presenter, so a scenario starts the same way in both. |

**Why:** each of these was a run that passed under `dotnet test` and failed from Presenter: the
config and the solution looked for in Presenter's folder, `Method not found` in WireMock on a
second run, a scenario on the wrong test set, and the suite's DLLs locked until Presenter closed.
Evidence (at 05200f2d):
[design.md](https://github.com/exact-mobile-apps/test-tooling/blob/05200f2d/.my/narrator/presenter/design.md).

**Ruled out:**
- referencing ASP.NET Core from Presenter itself;
- a shadow copy of the suite for a run: a library that looks beside itself does not find its files there;
- chasing every static that holds a collectible context, in libraries Brinell does not own;
- the suite's test class as the only place that loads a file's test set.

**Broken when:**
- a fixture or test-support class reads `AppContext.BaseDirectory` or `Assembly.Location` to find its files;
- a suite built while Presenter is open fails to copy its own assemblies;
- a scenario passes under `dotnet test` and fails from Presenter for where or how the suite was loaded.
