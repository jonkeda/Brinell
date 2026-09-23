# Phase 13 — Analyzer Pipeline: ControlObject & PageObject Generation

## Goal

Define and implement the complete analyzer pipeline that transforms scraped DOM snapshots into reusable ControlObjects and PageObjects. This phase formalizes the two-stage analysis process, establishes the data flow contracts, and ensures the pipeline is end-to-end functional with the Copilot SDK.

---

## Concept

The Brinell Scraper's core value proposition:

```
Scrape pages  →  Detect patterns  →  Generate ControlObjects  →  Generate PageObjects  →  Output .cs files
```

**ControlObjects** are reusable `ContainerBase<TParent, TScope>` classes that encapsulate a repeating DOM pattern (e.g., a login form, a date picker, a data table row, a navigation menu). They expose the interactive child elements as typed properties.

**PageObjects** are `HtmlPageObjectBase<Self>` classes that represent a full page. They expose the page's elements as typed properties, using both built-in Brinell control types (TextInputControl, ButtonControl, etc.) and the site-specific ControlObjects generated in the previous stage.

The pipeline has **two analyzers** and **two generators**, with user approval gates between stages:

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  Stage 1: ControlObject Analysis                                    │
│  ┌──────────┐    ┌───────────────────┐    ┌──────────────────────┐  │
│  │ Corpus   │───→│ ControlObject     │───→│ ControlObject        │  │
│  │ (all     │    │ Analyzer          │    │ Proposals            │  │
│  │  pages)  │    │ (cross-page       │    │ (name, signature,    │  │
│  │          │    │  pattern detect)  │    │  confidence, props)  │  │
│  └──────────┘    └───────────────────┘    └──────────┬───────────┘  │
│                                                      │              │
│                                             ┌────────▼────────┐     │
│                                             │ User Approval   │     │
│                                             │ (approve/reject │     │
│                                             │  each proposal) │     │
│                                             └────────┬────────┘     │
│                                                      │              │
│  ┌───────────────────┐    ┌──────────────────────┐   │              │
│  │ ControlObject     │───→│ Generated            │◄──┘              │
│  │ Generator         │    │ ControlObjects       │                  │
│  │ (LLM code gen +   │    │ (C# ContainerBase    │                  │
│  │  Roslyn validate) │    │  classes in registry)│                  │
│  └───────────────────┘    └──────────┬───────────┘                  │
│                                      │                              │
│  ┌───────────────────────────────────▼──────────────────────────┐   │
│  │ Skill Auto-Generation                                        │   │
│  │ Generate {site}-controls/SKILL.md with control signatures    │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  Stage 2: PageObject Generation                                     │
│  ┌──────────┐    ┌───────────────────┐    ┌──────────────────────┐  │
│  │ Corpus   │───→│ PageObject        │───→│ PageObject           │  │
│  │ (per     │    │ Analyzer +        │    │ Results              │  │
│  │  page)   │    │ Generator         │    │ (C# PageObjectBase   │  │
│  │          │    │ (LLM with control │    │ classes + containers)│  │
│  │ +Control │    │  object awareness)│    │                      │  │
│  │  Objects │    │                   │    │                      │  │
│  └──────────┘    └───────────────────┘    └──────────┬───────────┘  │
│                                                      │              │
│                                             ┌────────▼────────┐     │
│                                             │ User Review     │     │
│                                             │ (view code,     │     │
│                                             │  validate, fix) │     │
│                                             └────────┬────────┘     │
│                                                      │              │
│  ┌───────────────────┐    ┌──────────────────────┐   │              │
│  │ Code Output       │───→│ .cs Files            │◄──┘              │
│  │ (write to disk,   │    │ (project structure,  │                  │
│  │  scaffold project)│    │  ready to compile)   │                  │
│  └───────────────────┘    └──────────────────────┘                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 13.1 — ControlObject Analyzer

The ControlObject Analyzer examines the **entire corpus** (all pages for a site) to find **repeating DOM patterns** that appear across multiple pages or multiple times on a single page. These patterns become ControlObject candidates.

### What It Detects

| Pattern               | Example                                                                                   | ControlObject           |
| --------------------- | ----------------------------------------------------------------------------------------- | ----------------------- |
| Repeating form layout | Login form on `/login`, registration form on `/register` — same `<form>` structure | `LoginFormContainer`  |
| Shared navigation     | `<nav class="main-nav">` present on every page                                          | `MainNavContainer`    |
| Data table pattern    | `<table>` with `<thead>`/`<tbody>` on `/users`, `/orders`, `/products`        | `DataTableContainer`  |
| Custom widget         | `<div class="date-picker">` with input + calendar button on 5 pages                     | `DatePickerContainer` |
| Card/tile layout      | `<div class="card">` with image + title + body + actions, repeated in lists             | `CardContainer`       |
| Dialog/modal          | `<div role="dialog">` with header, body, footer buttons                                 | `DialogContainer`     |
| Toolbar               | `<div class="toolbar">` with grouped buttons/actions                                    | `ToolbarContainer`    |

