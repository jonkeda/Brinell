# Brinell.Scraper — Roadmap

## Vision

A WPF tool that lets a user browse any website in an embedded browser, visually inspect pages, and **automatically generate Brinell `PageObject` and `ControlObject` classes** using an LLM (GitHub Copilot SDK). The tool captures the live DOM, feeds it to the LLM, and outputs ready-to-compile C# code following existing Brinell conventions.

---

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Browser-in-app** | Embedded Chromium (WebView2) inside the WPF window — user navigates site normally |
| **DOM Inspector** | Captures page structure, element attributes, roles, visible text |
| **LLM Generator** | Sends DOM context + Brinell conventions to GitHub Copilot → receives generated code |
| **Code Preview** | Shows generated `PageObject` / `ControlObject` code before saving |
| **Project Output** | Writes `.cs` files into a target Brinell project (e.g. `Hours.Automation`) |

---

## Technology Choices

| Concern | Decision | Rationale |
|---------|----------|-----------|
| App framework | **WPF** (.NET 10) | Windows-only, mature, hosts WebView2 natively |
| Embedded browser | **WebView2** (Chromium) | Full DOM access via CDP/JS interop, same engine as target sites |
| LLM integration | **GitHub Copilot SDK** | In-house, supports code generation, chat completions |
| Code output | **Brinell.Html** page/control objects | Matches existing `HtmlPageObjectBase<TSelf>` + control hierarchy |
| Serialization | **System.Text.Json** | DOM snapshots, LLM prompts, settings |

---

## Phase 1 — WPF Shell & Embedded Browser

- [ ] **1.1** Create `Brinell.Scraper` WPF project (.NET 10)
- [ ] **1.2** Embed WebView2 control with address bar, back/forward, refresh
- [ ] **1.3** Navigation support — user browses to target site (e.g. Exact Online, Synergy)
- [ ] **1.4** Cookie / session persistence — stay logged in across sessions
- [ ] **1.5** Basic chrome: status bar, loading indicator, dev-tools toggle
- [ ] **1.6** Start screen / site selector — site corpus picker, new site dialog with URL aliases, per-site settings

> **Note:** MVVM foundation (ViewModelBase, commands, DI) is included in Phase 1 as step 1.2. The former Phase 2 has been absorbed.

## Phase 3 — Logging

- [ ] **3.1** Integrate `Microsoft.Extensions.Logging` with `ILogger<T>` throughout all services
- [ ] **3.2** File sink — structured log output to `logs/` folder (rolling daily files)
- [ ] **3.3** In-app log viewer panel — filterable by level (Debug/Info/Warning/Error)
- [ ] **3.4** LLM request/response logging — capture prompts, responses, token usage, latency
- [ ] **3.5** DOM capture logging — snapshot metadata, element counts, timing
- [ ] **3.6** Corpus & analysis logging — corpus CRUD operations, analysis start/complete, control approval, generation batch stats

## Phase 4 — DOM Inspection, Recording & Corpus Management

Split into 4A (DOM Inspection & Recording) and 4B (Corpus Management).

**4A — DOM Inspection & Recording:**
- [ ] **4.1** Inject JS into WebView2 to capture DOM snapshot (tag, id, class, name, data-testid, role, aria-label, text, bounding box)
- [ ] **4.2** Element highlight overlay — hover highlight with locator suggestion tooltip
- [ ] **4.3** DOM tree view panel from captured snapshot
- [ ] **4.4** Multi-select mode — TreeView checkboxes + browser Ctrl+click
- [ ] **4.5** Auto-detect forms, tables, lists, nav regions as ContainerBase candidates
- [ ] **4.6** SPA-aware page transition detection — MutationObserver, URL change detection, stable state wait
- [ ] **4.7** Recording mode — capture DOM snapshots to corpus on each page transition (no LLM generation)

**4B — Corpus Management:**
- [ ] **4.8** SQLite corpus store — per-site database with Sites, SiteAliases, Snapshots, Elements tables
- [ ] **4.9** Corpus browser view — DataGrid of all recorded pages with sort, filter, status icons
- [ ] **4.10** Snapshot diff — compare two versions of a page, show added/removed/changed elements
- [ ] **4.11** Export/import DOM snapshots as JSON

## Phase 5 — LLM-Powered Code Generation (GitHub Copilot SDK)

Split into 5A (Analysis Pass) and 5B (Generation Pass). Uses Copilot SDK with custom agents, tools, and skills.

**5A — Analysis Pass (cheaper model):**
- [ ] **5.1** Integrate GitHub Copilot SDK — custom agents (analyzer + generator), custom tools for corpus queries, skills for Brinell conventions
- [ ] **5.2** Build Brinell conventions as SKILL.md files — control hierarchy, locator strategies (text-first), expression-bodied style, examples
- [ ] **5.3** Custom tools for corpus queries — `search_corpus()`, `get_page_snapshot()`, `find_similar_elements()`, `get_generated_controls()`, `list_recorded_pages()`
- [ ] **5.3b** Analysis pass — analyzer agent queries corpus, identifies patterns, proposes custom controls with confidence %
- [ ] **5.3c** Custom control generation — generate ContainerBase classes for approved controls, store in control registry

