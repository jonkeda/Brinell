# Visual Studio extension: design and implementation plan

Status: **phases 1 and 2 built** (1.1-2.4), phase 3 built (3.1-3.4; 3.3 has no test-set values yet), phase 4 in progress (CodeLens, quick fixes, views built); checklists 6.1-6.3
and the spike (phase 0) not run. Date: 2026-09-27. Updated for the Bdd renames (AD-020, AD-021):
feature and step files, `Features/`, `Brinell.Bdd` under `Brinell.Uat`, and step definitions.
Builds on the proposal [visualstudio-extension.md](visualstudio-extension.md) and on
[vscode-extension.md](vscode-extension.md) for everything the two IDEs share. This plan sets the
**order**: the preview first, then running and debugging, then the editor, then the rest. Where
this plan and the proposal differ, this plan holds (the differences are listed in section 3).

## 1. Goal and order

A developer opens a `.feature.md` in Visual Studio and gets:

1. **Preview.** The scenario is shown as readable steps. It shows what the gate sees, such as a
   step that binds to nothing, and the results of the last run.
2. **Run and debug.** A scenario runs from the preview or from Test Explorer, whichever the
   developer is in. Debugging stops **at a step**, with the arguments in Locals, and F11 goes
   into the step definition.
3. **Editor.** Diagnostics while typing, completion, hover, go to definition, find references,
   outline and colours, from the language server.
4. **The rest.** CodeLens from C# to scenarios, quick fixes, the catalog and pages windows,
   config support, Copilot, and the VS Code client on the same server.

Each phase is usable on its own. The preview is worth having before anything else exists.

## 2. What holds today

| Fact | Consequence |
| --- | --- |
| Nothing is built: no language server, no preview renderer, no host. The VS Code proposal's markdown-it plugin doesn't exist either. | The preview renderer is new work, and its design can be chosen for Visual Studio first (P1). |
| The engine is two packages: `Brinell.Bdd` (parse, bind, run; no UI) and `Brinell.Uat` on top (pages, controls, page scope). Both target net8.0, net9.0 and net10.0. A suite's assemblies are net10.0. Visual Studio runs in-process extensions on .NET Framework 4.8, and out-of-process ones on .NET 8. | The extension can't load the engine or a suite. All engine work happens in our own net10 process (P2). |
| `UatMarkdownParser` (in `Brinell.Bdd`) gives the document with line numbers. `UatScenarioFiles.Load(...).Bind(catalogSet)` binds with the same diagnostics as the gate. A file in the old form (`*.uat.md`, `# UAT:`, `## Phrase:`) is UAT029, which `brinell-data validate --fix` rewrites. | The preview and the editor get parsing and binding for free. |
| `ScenarioCheck` (in `brinell-data`) builds the catalogs from the fixture type without starting it. But it loads the suite with a **non-collectible** context straight from `bin`. | Its loading moves into a shared library that shadow-copies and can unload, so a VS build never meets a locked file (AD-018). |
| `UatScenarioTestBase` gives one xUnit **v2** (2.9.3) theory case per scenario, `[filePath, scenarioName]`. Test Explorer already lists them. | Running from Test Explorer works today. There's no stable way to address one case from the command line: the display name holds the full path, escaped (VS Code Q2). |
| `UatStepExecutionSession.RunNextAsync` (in `Brinell.Bdd`) invokes one step at a time. Hooks (`[BddAfterStep]` and the rest) run in `UatScenarioRunner.RunAsync`, not in the session. | A break before a step is one check in one place (R2). |
| `__result.json` is written per scenario under `TestResults/<run-id>/suites/<suite>/`. It is named by the scenario name only. It has step statuses **only** with `IncludeRuntimeTrace`, and has no file or line. | For the preview to show results, the report must always name the file, each step's line and each step's status (P5). |
| The rules: one UI test process at a time, no build while one runs, rebuild the app before a UI run. | The extension enforces them. It is the first tool that can (R4). |

## 3. Decisions

### Preview