### Analysis Process

```
1. Load all corpus snapshots for the site
2. For each snapshot, run ControlGroupDetector (local, no LLM)
   → produces ControlGroupSuggestion[] per page
3. Aggregate groups across pages:
   - Exact-match: same tag + class + child structure
   - Fuzzy-match: similar structure (same tag hierarchy, different text/values)
4. Build frequency map: which patterns appear on how many pages
5. Send aggregated patterns to LLM (analyzer model) with prompt:
   "Here are DOM patterns found across {N} pages. Propose ControlObjects
    for patterns that appear 2+ times or are semantically significant."
6. LLM returns ControlProposal[] with:
   - name, domSignature, confidence, suggestedProperties[], exampleSnippet
7. Additionally, LLM returns LocatorReport:
   - Which attributes are stable across pages (data-testid, aria-label)
   - Which are unstable (dynamic IDs, random classes)
   - Recommendations for locator strategy
```

### Two-Phase Analysis (Local + LLM)

**Phase A — Local Detection** (`ControlGroupDetector`, already implemented):

- Runs entirely locally, no LLM cost
- Scans each page's DOM tree for semantic containers:
  - `<form>` → FormContainer
  - `<table>` with thead/tbody → TableContainer
  - `<ul>`/`<ol>` with 2+ `<li>` → ListContainer
  - `<nav>` → NavigationContainer
  - `<fieldset>` with `<legend>` → FieldsetContainer
  - `<div>` with `role="dialog|form|tablist"` → RoleContainer
- Groups are structural — they don't know if the same form appears on multiple pages

**Phase B — LLM Cross-Page Analysis** (`AnalysisService`):

- Receives aggregated patterns from Phase A
- Has access to corpus query tools (via Copilot SDK custom tools):
  - `list_recorded_pages()` — enumerate all pages
  - `get_page_snapshot(pageId)` — get full DOM for a page
  - `find_similar_elements(selector)` — search across pages
  - `get_generated_controls()` — see existing control objects
- LLM identifies:
  - Which local groups are actually the same control appearing on different pages
  - Patterns the local detector missed (e.g., custom widgets without semantic HTML)
  - Appropriate names and property sets for each control object
  - Confidence scores based on frequency and consistency

### Service: `ControlObjectAnalyzer`

```csharp
public class ControlObjectAnalyzer
{
    private readonly ControlGroupDetector _detector;
    private readonly CorpusService _corpus;
    private readonly ICopilotService _copilot;
    private readonly AnalysisResultParser _parser;
    private readonly PromptBuilder _prompts;
    private readonly ILogger<ControlObjectAnalyzer> _logger;

    /// <summary>
    /// Runs the full two-phase analysis pipeline.
    /// Phase A: local ControlGroupDetector on each snapshot.
    /// Phase B: LLM cross-page pattern analysis.
    /// </summary>
    public async Task<ControlObjectAnalysisResult> AnalyzeAsync(long siteId)
    {
        // Phase A: Local detection
        var snapshots = await _corpus.GetLatestSnapshotsAsync(siteId);
        var localGroups = new Dictionary<string, List<LocalGroupMatch>>();

        foreach (var snapshot in snapshots)
        {
            var dom = await _corpus.LoadSnapshotAsync(snapshot.Id);
            var groups = _detector.Detect(dom.RootElement);

            foreach (var group in groups)
            {
                var signature = ComputeStructuralSignature(group);
                if (!localGroups.ContainsKey(signature))
                    localGroups[signature] = [];
                localGroups[signature].Add(new(snapshot.PageName, snapshot.PageUrl, group));
            }
        }

        // Phase B: LLM analysis
        var aggregated = localGroups
            .Where(g => g.Value.Count >= 1)   // include single-page groups too
            .Select(g => new AggregatedPattern
            {
                Signature = g.Key,
                Frequency = g.Value.Count,
                Pages = g.Value.Select(m => m.PageName).Distinct().ToList(),
                ExampleHtml = FormatGroupHtml(g.Value.First().Group),
                ContainerType = g.Value.First().Group.ContainerType,
                ChildTags = ExtractChildTags(g.Value.First().Group)
            })
            .ToList();

        var prompt = _prompts.BuildControlObjectAnalysisPrompt(aggregated, snapshots.Count);
        var response = await _copilot.AnalyzeAsync(prompt);
        var result = _parser.Parse(response);

        return new ControlObjectAnalysisResult
        {
            Proposals = result.ProposedControls,
            LocatorReport = result.LocatorReport,
            LocalGroupCount = localGroups.Count,
            SnapshotsAnalyzed = snapshots.Count
        };
    }
}
```

