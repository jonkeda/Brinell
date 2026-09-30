# AD-022: The IDEs Run The Gate's Engine

What an IDE shows about a feature file, step file or suite config comes from the engine the xUnit
gate runs. It is served by one .NET process, `Brinell.Bdd.LanguageServer`:

| Decision | What holds |
| --- | --- |
| **One checker** (P1) | The preview, the diagnostics, completion, hover, navigation, the quick fixes, the BDD Explorer and the MCP tools all check through `BddFileAnalysis` (namespace `Brinell.Bdd.Preview`, in `Brinell.Bdd.Workspace`) and the suite's catalogs, so they say what the gate says, in its words. No IDE client parses or binds a file. The only grammar outside .NET is colours, and the server gives those too. |
| **The IDE holds no engine types** (P2) | The Visual Studio VSIX (.NET Framework) and the VS Code extension (TypeScript) send messages: LSP, `brinell/*` requests and MCP. The engine runs in the server's own net10 process, started as `dotnet Brinell.Bdd.LanguageServer.dll`. The server is as reusable as the preview renderer; the Presenter can take either. |
| **One way to load a suite** (L1) | `Brinell.Bdd.Workspace` finds the suites, reads their configs, and loads a suite's build from a shadow copy in a collectible context: a build never meets a locked file, and a reload releases the old build (AD-018). `brinell-data`, the server and the Presenter's check of its files load through it. A test-data definition loaded from the copy is placed at its project. |
| **A scenario has an address** (R1) | `[UatScenarioTheory]` gives each case the trait `BddScenario=<file relative to the config>#<slug>`. The IDEs, `dotnet test --filter` and CI all run one scenario by it. A suite that keeps `[Theory]` still runs in Test Explorer, but not from the preview. |
| **A step breakpoint is a file** (R2) | `%LOCALAPPDATA%\Brinell\bdd-breakpoints.json`, read by `BddBreakpoints` only while a debugger is attached. It is a file, not an environment variable, so a Debug that Test Explorer starts honours it, and both IDEs write the same one; the Presenter reads it to stop Run and toggles it from its step list. An app a debugged test starts is announced the same way (`DebuggedApps`, keyed by the test's process id), and the IDE attaches to it. |
| **Stepping goes through the same file** | While paused, the engine writes where (`bdd-paused.json`: file, line, call depth, whether it is a `## Step:` call, the test process); a step over, into or out is a one-shot `next` the IDE writes into `bdd-breakpoints.json`, for that process, before it lets the debugger go on. A call is a position of its own: the run stops at it, Step over runs it whole, Step into goes inside without running anything. Both IDEs, the same files; reading and writing them never fails a step. |

**As built:** Visual Studio runs two processes of the same server, one for the preview and one
for its `ILanguageClient`, so a failing editor client never takes the preview down. VS Code runs
one. Both are the same server, in messages. The Presenter checks its files with `BddFileAnalysis`
in its own process, against the suite's catalogs from `Brinell.Bdd.Workspace`. A Presenter *run*
still loads the build into its own session context, as the xUnit gate loads its own at run time.

**Why:** the gate is the truth about a scenario. An IDE that checked files its own way would pass
what the gate fails, and the reverse. Evidence (at ea9513e):
[visualstudio-plan.md](https://github.com/exact-mobile-apps/test-tooling/blob/ea9513e/.my/uat/tools/visualstudio-plan.md)
sections 3-5,
[vscode-extension.md](https://github.com/exact-mobile-apps/test-tooling/blob/ea9513e/.my/uat/tools/vscode-extension.md) section 1.

**Ruled out:**
- a markdown-it or TextMate grammar as the preview or the checker, since it knows syntax, not
  binding;
- loading the engine or a suite into Visual Studio's own process, which is .NET Framework and
  locks files;
- a debug adapter (DAP) that steps scenario by scenario: stepping by BDD step is the breakpoints
  file's one-shot move and the C# debugger's own Continue, with no adapter;
- an environment variable for breakpoints, which Test Explorer's own Debug cannot pass.

**Broken when:**
- an IDE client parses, binds or reads step values itself;
- a message carries an engine type rather than JSON;
- a tool loads a suite's build other than through `Brinell.Bdd.Workspace`, or loads it from `bin`
  in place;
- a scenario is run by display name or index rather than its `BddScenario` address;
- breakpoints are read without a debugger attached, or passed by environment variable;
- a step move is taken by a test process it was not written for, or reading or writing the files
  fails a step.
