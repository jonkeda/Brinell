# The Scraper, restarted in the IDE extensions: plan

Status: **built** (steps 0 to 11), 2026-09-30; the decisions are in AD-027, the guide is `docs/guides/scraper.md`; what is open is in section 13 (the spike, section 11; the engine project), 2026-09-30. Q1 to Q7 decided.
No backward compatibility: not with the old corpora, and the WPF tool need not keep working.
Supersedes [uat/tools/scraper-in-extension.md](../uat/tools/scraper-in-extension.md) (the
proposal of 2026-09-27) and the roadmap in [Scraper/](../Scraper/roadmap.md) (dormant since
2026-05).

## 1. What we are building

Browse an app, record its pages, and get Brinell ControlObjects and PageObjects for them, from
inside Visual Studio or VS Code.

| # | Requirement | How this plan meets it |
| --- | --- | --- |
| 1 | Move it to the extension | The Scraper becomes a headless engine behind the language server both extensions already start; its UI is a view in those extensions (sections 3, 6) |
| 2 | Remove the WPF version at the end | Step 11. Until then it stays, frozen; it need not keep working |
| 3 | Reuse what the Scraper docs and the proposal hold | Section 2 |
| 4 | Visual Studio and VS Code | Yes, both. One engine, one server, one UI page shown in a VS Code webview and a Visual Studio tool window, as the scenario preview is today (section 6) |
| 5 | The target is a C# UI test project: one per app, or several per app for a large one | A project is the unit: it holds its config, its recordings and the generated code. Several projects share controls through project references (section 4) |
| 6 | Recordings are stored in a folder of the C# project | `Recordings/` in the project, as text files. No SQLite, nothing in `%LOCALAPPDATA%` (section 4) |
| 7 | Html and Blazor first; Maui, WPF and others later | A capture source per platform behind one interface, and a snapshot model with no HTML in its shape (section 5) |
| 8 | Later: record clicks and write UI or UAT files from them | The recording is an ordered list of events from the start; the first version writes only page snapshots into it (section 7) |

## 2. What is reused

### From the proposal (`scraper-in-extension.md`)

| Kept | Changed |
| --- | --- |
| "Keep the brain, drop the body": corpus, capture script, pattern detection, matcher and compile gate become headless | It proposed a server of its own (`Brinell.Scraper.Mcp`). The engine is hosted by `Brinell.Bdd.LanguageServer`, which both extensions start and which already serves MCP (`--mcp`) |
| A headed Chromium through Playwright in place of WebView2 | The corpus was SQLite, per user. It is files in the project (requirement 6) |
| The model is reached through MCP tools; the server calls no model and does the deterministic work | The target was a standalone Pages project. It is the UI test project itself (requirement 5) |
| The repo's own skills (`html-control`, `html-ui-test`) are the conventions; the Scraper's own `SKILL.md` files go | The Copilot SDK and a headless CLI are dropped (Q3) |
| Generation is reviewed in the IDE's diff, not in a preview of its own | Phases reordered: recording into the project comes before generation |
| Scenario drafts from recorded actions, checked against the phrase catalog, never an invented phrase (AD-017) | It is after scraping (requirement 8) |
| The risks: model quality (the compile gate is mandatory), logged-in sites (a browser profile per project), customer data in snapshots (redaction), MCP trust | Redaction matters more now: recordings are in the repo |

### From the Scraper docs (`.my/Scraper/`, 192 files)

