# Discussion: Recording vs. Generation & Pattern-Aware Scraping

## Context

The current roadmap treats recording and code generation as tightly coupled — capture a page, immediately generate a PageObject. This discussion challenges that assumption and explores a more powerful architecture.

---

## 1. Separate Recording from Generation

**Your point:** Recording and generation should be decoupled.

**I agree strongly.** The current roadmap has recording triggering generation on each navigation (Phase 8.1). That's premature. Here's why:

- **Recording is cheap, generation is expensive.** Capturing DOM snapshots costs milliseconds. LLM generation costs seconds + tokens + money. You want to record liberally and generate selectively.
- **You can't generate well from a single page.** A login page in isolation tells you nothing about whether `div.btn-primary` is a pattern used across the entire site or a one-off. You need _corpus_ before you can make good decisions.
- **Recording sessions become a reusable asset.** A recorded session of Exact Online can be replayed, shared, diffed against future recordings. Generation is just one consumer of that data.

### Proposed Architecture Shift

```
Record (Phase A)          Analyze (Phase B)         Generate (Phase C)
─────────────────         ──────────────────        ─────────────────
Browse site           →   Pattern detection     →   PageObject output
Capture DOM snapshots     Control identification    ControlObject output
Store as corpus           Custom control suggest.   ContainerBase output
Tag & annotate            Cross-page analysis       Project scaffolding
```

Instead of: `navigate → capture → generate → next page`
Do: `navigate → capture → capture → capture → ... → analyze all → generate all`

This is a fundamentally different (and better) pipeline.

---

## 2. Corpus-Based Pattern Recognition

**Your point:** Recording a lot of HTML gives better understanding of patterns.

**This is the key insight.** A single page is ambiguous. 20 pages from the same site reveal the design system.

### What a corpus tells you

| Pattern | Single Page | 20-Page Corpus |
|---------|-------------|----------------|
| `div.btn` is a button | Maybe | Definitely — seen 47 times across 20 pages, always clickable |
| `div.form-group > label + input` | One form field | Standard form pattern — generate a `FormFieldContainer` |
| `table.data-grid` | One table | Consistent grid — generate a typed `DataGridControl` |
| `nav.sidebar > ul > li > a` | Navigation links | Site-wide nav — generate a `SidebarNavControl` |
| Custom date picker widget | Unknown div soup | Seen 8 times — it's a `DatePickerControl` |

### How to implement

- **Record** stores each page as a `DomSnapshot` in a local SQLite database (not just JSON files)
- **Corpus index:** element frequencies, attribute distributions, CSS class co-occurrences
- **Pattern detector:** runs across the full corpus, identifies:
  - Repeated component structures (same class/structure on 3+ pages = likely a reusable control)
  - Form patterns (label + input groups)
  - Navigation patterns (consistent menus/breadcrumbs)
  - Data display patterns (tables, lists, cards)
- Feed the pattern analysis to the LLM _alongside_ the page being generated — "this site uses `.btn-primary` as its standard button, seen 47 times"

---

## 3. Custom ControlObjects

**Your point:** Custom ControlObjects will be needed.

**Absolutely.** The current Brinell control hierarchy covers standard HTML elements (`ButtonControl`, `TextInputControl`, `SelectControl`, etc.) but real sites have composite widgets that don't map to a single HTML element:

### Examples from Exact Online / Synergy (likely)

| Site Widget | HTML Reality | Needed Control |
|-------------|-------------|----------------|
| Custom date picker | `div > input + button + dropdown calendar` | `ExactDatePickerControl` |
| Autocomplete search | `input + dropdown list + loading spinner` | `AutocompleteControl` |
| Editable data grid | `table + inline inputs + row actions` | `EditableGridControl` |
| Multi-step wizard | `div.steps > div.step-content` | `WizardControl` |
| Toast notification | Dynamically inserted `div.toast` | `ToastNotificationControl` |

### What this means for the tool

The Scraper shouldn't just generate PageObjects with existing controls — it should be able to **propose new ControlObject classes** when it detects composite widgets:

1. LLM sees a repeated complex structure that doesn't match any known Brinell control
2. LLM proposes: "This looks like a custom date picker. Should I generate a `DatePickerControl<TScope>` extending `ContainerBase<TParent, TScope>`?"
3. User approves → tool generates both the custom control class and uses it in the PageObject
4. Custom control gets added to the project's control library, reused across future page generations

This is where corpus analysis (point 2) feeds directly into control discovery.

---

