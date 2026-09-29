# Scenario Preview In Visual Studio

The Brinell BDD extension shows a feature or step file as readable steps, beside the editor: what
each step binds to and on which page, the gate's diagnostic on a step that binds to nothing, a
callable call unfolded into its body, and each Scenario Outline row. It is the same check the
xUnit gate and `brinell-data validate` run, on the text as you type it.

Visual Studio 2022 17.10 or later (2026 too), and the .NET 10 runtime, which a Brinell suite needs
anyway.

## Build And Install

The extension needs Visual Studio's MSBuild, so it has a solution of its own. From the Brinell root:

```powershell
& "<Visual Studio>\MSBuild\Current\Bin\MSBuild.exe" tools\Brinell.Bdd.VisualStudio\Brinell.Bdd.VisualStudio.slnx -restore
```

The build publishes `Brinell.Bdd.LanguageServer` into the VSIX and deploys the extension to the
experimental instance (`devenv /rootsuffix Exp`); pass `/p:DeployExtension=false` to only build it.
To install it for everyday use, open `tools\Brinell.Bdd.VisualStudio\bin\Debug\net48\Brinell.Bdd.VisualStudio.vsix`.
Each GitHub release carries both extensions: `Brinell.Bdd.VisualStudio-<version>.vsix` for Visual
Studio, and `brinell-bdd-vscode-<version>.vsix` for VS Code (**Extensions: Install from VSIX**).

The server checks that a suite is built against its own Brinell. When a Brinell assembly the
suite's build references differs from the server's by a major or minor version, the suite's config
gets a warning (`UATG010`), since the editor may then bind differently from the suite's own run.
Update the extension, or the suite's Brinell packages.

## Use

- Open a `.feature.md` or `.steps.md`: the preview is beside the text, where Visual Studio shows
  its Markdown preview for other `.md` files (that one is turned off for feature and step files).
  Drag its left edge to resize it.
- **Alt+Shift+V** shows or hides it; so do **Ctrl+K, V** (Ctrl released before the V;
  **Ctrl+K, Ctrl+V** is Visual Studio's own, Class View's search box) and right-click, **Show/Hide
  Scenario Preview**.
- **View > Other Windows > Scenario Preview** shows the same preview in a tool window, which you
  can float onto another screen. It follows the active file.
- The preview follows the active feature or step file, and renders again when you pause typing and
  after each build.
- The caret and the preview follow each other: move the caret and its step is highlighted; click a
  step and the caret goes there.
- The bar at the top says whether the file is **bound** or **parsed only**, and why. Until the
  suite is built, the preview parses only; build the solution and it binds.
- A file linked into several suites (AD-014) has a suite picker in the bar.
- The colours follow Visual Studio's theme, including high contrast.
- After a run (Test Explorer or `dotnet test`), each step shows its status, the scenario its
  result and duration, and a failed step its message and the screenshot taken then; click the
  thumbnail to open it. The preview finds the newest run of each scenario through the runs'
  `TestResults/<run-id>/manifest.json`, and refreshes when a run writes one.
- **Delete old results** so nothing old shows: **✕** beside a scenario's result deletes that
  scenario's (an outline's, every row's), and **Delete results** in the bar the whole file's. In the
  BDD Explorer's Results tab, **Delete** on a file and **Delete all results** for the suite. Each
  asks first, naming how many runs and files, and deletes for good in every run: the scenario's
  report and its screenshot. A run left with nothing else is removed; other tests' artifacts stay.
  A run written to in the last half minute, or while a run from the preview goes, is left alone.

## Run From The Preview

Each scenario heading has **Run**, and the title **Run file**. A run:

1. builds the solution through Visual Studio, so a stale app never runs;
2. runs `dotnet test <suite project> --no-build --filter "BddScenario=<address>"` (every row of an
   outline, or `BddScenario~<file>#` for the file), its output in the **Brinell BDD** Output pane;
3. shows the results in the preview when the run writes them.

The suite's test class must use `[UatScenarioTheory]`, which gives each case its address; with
`[Theory]` the buttons are disabled and say why. Test Explorer runs the same cases either way.

While a run goes, the UI test rules hold: a second run is refused, a build started during the run
is cancelled (it would overwrite the binaries under the run), and **Cancel run** in the bar stops
the test process and the app it started.

## Break At A Step

A step breakpoint stops the debugger before that step, whichever way the test was started (the
preview's Debug, or Test Explorer's own **Debug**).

- **Set one** by clicking a step's keyword in the preview, or with the caret on the step, by
  right-clicking and choosing **Toggle BDD Step Breakpoint**, or by pressing **Ctrl+K, F9** (or
  **Ctrl+K, Ctrl+F9**). A red
  dot shows in the preview's gutter and in the editor's glyph margin. A line that is not a step
  gets none, and the status bar says so.
