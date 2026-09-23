# Phase 5 — LLM-Powered Code Generation (GitHub Copilot SDK)

## Goal

Use the GitHub Copilot SDK as an agentic runtime to analyze the SQLite corpus and generate Brinell code in two passes:

- **Phase 5A — Analysis Pass**: An analyzer agent examines the full corpus to detect repeated UI patterns, propose custom controls, and identify optimal locator strategies. Uses a cheaper/faster model.
- **Phase 5B — Generation Pass**: A generator agent produces actual C# code — custom ControlObjects first (from approved proposals), then PageObjects that reference those controls. Uses a smarter model (Claude Opus 4.6 / GPT 5.4).

The LLM queries the corpus via custom tools registered with the SDK, and Brinell conventions are delivered as Skills (SKILL.md files) rather than monolithic prompt strings. Large context windows (200K+ tokens) mean full page DOMs fit without chunking.

## Tasks

### 5.1 — Copilot SDK Integration (Agentic Runtime)

The Copilot SDK is used as an agentic runtime — not raw API calls. It manages agents, tools, skills, hooks, and session persistence.

**NuGet package reference:**

```xml
<PackageReference Include="GitHub.Copilot.SDK" Version="*" />
```

> Package source: https://github.com/github/copilot-sdk — follow latest install instructions for the .NET variant.

**Authentication setup:**

- Use GitHub token-based auth (PAT or GitHub App token)
- Store token in user secrets or Windows Credential Manager — never in source

**Custom agents:**

Two agents are defined — one for analysis, one for generation:

- **analyzer** — cheaper model, analyzes DOM patterns across all pages in the corpus
- **generator** — smarter model (Claude Opus 4.6 / GPT 5.4), generates C# ControlObject and PageObject code

**Custom tools (corpus queries):**

The LLM does not receive raw data inline. Instead it queries the SQLite corpus on demand via registered tools:

| Tool | Purpose |
|------|---------|
| `search_corpus(query, tag?, attribute?)` | Full-text search across all stored DOM snapshots |
| `get_page_snapshot(pageId)` | Retrieve the full DOM snapshot for a specific page |
| `find_similar_elements(selector, minCount?)` | Find elements matching a pattern across all pages |
| `get_generated_controls()` | List previously generated custom controls from the registry |
| `list_recorded_pages()` | List all pages in the corpus with URL, title, element counts |

**Skills (loaded from disk):**

- `brinell-conventions` — full Brinell control hierarchy, locator patterns, code style, examples
- `{site}-controls` — auto-generated after custom control generation; contains site-specific control types and usage patterns

Skills are stored as `SKILL.md` files in `./corpus/skills/` and loaded into agent context automatically.

**Session configuration:**

```csharp
var copilotClient = new CopilotClient(new CopilotClientOptions
{
    AuthToken = tokenProvider.GetToken(),
});

var session = await copilotClient.CreateSessionAsync(new SessionConfig
{
    SkillDirectories = new[] { "./corpus/skills" },
    CustomAgents = new[]
    {
        new AgentConfig
        {
            Name = "analyzer",
            Description = "Analyzes DOM patterns across pages",
            Prompt = "You analyze DOM snapshots to identify reusable UI patterns.",
            Skills = new[] { "brinell-conventions" }
        },
        new AgentConfig
        {
            Name = "generator",
            Description = "Generates Brinell PageObject and ControlObject code",
            Prompt = "You generate C# PageObject and ControlObject classes for the Brinell framework.",
            Skills = new[] { "brinell-conventions", "{site}-controls" }
        }
    },
    CustomTools = new[]
    {
        CorpusTools.SearchCorpus,
        CorpusTools.GetPageSnapshot,
        CorpusTools.FindSimilarElements,
        CorpusTools.GetGeneratedControls,
        CorpusTools.ListRecordedPages,
    }
});
```

**Client initialization:**

- Register `CopilotClient` and `Session` as singletons in DI container
- Handle auth failures gracefully — show "Sign in to GitHub" prompt
- Support token refresh if using OAuth flow