## 4. Single-Page Application (SPA) Challenges

**Your point:** Modern SPA frameworks change the scraping game.

**Correct — and this is harder than it looks.** SPAs (React, Angular, Vue, Blazor) break several assumptions:

### SPA Problems

| Problem | Traditional Site | SPA |
|---------|-----------------|-----|
| Navigation | Full page load, new URL | DOM mutation, URL may not change |
| Page boundary | Clear — new HTML document | Unclear — same document, different content |
| When to capture | `NavigationCompleted` event | ??? |
| Element stability | Server-rendered, predictable | Client-rendered, dynamic IDs, virtual DOM |
| Loading state | Page load = done | Spinners, lazy loading, async data |

### SPA Recording Strategy

- **Don't rely on navigation events.** Instead, detect significant DOM mutations:
  - `MutationObserver` in injected JS watches for large DOM subtree changes
  - Threshold: if >30% of visible elements changed → likely a "page transition"
  - URL hash/path change + DOM mutation = definitely a new page
- **Wait for stable state:** after mutation detected, wait for:
  - No more mutations for 500ms
  - No pending XHR/fetch requests
  - No visible loading spinners (`[class*="loading"], [class*="spinner"]`)
- **User-triggered capture:** in addition to auto-detect, let the user manually trigger "Capture This State" for tricky SPAs

### Locator Implications

SPAs often generate dynamic IDs (`react-123`, `ng-scope-456`). The LLM + corpus analysis should:
- Avoid dynamic IDs as locators
- Prefer `data-testid`, `aria-label`, `role` attributes
- **Use labels and visible text as navigation anchors** — find the label "Email", then locate the adjacent input. This is how real users navigate a page and produces the most resilient locators
- Use Brinell's `Locator.ByText()`, `Locator.ByLinkText()`, `Locator.ByPartialLinkText()` for text-based locators
- Combine text + structural proximity: "the input next to the label 'Project Code'"
- Fall back to structural CSS selectors (`form > div:nth-child(2) > input`) only when no text/semantic locator is available
- Warn the user when no stable locator is available

---

## 5. LLM Indexing — Copilot as a Knowledge Base

**Your point:** GitHub Copilot might have ways to store and index pages, like it does with code in VS Code.

**This is an interesting direction.** Copilot's code indexing (workspace indexing) works by embedding code into a vector store and retrieving relevant context for each prompt. The same approach could work for DOM snapshots:

### How this could work

```
                    ┌─────────────────────────┐
                    │     Corpus Store         │
                    │  (SQLite + Embeddings)   │
                    ├─────────────────────────┤
  Record ──────────→│  DOM Snapshots          │
                    │  Element Catalog         │
                    │  Pattern Registry        │
                    │  Custom Control Defs     │
                    ├─────────────────────────┤
                    │  Vector Index            │
                    │  (element embeddings)    │
  Generate ────────→│                         │──→ LLM Prompt
                    │  RAG retrieval:          │    (with relevant
                    │  "find similar elements  │     context)
                    │   across all pages"      │
                    └─────────────────────────┘
```

### What gets indexed

1. **DOM elements** with their context (parent structure, siblings, attributes)
2. **Previously generated controls** — "last time we saw this pattern, we generated `DatePickerControl`"
3. **User corrections** — "user renamed `Div1` to `ProjectSelector`" → learn naming preferences
4. **Cross-page patterns** — "this sidebar appears on 18/20 pages" → shared component

### Copilot SDK possibilities

If the Copilot SDK supports:
- **Embeddings API** — embed DOM element descriptions, store in local vector DB
- **Chat with context** — send retrieved similar elements as context alongside the current page
- **Tool use** — let the LLM call back to query the corpus ("find all instances of `div.date-picker` across recorded pages")

This turns the Scraper from a single-page generator into a **site-aware code generator** that learns the target application's patterns over time.

### Practical first step

Even without embeddings, a simpler version works: store all generated controls in a registry, and include them in the system prompt as "known controls for this site." The LLM will naturally reuse them.

---

## 6. Model & Token Budget

**Your point:** Claude Opus 4.6 and GPT 5.4, with much larger token windows.

**This changes the game significantly.** With current models (late 2025 era), the DOM-too-large risk was real. With Opus 4.6 / GPT 5.4 token windows:

### What large context enables