- **Debug** beside each **Run** (and **Debug file** beside **Run file**) runs the same way, with
  the testhost waiting for a debugger (`VSTEST_HOST_DEBUG`). The extension attaches Visual Studio's
  debugger to the testhost and the run goes on to the first breakpoint. If the attach fails, the run
  is cancelled. The app the fixture starts is its own process and is not attached.
- The breakpoints move with their steps as you edit lines above them. Deleting a step's line
  removes its breakpoint.
- The app under test is debugged too. The MAUI app on Windows, or a web app started from its
  build, is attached after the testhost, so a breakpoint in the app's own code also stops. A web
  app started with `dotnet run` is not, since that process is the CLI.

### Step Through A Scenario

While the run is paused at a step, the preview shows a bar with where it is paused, the step is
marked, and the editor shows its line (in the step file, for a call's step). From there:

| Move | Keys | Goes to |
| --- | --- | --- |
| **Step over** | Alt+Shift+F10 | the next step; a `## Step:` call runs whole |
| **Step into** | Alt+Shift+F11 | inside a `## Step:` call, before its first step, without running anything |
| **Step out** | Ctrl+Shift+F11 | the first step after the call the run is in |
| **Continue** | F5 | the next step breakpoint |
| **Stop** | Shift+F5 | the end of the run |

The same moves are the bar's buttons and the **Debug** menu's **Step Over BDD Step**, **Step Into
BDD Call** and **Step Out of BDD Call**. A `## Step:` call is a position of its own: the run stops
at the call first (its line in the feature file), and Step into goes inside. F10 and F11 stay the
C# debugger's: F11 at a paused step goes into its step definition's code.

The breakpoints are a per-user file, `%LOCALAPPDATA%\Brinell\bdd-breakpoints.json`, read only
while a debugger is attached:

```json
{ "files": { "C:\\src\\Suite\\Features\\TOD.01-see-my-todos.feature.md": [19, 25] }, "breakOnFailure": false }
```

- A line of a feature file stops before that step; the line of a call to a callable stops once,
  at the call, before its first step.
- While paused, the engine writes `bdd-paused.json` beside it (where, and the test process); a
  step move is a one-shot `"next": { "mode": "over", "depth": 0, "process": 4711 }` the extension
  writes and the engine removes when it stops.
- A line in a `.steps.md` stops at that step each time a scenario calls it.
- `breakOnFailure` stops after a step that fails, before the scenario ends.

The debugger stops in `BddStepExecutionSession.RunNextAsync`. The local `stepBreak` holds the
step's text, its arguments as read, the app, the page and the call chain; **F11** goes into the
step definition. A test runs the build output's copy of a feature file; a listed file matches it
by its path below the `Features` folder.

The extension starts `dotnet Server\Brinell.Bdd.LanguageServer.dll` from its own folder; its log is
the **Brinell BDD** pane of the Output window.

## In The Editor

Feature files, step files and the suite configs (`uat.config.md`, `bdd.config.md`) open in the
Markdown editor with a language server of their own, the same `Brinell.Bdd.LanguageServer`:

- **Diagnostics.** The gate's problems show as squiggles and in the Error List, word for word as
  the xUnit gate and `brinell-data validate` report them: the open file as you type, the whole suite
  on save and after a build. A problem inside a callable shows at the call, with the steps it went
  through. A step that failed in the last run is an error at its line (`UATRUN`), with the failure's
  message.
- **Outline, folding and colours.** The headings show as a tree in the Document Outline and the
  navigation bar: Feature, Scenario, Background, Examples, `## Step:` and the other sections. The
  sections, fences, tables and doc strings fold. Step keywords, section keywords, quoted values,
  `{x}` parameters, `<x>` placeholders, tags and comments in a fence get their own colours, from the
  server (Visual Studio has no grammar for these files). All three work before a build and while
  the file does not parse.
- **Completion.** At the start of a step line you get the keywords. Outside a fence you get the
  sections (`## Scenario:`, `## Step:`, a gherkin fence). After the keyword you get the step texts
  that hold every word you typed, in any order: the C# steps of the keyword and the callable steps
  the file sees. Steps on the current page come first, and a pick puts in the text with each `{x}`
  as a tab stop. In a parameter, you get its values: pages for `{page}`, the controls of the page
  the scenario is on at that line for `{control}`, users and apps, enum members, and in an outline
  the Examples columns as `<x>` (also before a build). Needs a build for the steps and values.
- **Hover.** On a step: what it binds to (the method, or the built-in), the page and app it runs
  on, the method's `<summary>` when the build writes an XML doc file, and each argument as written
  and as the step definition reads it (`+2d` as a date). On a call: the `## Step:` it calls and its
  body.
- **Go to definition (F12).** A C# step opens its method, found through the build's portable PDB.
  A control verb opens the control on the page the scenario is on at that line, a page step opens
  the page class, and a call opens its `## Step:` heading.
- **Find all references (Shift+F12).** On a step: every step across the suite (linked files too)
  that binds to the same method; a C# step used inside a callable counts at the body's line. On a
  call, or on a `## Step:` heading: every call of that step.

- **Quick fixes (Ctrl+.).**
  - A value with one right answer (`2026-05-12` for a date) is rewritten, as
    `brinell-data validate --fix` does.
  - Old headings are rewritten, and an old file name (UAT029) is renamed.
  - On a step that binds to nothing, **Create step definition** adds a stub to the suite's
    `[BddStepDefinitions]` class: its phrase has a placeholder for each quoted value and number.
  - Select steps and **Extract** them to a `## Step:` of the file; the call takes their place, named
    "the extracted steps", so rename it.
  - On a call, **Inline** puts its body in its place, with the call's values.
- **Rename (F2)** on a `## Step:` heading or a call renames the callable step and every call of it
  across the suite. Each call keeps its values, so the new phrase must keep the same `{x}`
  placeholders.

## Suite Configs

`uat.config.md` and `bdd.config.md` are checked as you type, with diagnostics `UATG001`-`UATG009`:
- an unknown section, field or assembly kind;
- a Runtime table without a Fixture;
- a head, boolean or test set that is not one;
- an app without a name;
- an AppsFile that is not found;
- an assembly not built yet;
- a `Using` namespace nothing is in;
- a suite that cannot bind, with the reason;
- a `testdata.schema.json` out of date with the test-data definition. **Ctrl+.** regenerates it, as
  `brinell-data schema` does.

Completion offers the fields of each table and their values (heads, `true`/`false`, test sets),
the assembly kinds, the namespaces, and the heads in the Apps table. Hover on a field says what it
does. In a feature file, the `TestData` metadata offers the suite's test sets.

## BDD Explorer

**View > Other Windows > BDD Explorer** shows a suite of the solution; pick another in its bar.

- **Steps**: every step the suite can bind to, grouped by the class that declares it, the page of a
  page phrase, control verbs and built-ins; then the callable steps of each step file; then the
  hooks of each step-definition class, with their tags.
- **Pages**: each page with its controls (their type, and the page tapping one goes to) and its
  page phrases.
- **Results**: the last run of each scenario, by feature file.

Click an entry to open its source: the method, the page class, the control, the `## Step:`
heading or the scenario. The window refreshes after a build and when a run writes results.

## CodeLens In C#

Above a step definition, a page phrase or a page object's control, CodeLens says how many UAT
steps use it: "used in 12 BDD steps · 5 scenarios". Click it for the list: file, line, step and
scenario. It counts every suite of the solution, linked files included. A control counts the
control verbs on its page (`When I tap Sync` on the Todo List page for `TodoListPage.Sync`). A
solution without a `uat.config.md` or `bdd.config.md` never starts the server for it. The counts
come from the last build: build to see new steps.

## Copilot And Other Agents

The same server is an MCP server for GitHub Copilot's agent mode (Visual Studio 17.14 and later,
VS Code) and any other MCP client:
- `list_suites`, `list_steps`, `list_pages` and `list_test_sets` give what a scenario can say;
- `validate_scenario` checks a draft as the gate does;
- `find_step_usages` finds where a C# member is used.