---

### 5.2 — System Prompt via Skills

Brinell conventions are delivered as **Skills** (`SKILL.md` files) rather than a single monolithic prompt string. The `brinell-conventions` skill contains the full control hierarchy, patterns, code style rules, and examples.

**Skill: `brinell-conventions/SKILL.md`** covers:

- **Base Classes** — `HtmlPageObjectBase<TSelf>`, `ContainerBase<TParent, TScope>`
- **Built-in Control Types** — all generic with `<TScope>` parameter (TextInputControl, ButtonControl, SelectControl, LabelControl, CheckBoxControl, RadioButtonControl, LinkControl, FileInputControl, TextAreaControl, ImageControl, ElementControl)
- **Code Style** — sealed classes, expression-bodied properties, PascalCase names, namespace conventions
- **Examples** — complete PageObject and ContainerBase samples

**Locator Strategies (preference order):**

The locator preference order prioritizes resilience over specificity:

1. **Labels and visible text** — `Locator.ByText("value")`, `Locator.ByLinkText("value")`, `Locator.ByPartialLinkText("value")` — primary, most resilient to DOM changes
2. **data-testid** — `Locator.ByDataTestId("value")` — explicit test hooks
3. **aria-label** — `Locator.ByAriaLabel("value")` — accessibility attributes
4. **id** — `Locator.ById("value")` — only if stable/not dynamically generated
5. **CSS selector** — `Locator.ByCss("selector")` — last resort, emit a warning to the user when used

**Skill: `{site}-controls/SKILL.md`** (auto-generated after Phase 5A/5C):

- Site-specific custom control types and their DOM signatures
- Usage patterns and examples specific to the target application
- Generated automatically during custom control generation (Task 5.3c)

---

### 5.3 — Feed DOM Elements to LLM (Corpus-Based)

DOM data comes from the **SQLite corpus** — not passed inline as a single blob. The LLM queries the corpus on demand via custom tools registered in Task 5.1.

**Corpus query flow:**

1. LLM calls `list_recorded_pages()` to see all available pages
2. LLM calls `get_page_snapshot(pageId)` to retrieve full DOM for a specific page
3. LLM calls `search_corpus(query)` to find elements across all pages
4. LLM calls `find_similar_elements(selector)` to detect repeated patterns

**Large context windows (200K+ tokens):**

- Full page DOMs fit within a single context window — no chunking needed
- Multiple pages can be sent at once for cross-page pattern detection
- The analyzer agent can examine the entire corpus in a single session

**DOM element formatting (when returned by tools):**

Tools return simplified HTML-like representations:

```
<input id="username" name="username" type="text" placeholder="Enter username" data-testid="user-input" />
<input id="password" name="password" type="password" placeholder="Password" />
<button type="submit" class="btn btn-primary" data-testid="login-btn">Sign In</button>
<div class="error-message" role="alert"></div>
```

**Formatting rules:**

- Only include attributes that have values (skip null/empty)
- For elements with children, show nested structure with indentation
- Include visible text content as element body
- Strip inline styles and script-related attributes
- Include `<!-- N children omitted -->` for truncated subtrees

**Page context included:**

```
Page URL: https://example.com/login
Page Title: Login - Example App
Element count: 47
```

---

### 5.3b — Analysis Pass (Phase 5A)

The analyzer agent examines the full corpus to detect patterns and propose custom controls. This runs before any code generation.

**Input:** Corpus summary — page list, element counts, common tags/attributes.

**Workflow:**

1. Send corpus summary to the analyzer agent
2. Analyzer queries corpus via tools:
   - `search_corpus()` — find elements by tag, attribute, or text
   - `find_similar_elements()` — detect repeated DOM structures across pages
3. Analyzer identifies:
   - **Element frequencies** — which elements appear on many pages
   - **Repeated structures** — groups of elements that always appear together (e.g., search bar + button + suggestions)
   - **CSS class patterns** — shared class names indicating a design system or component library
   - **Locator stability** — which attributes are stable vs. dynamic across pages