**5B — Generation Pass (smarter model):**
- [ ] **5.4** Prompt template with corpus context — include custom controls from registry, site patterns, locator preference (text → data-testid → aria-label → id → CSS)
- [ ] **5.5** Parse LLM response → extract C# code blocks (handles both ControlObjects and PageObjects)
- [ ] **5.6** Validate generated code — Roslyn syntax check, dynamic control type validation from registry

## Phase 6 — Code Preview & Editing

- [ ] **6.1** C# code editor panel (AvalonEdit) with syntax highlighting — editing/formatting left to VS Code / Visual Studio
- [ ] **6.2** Re-generate single control: select different element → regenerate one property
- [ ] **6.3** Roslyn validation — parse generated code, report syntax errors inline
- [ ] **6.4** Roslyn formatting — auto-format generated code with `Microsoft.CodeAnalysis.CSharp`
- [ ] **6.5** Copy to clipboard / open in VS Code

## Phase 7 — Project Integration & Output

Generated page objects are written to **standalone projects** per target system (e.g. `ExactOnline.Pages`, `Synergy.Pages`). Each project references `Brinell.Html` and compiles independently — no risk of breaking downstream code.

- [ ] **7.1** Project scaffolding — create new `.csproj` per target system with `Brinell.Html` reference
- [ ] **7.2** Configure output project path + namespace (not tied to any specific consuming solution)
- [ ] **7.3** Write generated `.cs` files with proper `using` statements
- [ ] **7.3b** Write custom ControlObject files to `Controls/` subfolder (generated before pages)
- [ ] **7.4** Roslyn compile check — `dotnet build` the standalone project to verify generated code compiles
- [ ] **7.5** Detect existing page objects → update mode (merge new controls into existing class)
- [ ] **7.6** Generate companion test scaffold (optional) — xUnit test class with basic smoke test

## Phase 8 — Workflow Orchestration

End-to-end pipeline automation and guided workflows. Recording moved to Phase 4.

- [ ] **8.1** Pipeline orchestrator — manages Record → Analyze → Approve → Generate Controls → Generate Pages → Save → Build
- [ ] **8.2** Guided first-run wizard — step-by-step walkthrough for new site corpus
- [ ] **8.3** Pipeline resume — persist pipeline state in corpus SQLite, resume after app restart
- [ ] **8.4** Auto-analyze after recording — configurable (default: on)
- [ ] **8.5** Incremental re-generation — detect changed pages, regenerate only those
- [ ] **8.6** Site-specific skills auto-update — refresh SKILL.md files after each analysis pass

## Phase 9 — Unit Tests

- [ ] **9.1** Unit tests for `ViewModelBase` — `SetProperty`, `OnPropertyChanged`, equality checks
- [ ] **9.2** Unit tests for `RelayCommand` / `AsyncRelayCommand` — execute, can-execute, cancellation
- [ ] **9.3** Unit tests for DOM capture service — snapshot parsing, element extraction, attribute mapping
- [ ] **9.4** Unit tests for LLM prompt builder — system prompt assembly, DOM-to-prompt conversion
- [ ] **9.5** Unit tests for code output service — file naming, namespace detection, merge logic
- [ ] **9.6** Unit tests for Roslyn validation — syntax error detection, formatting output
- [ ] **9.7** Unit tests for DomDiffService — added/removed/changed detection, element matching priority
- [ ] **9.8** Unit tests for ControlRegistryService — store, retrieve, approve/reject, duplicate detection
- [ ] **9.9** Unit tests for PipelineOrchestrator — state progression, resume, incremental generation

## Phase 10 — Automated (Integration) Tests

- [ ] **10.1** Integration test: WebView2 loads page → DOM capture returns valid snapshot
- [ ] **10.2** Integration test: DOM snapshot → LLM → generated code compiles with Roslyn
- [ ] **10.3** Integration test: generated page object → write to project → `dotnet build` succeeds
- [ ] **10.4** Integration test: re-scrape & diff detects added/removed elements correctly
- [ ] **10.5** Smoke test suite — end-to-end: navigate → capture → generate → compile for a known test page
- [ ] **10.6** Integration test: Copilot SDK custom tools return correct data from test corpus
- [ ] **10.7** Integration test: incremental re-generation only processes changed pages

## Phase 11 — Polish & Extensibility

- [ ] **11.1** Settings: LLM model selection, temperature, token limits
- [ ] **11.2** Prompt customization — user can edit/extend the system prompt
- [ ] **11.3** Control type mapping overrides (e.g. "this `<div>` is actually a button")
- [ ] **11.4** Export/import DOM snapshots for team sharing
- [ ] **11.5** Plugin architecture for non-HTML platforms (future: WinForms/WPF UIA scraping)
- [ ] **11.6** Evaluate MCP server extraction — consider extracting corpus tools into an MCP server for cross-tool sharing