**P1. The engine renders the preview, in .NET, and a WebView2 shows it.** The proposal reused a
markdown-it plugin, which only knows syntax. A renderer over the engine's parsed document knows
more:

- which step binds to nothing, and why;
- the page each step runs on;
- a callable's body with the values filled in;
- each argument's value as read (`+2d` → `29-Sep-2026`).

That is the "one checker" rule applied to the preview. It works without a build too: then it
parses only and says binding is off. VS Code gets the same HTML in its own scenario view (its
proposal's Q1 fallback). Its markdown-it plugin becomes optional, for colours in the built-in
preview only.

**P2. One .NET process from day one: `Brinell.Uat.LanguageServer`.** The extension starts it over
stdio with JSON-RPC (StreamJsonRpc, as Visual Studio's own LSP client uses). The first thing it
serves is a custom request, `brinell/preview`. The LSP methods arrive in phase 3 in the same
process. So nothing built for the preview is thrown away, and the editor phase adds handlers
rather than a process. The extension itself stays thin: it holds no engine types, only the
messages.

As built (3.1): the editor's `ILanguageClient` starts a second process of the same server rather
than sharing the preview's. The preview works today and has no dependency on S2. Visual Studio's
LSP client could fail on a content type it does not like, and that must not break the preview.
Joining them later means sending the preview's `brinell/*` requests through
`ILanguageClientCustomMessage2`.

**P3. The renderer is a library, `Brinell.Uat.Preview`.** It holds the document model and the
catalog set, and turns them into HTML plus a stylesheet with CSS variables. It uses Markdig for
the Markdown around the scenarios, with raw HTML switched off. Every block carries `data-line`,
for scroll sync and for the buttons. It has no IDE and no process of its own, so the Presenter
could use it later, and snapshot tests cover it.

**P4. The extension model is VSSDK in-process** (an `AsyncPackage`), unless the spike (S1) shows
VisualStudio.Extensibility can do the first two phases. Those phases need three things:

- a tool window hosting WebView2;
- attaching the debugger to a process;
- Test Explorer and build events.

All three are well-trodden in-process APIs. The out-of-process model is where Microsoft is going,
but it hosts Remote UI, not arbitrary WPF. For LSP, VSSDK's `ILanguageClient` is the mature one.
The proposal preferred out-of-process; this reverses it for these reasons. The server doesn't
care which model starts it.

**P5. The run report names its file, lines and steps.** `__result.json` gains:

- the scenario's `file` (relative to the config) and `line`;
- per step, always, whatever the trace setting: `line`, `status`, `message` and, inside a
  callable, the call's line.

The catalog and discovery sections stay behind `IncludeRuntimeTrace`. The server finds the newest
report for a file through the artifact manifest (AD-007), not by guessing the run id. The file
name collision (two files with the same scenario name) is fixed by prefixing the file's stem.
Check the manifest's other readers before renaming.

### Run and debug

**R1. A scenario has a stable address: `[UatScenarioTheory]`.** It is a theory attribute with its
own discoverer, in `Brinell.Uat`, which already references `xunit.extensibility.core`. It makes
the same one case per scenario, and gives each case:

- a trait `UatScenario=<file relative to the config>#<scenario slug>`, with the slug limited to
  `[A-Za-z0-9._/-]` so that `dotnet test --filter` needs no escaping;
- if the spike (S5) shows xUnit v2 keeps it, the scenario's `.md` file and line as the case's
  source, so Test Explorer's own *Go to test* opens the scenario.

Suites change `[Theory]` to `[UatScenarioTheory]`, a one-line change. A suite that keeps `[Theory]`
still runs from Test Explorer. The preview then says it can't run that scenario, and why.

**R2. Break at a step, from breakpoints in a file.** The breakpoints are kept in a per-user
`%LOCALAPPDATA%\Brinell\uat-breakpoints.json`, as a file and a list of lines. The executor reads
it only when `Debugger.IsAttached`, so it costs nothing otherwise. Before a step whose line (or
whose call's line, in a `*.steps.md`) is listed, the session:

- builds a `UatStepBreak` local holding the step text, the arguments as read, the app, the page
  and the call chain;
- then calls `Debugger.Break()`.

The developer lands in `RunNextAsync` with that local in view, and F11 steps into the handler.
It's a file, not an environment variable, so a **Debug** started from Test Explorer honours the
breakpoints too. The extension can't pass settings to Test Explorer's runs. VS Code can write the
same file later.

**R3. Run from the preview; Test Explorer as it is.** Test Explorer needs nothing new except R1.
The preview's **Run** and **Debug** buttons (per scenario, and **Run file**) do these things in
order:

1. Build the solution through Visual Studio. That builds the app too, so a stale app never runs,
   and there's no second MSBuild against the same outputs.
2. Run the case. If the spike (S3) finds an API that runs a Test Explorer case by id, use it, so
   results land in Test Explorer too. Otherwise run
   `dotnet test <project> --no-build --filter UatScenario=<address>` and write its output to an
   Output window pane.
3. For **Debug** in the fallback: set `VSTEST_HOST_DEBUG=1`, read the testhost's process id from
   the output, attach Visual Studio's debugger to it, and continue past the testhost's own
   initial break. As built (2.4): `VSTEST_DEBUG_NOBP=1` skips that break, so there's nothing to
   continue past; S4 still has to confirm it with the Todo fixture.
4. Refresh the preview from the new `__result.json`.

**R4. The extension keeps the UI test rules.** While a run is going:

- a second run is refused with a message;
- a build is cancelled (`IVsUpdateSolutionEvents.UpdateSolution_Begin` can cancel), saying why;
- **Cancel** stops the test process and the app it started.

### Shared loading

**L1. `Brinell.Uat.Workspace`, a shared net10 library, loads a suite one way for every tool.** It
comes from `ScenarioCheck` and the Presenter's `MsBuildProjectEvaluator`, and does these things:

- finds the suites (projects with a `uat.config.md`, or a `bdd.config.md` for a `service` suite);
- reads the config, including an `AppsFile` copied at build time;
- loads the fixture's assemblies from a shadow copy, in a collectible context;
- builds the `UatCatalogSet` through `Brinell.Uat`, so a UI suite's pages and control verbs are in
  it; a service suite needs them too, to report UATB013;
- reloads when told the build finished.

`brinell-data` moves onto it in the same step, with its tests unchanged. The Presenter can follow
later. The three libraries keep `Uat` names: they need `Brinell.Uat` for pages and page scope. The
preview's parse-only mode needs `Brinell.Bdd` only.

## 4. Architecture

```text
Visual Studio 2022 17.10+ / 2026
 +-- Brinell.Uat.VisualStudio (VSIX, net48, in process)
 |    +-- Scenario Preview tool window (WebView2) --+
 |    +-- Run / Debug / Cancel, Output pane         |  JSON-RPC over stdio
 |    +-- build events, debugger attach             |
 |    +-- (phase 3) ILanguageClient ----------------+-- Brinell.Uat.LanguageServer (net10)
 |                                                        +-- Brinell.Uat.Preview   (HTML)
 |                                                        +-- Brinell.Uat.Workspace (suites, catalogs)
 |                                                        +-- Brinell.Uat           (pages, controls, page scope)
 |                                                        +-- Brinell.Bdd           (parse, bind)
 +-- Test Explorer (xUnit adapter) ---------- dotnet test / testhost
                                                  +-- UatScenarioTestBase, [UatScenarioTheory]
                                                  +-- executor: break at a step (R2)
```

| Project | Where | Framework | Tests |
| --- | --- | --- | --- |
| `Brinell.Uat.Workspace` | `tools/` | net10.0 | `testsnew/Brinell.Uat.Workspace.Tests`, against the Todo suites' build output |
| `Brinell.Uat.Preview` | `tools/` | net10.0 | snapshot tests over the Todo feature files |
| `Brinell.Uat.LanguageServer` | `tools/` | net10.0 | JSON-RPC over an in-memory stream |
| `Brinell.Uat.VisualStudio` | `tools/`, **own `.slnx`**; the VSIX needs Visual Studio's MSBuild, so it stays out of `Brinell.sln` | net48 | a manual checklist in the experimental instance (section 6) |

The VSIX carries the server as a framework-dependent publish and starts `dotnet <server>.dll`. It
needs the .NET 10 runtime, which a developer of a Brinell suite has.

Messages between the preview and the extension, and between the extension and the server:

| From → to | Message |
| --- | --- |
| extension → server | `initialize {solutionDir, suites}`, `didOpen`/`didChange {uri, text, version}`, `brinell/buildDone`, `brinell/preview {uri, suite?} → {html, version, suites, bindState}` |
| server → extension | `brinell/resultsChanged {uri}` (a new `__result.json`) |
| webview → extension | `run {line}`, `debug {line}`, `runFile`, `toggleBreak {line}`, `reveal {line}`, `openScreenshot {path}`, `pickSuite {suite}` |
| extension → webview | `update {html}` (the scroll position is kept), `scrollTo {line}`, `theme {vars}`, `breaks {lines}`, `running {line?}` |

## 5. Phases

Each numbered step is one commit, with its tests green. It updates docs and CHANGELOG where it
changes something a user sees.

### Phase 0: spike (throwaway, answers S1-S6)

A throwaway VSIX: a tool window with WebView2, a button that attaches the debugger to a process,
and a call into Test Explorer. Its findings go into section 7, and the code is not kept.

### Phase 1: preview

| # | Step | Done when |
| --- | --- | --- |
| 1.1 | **`Brinell.Uat.Workspace`** (L1): suite discovery, config reading, shadow-copied collectible loading, catalogs, reload. `brinell-data` uses it. | `brinell-data validate` gives the same output on the Todo suites; a test rebuilds the Todo UAT project while the workspace holds its catalog, and the build succeeds. |
| 1.2 | **`Brinell.Uat.Preview`, parse-only**: Markdown via Markdig; each gherkin fence as a scenario block, with the keyword in a gutter, the step text, quoted values and `{x}` arguments marked, tables as tables and doc strings as blocks; Metadata as a small table; `## Step:` sections; `data-line` on every block; a file in the old form shows UAT029 with its fix. A stylesheet with light, dark and high-contrast variables. | Snapshot tests over every Todo feature and step file; a file with a parse error renders with the error at its line, not as a blank. |
| 1.3 | **Binding in the preview**: with a catalog, each step shows what it binds to (as a tooltip: the method and the page), marks a step that binds to nothing with the gate's diagnostic, shows the page per step once it changes, and can unfold a callable call into its body with values filled in. The first Scenario Outline row is shown as bound; each row can be expanded. | Snapshot tests with the Todo catalog; the text of the TOD.11-without-using file's UATB010 appears in the preview word for word as the gate reports it. |
| 1.4 | **`Brinell.Uat.LanguageServer`, skeleton**: stdio JSON-RPC, `initialize`, document sync, `brinell/preview`, `brinell/buildDone`; a file belongs to the suite whose project holds it, with a picker for a file linked into several (AD-014). Parse-only until a build exists, saying so. | JSON-RPC tests: open, change, preview, build done reloads the catalog. |
| 1.5 | **The VSIX and the Scenario Preview window**: starts the server when the first `.feature.md` or `.steps.md` opens; the window follows the active document; refreshes on edit (debounced) and on build done; scroll sync both ways (caret → block, click a block → caret); VS theme → CSS variables, live on theme change; WebView2 user data under `%LOCALAPPDATA%\Brinell\WebView2`; images through a virtual host mapping, never `file://`. | Checklist 6.1 passes in the experimental instance with the Todo solution. |
| 1.6 | **Results in the preview** (P5): the report change in `UatScenarioTestBase` and the manifest lookup in the server; each step shows its status icon with a text alternative; a scenario shows its duration; a failed step shows its message and a screenshot thumbnail that opens full size. | A Todo UAT run from the command line, then the preview shows it; the Todo UAT suites and the service suite are still green with the new report. |

### Phase 2: run and debug

| # | Step | Done when |
| --- | --- | --- |
| 2.1 | **`[UatScenarioTheory]`** (R1): discoverer, trait, and source location if S5 allows. Todo's MAUI, web, mixed and service suites move to it. | `dotnet test --filter "UatScenario=Features/TOD.11-manage-users.feature.md#<slug>"` runs exactly one case; Test Explorer lists the same cases as before; the Todo counts are unchanged (MAUI 43, web 35, mixed 4, service 3). |
| 2.2 | **Break at a step** (R2): the breakpoint file, the check in `RunNextAsync` behind a seam (`IUatBreakHook`) so unit tests don't need a debugger, `UatStepBreak`, breaks inside callables by the call's line. | `Brinell.Bdd.Tests` (the session is in `Brinell.Bdd`): a listed line breaks, an unlisted one doesn't, nothing is read without a debugger, a line in a `*.steps.md` breaks at each call. |
| 2.3 | **Run from the preview** (R3, R4): Run and Run file buttons on the headings, build first, then the Test Explorer API or `dotnet test --no-build`, an Output pane, one run at a time, builds cancelled during a run, Cancel. | Checklist 6.2 passes. |
| 2.4 | **Debug from the preview**: the attach route (or the Test Explorer API's debug), and the breakpoint gutter in the preview (a click on a step's gutter toggles it, as a red dot). An editor command, *Toggle UAT step breakpoint*, on the context menu and a key binding for `.feature.md` files, shows the same dot in the editor's glyph margin. | Checklist 6.3 passes, including a break hit from Test Explorer's own Debug. |

### Phase 3: editor

The server's LSP features, shared with VS Code (its proposal, section 4). The VS client is an
`ILanguageClient` for `.feature.md`, `.steps.md`, `uat.config.md` and `bdd.config.md` on top of the
Markdown editor (S2).

| # | Step | Done when |
| --- | --- | --- |
| 3.1 | **Diagnostics**: parse on each change, bind the open file on change and the suite on save and on build done; the text is the gate's; a problem inside a callable shows at the call. Failed steps of the last run also show as errors at their lines, so they reach the Error List. | Server tests on the Todo files; the Error List shows UATB010 for the without-using file. |
| 3.2 | **Outline, folding, semantic colours** (no TextMate in VS). | Server tests; checklist. |
| 3.3 | **Completion**: keywords and sections, step texts by words (not just by prefix) with snippet placeholders, and values: pages, the controls of the page at that line, users and apps, enum members, test sets, and Examples columns. | Server tests per kind. |
| 3.4 | **Hover, go to definition, find references**: method and page from the bound step, the `<summary>`, the argument values as read; definition via the portable PDB; references across the suite. | Server tests; checklist: F12 on a step opens the step definition in C#. |

### Phase 4: the rest

In rough value order; each is planned when started.

| Item | Notes |
| --- | --- |
| CodeLens from C# to scenarios | "used in 12 steps · 5 scenarios" on step definitions, page phrases, page-object members, control verbs; the proposal's Q2 (metadata token lookup). |
| Quick fixes and code actions | the `brinell-data validate --fix` fixes (values, and the old file forms: UAT029), Create step definition, Extract and Inline callable step, Rename a callable step. |
| Views | Step catalog, Pages and Results tool windows; hooks listed with their step-definition classes. |
| Config and data files | `uat.config.md` / `bdd.config.md` and the apps table, test-data JSON schema. |
| Copilot | the Brinell MCP server ([scraper-in-extension.md](scraper-in-extension.md)). |
| VS Code client | the same server and preview HTML, in a TypeScript client; its Test Explorer on R1's addresses. |
| Stepping through steps (DAP) | only if R2 turns out not to be enough. |
| Attach to the app too | for breakpoints in the MAUI app's own code, if the fixture can say its process id. |
| Packaging | a `.vsix` in the release, or the Marketplace; the server reports a version mismatch with the suite's Brinell packages. |

After phase 3, the decisions P1, P2, R1, R2 and L1 go to `.docs/` as an AD. The proposal and this
plan are then extracted and deleted, as `.my/` requires.

## 6. Manual checklists

Run in the VS experimental instance with `samples/Todo/Brinell.Samples.Todo.slnx`, built. The
engine side has automated tests. These checklists cover what only Visual Studio shows.

### 6.1 Preview

- Opening `TOD.11-manage-users.feature.md` shows the preview, in the current theme; switching the VS
  theme changes it without a reload.
- Typing a typo in a step marks that step within a second; fixing it clears the mark.
- The caret on a step highlights it in the preview; a click in the preview moves the caret.
- Before any build (a clean clone), the preview says it only parses, and why.
- A file linked into the web suite offers the MAUI and web suites in the picker.

### 6.2 Run

- Run on a scenario builds, runs only that scenario, and the preview shows its step results.
- A second Run while one is going is refused; a build started during a run is cancelled with a
  message.
- Cancel stops the test process and the Todo app.

### 6.3 Debug

- A step breakpoint set in the preview stops the debugger before that step; `UatStepBreak` shows
  the text and arguments; F11 reaches the step definition.
- A breakpoint in a `.steps.md` body stops at each call.
- Test Explorer → Debug on the same case stops at the same breakpoint.
- A C# breakpoint in a page object also stops, as usual.

## 7. Spike questions

| # | Question | If no |
| --- | --- | --- |
| S1 | Can VisualStudio.Extensibility (out of process) host a WebView2 tool window, attach the debugger and cancel a build? | VSSDK in process (P4's default). |
| S2 | Does `ILanguageClient` serve files the Markdown editor owns (the `markdown` content type, filtered to our file names), with semantic tokens? | A content type of our own for `*.feature.md` and `*.steps.md` that derives from Markdown (not plain `*.feature`, which Reqnroll's extension owns); semantic colours through a VS classifier fed by the server. |
| S3 | Is there a supported API to run or debug a Test Explorer case by id (`ITestsService` or similar)? | `dotnet test` + attach (R3). |
| S4 | Does `VSTEST_HOST_DEBUG=1` + attach + continue work reliably with xUnit v2 and the Todo fixture's app start? | Start the testhost ourselves with `vstest.console` under the debugger. |
| S5 | Can an xUnit v2 test case keep a source location we set (the `.md` file and line), past the VS adapter's own lookup? | Our own *Go to scenario* on the case, from the trait. |
| S6 | Does a WebView2 in a VS tool window keep focus and keys sane (Ctrl+F, Escape, copy) and survive dock and undock? | Known issues, handled per case in 1.5. |

## 8. Open questions

| Question | Notes |
| --- | --- |
| Where do the libraries live? | `tools/` here, since only tools use them. If the Presenter adopts `Brinell.Uat.Workspace` or `Brinell.Uat.Preview`, move them to `srcnew/`. |
| A preview without pages | A service suite's features could be previewed with `Brinell.Bdd` alone, but binding still needs the UI step patterns for UATB013 (AD-020). Keep one server on `Brinell.Uat` unless a suite without any UI package appears. |
| xUnit v3 | R1's discoverer is v2-specific. A v3 move would redo it (v3 lets a case set its source location, which would make S5 moot). Not a reason to move now. |
| The Presenter's renderer | The Presenter shows steps with its own views. Should it show the preview HTML instead? Not in this plan. |
| Break on failure | Stop at a failing step before the scenario ends, with the screenshot taken. It's easy on top of R2 (`BreakOnFailure` in the same file). Add it in 2.2 if it's cheap. |
| Where the VSIX is built | It needs Visual Studio's MSBuild, so it can't be built with `dotnet build` on CI. Build it in a separate CI job, or only locally until packaging. |