### Output: `ControlObjectAnalysisResult`

```csharp
public class ControlObjectAnalysisResult
{
    public List<ControlProposal> Proposals { get; set; } = [];
    public LocatorReport? LocatorReport { get; set; }
    public int LocalGroupCount { get; set; }
    public int SnapshotsAnalyzed { get; set; }
}
```

### User Approval Gate

After analysis, the user reviews proposals in the **Control Objects tab** (Phase 12.4):

1. Each `ControlProposal` is shown as a card with name, confidence, DOM signature, example snippet
2. User can **Approve** — marks for code generation
3. User can **Reject** — excludes from generation
4. User can **Edit** — modify name, properties, or DOM signature before approval
5. Approved proposals are passed to the ControlObject Generator

---

## 13.2 — ControlObject Generator

Generates C# `ContainerBase<TParent, TScope>` classes from approved `ControlProposal` items.

### Generation Process

For each approved proposal:

```
1. PromptBuilder.BuildControlObjectPrompt():
   - Control name and namespace
   - DOM signature (CSS pattern)
   - Example HTML snippet
   - Suggested properties with types
   - Locator preferences (from LocatorReport)
   - Brinell conventions (from SKILL.md)

2. ICopilotService.GenerateAsync(prompt) → generator model (gpt-4o)
   LLM generates:
   - ContainerBase<TParent, ControlName<TParent>> class
   - Properties for each interactive child element
   - Typed as appropriate: TextInputControl, ButtonControl, SelectControl, etc.
   - Locators using preference order: ByText > ByDataTestId > ByAriaLabel > ById > ByCss

3. CodeBlockParser.ExtractCSharpBlocks(response)
   - Regex extracts ```csharp...``` blocks

4. CodeValidator.Validate(code)
   - Roslyn syntax check
   - Control type name validation (built-in types only at this stage)
   - Locator method validation
   - ByCss usage warning

5. If errors → auto-retry once with error feedback appended to prompt

6. IControlRegistry.StoreControl(generatedControl)
   - Persists to SQLite registry table

7. SkillService.GenerateSiteControlsSkill(siteControls)
   - Updates {site}-controls/SKILL.md with new control definitions
   - This skill is loaded into the generator agent context for page generation
```

### Generated ControlObject Example

```csharp
/// <summary>
/// Container for the login form pattern: form.login-form
/// Found on: /login, /register, /reset-password
/// </summary>
public sealed class LoginFormContainer<TParent>
    : ContainerBase<TParent, LoginFormContainer<TParent>>
{
    public LoginFormContainer(TParent parent, ILocator locator)
        : base(parent, locator) { }

    public TextInputControl<LoginFormContainer<TParent>> Username =>
        Control<TextInputControl<LoginFormContainer<TParent>>>(
            Locator.ByText("Username"));

    public TextInputControl<LoginFormContainer<TParent>> Password =>
        Control<TextInputControl<LoginFormContainer<TParent>>>(
            Locator.ByText("Password"));

    public ButtonControl<LoginFormContainer<TParent>> Submit =>
        Control<ButtonControl<LoginFormContainer<TParent>>>(
            Locator.ByText("Sign In"));

    public CheckboxControl<LoginFormContainer<TParent>> RememberMe =>
        Control<CheckboxControl<LoginFormContainer<TParent>>>(
            Locator.ByText("Remember me"));
}
```

### Service: `ControlObjectGenerator`

This is the existing `ControlGenerationService` — no new service needed. Phase 13 formalizes its role in the pipeline and ensures it integrates cleanly with the analyzer output.

```csharp
// Existing service, used as-is
public class ControlGenerationService
{
    public async Task<List<GeneratedControl>> GenerateAllApprovedAsync(
        List<ControlProposal> proposals,
        string targetNamespace,
        LocatorReport? locatorReport)
    {
        var results = new List<GeneratedControl>();
        foreach (var proposal in proposals.Where(p => p.IsApproved))
        {
            var result = await GenerateControlAsync(proposal, targetNamespace, locatorReport);
            results.Add(result);
        }
        return results;
    }
}
```