4. Analyzer proposes custom controls with:
   - Proposed control name (PascalCase)
   - DOM signature (tag + key attributes/classes)
   - Frequency count (how many pages contain this pattern)
   - Confidence percentage (0–100%)
   - Example DOM snippet
5. Results are returned as structured JSON for the Analysis View

**Analysis output schema:**

```json
{
  "proposedControls": [
    {
      "name": "DataGrid",
      "domSignature": "div.ag-root-wrapper > div.ag-body-viewport",
      "frequency": 12,
      "confidence": 92,
      "exampleSnippet": "<div class=\"ag-root-wrapper\">...</div>",
      "suggestedProperties": ["HeaderRow", "DataRows", "PaginationBar"]
    }
  ],
  "locatorReport": {
    "stableAttributes": ["data-testid", "aria-label"],
    "unstableAttributes": ["id (dynamic on 8/15 pages)"],
    "recommendations": "Prefer ByText() and ByDataTestId(). Avoid ById() on pages: Dashboard, Settings."
  }
}
```

**User approval:** Results are presented in the Analysis View (Phase 7 UI). User can approve, reject, or modify each proposed control before generation proceeds.

---

### 5.3c — Custom Control Generation (Phase 5B — Controls)

After user approves proposed controls, the generator agent produces `ContainerBase<TParent, TScope>` classes for each custom control.

**Input:** Approved control proposals from Task 5.3b.

**Workflow:**

1. Generator agent receives the approved control proposals
2. For each approved control, generates a `ContainerBase<TParent, TScope>` class:
   - Class name from the proposal
   - Properties derived from the DOM signature and suggested properties
   - Locators chosen per the preference order (Task 5.2)
3. Generated controls are stored in the **control registry** (SQLite table):
   - Control name, namespace, generated code, DOM signature, creation timestamp
4. Auto-generates a `{site}-controls/SKILL.md` file containing:
   - All custom control type names and their DOM signatures
   - Usage examples showing how PageObjects should reference them
   - This skill is loaded into the generator agent's context for subsequent page generation

**Example generated custom control:**

```csharp
namespace MyProject.Controls;

public sealed class DataGridContainer<TParent> : ContainerBase<TParent, DataGridContainer<TParent>>
{
    public ElementControl<DataGridContainer<TParent>> HeaderRow =>
        Control<ElementControl<DataGridContainer<TParent>>>(Locator.ByCss(".ag-header-row"));

    public ElementControl<DataGridContainer<TParent>> DataRows =>
        Control<ElementControl<DataGridContainer<TParent>>>(Locator.ByCss(".ag-body-viewport"));

    public ElementControl<DataGridContainer<TParent>> PaginationBar =>
        Control<ElementControl<DataGridContainer<TParent>>>(Locator.ByText("Page"));
}
```

**Control registry schema (SQLite):**

```sql
CREATE TABLE GeneratedControls (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE,
    Namespace TEXT NOT NULL,
    Code TEXT NOT NULL,
    DomSignature TEXT NOT NULL,
    Confidence REAL NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);
```

---

### 5.4 — Prompt Template for Page Generation (Phase 5B — Pages)

Build the user prompt for PageObject generation. The prompt now includes previously generated custom controls and site-specific patterns from analysis.

**Prompt template:**

```
Generate a Brinell page object class with the following details:

Class Name: {className}
Namespace: {namespace}
Page URL: {pageUrl}
Page Title: {pageTitle}

## Available Custom Controls

The following site-specific custom controls have been generated and should be used
when their DOM patterns are detected:

{customControlSummary}

## Site-Specific Patterns

{sitePatterns}

## Page Elements

The page contains these elements (selected for automation):

{domSnippet}

{containerInstructions}

Generate a sealed class inheriting from HtmlPageObjectBase<{className}> with expression-bodied
control properties for each element. Use custom controls when their DOM signature matches.
Choose the most appropriate control type and locator strategy for each element.
```