It calls no model: it holds the catalog and runs the checks, and Copilot does the writing.

Register it in the solution folder's `.mcp.json` (Visual Studio) or `.vscode/mcp.json` (VS Code).
From the Brinell root, for the Todo sample:

```json
{
  "servers": {
    "brinell-bdd": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "tools/Brinell.Bdd.LanguageServer", "--", "--mcp", "--solution", "samples/Todo"]
    }
  }
}
```

Build the suite first; its steps come from its build. Then, in Copilot Chat's agent mode, run the
prompt `/brinell-write-scenario` (`.github/prompts/brinell-write-scenario.prompt.md`). It writes a
feature file with only the suite's steps, and checks it with `validate_scenario` until the gate
passes it. A step with no phrase is left as a marked gap, never an invented phrase.

## Without Visual Studio

The server renders one file as a page, for a look or a bug report (from the Brinell root, after
`dotnet build tools\Brinell.Bdd.LanguageServer`):

```powershell
tools\Brinell.Bdd.LanguageServer\bin\Debug\net10.0\Brinell.Bdd.LanguageServer.exe --preview samples\Todo\tests\Brinell.Samples.Todo.Uat\Features\TOD.01-see-my-todos.feature.md --solution samples\Todo --out TestResults\preview.html
```

`--host` writes the page the IDE's webview loads, with the preview posted into it.