### Skill Auto-Generation

After control objects are generated, `SkillService` creates/updates the `{site}-controls/SKILL.md` file:

```markdown
# Bouw7 — Custom Control Objects

## LoginFormContainer
- DOM signature: `form.login-form`
- Found on: /login, /register, /reset-password
- Properties: Username (TextInput), Password (TextInput), Submit (Button), RememberMe (Checkbox)
- Usage:
  ```csharp
  public LoginFormContainer<MyPage> LoginForm =>
      Control<LoginFormContainer<MyPage>>(Locator.ByCss("form.login-form"));
```

## DataTableContainer

- DOM signature: `table.data-grid`
- Found on: /users, /orders, /products
- Properties: Headers (Row), Rows (RowCollection), Pagination (PaginationContainer)

```

This skill is loaded into the LLM generator agent context during PageObject generation, so the LLM knows which custom ControlObjects are available and when to use them.

---

## 13.3 — PageObject Analyzer + Generator

The PageObject stage is a **combined analyze-and-generate** step. Unlike ControlObjects (which require cross-page pattern detection), each PageObject corresponds directly to one corpus page snapshot. The "analysis" is embedded in the generation prompt.

### Generation Process (Per Page)

```

1. Load DomSnapshot from corpus (latest version for the page)
2. Gather actionable elements:

   - If user selected specific elements during inspection → use those
   - Otherwise → filter to actionable tags:
     input, button, select, textarea, a, img, label, form, nav, table
3. Run ControlGroupDetector on the page's DOM
   → identifies forms, tables, lists, etc. that should become
   inline ContainerBase classes (page-specific containers)
4. Match against existing ControlObjects:

   - Compare DOM patterns against registered control objects
   - If a form on this page matches LoginFormContainer's signature → use it
   - If a pattern doesn't match any control object → generate inline container
5. PromptBuilder.BuildPageObjectPrompt():

   - Page metadata (URL, title, element count)
   - Actionable elements (formatted DOM snippet)
   - Available ControlObjects from registry (with signatures + usage examples)
   - Locator preferences from LocatorReport
   - Container group suggestions (for inline containers)
   - Brinell conventions (from SKILL.md)
6. ICopilotService.GenerateAsync(prompt) → generator model (gpt-4o)
   LLM generates:

   - Main PageObject class: HtmlPageObjectBase`<ClassName>`
   - Optional inline ContainerBase classes for page-specific groups
   - Properties typed as:
     a) Built-in types (TextInputControl, ButtonControl, etc.)
     b) Custom ControlObjects (LoginFormContainer, DataTableContainer, etc.)
     c) Inline containers (only for patterns unique to this page)
7. CodeBlockParser — extracts multiple C# blocks:

   - First block = main PageObject
   - Subsequent blocks = inline container classes
8. CodeValidator.ValidateWithRegistry(code, registry):

   - Syntax check (Roslyn)
   - Control type resolution (built-in + custom from registry)
   - Locator method validation
   - ByCss warning
9. If errors → auto-retry once with feedback
10. Return PageGenerationResult

```

### Generated PageObject Example

```csharp
public sealed class LoginPage : HtmlPageObjectBase<LoginPage>
{
    public LoginPage(IHtmlTestContext context) : base(context) { }

    // Uses custom ControlObject (from registry)
    public LoginFormContainer<LoginPage> LoginForm =>
        Control<LoginFormContainer<LoginPage>>(
            Locator.ByCss("form.login-form"));

    // Uses custom ControlObject (from registry)
    public MainNavContainer<LoginPage> Navigation =>
        Control<MainNavContainer<LoginPage>>(
            Locator.ByCss("nav.main-nav"));

    // Direct element — no container needed
    public TextControl<LoginPage> PageTitle =>
        Control<TextControl<LoginPage>>(
            Locator.ByDataTestId("page-title"));

    // Direct element
    public LinkControl<LoginPage> ForgotPassword =>
        Control<LinkControl<LoginPage>>(
            Locator.ByText("Forgot your password?"));
}
```

### Service: `PageObjectGenerator`

This is the existing `PageGenerationService` — no new service needed. Phase 13 formalizes its role and ensures ControlObject awareness.

```csharp
// Existing service, used as-is
public class PageGenerationService
{
    public async Task<PageGenerationResult> GeneratePageAsync(
        DomSnapshot snapshot,
        IReadOnlyList<GeneratedControl> availableControls,
        LocatorReport? locatorReport,
        string targetNamespace,
        List<ControlGroupSuggestion>? containerGroups = null)
    {
        // ... builds prompt, calls LLM, validates, returns result
    }
}
```