**Custom control summary (from registry):**

```
- DataGridContainer — matches: div.ag-root-wrapper > div.ag-body-viewport
- NavigationMenuContainer — matches: nav[role="navigation"] ul.nav-items
```

**Container instructions (appended when auto-detected groups exist):**

```
The following element groups should be generated as ContainerBase<{parentClass}, TContainer> classes:

Group "{groupName}" (root: {rootTag} {rootAttributes}):
{groupDomSnippet}
```

**C# prompt builder:**

```csharp
public sealed class PromptBuilder
{
    public string BuildUserPrompt(
        string className,
        string namespaceName,
        string pageUrl,
        string pageTitle,
        IReadOnlyList<DomElement> selectedElements,
        IReadOnlyList<GeneratedControl> customControls,
        AnalysisReport? sitePatterns = null,
        IReadOnlyList<ContainerGroup>? containerGroups = null)
    {
        // Include custom controls from registry
        // Include site-specific patterns from analysis
        // Build formatted DOM snippet
        // Append container instructions if groups exist
        // Return assembled prompt string
    }
}
```

---

### 5.5 — Parse LLM Response

Extract C# code blocks from the LLM's markdown-formatted response. Handles both **ControlObject** and **PageObject** responses.

**Extraction logic:**

```csharp
public static class CodeBlockParser
{
    private static readonly Regex CodeBlockRegex = new(
        @"```csharp\s*\n(.*?)```",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractCSharpBlocks(string llmResponse)
    {
        return CodeBlockRegex
            .Matches(llmResponse)
            .Select(m => m.Groups[1].Value.Trim())
            .ToList();
    }
}
```

**Handling multiple code blocks:**

- For **ControlObject generation** (Task 5.3c): each block is a separate custom control class
- For **PageObject generation** (Task 5.4): first block = main PageObject class, subsequent blocks = ContainerBase classes
- If only one block returned and containers were requested, attempt to split by class declarations

