# The Scraper, restarted in the IDE extensions: plan

Status: **plan, decided** (Q1 to Q7, 2026-09-30). Nothing is built.
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

`tools/Brinell.Scraper.TestSite` (catalog, checkout, support, frames) stays as the test site.
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
      001-invoice-list.snapshot.json
      001-invoice-list.png
      002-invoice-edit.snapshot.json
  Pages/                     generated PageObjects
  Controls/                  generated ControlObjects
```

| Decision | What holds |
| --- | --- |
| **Text files, not a database** | A snapshot is formatted JSON with a stable element order, so a re-recorded page gives a readable diff. The index the tools need (frequencies, signatures) is built in memory when a project is opened |
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
| 1 | Engine project | The WPF project renamed; `srcnew/Brinell.Scraper` with the services and models of section 2, and their tests moved. The WPF app leaves the solution if it no longer builds | The moved tests pass identically |
| 2 | Snapshot model | The neutral node tree and `IScrapeSource` in `Brinell.Core`; the Html attribute bag; stable JSON | Round-trip and ordering tests |
| 3 | Store | `scraper.config.md`, `Recordings/`, the in-memory index, redaction | Tests on a temp project |
| 4 | Html source | Record with transitions, manual capture, frames, highlight and pick, the profile per project | Tests against the test site, headless |
| 5 | Server surface | MCP tools and view requests: `start_recording`, `capture_page`, `stop_recording`, `list_recorded_pages`, `get_page_snapshot`, `find_similar_elements`, `search_recordings`, `list_controls`, `detect_patterns`, `match_controls` | Server tests; one run from an agent |
| 6 | Generate | `write_code`, `check_code` (Roslyn, the analyzers, `dotnet build`), the header line, the prompt files | The test site's pages generated by an agent, building |
| 7 | VS Code view | Record, the recordings tree, the DOM tree, the commands | By hand, on the test site |
| 8 | Visual Studio view | The same page in a tool window | By hand |
| 9 | Blazor | The `Brinell.Blazor` controls in the matcher and the prompt | The Blazor sample's pages generated, building |
| 10 | Refresh and acceptance | `diff_page`, "changed since generation" in the tree; the demo-site walkthrough run in both IDEs; one public site per framework recorded and generated (section 10) | The results recorded here |
| 11 | Remove and close | `tools/Brinell.Scraper.Wpf` deleted with its solution, task and launch entries; an AD and a guide in `docs/`; `.my/Scraper/`, the proposal and this folder deleted (docs-upkeep) | The solution builds; no link into the deleted folders |

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

## 10. Test sites

Two kinds. **Ours** run locally and are what automated tests use: a test never depends on a
public site. **Public** sites are for the acceptance runs by hand (step 10): real frameworks,
real component libraries, markup we did not write.

Each public site answered on 2026-09-30. The framework column is from what is known about the
site, not read from its markup: step 0 confirms it.

| Kind | Site | Framework | Good for |
| --- | --- | --- | --- |
| Html, ours | `tools/Brinell.Scraper.TestSite` | plain | Frames, forms; every automated capture test |
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