### ControlObject Matching Logic

When generating a PageObject, the generator matches page elements against existing ControlObjects:

```csharp
public class ControlObjectMatcher
{
    /// <summary>
    /// Given a DOM element and the registered control objects,
    /// determines if the element matches a known control object pattern.
    /// </summary>
    public ControlObjectMatch? FindMatch(DomElement element, IReadOnlyList<GeneratedControl> controls)
    {
        foreach (var control in controls)
        {
            if (MatchesSignature(element, control.DomSignature))
            {
                return new ControlObjectMatch
                {
                    ControlName = control.Name,
                    Element = element,
                    Confidence = ComputeMatchConfidence(element, control)
                };
            }
        }
        return null;
    }

    private bool MatchesSignature(DomElement element, string signature)
    {
        // Parse CSS-like signature: "form.login-form", "div.date-picker > input + button"
        // Match against element's tag, classes, and child structure
        // Supports: tag, .class, #id, [attr], > child, + sibling
    }
}
```

---

## 13.4 — Pipeline Orchestration

The full pipeline is orchestrated by a new `PipelineService` that coordinates the stages and manages state.

### Pipeline States

```
┌──────────┐   Scrape    ┌──────────┐  Analyze   ┌──────────────┐
│  Empty   │ ──────────→ │  Corpus  │ ─────────→ │  Proposals   │
│          │             │  Ready   │            │  (pending)   │
└──────────┘             └──────────┘            └──────┬───────┘
                                                        │
                                                  User Approve
                                                        │
                                                 ┌──────▼───────┐
                                                 │  Approved    │
                                                 │  ControlObjs │
                                                 └──────┬───────┘
                                                        │
                                                  Generate Code
                                                        │
                                                 ┌──────▼───────┐
                                                 │  Generated   │
                                                 │  ControlObjs │
                                                 └──────┬───────┘
                                                        │
                                                 Generate PageObjs
                                                        │
                                                 ┌──────▼───────┐
                                                 │  Generated   │
                                                 │  PageObjects │
                                                 └──────┬───────┘
                                                        │
                                                   Write .cs
                                                        │
                                                 ┌──────▼───────┐
                                                 │  Output      │
                                                 │  Complete    │
                                                 └──────────────┘
```

### Service: `PipelineOrchestrator`

```csharp
public class PipelineOrchestrator
{
    private readonly ControlObjectAnalyzer _controlAnalyzer;
    private readonly ControlGenerationService _controlGenerator;
    private readonly PageGenerationService _pageGenerator;
    private readonly SkillService _skillService;
    private readonly CodeOutputService _codeOutput;
    private readonly CorpusService _corpus;
    private readonly IControlRegistry _registry;
    private readonly ILogger<PipelineOrchestrator> _logger;

    /// <summary>
    /// Stage 1A: Analyze corpus for ControlObject patterns.
    /// Returns proposals for user approval.
    /// </summary>
    public async Task<ControlObjectAnalysisResult> AnalyzeForControlObjectsAsync(long siteId)
    {
        _logger.LogInformation("Starting ControlObject analysis for site {SiteId}", siteId);
        return await _controlAnalyzer.AnalyzeAsync(siteId);
    }

    /// <summary>
    /// Stage 1B: Generate code for approved ControlObject proposals.
    /// Stores in registry and updates skills.
    /// </summary>
    public async Task<List<GeneratedControl>> GenerateControlObjectsAsync(
        List<ControlProposal> approvedProposals,
        string targetNamespace,
        LocatorReport? locatorReport)
    {
        var controls = await _controlGenerator.GenerateAllApprovedAsync(
            approvedProposals, targetNamespace, locatorReport);

        // Update skills for page generation awareness
        await _skillService.GenerateSiteControlsSkillAsync(
            targetNamespace.Split('.')[0], controls);

        return controls;
    }

    /// <summary>
    /// Stage 2: Generate PageObjects for all corpus pages using approved ControlObjects.
    /// </summary>
    public async Task<List<PageGenerationResult>> GeneratePageObjectsAsync(
        long siteId,
        string targetNamespace,
        LocatorReport? locatorReport)
    {
        var snapshots = await _corpus.GetLatestSnapshotsAsync(siteId);
        var controls = await _registry.GetControlsAsync(siteId);
        var results = new List<PageGenerationResult>();

        foreach (var summary in snapshots)
        {
            var snapshot = await _corpus.LoadSnapshotAsync(summary.Id);
            var groups = new ControlGroupDetector().Detect(snapshot.RootElement);

            var result = await _pageGenerator.GeneratePageAsync(
                snapshot, controls, locatorReport, targetNamespace, groups);

            results.Add(result);
            _logger.LogInformation("Generated PageObject {ClassName} from {PageUrl}",
                result.ClassName, summary.PageUrl);
        }

        return results;
    }

    /// <summary>
    /// Stage 3: Write generated code to disk.
    /// </summary>
    public async Task OutputAsync(
        List<GeneratedControl> controlObjects,
        List<PageGenerationResult> pageObjects,
        string outputPath,
        string targetNamespace)
    {
        await _codeOutput.WriteProjectAsync(outputPath, targetNamespace,
            controlObjects, pageObjects);
    }
}
```

