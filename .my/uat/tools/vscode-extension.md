# VS Code extension for UAT scenarios: high-level design

Status: **proposal**, drafted 2026-09-27. Nothing is built.
Area: a new `tools/Brinell.Uat.VsCode` (TypeScript client) and `tools/Brinell.Uat.LanguageServer`
(.NET); a headless runner split out of `srcnew/Brinell.Presenter`.

## 1. Goal

A tester or developer writes, reads and runs `.uat.md` scenarios in VS Code with the help an IDE
gives C#: completion from the real phrase catalog, errors while typing, click-through to the code
a step runs, a readable preview, and run and debug from the scenario itself.

Three rules shape everything below:

- **One checker.** The editor reports exactly what the xUnit gate and `brinell-data validate`
  report, because it runs the same parser, binder and value reader (AD-016's "one checker"). The
  extension does not re-implement the grammar in TypeScript. The only TypeScript grammar is a
  TextMate file for colours.
- **The file stays plain Markdown.** Nothing the extension adds is needed to read a scenario on
  GitHub or in the Presenter. No front matter, no magic comments.
- **The Presenter stays.** It is the demo and tester tool that needs no IDE. The extension is for
  people who have one. Both use the same headless runner (section 6).

## 2. What exists to build on

| Piece | Where | Gives the extension |
| --- | --- | --- |
| `UatMarkdownParser` | `Brinell.Uat` | the document model with line numbers, `UAT0xx` codes, `*.phrases.md` |
| `UatCatalogSet`, `UatCommandCatalog` | `Brinell.Uat` | every phrase per head and keyword, with the declaring method, parameters and types |
| `UatBinder`, `UatPreparedCallables` | `Brinell.Uat` | binding with `UATB0xx`/`UATA0xx`/`UATC0xx`, callable frames (`UatCallFrame`) |
| `UatValues` | `Brinell.Uat` | value reading and the one-right-answer fixes |
| `ScenarioCheck` | `tools/Brinell.DataValidator` | building the catalogs from a fixture type **without starting it** |
| `brinell-data` | `tools/Brinell.DataValidator` | `validate --format json --fix`, `phrases`, `snippet`, `describe`, `schema` |
| `UatExecutionService`, `PresenterUatExecutionSession` | `Brinell.Presenter` | running a scenario step by step, with pacing and AUT placement |
| `UatScenarioTestBase` | `Brinell.Uat` | one xUnit theory case per scenario, and `<scenario>__result.json` per run |
| `MsBuildProjectEvaluator`, `FixtureInspector` | `Brinell.Presenter` | finding the built Pages assembly and the fixture without loading it (AD-018) |

## 3. Architecture

```text
VS Code
 +-- Brinell UAT extension (TypeScript)
 |    +-- LSP client ------------------------------ Brinell.Uat.LanguageServer (.NET)
 |    |                                              parser, catalogs, binder, values
 |    |                                              (reuses Brinell.Uat and ScenarioCheck)
 |    +-- TextMate injection: gherkin fences in Markdown (colours only)
 |    +-- markdown-it plugin: preview rendering
 |    +-- Test controller (Testing API) ---------+
 |    +-- Debug adapter (DAP) -------------------+-- Brinell.Uat.Host (.NET, headless runner)
 |                                                   one scenario session, step by step,
 |                                                   the app under test, __result.json
 +-- C# Dev Kit / C# extension (optional): "step into C#" attaches to Brinell.Uat.Host
```

- **Language server in .NET.** It references `Brinell.Uat` and the catalog-building part of
  `ScenarioCheck`, which moves from the tool into a small shared library so the tool, the server
  and the Presenter load workspaces one way.
- **One server per UAT project** (per `uat.config.md`). A workspace with the Todo MAUI, web and
  mixed suites has three catalogs; a scenario file is checked against the suite it belongs to
  (and, for a linked file, against every suite that links it: see section 9).
- **The catalog needs a build.** The server reads the built Pages assembly. It loads it in a
  collectible context per catalog and reloads it when the build output changes, so building
  never fails with the file in use (the AD-018 lesson). Until a build exists, the server does
  parsing only and says why binding is off.
- **Workspace trust.** Building a catalog loads the suite's assemblies. The server only does
  that in a trusted workspace; in restricted mode it parses and colours.

## 4. The scenario editor

The extension adds language features to Markdown files named `*.uat.md` and `*.phrases.md`
(and `uat.config.md`, section 8). The built-in Markdown features stay.

### 4.1 While typing

| Feature | What it does |
| --- | --- |
| Diagnostics | Every `UAT0xx`, `UATB0xx`, `UATA0xx`, `UATC0xx` at its line and column, with the same text as the gate. A problem inside a callable call is shown at the call, with the body step it came through as related information. |
| Completion: keywords and sections | `Given`/`When`/`Then`/`And`/`But`; `## Scenario:`, `## Scenario Outline:` with an `### Examples` table, `## Background`, `## Data:`, `## Phrase:`, `## Phrase for each row:`, `## Metadata` with its known fields. |
| Completion: phrases | The phrases of the step's effective keyword, from the suite's catalog, C# and callable alike. Typing filters by words, not just by prefix (`tap sav` finds `I tap {control}` with `Save`). Snippet placeholders for each `{x}`. |
| Completion: values | `{page}`: page names. `{control}`: the controls of the page the scenario is on at that line, then the rest; exact once static page tracking exists. `{user}`/`{app}`: the `Apps` table. Enum parameters: member names. Dates: `today`, `+2d`. `TestData`: test set names. Table columns for record parameters. `<column>` in an outline: the Examples columns. |
| Hover | The phrase, the method it binds to (`TodoListPage.AddButton` → `Click`), the XML `<summary>`, each argument with its type and the value as read (`+2d` → `29-Sep-2026`). For a callable call: its steps with the values filled in. |
| Semantic colours | Keywords, the literal part of a phrase, arguments, quoted strings, placeholders, tags, a step that binds nowhere. |
| Inlay hints (optional) | The page a step runs on, and argument types, dimmed at the end of the line. |
| Formatting | Aligns Markdown tables; nothing else is reformatted. |
| Outline and folding | Feature, scenarios, outline rows, phrase sections. Breadcrumbs follow. |

### 4.2 Click-through

| From | Go to definition | Find references |
| --- | --- | --- |
| A step bound to a C# phrase | the method (`[UatWhen]` on a phrase class, a page-object method, a control verb) | every step in the suite that binds to it |
| `{control}` / `{page}` value | the page-object property or page class | every step that names it |
| A step bound to a callable | the `## Phrase:` heading | every call |
| A `<x>` in a callable body | the heading's `{x}` | - |
| `TestData` value | the test set's file | scenarios that use it |
| A `@tag` | - | scenarios with the tag |

Source locations come from the **portable PDB** next to the built assembly (method → document
and line through `System.Reflection.Metadata`), so no Roslyn workspace is needed. A property is
found through its getter. Without a PDB, the extension falls back to a workspace symbol search by
name, which C# Dev Kit answers.

### 4.3 Code actions

- **Quick fixes** for the diagnostics that have one right answer: the same fixes as
  `brinell-data validate --fix` (a date format, a misspelled page or control name with one near
  match, a missing Examples column).
- **Create phrase** for an unknown step: inserts the `brinell-data snippet` C# stub into a phrase
  class the developer picks. Generates text only; it never compiles or edits a page object.
- **Extract phrase** from selected steps: turns them into a `## Phrase:` in the file or in
  `Scenarios/*.phrases.md`, with `{x}` for the values that differ, and replaces the steps with a
  call. Refuses a selection with steps whose meaning depends on the page it would move away from.
- **Inline phrase**: the reverse, for one call.
- **Rename** a callable's heading and all its calls.

## 5. The preview

The built-in Markdown preview, extended rather than replaced, so scenarios keep looking like
Markdown and the preview keeps working with every other extension.

- **A markdown-it plugin** (`markdown.markdownItPlugins`) renders a ```` ```gherkin ```` fence
  (and a fence in a `.uat.md` file with no language) as a scenario block, not as code: one step
  per line, the keyword in a fixed-width gutter, the step text in a readable proportional font,
  arguments and quoted values coloured, tables as real tables, doc strings as quoted blocks,
  `<table>` and `<doc string>` as small badges.
- **A preview stylesheet** (`markdown.previewStyles`) with the colours as CSS variables, for
  light, dark and high-contrast themes. The font is configurable; the default is the editor's UI
  font, not the code font.
- **Last run results** (optional): when a run exists, each step shows its status icon and a
  scenario shows its duration, taken from the run's `__result.json`. The preview refreshes when a
  run ends. A failed step links to its screenshot.
- **Run from the preview.** Each scenario heading gets **Run** and **Debug** links. See open
  question Q1 about command links in the built-in preview; the fallback is a small "scenario
  view" webview of our own beside the editor.

The same rendering is not needed on GitHub: a `gherkin` fence already renders there as
highlighted code, which is enough.

## 6. Running and debugging

### 6.1 Run: the Testing API

A test controller shows every UAT suite in the Test Explorer: suite → file → scenario → outline
row. CodeLens above each `## Scenario:` offers **Run** and **Debug**, and the gutter shows the
last result.

- **Run** calls `dotnet test` on the suite's project with a filter on the scenario's theory case
  (one case per scenario already exists), then reads `__result.json` for step results, the
  near-misses and the screenshots (AD-007 layout). A failing step is marked at its line with the
  message; a failure inside a callable is marked at the call.
- **Run all in file / tag / folder** maps to a filter on several cases.
- This needs nothing new in Brinell except, perhaps, a stable way to address one case from the
  command line (Q2).

### 6.2 Debug: step through a scenario

`dotnet test` cannot pause between steps. Debug uses a headless runner instead:

- **`Brinell.Uat.Host`**: the Presenter's session code (`UatExecutionService`, the collectible
  load context, AUT placement, pacing) moved out of the MAUI app into a console host. It speaks a
  small JSON protocol over stdio: load workspace, start scenario, next step, continue, stop,
  results. The Presenter then uses the same library in-process.
