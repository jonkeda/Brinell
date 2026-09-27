# Visual Studio extension for UAT scenarios: high-level design

Status: **proposal**, drafted 2026-09-27. Nothing is built. The order of work and the design
choices that differ from this proposal are in [visualstudio-plan.md](visualstudio-plan.md): the
preview first, rendered by the engine; then run and debug; then the editor.
Companion to [vscode-extension.md](vscode-extension.md), which this reuses for everything that is
not specific to the IDE. Read that first. This doc only says what is **different** in Visual Studio.

## 1. Why a second IDE

VS Code is where testers write scenarios. Visual Studio is where the Construction developers
spend their day: the app, the page objects and the phrase classes all live there. They will:

- write the C# side of a phrase and want to see which scenarios use it;
- debug a failing scenario into a page object;
- fix a scenario next to the code change that broke it.

The goal is the same editor, run and debug help, in Visual Studio, **without a second checker or
a second runner**.

## 2. What is shared, and what is per IDE

The VS Code design already puts the work into .NET processes that know nothing about the IDE. So
the Visual Studio extension is a second thin client:

| Piece | VS Code | Visual Studio |
| --- | --- | --- |
| `Brinell.Uat.LanguageServer` (.NET, LSP) | shared | shared, unchanged |
| `Brinell.Uat.Host` (headless runner, JSON over stdio) | shared | shared, unchanged |
| Preview renderer (markdown-it plugin + CSS) | built-in Markdown preview | our own tool window (section 5) |
| Brinell MCP server (catalog, validate, scraper tools; see [scraper-in-extension.md](scraper-in-extension.md)) | shared | shared |
| Client | TypeScript extension | .NET extension (section 3) |

Any feature added to the server or host lands in both IDEs. The Visual Studio client is kept
small on purpose.

## 3. Architecture

```text
Visual Studio 2022 / 2026
 +-- Brinell UAT extension (.NET, VisualStudio.Extensibility, out of process)
 |    +-- LSP client (LanguageServerProvider) ---- Brinell.Uat.LanguageServer
 |    |      for *.feature.md, *.steps.md, uat.config.md, on top of the Markdown editor
 |    +-- Commands: Run scenario, Debug scenario, Run file, Go to scenario
 |    +-- Tool windows: Scenario preview (WebView2), Phrase catalog, Pages
 |    +-- Test Explorer: the xUnit cases already there (section 6)
 +-- The C# debugger (in the box) ------------- Brinell.Uat.Host or dotnet test
 +-- GitHub Copilot (agent mode) ------------- Brinell MCP server
```

- **VisualStudio.Extensibility, out of process.** It's the current model: a .NET extension in its
  own process that can't hang or crash the IDE, and it has an LSP host. If its LSP support turns
  out to be missing something we need (Q1), the fallback is the classic VSSDK `ILanguageClient`.
  That runs in process and is mature. The language server doesn't care which one starts it.
- **The Markdown editor stays.** Visual Studio opens `.md` files in its own Markdown editor with
  a preview. The extension adds language features to that editor through LSP. It doesn't register
  a new editor, so a scenario is still a Markdown file for everything else.
- **Workspace trust.** The server loads the suite's assemblies only for a solution the user has
  opened. That's Visual Studio's own trust boundary, and it has no restricted mode like VS Code's.

## 4. The scenario editor

Everything in VS Code section 4 comes from the language server, so it works here the same way:

- diagnostics;
- completion of keywords, phrases and values;
- hover;
- go to definition and find references (VS's *Find All References* window);
- the quick fixes and code actions;
- document outline;
- semantic colours.

Differences:

| Feature | In Visual Studio |
| --- | --- |
| Colours | Semantic tokens from the server. TextMate injection into Markdown is not reliable in VS, so the server's tokens do the whole job. |
| Go to definition into C# | Better than VS Code. The target is a file in the open solution, so VS opens it with full C# tooling. The PDB lookup gives the location; no workspace symbol search is needed. |
| **From C# to scenarios** | New in VS: CodeLens on a phrase method, a page-object property or a control verb, e.g. "used in 12 steps · 5 scenarios". It opens the list. This needs the server's "find references" to answer for a C# symbol (Q2). It's the feature developers will use most. |
| Rename | Renaming a callable's heading works as in VS Code. Renaming a C# phrase's text is not offered: the text is in an attribute string, and changing it breaks scenarios. A quick fix on the broken steps is the safe route. |

## 5. The preview

Visual Studio's Markdown preview can't take a markdown-it plugin. So the extension adds a
**Scenario preview** tool window: a WebView2 with the **same** markdown-it plugin and stylesheet
as VS Code, bundled as one shared asset.

- It follows the active `.feature.md` editor and scrolls with it.
- It reads the VS theme (light, dark, blue, high contrast) and maps it to the stylesheet's CSS
  variables.
- **Run** and **Debug** on each scenario heading work, because it's our own window. VS Code's
  open question Q1 does not exist here.
- Last-run step results come from `__result.json`, as in VS Code.

## 6. Running

**Test Explorer already shows the scenarios.** `UatScenarioTestBase` makes one xUnit theory case
per scenario, and Visual Studio's xUnit adapter lists them with no extension at all. What the
extension adds:

| Gap | Fix |
| --- | --- |
| Test Explorer's "Go to test" opens the C# test method, not the scenario | A **Go to scenario** command on a UAT case, from the case's `[filePath, scenarioName]` data. Better: `UatScenarioTestBase` reports the scenario file and line as the case's source location, if xUnit v3 lets a theory case carry its own (Q3). Then VS does it natively. |
| No run from the scenario | **Run scenario** on the heading (a CodeLens-style adornment or the editor's context menu). It runs `dotnet test` with a filter for that case, the same as VS Code phase 3, and needs the same stable filter (VS Code Q2). |
| Step results | After a run, the server reads `__result.json` and marks the failing step as a diagnostic at its line (an error with the message and the screenshot path), so it shows in the Error List. |

## 7. Debugging

Visual Studio has the C# debugger in the box. So the order of work flips compared to VS Code:
stepping into C# is free, and stepping through scenario steps is the extra.

**Level 1: debug the test case (no new runner).** *Debug scenario* starts the xUnit case under the
debugger. Breakpoints in page objects and phrase classes just work. To stop **at a step**, a
breakpoint on a step line in the `.feature.md` is passed to the run as `BRINELL_UAT_BREAK_AT=<file>:<line>`.
The executor calls `Debugger.Break()` just before invoking that step. The user lands in C# at the
invocation, with the bound arguments in Locals and **F11** stepping into the phrase method.

- This is small: one check in `UatScenarioExecutor`, and it works from both IDEs.
- It's the cheapest real debugging story in either IDE, so VS Code should get it too.

**Level 2: step through scenario steps.** It's the DAP adapter from VS Code section 6.2, over
`Brinell.Uat.Host`. Visual Studio can host DAP debug adapters, but it's a less-trodden path than in
VS Code (Q4). With Level 1 in place, Level 2 is only for "step over one scenario step at a time".
It may not be worth it in VS.

## 8. Views

The same data as in VS Code section 7, as tool windows:

- **Phrase catalog**: phrases by keyword and source, with search. Double-click inserts at the
  cursor, and *Go to definition* opens the C#.
- **Pages**: pages and controls with UAT names and AutomationIds.
- **UAT results**: the last run per scenario, with screenshots and the near-miss report.

## 9. Copilot

GitHub Copilot in Visual Studio (agent mode) uses MCP servers. The **Brinell MCP server** is the
same one VS Code uses. It offers `list_phrases`, `validate_scenario`, `explain_step` and the
scraper tools. Copilot can then write a scenario against the real catalog and check it before
proposing it, in either IDE. The repo's own `.github/copilot-instructions.md` and skills already
tell Copilot the Brinell rules.

## 10. Things easy to forget

- **VS versions.** Target VS 2022 17.10+ and VS 2026. Check which VisualStudio.Extensibility
  features each has (Q1) before promising a feature.
- **Arm64 and x64.** An out-of-process extension and the .NET language server run on both;
  nothing native is shipped.
- **Solution load.** Find the UAT suites from the solution's projects (a project with a
  `uat.config.md`), not by scanning the disk. Start the server lazily, when the first scenario
  file opens.
- **Stale catalog.** VS knows when a build finishes, so the server reloads on *build done*
  instead of watching files.
- **Rider.** Not proposed. The language server would serve it through an LSP plugin if it's ever
  wanted.
- **Marketplace.** Publish on the Visual Studio Marketplace, or share the `.vsix` internally.
  Either way, the server version must match the Brinell packages (VS Code section 9).

## 11. Phases

| # | Phase | Delivers | Depends on |
| --- | --- | --- | --- |
| 0 | Spike | the VS client starts the language server on a `.feature.md`, and diagnostics show in the Markdown editor; answers Q1 and Q3 | VS Code phase 0 (the server) |
| 1 | Editor | everything the server gives: completion, hover, go to definition, outline, colours | VS Code phase 1 |
| 2 | Debug level 1 | `BRINELL_UAT_BREAK_AT` in the executor, and *Debug scenario* | - (useful in both IDEs) |
| 3 | Run and Test Explorer | *Run scenario*, *Go to scenario*, results in the Error List | VS Code Q2 |
| 4 | C# to scenarios | CodeLens on phrase methods and page-object members | the server's reference index |
| 5 | Preview and views | the WebView2 preview, catalog, pages, results | the shared preview asset |
| later | Debug level 2 | the DAP adapter in VS | Q4 |

## 12. Open questions

| # | Question | Notes |
| --- | --- | --- |
| Q1 | Does VisualStudio.Extensibility's LSP support cover what we need (semantic tokens, code actions, a language server on files the Markdown editor owns)? | Settle in the spike. If not, use VSSDK `ILanguageClient`. |
| Q2 | "Find references" from a C# symbol | The server knows method → steps. VS needs a C# symbol to ask with: the CodeLens provider gets the member at the cursor, and the server matches it by metadata token from the built assembly. |
| Q3 | Can a UAT theory case report the scenario file as its source location? | If xUnit v3 allows it, Test Explorer's own "Go to test" opens the scenario, and part of section 6 disappears. |
| Q4 | DAP in Visual Studio | Supported, but with less documentation and fewer users than in VS Code. Only needed for Level 2. |
| Q5 | One repo for both clients? | The server, host, MCP server and preview asset live here. The TypeScript and VS clients could live here too, under `tools/`, or in one IDE-extensions repo. |