---

## 13.5 — Data Flow Contract

### Complete Model Chain

```
DomSnapshot                          (Input: scraped page DOM)
    │
    ├─ ControlGroupDetector.Detect()
    │   └→ ControlGroupSuggestion[]  (Local: semantic containers per page)
    │
    ├─ ControlObjectAnalyzer.AnalyzeAsync()
    │   └→ ControlObjectAnalysisResult
    │       ├─ ControlProposal[]     (LLM: proposed control objects)
    │       │   ├─ name              e.g. "LoginFormContainer"
    │       │   ├─ domSignature      e.g. "form.login-form"
    │       │   ├─ frequency         e.g. 3 (found on 3 pages)
    │       │   ├─ confidence        e.g. 92
    │       │   ├─ exampleSnippet    HTML fragment
    │       │   ├─ suggestedProperties[]
    │       │   │   ├─ name          e.g. "Username"
    │       │   │   ├─ controlType   e.g. "TextInputControl"
    │       │   │   └─ selector      e.g. "[name=user]"
    │       │   └─ IsApproved        set by user
    │       │
    │       └─ LocatorReport         (LLM: site-wide locator strategy)
    │           ├─ stableAttributes[]    e.g. ["data-testid", "aria-label"]
    │           ├─ unstableAttributes[]  e.g. ["id (dynamic)"]
    │           └─ recommendations       text summary
    │
    ├─ ControlGenerationService.GenerateAllApprovedAsync()
    │   └→ GeneratedControl[]        (LLM: C# ContainerBase code)
    │       ├─ name                  e.g. "LoginFormContainer"
    │       ├─ namespace             e.g. "Bouw7.Controls"
    │       ├─ code                  full C# class text
    │       ├─ domSignature          e.g. "form.login-form"
    │       ├─ confidence            e.g. 92
    │       └─ createdAt             timestamp
    │
    ├─ SkillService.GenerateSiteControlsSkillAsync()
    │   └→ {site}-controls/SKILL.md  (Disk: LLM context for page gen)
    │
    ├─ PageGenerationService.GeneratePageAsync()
    │   └→ PageGenerationResult      (LLM: C# PageObject code)
    │       ├─ className             e.g. "LoginPage"
    │       ├─ namespace             e.g. "Bouw7.Pages"
    │       ├─ mainCode              full C# PageObject class text
    │       ├─ containerCodes[]      inline container classes (if any)
    │       ├─ validation            Roslyn validation result
    │       └─ customControlsUsed[]  which control objects are referenced
    │
    └─ CodeOutputService.WriteProjectAsync()
        └→ .cs files on disk         (Output: compilable project)
            ├─ Controls/
            │   ├─ LoginFormContainer.cs
            │   ├─ DataTableContainer.cs
            │   └─ NavMenuContainer.cs
            └─ Pages/
                ├─ LoginPage.cs
                ├─ DashboardPage.cs
                └─ UsersPage.cs
```

### Persistence Points