| Reuse | From | For |
| --- | --- | --- |
| The pipeline: record many pages, then analyze across all of them, then controls before pages, with an approval between | `DISCUSSION-recording-vs-generation.md`, `phase-13-analyzer-pipeline.md` | Section 3 and step 6 |
| SPA page transitions: URL change or a large DOM change, then wait for a stable state; a manual capture beside it | `DISCUSSION`, `phase-04-steps/06` | The Html capture source |
| Locator preference: text and label first, then `data-testid`, `aria-label`, `id`, CSS; no dynamic ids | `DISCUSSION` section 4, `phase-05-steps/08` | The generation prompt |
| What a snapshot holds per element | `phase-04-steps/01` | The snapshot model |
| Frames: the three open RCAs (025, 026, 027) are all about iframes in recording and manual capture | `rca/` | Test cases for the capture source, written before it |
| The demo-site walkthrough | `uat/uat-phase-14-demosite-scraping.md` | The acceptance run (step 10) |
| Ideas: tabs, tree filter that expands, hover from tree to browser, tag search | `newideas.md` | The view (steps 7, 8), where cheap |

Not reused: the WPF shell and UI design (phases 1, 12), the log viewer (3), the code preview (6),
the standalone Pages project (7), the orchestrator and wizard (8), the Copilot SDK research, the
24 fixed RCAs, and the step and test files of those phases. They describe code that goes.

### From the code (`tools/Brinell.Scraper`, about 12,900 lines)

By folder name and dependency; step 1 checks each file.

| Goes to the engine | Rewritten | Dropped |
| --- | --- | --- |
| `ControlGroupDetector`, `CssSignatureParser`, `ControlObjectMatcher`, `ControlObjectAnalyzer`, `DomDiffService`, `CodeValidator`, `CodeOutputService`, `CodeBlockParser`, `SnapshotExportService`, the `Dom*` models | `DomCaptureService`, `ElementHighlightService`, `PageTransitionDetector`: their injected scripts stay, WebView2 becomes Playwright. `CorpusService`, `CorpusDatabase`, `ControlRegistry`: files in the project. `CorpusTools`: MCP tools. `PromptBuilder`: prompt files | `Views/`, `ViewModels/` (about 5,900 lines), `Converters/`, `Logging/`, `CopilotService`, `AnalysisService`, the generation services, `PipelineOrchestrator`, `LlmRetryHelper`, `SkillService`, the `Llm*` exceptions |

The test site (catalog, checkout, support, frames) moved to `testsnew/Brinell.Scraper.Tests/TestSite`, with a small single-page app added (`pages/spa.html`).
The tests in `tools/Brinell.Scraper.Tests` move with the code they test.

## 3. Shape

```text
Visual Studio / VS Code
 +-- Scraper view (one web page) ------ requests --+
 +-- Copilot, Claude, any agent -------- MCP ------+-- Brinell.Bdd.LanguageServer
                                                         +-- Brinell.Scraper (engine, no UI)
                                                               +-- capture source: Html (Playwright)
                                                               |     headed Chromium, one per recording
                                                               +-- recordings: files in the project
                                                               +-- detect, match, diff, write, check
```

| Part | What | Where |
| --- | --- | --- |
| Contracts | `IScrapeSource` and the snapshot node model: no platform type in them | `srcnew/Brinell.Core` (Q2) |
| Engine | The recording store, detection, matching, diff, code output, the compile gate. No UI, no model call | `srcnew/Brinell.Scraper`, `net10.0` |
| Html capture source | Starts the browser, injects the capture and highlight scripts, reports page transitions | In the engine until a second source exists (Q2) |
| Server surface | MCP tools for an agent, and requests for the view, over the same engine calls | `tools/Brinell.Bdd.LanguageServer` |
| View | Record, the recordings tree, the DOM tree, generate commands | One page, hosted by both extensions |
| Prompts | `.github/prompts/brinell-scrape-*.prompt.md`, which name the skills and make the compile gate mandatory | The repo that uses the Scraper |

The WPF project is named `Brinell.Scraper` today. Step 1 renames it to `Brinell.Scraper.Wpf`
so the engine can take the name. Its services move out in the same step; if it no longer
builds after that it leaves the solution until step 11 deletes it.

## 4. The project is the unit

A UI test project that is scraped holds:

```text
MyApp.UITests/
  scraper.config.md          start URL, URL aliases, namespaces, the Pages and Controls folders, redaction rules
  Recordings/
    2026-09-30-invoices/     one folder per recording session
      recording.json         the ordered events: pages captured now, actions later (section 7)
      001-invoice-list.nodes.jsonl
      001-invoice-list.png
      002-invoice-edit.nodes.jsonl
  Pages/                     generated PageObjects
  Controls/                  generated ControlObjects
```

| Decision | What holds |
| --- | --- |
| **Text files, not a database** | A snapshot is one node per line (JSON Lines: its number, its parent's number, its fields, no nulls), in document order, so a re-recorded page gives a readable diff and no parser meets a depth limit (section 11). The index the tools need (frequencies, signatures) is built in memory when a project is opened |
| **The code is the control registry** | "Which controls exist" is read from the project and the projects it references (Roslyn), not from a table. A control that was edited or deleted by hand is seen as it is |
| **Generation state is in the code too** | A generated file names the snapshot it came from and that snapshot's hash in a header line. "Changed since generation" is a recording whose hash differs |
| **One project per app, or several** | A module's project references the shared project; the matcher offers the shared project's controls first. A new shared control is proposed for the project that owns the pattern's first page, and the user can move it |
| **The config is Markdown** | As `uat.config.md` is, in the same project. A project is scrapeable when it has a `scraper.config.md` |
| **Redaction before writing** | Input values are never stored; visible text is stored, with rules in the config to mask it (Q5). Nothing unredacted reaches disk or a tool result |
| **The login is not in the project** | The browser profile per project is under the user's profile folder, not in the repo |

## 5. Designed for more than Html

| Piece | Neutral | Html-specific |
| --- | --- | --- |
| Snapshot | A tree of nodes: role, name, text, bounds, state, a platform and an attribute bag, child nodes, a frame or window id | The bag holds tag, id, class, `data-testid`, `aria-*` |
| Capture source | `IScrapeSource`: start, capture, highlight a node, pick a node, transitions, stop | Playwright and the injected scripts |
| Signature | A pattern's identity is computed by the source's own signature function | `CssSignatureParser` |
| Generation | The prompt names the platform's skills and control base types | `html-control`, `html-ui-test`; for Blazor also the `Brinell.Blazor` controls |

Blazor is the Html source with one addition: the matcher knows the `Brinell.Blazor` controls,
and the config says the app is Blazor. Maui and WPF later are another `IScrapeSource` over the
UI Automation tree (FlaUI, Appium), which Presenter's Discovery already walks; nothing in the
store, the tools or the view changes for them. That is the test of this section: a second
source adds a class and a prompt, and edits no model type.

## 6. Both IDEs

The extensions exist: `tools/Brinell.Bdd.VsCode` (TypeScript) and `tools/Brinell.Bdd.VisualStudio`
(a VSIX with WebView2 tool windows). Both start the language server, and the scenario preview is
already one page shown in both. The Scraper view is built the same way:

| | VS Code | Visual Studio |
| --- | --- | --- |
| View | A webview panel and a tree view | A tool window with WebView2 |
| Commands | **Record**, **Capture page**, **Generate page object**, **Regenerate control**, **Compare** | The same, on the tool window and the project's context menu |
| Generation | Opens the agent chat with the prompt file and the recording filled in | The same prompt file |
| Review | Source Control diff | Git Changes |

The recording browser is a window of its own beside the IDE, in both. It is not embedded: one
route, the same on every OS, and the IDE keeps its layout.

## 7. Later: actions, and tests from them

Not in this plan's steps; designed for now so it needs no new format.

- `recording.json` is a list of events. Today each event is a page capture. Later the capture
  script also reports clicks, typed text, selections and navigation, each with the node it hit.
- A node in an action is resolved to a PageObject member through the generated code's header
  lines, so an action reads as `InvoiceEditPage.Save.Click()`.
- From that: an xUnit UI test (page-object calls), or a `.feature.md` scenario that uses only
  phrases from the catalog and is checked with `validate_scenario`; a step with no phrase is a
  marked gap.
- Typed values go through the same redaction as snapshots.

## 8. Steps

One commit per step. Unit tests with each step; UI checks only where a step names them.

| # | Step | Delivers | Verify |
| --- | --- | --- | --- |
| 0 | Spike | The language server starts a headed Chromium, injects today's capture script, and writes one snapshot of the test site into a project folder. Frames included | The snapshot of `frames/` holds the frame content (RCA 025 to 027) |
| 1 | Engine project | The WPF project renamed; `srcnew/Brinell.Scraper` with the services and models of section 2, and their tests moved. The WPF app leaves the solution if it no longer builds | **Done.** `tools/Brinell.Scraper.Wpf` (+ `.Wpf.Tests`) references `srcnew/Brinell.Scraper` and still builds; 9 services and 9 models moved, 36 tests to `testsnew/Brinell.Scraper.Tests`, 170 stay: 206 pass, as before |
| 2 | Snapshot model | The neutral node tree and `IScrapeSource` in `Brinell.Core`; the Html attribute bag; stable JSON | **Done.** `Brinell.Core.Scraping`: `ScrapeSnapshot`, `ScrapeNode` (number, parent, kind, type, role, name, text, bounds, frame, attributes), `IScrapeSource`; `SnapshotFile` writes `*.nodes.jsonl`; `HtmlAttributes` says what Html keeps (never `value`). 14 tests. The moved services still work on the old `DomElement`: they move to `ScrapeNode` in step 6, where they are used |
| 3 | Store | `scraper.config.md`, `Recordings/`, the in-memory index, redaction | **Done.** `Brinell.Scraper.Projects`: `ScraperConfig` (`## App`, `## Aliases`, `## Test ids`, `## Masks`), `ScrapeProject` (open, find, find all, own pages, sessions, profile folder under local app data), `RecordingSession` (`recording.json`, `NNN-<slug>.nodes.jsonl`, `.png`), `RecordingIndex` (by `PageKey`, newest first, search), `Redactor`. 19 tests |
| 4 | Html source | Record with transitions, manual capture, frames, highlight and pick, the profile per project | **Done.** `PlaywrightScrapeSource`: a persistent Chromium context per profile folder, a new tab followed in the recorded page, a capture that waits for 500 ms without DOM change (at most 15 s), every frame captured on its own and hung under its iframe, highlight and pick in any frame, screenshot. The scripts are embedded `.js` files. `IScrapeSource` got `ScreenshotAsync`. 13 browser tests (`Category=Browser`, 25 s, headless) on the test site: frames (cross-origin, nested), late rendering, hash and pushState transitions, shadow roots, a dialog at the end of body, typed text not recorded, highlight, pick and Escape, a capture recorded into a project. Headed runs are by hand (step 7) |
| 5 | Server surface | MCP tools and view requests: `start_recording`, `capture_page`, `stop_recording`, `list_recorded_pages`, `get_page_snapshot`, `find_similar_elements`, `search_recordings` | **Done.** `ScraperService` in the engine (one recording at a time, auto capture on a transition to the app's own pages, an unchanged page not captured twice, read, search, similar by `NodeSignature`); `SnapshotOutline` gives a page as short text with node numbers. The language server's `brinell/scraper/*` methods (projects, state, start, capture, stop, pages, outline, highlight, pick) and the `brinell/scraper/changed` notification; the eight MCP tools above plus `list_scraped_projects`. An outline is plain text, the rest JSON. 10 service tests, 4 server tests; run once over MCP from a pipe with a headed browser on the test site: 2 pages, 2 screenshots. The language server and the MCP server are two processes, each with its own recording; the files are what they share. `list_controls`, `detect_patterns` and `match_controls` moved to step 6 |
| 6 | Generate | `list_controls`, `detect_patterns`, `match_controls` (the moved services on `ScrapeNode`), `write_code`, `check_code` (Roslyn, the analyzers, `dotnet build`), the header line, the prompt files | **Done.** New code on `ScrapeNode`, not the moved services (those stay only for the WPF tool, and go with it in step 11): `ProjectCode` (page objects and controls read from source with Roslyn syntax, and from referenced projects that are scraped too), `ScrapedHeader` (`// <scraped page=… from=… hash=… />` or `control=…`: the code is the registry), `PatternDetector` (repeated parts across pages or within one; forms, tables, lists, navigation, dialogs, frames; a part of a pattern not given again), `CodeGate` (write inside the project only, syntax first; `dotnet build` with its errors by file and line), `SnapshotFile.ContentHash` (without boxes). MCP: `list_controls`, `detect_patterns`, `match_controls`, `scraped_header`, `write_code`, `check_code`; the view: `brinell/scraper/patterns`, `controls`. The `scrape-pages` skill and `/brinell-scrape-pages` prompt. 10 tests. **Acceptance:** acting as the agent over MCP, the test site's 4 pages recorded into a scratch `Shop.UITests` referencing `Brinell.Html`; 13 patterns found (the site header on all 4 pages, cards, rows, a custom element, table, list, forms, frames); `SiteHeader<TParent>` and `CatalogPage` (with its frame) written and `check_code` passed first time; `match_controls` then finds the header on every page and the page object current. Not run as a UI test against the site |
| 7 | VS Code view | Record, the recordings tree, the DOM tree, the commands | **Done.** The view is the server's page (`ScraperPage`, `brinell/scraper/host`): Record, Capture, Pick, Stop; tabs Pages (each page and its captures, a capture as an outline, a node's details: highlight in the live browser, how often its kind occurs, Make a control), Patterns, Code (opens the file); Compare (two captures in the IDE's diff), Generate (the agent chat with `/brinell-scrape-pages`). New requests `similar`, `header`; a `ScraperResult` always sends `value` and `problem`. An IDE only relays: VS Code's `src/scraper.ts` and the command **Brinell BDD: Open Scraper** (also on a `scraper.config.md`'s title bar). Tried in a browser through a stand-in host that relays to the real server: the pages, an outline, a node, a recording started, captured, stopped, the patterns and code tabs, and the open and chat messages all worked. **Not tried inside VS Code** (the relay compiles), and Pick needs a person's click in the recording browser |
| 8 | Visual Studio view | The same page in a tool window | **Done, not run in Visual Studio.** View, Other Windows, **Scraper** (`ScraperWindow`, `ScraperController`): the same page in WebView2; requests relayed through `BddServerClient.RequestAsync`, `brinell/scraper/changed` forwarded; open (a document), diff (`IVsDifferenceService`), and the Generate prompt on the clipboard with a status bar line, since Copilot Chat takes no prompt from a command. Compiles with `dotnet build -p:DeployExtension=false`. **Found:** the language server now carries Playwright's driver (`.playwright`, 102 MB for win-x64), so the VSIX's server folder grows from about 50 to 152 MB; see Q8 |
| 9 | Blazor | The `Brinell.Blazor` controls in the matcher and the prompt | **Done.** `ControlAdvisor`: for a node, or every node a page object would name (acted on, read, a test id, a heading, a frame; not an option), the control that fits (the `Brinell.Blazor.Controls` one in a Blazor project), a name (the app's test id first), and a locator in the skill's order, unique within its frame document, with whether it is stable and why. MCP `suggest_controls`, `brinell/scraper/suggest`, and the view's node details. `BlazorPageObjectBase` is a page base for `ProjectCode`. 8 tests. **Acceptance:** `testsnew/Brinell.Blazor.UITests` got a `scraper.config.md` (test ids `data-testid`, `data-automation-id`); the sample's 13 pages recorded into its `Recordings/` (217 KB); acting as the agent, `UserFormPage` written from `suggest_controls` and `check_code` passed; `UserFormPageTests` (2) drive it against the sample and pass, and so does the rest of the project (35) |
| 10 | Refresh and acceptance | `diff_page`, "changed since generation" in the tree; the demo-site walkthrough run in both IDEs; one public site per framework recorded and generated (section 10) | **Done, except the IDE walkthrough** (no IDE here; section 12). `SnapshotDiff` (nodes matched by path: type, test id or id, which like sibling), `diff_page` (from the capture the page object was made from, else the one before, to the newest), the pages list says which page object a page has and whether it changed since, and the view's **Changes** and **Update page object**. **Public sites** (section 12): the-internet, react-admin demo, vue-element-admin and MudBlazor docs recorded, one page object each written from `suggest_controls`, each project built by `check_code`, and all 18 locators found exactly one element on the live sites |
| 11 | Remove and close | `tools/Brinell.Scraper.Wpf` deleted with its solution, task and launch entries; an AD and a guide in `docs/`; `.my/Scraper/`, the proposal and this folder deleted (docs-upkeep) | **Done, but this folder is kept** until section 13 is worked through. The WPF tool, its tests, the moved services and their 36 tests, `.vscode/` tasks, and the packages only it used are gone; AD-027 and `docs/guides/scraper.md` written; `.my/Scraper/` and the proposal deleted. The solution builds with 0 errors; Scraper 78, language server 83, Core 41, Blazor UI 35 tests pass |

Step 5 already pays off without a view: an agent can record and generate through MCP.

## 9. Decisions

| # | Question | Decided (2026-09-30) |
| --- | --- | --- |
| Q1 | Inside the Brinell BDD extensions, or an extension of its own? | Inside |
| Q2 | Where is the Html capture source? | In the engine until a second source exists. The interfaces (`IScrapeSource`, the snapshot model) are in `Brinell.Core` |
| Q3 | The Copilot SDK and the headless CLI | Dropped; MCP only |
| Q4 | Are recordings committed? | Yes; screenshots optional in the config |
| Q5 | Visible text in snapshots | Stored as it is; mask rules in the config |
| Q6 | Old SQLite corpora | Not migrated |
| Q7 | First real target | `testsnew/Brinell.Blazor.UITests` |
| Q8 | The VSIX carries Playwright's driver, 102 MB (step 8) | Ship the driver in the extensions (about 100 MB) for now; a separate updater later ([next-plan.md](next-plan.md), section 1) |

## 10. Test sites

Two kinds. **Ours** run locally and are what automated tests use: a test never depends on a
public site. **Public** sites are for the acceptance runs by hand (step 10): real frameworks,
real component libraries, markup we did not write.

Each public site answered on 2026-09-30. The framework column is from what is known about the
site, not read from its markup: step 0 confirms it.

| Kind | Site | Framework | Good for |
| --- | --- | --- | --- |
| Html, ours | `testsnew/Brinell.Scraper.Tests/TestSite` | plain, and `pages/spa.html` | Frames, forms; every automated capture test |
| Html | [the-internet](https://the-internet.herokuapp.com/) | plain, server-rendered | One feature per page: iframes, nested frames, dynamic loading, tables, new windows, shadow DOM, a login |
| Html | [UI Testing Playground](http://uitestingplayground.com/) | plain with Bootstrap | Locator traps: dynamic ids, changing class order, hidden layers, late-loading content |
| React | [Sauce Demo](https://www.saucedemo.com/) | React | A small shop with a login (public test users), `data-test` attributes, a list of repeated cards, a checkout flow over several pages |
| React | [react-admin demo](https://marmelab.com/react-admin-demo/) | React, MUI | A real admin app: data grids, filters, edit forms, dialogs, generated class names. The pattern-detection test |
| React | [DemoQA](https://demoqa.com/) | React | A page per widget: date picker, autocomplete, modal, tabs, frames |
| Vue 2 | [vue-element-admin](https://panjiachen.github.io/vue-element-admin/) | Vue 2, Element UI | A real admin app with a login, tables, forms, a tabbed SPA whose URL is a hash route. The closest to an Exact-style back office |
| Vue 2 | [Vuetify 2 docs](https://v2.vuetifyjs.com/en/components/data-tables/) | Vue 2, Vuetify | Component pages: data tables, selects, dialogs with teleported overlays |
| Vue 2 | [Element UI docs](https://element.eleme.io/#/en-US/component/form) | Vue 2, Element UI | The same components as the admin app, one per page |
| Blazor, ours | `samples/Brinell.Samples.Blazor.App`, the Todo web head | Blazor | Automated tests; has page objects to compare the generated ones with |
| Blazor | [MudBlazor docs](https://mudblazor.com/components/table) | Blazor WebAssembly, MudBlazor | The most used Blazor component library: tables, dialogs, pickers |
| Blazor | [Radzen Blazor demos](https://blazor.radzen.com/) | Blazor, Radzen | Data grid, scheduler, forms; another library's markup |
| Blazor | [Fluent UI Blazor](https://www.fluentui-blazor.net/) | Blazor, web components | Shadow DOM: the capture script has to look into it |

One per framework for step 10: the-internet, react-admin demo, vue-element-admin, MudBlazor docs.

What these sites ask of the design, before step 0:

- **Shadow DOM** (Fluent UI Blazor, the-internet): the snapshot needs a node kind for a shadow root.
- **Overlays outside the page's tree** (MUI, Vuetify, Element, MudBlazor put dialogs and menus at the end of `body`): a capture while one is open, and the matcher has to tie it to the control that opened it.
- **Hash routes** (vue-element-admin): a transition with no path change.
- **Generated class names** (MUI): a signature cannot lean on classes alone.
- Public sites are recorded into a scratch project, not into the repo: their content is not ours to commit.

## 11. Step 0: what the spike found

The spike is [spike0/](spike0/Program.cs): a console with Playwright 1.62 that serves the test
site, starts Chromium, runs the Scraper's capture script (changed as below) in every frame, hangs
each frame's tree under its `<iframe>` node, and writes a snapshot per page. It ran headless and
headed on 2026-09-30. It ran as a console, not in the language server: hosting is step 5, and the
server is a plain `net10.0` process like the console.

| Checked | Result |
| --- | --- |
| Test site, 4 pages | 54 to 88 nodes each, 20 to 70 ms a capture |
| Same-origin, nested and **cross-origin** frames (RCA 025 to 027) | All filled. Each frame is captured on its own with `frame.EvaluateAsync`; the parent's script puts every iframe element in a `WeakMap` with an id, and `FrameElementAsync()` evaluated in the parent gives that id back. No DOM change, no guessing by order or name as the WebView2 code did. The cross-origin case was an iframe to `127.0.0.1` in a page on `localhost` |
| Open shadow roots | Captured as a node of kind `shadow-root`: the test site (1), the-internet `/shadowdom` (2), Fluent UI Blazor (25) |
| Page transitions | `FrameNavigated` on the main frame saw all 4 navigations |
| The framework per public site | Read in the page: jQuery (the-internet, UI Testing Playground); React (Sauce Demo, react-admin, DemoQA); **Vue 2** 2.6.10 (vue-element-admin), 2.7.8 (Vuetify 2), 2.5.21 (Element UI); Blazor WebAssembly (MudBlazor); Blazor Web (Fluent UI Blazor). Hash routes on react-admin, vue-element-admin and Element UI |

What it changes in the design:

| Finding | Change |
| --- | --- |
| Nested, indented JSON is huge: MudBlazor's table page, 17,995 elements, is 26 MB; Vuetify's 6.9 MB. Most of it is indentation | One node per line with parent numbers and no nulls: 1.9 MB and 367 KB. Still large for docs sites; the capture also drops `head`, `script`, `style`, `template` and the inside of `svg`, and a capture of a selected region stays possible |
| The tree is deeper than `System.Text.Json`'s default of 64 (Radzen) | The script emits the flat list itself; nothing parses a nested tree |
| "Network idle" is too early for Blazor: Fluent UI Blazor had 17 elements at idle, 586 once rendered | A capture waits for a stable DOM: the element count unchanged over 1 s (took 1.3 to 4.1 s on these sites), at most a named limit. This is the transition detector's job anyway |
| Radzen answers headless Chromium with **403 Forbidden**, and loads headed | Recording is headed (as planned); a headless capture over a URL list (for tests) is only for our own sites |
| MudBlazor's docs page is 18k elements | Docs sites are for trying components one by one; the admin demos are the sites for pattern detection across pages |
| `data-test` (Sauce Demo) next to `data-testid` | The attribute bag keeps every `data-*` attribute; the config names which ones are test ids |

## 12. Step 10: the public sites

Each site was recorded into a scratch project of its own (outside the repo, with `Brinell.Html` and
`Brinell.Blazor` referenced), over MCP as an agent would, on 2026-09-30.

| Site | Recorded | Page object | Built | Locators live |
| --- | --- | --- | --- | --- |
| [the-internet](https://the-internet.herokuapp.com/) | `/login` (29 nodes), `/tables` (124) | `LoginPage`: heading, username, password, login | yes | 4 of 4 unique |
| [react-admin demo](https://marmelab.com/react-admin-demo/) | `#/login` (31) | `LoginPage`: username, password, show password, sign in | yes | 4 of 4 |
| [vue-element-admin](https://panjiachen.github.io/vue-element-admin/) | `#/login` (56) | `LoginPage`: title, username, password, login, third-party sign-in | yes | 5 of 5 |
| [MudBlazor docs](https://mudblazor.com/components/button) | `/components/button` (2,400), `/components/textfield` (4,231) | `ButtonDocsPage`: search button and field, three links | yes | 5 of 5 |

What they found, fixed in this step:

| Found | Fix |
| --- | --- |
| react-admin was captured as `Loading...` (5 nodes): the page is quiet while the script bundle downloads | A capture waits for the network to be idle first (bounded by the settle limit), then for the quiet DOM |
| Vue's scoped-style attributes (`data-v-09f01a64`) in every node | Not kept: they change with every build |
| A button's text in a child span (Element UI, MUI) gave no name and a position | The text of a button or link is looked for two levels down |
| MudBlazor's ids are made per render (`mudinputfmqznq5c`, `popovercontent-<guid>`) | UI libraries' numbered id prefixes are not stable ids |
| A position was a path from `body`, 10 steps long | It starts at the nearest ancestor with a test id or stable id of its own |
| A third-party Blazor app sets no Brinell busy signal | The skill says: `BlazorPageObjectBase` only for an app that sets it |

Still rough: on MudBlazor's docs 254 of 364 suggestions have only a position (the drawer's items
and icon buttons have no test id, and the same text twice); the pattern list of a docs site is
dominated by its navigation. A real app with test ids does far better (the Blazor sample's `/userform`: 0 of 45 by position).

## 13. Open

| Item | From | Status |
| --- | --- | --- |
| The views tried inside VS Code and Visual Studio (the page was tried through a stand-in host; both relays build); Pick needs a person's click | steps 7, 8 | to try |
| Q8: the language server carries Playwright's driver (102 MB), and Playwright releases about monthly | step 8 | decided: ship it in the extensions for now; an updater later ([next-plan.md](next-plan.md)) |
| Record actions (clicks, typing) into `recording.json` and draft UI tests or UAT scenarios from them, phrases from the catalog only | section 7 | later |
| A UI Automation source (MAUI, WPF) behind `IScrapeSource`; then the Html source moves to a project of its own | section 5 (AD-026, R3) | later |
| On a docs site without test ids most suggestions are positions (MudBlazor: 254 of 364) | section 12 | to improve |