- **A debug adapter** (DAP) in the extension maps debugging onto scenarios:

| DAP concept | In a scenario |
| --- | --- |
| Breakpoint | a step line (also inside a callable body or a `*.phrases.md` file) |
| Step over | run the next step |
| Step into | into a callable call, one body step at a time; on a C# phrase, into C# (below) |
| Step out | finish the callable |
| Call stack | the scenario, then each `UatCallFrame` (callable and row) |
| Variables | the step's arguments as read, the current app and page, the scenario's test set, per-app state |
| Pause on failure | stop at the failing step with the screenshot in the variables view |
| Watch | not supported: there are no variables in scenario language (AD-017) |

- **Step into C#.** The host is a normal .NET process, so the extension can start it under the
  C# debugger through a compound launch: the scenario-level adapter drives the steps, and the
  .NET debugger stops at breakpoints in page objects and phrase classes. Which .NET debugger is
  used depends on what the machine has (Q3).
- **Pacing and placement**: a setting for the delay between steps (as the Presenter's), and the
  app under test placed beside VS Code on Windows.

## 7. Views

- **Phrase catalog** tree (like the Presenter's Command Catalog): phrases by keyword and source
  (control verbs, pages, phrase classes, callables), with search; click inserts at the cursor,
  and each entry goes to its definition.
- **Pages** tree (like Discovery): pages and their controls with UAT names and AutomationIds.
- **Results**: the last run per scenario with screenshots and the near-miss report, opened from
  a failed step.

## 8. The other files

| File | Support |
| --- | --- |
| `uat.config.md` | completion of sections and fields, validation through `UatWorkspaceConfigInspector` (the same diagnostics as the Presenter's Config tab), the derived values (target, Pages assembly, fixture) as inlay hints |
| `apps.md` / `Apps` table | the known columns; a user or app name used in a scenario goes to its row |
| Test data JSON | a JSON schema association from `brinell-data schema`, and `brinell-data validate` diagnostics |
| `journeys.md` | links from a journey id to its scenarios, and a coverage hint for journeys with no scenario |

## 9. Things easy to forget

- **Linked scenario files.** One scenario file serves every head (AD-014): the web suite links the
  MAUI one's files. A file belongs to several suites, so diagnostics say which head they are for,
  and a step that binds on one head only is a warning on the file, not an error per suite.
- **Mixed suites** bind each step against its app's head catalog (AD-015); the server needs the
  `Apps` table to know which.
- **Stale catalog.** Diagnostics come from the last build. The status bar says when the build is
  older than the phrase classes and page objects, and offers **Build**.
- **Performance.** Reparse on each keystroke is cheap; binding a large suite is not. Bind the open
  file on change and the rest of the suite on save.
- **Multi-root and several suites** in one window; the Construction repo will bring its own.
- **Where it runs.** The language server needs .NET 10 and runs on any OS. Running a MAUI suite
  needs Windows (or an emulator for Android). In Remote, WSL or Codespaces, editing works and a
  Windows-only run says so.
- **Packaging.** A `.vsix` with the language server as a framework-dependent build, or installed
  as a `dotnet tool`; the server version must match the Brinell packages the suite references,
  so it reports a mismatch instead of guessing.
- **AI assistants.** The same catalog as language-model tools (`list phrases`, `validate
  scenario`, `explain step`), so Copilot or Claude writes scenarios against real phrases and
  checks them before proposing. This is the cheapest high-value part after the server exists.
- **Accessibility.** Status icons in the preview and gutter need text alternatives; colours must
  meet contrast in every theme.
- **Tests.** The server's features are tested in .NET against `Brinell.Uat.Tests` fixtures; the
  extension with `@vscode/test-electron`; the preview plugin with markdown-it snapshot tests.
- **Localization.** Phrases can be in any language (AD-015): completion and matching must not
  assume English word boundaries beyond whitespace.

## 10. Phases

| # | Phase | Delivers | Depends on |
| --- | --- | --- | --- |
| 0 | Spike | a language server that loads the Todo suite's catalog and reports diagnostics in VS Code; answers Q1-Q3 | - |
| 1 | Editor basics | diagnostics, keyword and phrase completion, hover, go to definition, outline, TextMate colours | shared catalog-loading library |
| 2 | Preview | markdown-it plugin and styles, themes | - (parallel to 1) |
| 3 | Run | Test Explorer, CodeLens, results from `__result.json` | 1 for locations |
| 4 | Debug | `Brinell.Uat.Host`, the debug adapter, step into callables, compound C# debugging; the Presenter moves onto the host library | 3 |
| 5 | The rest | code actions, views, config and test-data support, language-model tools, inlay hints | 1 |

Phase 1 alone pays for itself: a tester sees an unknown step or a bad date while typing instead of
after a run.

## 11. Decisions this would record

If the proposal goes ahead, these rule things out and belong in `.docs/` as ADs:

- The editor's checker is the engine's checker: no second grammar or binder in TypeScript.
- Scenario files stay plain Markdown: no editor-only syntax.
- One headless runner for the extension and the Presenter.

## 12. Open questions

| # | Question | Notes |
| --- | --- | --- |
| Q1 | Can the built-in Markdown preview show links that run extension commands? | If not, Run/Debug live in CodeLens and our own scenario view, and the built-in preview only shows results. Settle in the spike. |
| Q2 | How is one scenario case addressed from `dotnet test`? | Theory data is `[filePath, scenarioName]`; a filter on the display name works but is fragile. A trait per scenario, or a `BRINELL_UAT_SCENARIO` filter read by `UatScenarioTestBase`, would be stable. |
| Q3 | Which .NET debugger for "step into C#"? | The Microsoft debugger is licensed for Microsoft's own tools (C# Dev Kit); `netcoredbg` is open. Attaching through the user's C# extension avoids shipping one. |
| Q4 | One extension or two (editor + runner)? | One is simpler to install; two let the editor work where the runner cannot. |
| Q5 | Where does the TypeScript live? | In this repo under `tools/`, built by its own npm scripts, or its own repo. The language server belongs here either way. |
| Q6 | Should the Presenter become a VS Code webview? | Not proposed: the Presenter must run without an IDE. Sharing the host library is enough. |