| Stage                    | What's Persisted                            | Where                                         |
| ------------------------ | ------------------------------------------- | --------------------------------------------- |
| Scraping                 | DomSnapshot (DOM tree JSON)                 | SQLite: Snapshots + Elements tables           |
| ControlObject Analysis   | ControlProposal[] (with approval status)    | SQLite: AnalysisResults table (new)           |
| ControlObject Analysis   | LocatorReport                               | SQLite: AnalysisResults table (new)           |
| ControlObject Generation | GeneratedControl (C# code + metadata)       | SQLite: Controls table (via IControlRegistry) |
| Skill Generation         | SKILL.md files                              | Disk:`./corpus/skills/{site}-controls/`     |
| PageObject Generation    | PageGenerationResult (C# code + validation) | SQLite: PageObjects table (new)               |
| Code Output              | .cs files                                   | Disk: configured output path                  |

### New Database Tables

```sql
-- Stores analysis results (one per analysis run)
CREATE TABLE AnalysisResults (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL REFERENCES Sites(Id),
    ProposalsJson TEXT NOT NULL,        -- JSON array of ControlProposal
    LocatorReportJson TEXT,             -- JSON of LocatorReport
    SnapshotsAnalyzed INTEGER NOT NULL,
    LocalGroupCount INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (SiteId) REFERENCES Sites(Id)
);

-- Stores generated page objects
CREATE TABLE PageObjects (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL REFERENCES Sites(Id),
    SnapshotId INTEGER NOT NULL REFERENCES Snapshots(Id),
    ClassName TEXT NOT NULL,
    Namespace TEXT NOT NULL,
    MainCode TEXT NOT NULL,
    ContainerCodesJson TEXT,           -- JSON array of strings
    ValidationJson TEXT,               -- JSON of ValidationResult
    UsedControlObjectsJson TEXT,       -- JSON array of control names
    GeneratedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (SiteId) REFERENCES Sites(Id),
    FOREIGN KEY (SnapshotId) REFERENCES Snapshots(Id)
);
```

---

## 13.6 — Copilot SDK Integration Points

The pipeline requires the `ICopilotService` implementation (GitHub Copilot SDK). These are the integration points:

### Custom Tools (Registered with Copilot SDK Agent)

| Tool                                | Purpose                                          | Used By                                      |
| ----------------------------------- | ------------------------------------------------ | -------------------------------------------- |
| `list_recorded_pages()`           | List all pages in the corpus with metadata       | ControlObject Analyzer                       |
| `get_page_snapshot(pageId)`       | Get full DOM tree for a specific page            | ControlObject Analyzer                       |
| `find_similar_elements(selector)` | Search elements across all pages by CSS selector | ControlObject Analyzer                       |
| `get_generated_controls()`        | List existing ControlObjects with signatures     | ControlObject Analyzer, PageObject Generator |
| `search_corpus(query)`            | Full-text search across page elements            | Both analyzers                               |

### Agent Sessions

| Agent           | Model                 | Purpose                     | Skills Loaded                                 |
| --------------- | --------------------- | --------------------------- | --------------------------------------------- |
| Analyzer Agent  | gpt-4o-mini (cheaper) | Cross-page pattern analysis | `brinell-conventions`                       |
| Generator Agent | gpt-4o (smarter)      | C# code generation          | `brinell-conventions` + `{site}-controls` |

### Prompt Templates

**ControlObject Analysis Prompt:**

```
Analyze the following corpus of {pageCount} web pages for the site "{siteName}".

Use the corpus query tools to examine each page's DOM structure.
Identify repeating DOM patterns that should be extracted as reusable ControlObjects.

Local analysis has already detected {localGroupCount} structural groups:
{aggregatedPatterns}

For each proposed ControlObject, provide:
- name: PascalCase name ending in "Container"
- domSignature: CSS-like pattern (e.g., "form.login-form")
- frequency: how many pages it appears on
- confidence: 0-100
- exampleSnippet: representative HTML
- suggestedProperties: [{name, controlType, selector}]

Also provide a LocatorReport with stable/unstable attribute analysis.

Respond with a JSON block: { "proposedControls": [...], "locatorReport": {...} }
```

**PageObject Generation Prompt:**

```
Generate a Brinell HtmlPageObjectBase<{ClassName}> for the page at {pageUrl}.

Available custom ControlObjects:
{controlObjectList}

Page DOM (actionable elements):
{domSnippet}

Container groups detected:
{containerGroups}

Locator preferences (in order):
1. ByText (display text)
2. ByDataTestId (data-testid attribute)
3. ByAriaLabel (aria-label)
4. ById (id attribute — only if stable)
5. ByCss (last resort — emit warning comment)

Use custom ControlObjects when the DOM matches their signature.
Generate inline ContainerBase classes for page-specific groups not covered by existing ControlObjects.
```

---

## 13.7 — Error Handling & Recovery

### Retry Strategy

| Stage                          | On Failure                                | Max Retries | Fallback                                        |
| ------------------------------ | ----------------------------------------- | ----------- | ----------------------------------------------- |
| ControlObject Analysis (LLM)   | Append error feedback to prompt, retry    | 1           | Return partial results (local groups only)      |
| ControlObject Generation (LLM) | Append Roslyn errors to prompt, retry     | 1           | Mark as failed, skip to next proposal           |
| PageObject Generation (LLM)    | Append validation errors to prompt, retry | 1           | Mark as failed, show errors in Page Objects tab |
| Roslyn Validation              | —                                        | —          | Show errors in UI, allow manual edit            |
| Corpus Query (tools)           | Log and skip                              | 2           | Return empty, LLM works with reduced context    |

### Common Failure Modes

| Failure                                | Cause                        | Recovery                                                             |
| -------------------------------------- | ---------------------------- | -------------------------------------------------------------------- |
| LLM returns no JSON                    | Malformed response           | Parser falls back to regex extraction                                |
| Unknown control type in generated code | LLM invented a type          | Validator catches, retry prompt includes "Only use these types: ..." |
| ByCss overuse                          | LLM lazy locator choice      | Warning in validation, user can regenerate with stricter prompt      |
| Duplicate ControlObject names          | Two proposals named the same | Suffix with number, log warning                                      |
| Token limit exceeded                   | Too many elements in prompt  | Truncate DOM to actionable elements only, paginate if needed         |

---

## Implementation Steps

| Step  | Task                                                               | Files                                        |
| ----- | ------------------------------------------------------------------ | -------------------------------------------- |
| 13.1a | Create `ControlObjectAnalyzer` service                           | `Services/ControlObjectAnalyzer.cs`        |
| 13.1b | Create `ControlObjectAnalysisResult` model                       | `Models/ControlObjectAnalysisResult.cs`    |
| 13.1c | Create `ControlObjectMatcher` service                            | `Services/ControlObjectMatcher.cs`         |
| 13.2a | Create `PipelineOrchestrator` service                            | `Services/PipelineOrchestrator.cs`         |
| 13.2b | Add `AnalysisResults` table to database                          | `Services/CorpusService.cs` or migration   |
| 13.2c | Add `PageObjects` table to database                              | `Services/CorpusService.cs` or migration   |
| 13.3a | Update `PromptBuilder` with `BuildControlObjectAnalysisPrompt` | `Services/PromptBuilder.cs`                |
| 13.3b | Update `PromptBuilder` with improved `BuildPageObjectPrompt`   | `Services/PromptBuilder.cs`                |
| 13.4a | Register pipeline services in DI                                   | `App.xaml.cs`                              |
| 13.4b | Wire `ControlObjectsTabViewModel` to pipeline                    | `ViewModels/ControlObjectsTabViewModel.cs` |
| 13.4c | Wire `PageObjectsTabViewModel` to pipeline                       | `ViewModels/PageObjectsTabViewModel.cs`    |
| 13.5  | Implement `ICopilotService` (Copilot SDK integration)            | `Services/CopilotService.cs`               |
| 13.6  | Implement custom tools for corpus queries                          | `Services/CorpusTools.cs`                  |
| 13.7  | Implement `CodeOutputService` (.cs file writing)                 | `Services/CodeOutputService.cs`            |
| 13.8  | Update tests                                                       | `Brinell.Scraper.Tests/`                   |
| 13.9  | Build + end-to-end test                                            | —                                           |

---

## Dependencies

| This Phase                     | Depends On                                                                      |
| ------------------------------ | ------------------------------------------------------------------------------- |
| 13.1 (ControlObject Analyzer)  | Phase 4 (DOM capture), Phase 5 (existing AnalysisService, ControlGroupDetector) |
| 13.2 (ControlObject Generator) | Phase 5 (existing ControlGenerationService)                                     |
| 13.3 (PageObject Generator)    | Phase 5 (existing PageGenerationService), 13.2 (generated ControlObjects)       |
| 13.4 (Pipeline Orchestrator)   | 13.1 + 13.2 + 13.3                                                              |
| 13.5 (Copilot SDK)             | GitHub Copilot SDK package                                                      |
| 13.7 (Code Output)             | Phase 7 design (existing spec)                                                  |
| UI integration                 | Phase 12 (Control Objects tab, Page Objects tab, Corpus tab)                    |

## Notes

- The existing `AnalysisService`, `ControlGenerationService`, and `PageGenerationService` are **not replaced** — they are composed by the `PipelineOrchestrator` and `ControlObjectAnalyzer`.
- The `ControlGroupDetector` (local, no LLM) serves as Phase A of the analysis — it provides the initial candidates that the LLM then refines and merges.
- The pipeline is **incremental**: re-recording a single page only requires re-generating that page's PageObject, not re-analyzing the entire corpus for ControlObjects.
- ControlObjects are **site-scoped**: different sites have independent ControlObject registries. A control object from Bouw7 is not shared with ExactOnline.
- The skill auto-generation step is critical: without it, the PageObject generator doesn't know about custom ControlObjects and will generate inline containers for patterns that already have dedicated classes.
