# Scenario Preview In Visual Studio

The Brinell UAT extension shows a feature or step file as readable steps, beside the editor: what
each step binds to and on which page, the gate's diagnostic on a step that binds to nothing, a
callable call unfolded into its body, and each Scenario Outline row. It is the same check the
xUnit gate and `brinell-data validate` run, on the text as you type it.

Visual Studio 2022 17.10 or later (2026 too), and the .NET 10 runtime, which a Brinell suite needs
anyway.

## Build And Install

The extension needs Visual Studio's MSBuild, so it has a solution of its own. From the Brinell root:

```powershell
& "<Visual Studio>\MSBuild\Current\Bin\MSBuild.exe" tools\Brinell.Uat.VisualStudio\Brinell.Uat.VisualStudio.slnx -restore
```

The build publishes `Brinell.Uat.LanguageServer` into the VSIX and deploys the extension to the
experimental instance (`devenv /rootsuffix Exp`); pass `/p:DeployExtension=false` to only build it.
To install it for everyday use, open `tools\Brinell.Uat.VisualStudio\bin\Debug\net48\Brinell.Uat.VisualStudio.vsix`.

## Use

- Open a `.feature.md` or `.steps.md` and press **Ctrl+K, V** (or right-click, **Open Scenario
  Preview**; or **View > Other Windows > Scenario Preview**).
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

## Run From The Preview

Each scenario heading has **Run**, and the title **Run file**. A run:

1. builds the solution through Visual Studio, so a stale app never runs;
2. runs `dotnet test <suite project> --no-build --filter "UatScenario=<address>"` (every row of an
   outline, or `UatScenario~<file>#` for the file), its output in the **Brinell UAT** Output pane;
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
  right-clicking and choosing **Toggle UAT Step Breakpoint**, or by pressing **Ctrl+K, F9**. A red
  dot shows in the preview's gutter and in the editor's glyph margin. A line that is not a step
  gets none, and the status bar says so.
- **Debug** beside each **Run** (and **Debug file** beside **Run file**) runs the same way, with
  the testhost waiting for a debugger (`VSTEST_HOST_DEBUG`). The extension attaches Visual Studio's
  debugger to the testhost and the run goes on to the first breakpoint. If the attach fails, the run
  is cancelled. The app the fixture starts is its own process and is not attached.
- The breakpoints move with their steps as you edit lines above them. Deleting a step's line
  removes its breakpoint.

The breakpoints are a per-user file, `%LOCALAPPDATA%\Brinell\uat-breakpoints.json`, read only
while a debugger is attached:

```json
{ "files": { "C:\\src\\Suite\\Features\\TOD.01-see-my-todos.feature.md": [19, 25] }, "breakOnFailure": false }
```

- A line of a feature file stops before that step; the line of a call to a callable stops once,
  before the call's first step.
- A line in a `.steps.md` stops at that step each time a scenario calls it.
- `breakOnFailure` stops after a step that fails, before the scenario ends.

The debugger stops in `UatStepExecutionSession.RunNextAsync`. The local `stepBreak` holds the
step's text, its arguments as read, the app, the page and the call chain; **F11** goes into the
step definition. A test runs the build output's copy of a feature file; a listed file matches it
by its path below the `Features` folder.

The extension starts `dotnet Server\Brinell.Uat.LanguageServer.dll` from its own folder; its log is
the **Brinell UAT** pane of the Output window.

## In The Editor

Feature files, step files and the suite configs (`uat.config.md`, `bdd.config.md`) open in the
Markdown editor with a language server of their own, the same `Brinell.Uat.LanguageServer`:

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

## CodeLens In C#

Above a step definition, a page phrase or a page object's control, CodeLens says how many UAT
steps use it: "used in 12 UAT steps · 5 scenarios". Click it for the list: file, line, step and
scenario. It counts every suite of the solution, linked files included. A control counts the
control verbs on its page (`When I tap Sync` on the Todo List page for `TodoListPage.Sync`). A
solution without a `uat.config.md` or `bdd.config.md` never starts the server for it. The counts
come from the last build: build to see new steps.

## Without Visual Studio

The server renders one file as a page, for a look or a bug report (from the Brinell root, after
`dotnet build tools\Brinell.Uat.LanguageServer`):

```powershell
tools\Brinell.Uat.LanguageServer\bin\Debug\net10.0\Brinell.Uat.LanguageServer.exe --preview samples\Todo\tests\Brinell.Samples.Todo.Uat\Features\TOD.01-see-my-todos.feature.md --solution samples\Todo --out TestResults\preview.html
```

`--host` writes the page the IDE's webview loads, with the preview posted into it.