| Approach | 8K tokens (old) | 200K+ tokens (new) |
|----------|-----------------|---------------------|
| DOM per page | Pruned to selected elements only | Full page DOM, maybe multiple pages |
| Cross-page context | Impossible | Send 5-10 pages at once for pattern analysis |
| System prompt | Minimal conventions | Full Brinell control hierarchy + examples + corpus patterns |
| Custom controls | One at a time | Generate entire control library in one pass |
| Conversation | Stateless per page | Multi-turn: "now look at page 2 and compare" |

### Revised strategy

- **Drop the chunking concern.** With 200K+ tokens, even large SPAs fit in a single prompt.
- **Send corpus context.** Include pattern analysis, previously generated controls, and multiple page snapshots in the same prompt.
- **Multi-page generation.** Instead of page-by-page, generate an entire site's page objects in one or few LLM calls:
  - "Here are 15 pages from Exact Online. Generate all PageObjects and any custom ControlObjects needed."
  - The LLM sees the full picture — consistent naming, shared controls, no duplicates.
- **Two-pass generation:**
  1. **Analysis pass:** "Here are 15 DOM snapshots. Identify repeated patterns, suggest custom controls, propose a control library."
  2. **Generation pass:** "Using these custom controls, generate PageObjects for all 15 pages."

### Cost consideration

More tokens = higher cost per call. But fewer calls overall (batch vs. page-by-page), and much better output quality. Net effect: likely cheaper _and_ better.

---

## Revised Pipeline Proposal

Based on all 6 points, here's what the pipeline should look like:

```
Phase A: Record                    Phase B: Analyze                Phase C: Generate
──────────────                     ────────────────                ─────────────────

1. Browse site in WebView2         1. Corpus pattern analysis      1. Generate custom controls
2. Auto-detect page transitions       - Element frequencies           (new ControlObject classes)
3. Capture full DOM snapshots         - Repeated structures        2. Generate PageObjects
4. Handle SPAs (mutation detect)      - CSS class distributions       (using custom + standard)
5. Store in SQLite corpus          2. Custom control proposals     3. Generate ContainerBase
6. Tag pages (login, dashboard,       - Composite widgets             for page regions
   time entry, etc.)                  - SPA components             4. Roslyn validate + format
7. Export/share corpus             3. LLM analysis pass            5. Write to standalone project
                                      - "Here are 15 pages,        6. dotnet build verify
                                         what patterns do 
                                         you see?"
                                   4. User review + approve
                                      custom controls
```

### Multiple Recording Runs

Recording is not a one-shot operation. The workflow is iterative:

1. **First run:** Record 10-20 pages to build initial corpus
2. **Analyze:** LLM identifies patterns, proposes custom controls
3. **Generate:** Create custom controls first, then PageObjects
4. **Second run:** Record more pages, or re-record after site changes
5. **Update:** Corpus grows, patterns refine, existing PageObjects get updated
6. **Repeat** as the target site evolves

The corpus is append-only by default — new recordings add to it, old snapshots remain for comparison. The tool should support:
- Adding new pages to an existing corpus
- Re-recording a known page (overwrite snapshot, keep history)
- Incremental generation — only generate/update PageObjects for new or changed pages
- Diff view: what changed since last recording?

### Impact on roadmap

This would restructure the phases:

| Current Phase | Change |
|---------------|--------|
| Phase 4 (DOM Inspection) | Split: **4A Recording**, **4B Corpus Management** |
| Phase 5 (LLM Generation) | Split: **5A Analysis Pass**, **5B Generation Pass** |
| Phase 7 (Project Output) | Add: custom control project output |
| Phase 8 (Workflow) | Absorbed into 4A (recording is the workflow) |

---

## Resolved Questions

- [x] Should the corpus be per-site or global? **Per-site** — patterns are site-specific. Note: some URLs share the same app (e.g. Exact Online NL, BE, DE) — support aliasing multiple URLs to one site corpus.
- [x] How to handle sites that change between recording sessions? **No versioning** — just continue adding to the corpus. Old snapshots stay for diffing but don't block new recordings.
- [x] Copilot SDK: does it support embeddings or just chat completions? **Needs investigation** — see [RESEARCH-copilot-sdk-knowledge-base.md](RESEARCH-copilot-sdk-knowledge-base.md)
- [x] Two-model strategy: use one model for analysis, another for generation? **Yes** — analysis/pattern detection can use a simpler/cheaper model; page generation is straightforward and can be simpler too. Custom control generation needs the heavyweight model.
- [x] Should custom controls be manually approved before use? **Yes** — custom control generation is a separate step that runs first, user approves, then PageObject generation uses the approved controls.