---

## Architecture Overview

```
┌───────────────────────────────────────────────────────────────┐
│                    Brinell.Scraper (WPF)                      │
├──────────┬────────────────────────────────────────────────────┤
│ Sidebar  │  Main Content Area                                 │
│ ┌──────┐ │  ┌──────────────────────────────────────────────┐ │
│ │Corpus│ │  │  BrowserView / InspectorView / RecordingView │ │
│ │Stats │ │  │  AnalysisView / ControlsView / GenerationView│ │
│ │      │ │  │  CorpusView                                   │ │
│ │Pages │ │  └──────────────────────────────────────────────┘ │
│ │ ✅   │ │                                                    │
│ │ ⏳   │ │  ┌──────────────────────────────────────────────┐ │
│ │ 🆕   │ │  │  Log Viewer (collapsible bottom panel)       │ │
│ │      │ │  └──────────────────────────────────────────────┘ │
│ │Ctrls │ │                                                    │
│ │ ✅   │ │                                                    │
│ │ ⏳   │ │                                                    │
│ └──────┘ │                                                    │
├──────────┴────────────────────────────────────────────────────┤
│                      Services Layer                           │
│  ┌──────────┐ ┌──────────────┐ ┌────────────┐ ┌───────────┐ │
│  │ Corpus   │ │ Copilot SDK  │ │ Code       │ │ Pipeline  │ │
│  │ Service  │ │ ┌──────────┐ │ │ Output     │ │ Orchest.  │ │
│  │ (SQLite) │ │ │ Analyzer │ │ │ Service    │ │           │ │
│  │          │ │ │ Agent    │ │ │            │ │           │ │
│  │ Snapshot │ │ ├──────────┤ │ │ Controls/  │ │ Record    │ │
│  │ Elements │ │ │Generator │ │ │ Pages/     │ │ Analyze   │ │
│  │ Controls │ │ │ Agent    │ │ │            │ │ Generate  │ │
│  │ Patterns │ │ └──────────┘ │ │ .csproj    │ │ Build     │ │
│  └──────────┘ └──────────────┘ └────────────┘ └───────────┘ │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Skills: brinell-conventions / {site}-patterns / -ctrls  │ │
│  │ Tools: search_corpus / get_page_snapshot / find_similar │ │
│  └─────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────┘
```

---

## LLM Prompt Strategy

The system prompt sent to GitHub Copilot will include:

1. **Brinell conventions** — full control hierarchy, base classes, locator strategies
2. **Example page object** — a complete reference implementation
3. **DOM snapshot** — the captured elements with attributes and structure
4. **Instructions** — generate a `sealed class` extending `HtmlPageObjectBase<TSelf>`, use expression-bodied properties, pick the most specific control type, prefer `data-testid` > `id` > `css` for locators

Example system prompt excerpt:
```
You are a code generator for the Brinell UI testing framework.
Generate a PageObject class following these rules:
- Extend HtmlPageObjectBase<{ClassName}>
- Constructor takes IHtmlTestContext
- Each interactive element becomes an expression-bodied property
- Use the most specific control type: ButtonControl for buttons,
  TextInputControl for text inputs, SelectControl for <select>, etc.
- Locator preference: data-testid > id > name > css selector
- Class is sealed, namespace matches target project
```

---

## Generated Code Example

Given a login page DOM, the tool would produce:

```csharp
namespace Hours.Automation.Pages.ExactOnline;

public sealed class ExactLoginPage : HtmlPageObjectBase<ExactLoginPage>
{
    public ExactLoginPage(IHtmlTestContext context) : base(context) { }

    public TextInputControl<ExactLoginPage> EmailInput =>
        new(this, Locator.ById("email"));

    public TextInputControl<ExactLoginPage> PasswordInput =>
        new(this, Locator.ById("password"));

    public ButtonControl<ExactLoginPage> SignInButton =>
        new(this, Locator.ByCss("button[type='submit']"));

    public LabelControl<ExactLoginPage> ErrorMessage =>
        new(this, Locator.ByCss(".error-message"));
}
```

---

## Key Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| LLM generates incorrect control types | Validation step + user preview/edit before save |
| DOM too large for LLM context window | Prune to selected region; summarize non-interactive elements |
| WebView2 JS injection blocked by CSP | Use CDP protocol for DOM access instead of JS eval |
| Sites require auth (SSO, MFA) | User logs in manually in the embedded browser; tool captures post-auth DOM |
| Generated locators are brittle | Prefer stable attributes (`data-testid`, `id`); warn on positional CSS |

---

## Resolved Questions

- [x] GitHub Copilot SDK — **https://github.com/github/copilot-sdk** (.NET package)
- [x] Should the tool support generating `ContainerBase` classes? **Yes** — generate for complex page regions
- [x] Max DOM depth / element count — **may need chunking strategy** for large pages; prune to selected region first
- [x] Target scope — **Brinell.Html only** (no WPF/WinForms UIA generation)