**Fallback:** If no fenced code blocks found, treat the entire response as code (strip any leading/trailing prose lines that don't look like C#).

---

### 5.6 — Validate Generated Code

Use Roslyn to parse and validate the generated C# code.

**Syntax validation:**

```csharp
public static class CodeValidator
{
    public static ValidationResult Validate(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var diagnostics = syntaxTree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        return new ValidationResult
        {
            IsValid = diagnostics.Count == 0,
            Errors = diagnostics.Select(d => new CodeError
            {
                Message = d.GetMessage(),
                Line = d.Location.GetLineSpan().StartLinePosition.Line + 1,
                Column = d.Location.GetLineSpan().StartLinePosition.Character + 1
            }).ToList()
        };
    }
}
```

**Control type validation (dynamic registry):**

Known control types are no longer a static `HashSet`. The validator loads built-in types plus any custom controls from the site's control registry in SQLite:

```csharp
private static HashSet<string> GetKnownControlTypes(IControlRegistry registry)
{
    // Built-in Brinell controls
    var known = new HashSet<string>
    {
        "TextInputControl", "ButtonControl", "SelectControl",
        "LabelControl", "CheckBoxControl", "RadioButtonControl",
        "LinkControl", "FileInputControl", "TextAreaControl",
        "ImageControl", "ElementControl"
    };

    // Add site-specific custom controls from registry
    foreach (var custom in registry.GetAllControls())
    {
        known.Add(custom.Name);
    }

    return known;
}
```

After parsing, walk the syntax tree to find all generic type references and verify each control type name exists in the combined set. Flag unknown types as warnings.

**Locator validation:**

- Walk syntax tree for `Locator.By*()` invocations
- Verify method name is one of: `ByText`, `ByLinkText`, `ByPartialLinkText`, `ByDataTestId`, `ByAriaLabel`, `ById`, `ByCss`
- Verify argument is a non-empty string literal
- Emit a warning if `ByCss` is used (last-resort locator)

**Auto-retry on failure:**

- If validation finds errors, re-send to LLM with: "The generated code has these errors: {errors}. Please fix and regenerate."
- Maximum 2 retry attempts before surfacing errors to user

---

## UI Design — Analysis View

User clicks 🔬 (Analyze) or answers "yes" after stopping recording. LLM analyzes corpus for patterns and proposes custom controls.

```
┌──────────────────┬───────────────────────────────────────────────────┐
│ 📁 Exact Online  │ 🔬 Analysis Results                               │
│ ─────────────── │                                                   │
│ Corpus: 50 pages │ Analyzed 50 pages in 12.4s (analyzer model)       │
│ Controls: 5      │                                                   │
│                   │ ── Proposed Custom Controls ─────────────────── │
│ ── Pages ──────  │                                                   │
│ ✅ LoginPage     │ 1. 📦 DatePickerControl (NEW)                    │
│ ✅ Dashboard     │    Found on: 8 pages                              │
│ ✅ TimeEntry     │    Pattern: div.date-picker > input + button.cal  │
│ ⏳ ProjectList   │    Confidence: 94%                                │
│ ⏳ InvoiceEdit   │    [Preview Code] [✅ Approve] [❌ Reject]       │
│ ⏳ SettingsPage  │                                                   │
│ ⏳ UserProfile   │ 2. 📦 AutocompleteControl (NEW)                  │
│ ⏳ ReportPage    │    Found on: 12 pages                             │
│                   │    Confidence: 89%                                │
│ ── Controls ──── │    [Preview Code] [✅ Approve] [❌ Reject]       │
│ ✅ DataGrid      │                                                   │
│ ⏳ DatePicker    │ 3. 📦 FileUploadControl (UPDATED)                │
│ ⏳ Autocomplete  │    Pattern changed: now includes drag-drop area   │
│                   │    [Preview Code] [✅ Approve] [❌ Reject]       │
│                   │                                                   │
│                   │ ── Pattern Summary ──────────────────────────── │
│                   │ • 38 pages use standard Brinell controls only    │
│                   │ • 12 pages have custom widget patterns            │
│                   │ • Locator strategy: aria-label (72%), id (18%),   │
│                   │   text (10%)                                      │
│                   │                                                   │
│                   │ [✅ Approve All] [Generate Controls] [Re-analyze] │
└──────────────────┴───────────────────────────────────────────────────┘
```

### Analysis Actions

| Button | Action |
|--------|--------|
| Preview Code | Opens code preview panel with generated ControlObject class |
| ✅ Approve | Mark control for generation |
| ❌ Reject | Skip this control (use standard Brinell controls instead) |
| Approve All | Approve all proposed controls |
| Generate Controls | Generate approved custom control classes |
| Re-analyze | Run analysis again (e.g. after recording more pages) |

---

## UI Design — Custom Controls Manager

After approving and generating controls, or via `Site → Manage Controls`. Shows all custom controls for the active site.

```
┌──────────────────┬──────────────────────────┬────────────────────────┐
│ 📁 Exact Online  │  Custom Controls          │  Code Preview          │
│ ─────────────── │                           │                         │
│ Corpus: 50 pages │  ✅ DatePickerControl     │  // DatePickerControl  │
│ Controls: 5      │     8 pages │ 94% conf   │                         │
│ Generated: 42/50 │     Created: Apr 18       │  1 │ namespace Exact..  │
│                   │     ◄ selected             │  2 │                    │
│ ── Pages ──────  │                           │  3 │ public sealed ..  │
│ ✅ LoginPage     │  ✅ AutocompleteControl   │  4 │   DatePickerCon..  │
│ ✅ Dashboard     │     12 pages │ 89% conf   │  5 │   : ContainerBa..  │
│                   │                           │ ...                     │
│ ── Controls ──── │  ⏳ FileUploadControl     │                         │
│ ✅ DatePicker ◄  │     Approved, not yet gen │  ✅ Roslyn: No errors  │
│ ✅ Autocomplete  │                           │                         │
│ ✅ DataGrid      │  [Generate Pending]       │  [📋 Copy] [✏️ Edit]  │
│ ⏳ FileUpload    │  [+ Manual Control]       │  [💾 Save to Project] │
└──────────────────┴──────────────────────────┴────────────────────────┘
```

### Control Actions

| Button | Action |
|--------|--------|
| Generate Pending | Generate all approved but not yet generated controls |
| + Manual Control | Create a custom control manually (power user) |
| ✏️ Edit | Open control code in editable AvalonEdit mode |
| 🔄 Regenerate | Re-generate this control from latest corpus patterns |

---

## Acceptance Criteria

- [ ] Copilot SDK client initializes with valid GitHub auth token and creates a session with agents, tools, and skills
- [ ] Two agents are configured: analyzer (cheaper model) and generator (Claude Opus 4.6 / GPT 5.4)
- [ ] Custom tools (`search_corpus`, `get_page_snapshot`, `find_similar_elements`, `get_generated_controls`, `list_recorded_pages`) are registered and callable by the LLM
- [ ] Skills load from `./corpus/skills/` — `brinell-conventions` at minimum
- [ ] Analyzer agent queries the corpus and produces structured analysis output (proposed controls with confidence %)
- [ ] Analysis results are presentable in the Analysis View for user approval
- [ ] Generator agent produces valid `ContainerBase<TParent, TScope>` classes for approved custom controls
- [ ] Generated custom controls are stored in the SQLite control registry
- [ ] A `{site}-controls` SKILL.md is auto-generated after custom control generation
- [ ] Generator agent produces valid PageObject classes that reference custom controls when DOM patterns match
- [ ] Locator preference order is: ByText/ByLinkText → ByDataTestId → ByAriaLabel → ById → ByCss (with warning)
- [ ] Code block parser extracts all C# blocks from LLM response (both ControlObject and PageObject)
- [ ] Roslyn validation catches syntax errors and reports line/column
- [ ] Control type validation uses dynamic registry (built-in + site custom controls)
- [ ] Auto-retry on validation failure re-prompts the LLM and produces corrected code
- [ ] Full page DOMs fit in context without chunking (200K+ token windows)

## Dependencies

- **Phase 4** — DOM inspection and element selection must be complete
- **GitHub.Copilot.SDK** — NuGet package from https://github.com/github/copilot-sdk
- **Microsoft.CodeAnalysis.CSharp** — Roslyn for syntax validation (also used in Phase 6)
- **Microsoft.Data.Sqlite** — corpus queries from custom tools and control registry
- **GitHub authentication** — user must have a valid GitHub token with Copilot access
- **System.Text.Json** — for DOM element serialization and analysis output parsing

---

## Unit Test Plan

### Testable Components (~35 tests)

| Component | Tests | Strategy |
|-----------|-------|---------|
| Corpus query tools | 8 | `search_corpus`, `get_page_snapshot`, `find_similar_elements`, `list_recorded_pages` return correct data from in-memory SQLite |
| Code block parser | 6 | Extract C# from markdown fences, multiple blocks, empty response, malformed input |
| Roslyn syntax validator | 5 | Valid code passes, syntax errors detected with line/col, missing braces, empty input |
| Control type validator | 4 | Built-in types recognized, custom types from registry, unknown types flagged |
| Locator preference logic | 5 | Priority order: text > data-testid > aria-label > id > CSS, warning on CSS fallback |
| Analysis result parser | 4 | Pattern proposals, control suggestions, confidence scores, malformed output |
| Auto-retry logic | 3 | Retry on validation failure, max retries respected, corrected code accepted |

### Not Unit-Tested

- `CopilotClient` / SDK session management — requires live GitHub auth
- Actual LLM responses — non-deterministic; verified by integration tests
- Prompt construction from SKILL.md files — verified by inspection

### Test Infrastructure

- **Database:** In-memory SQLite with pre-seeded corpus data
- **Mocking:** `ICopilotClient` mocked to return canned LLM responses
- **Test data:** Sample DOM snapshots + expected generated code pairs
