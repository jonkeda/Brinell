# Research: Copilot SDK as a Knowledge Base for DOM Corpus

## Question

Can the GitHub Copilot SDK be used to store, index, and retrieve DOM snapshots as a knowledge base — similar to how VS Code's workspace indexing gives Copilot context about your codebase?

---

## What the Copilot SDK Actually Is

Based on research of https://github.com/github/copilot-sdk (v0.2.2, April 2026):

The Copilot SDK is an **agent runtime**, not a raw LLM API. It wraps the Copilot CLI and provides:

| Feature | Description |
|---------|-------------|
| **Chat sessions** | Send prompts, receive streaming responses |
| **Custom agents** | Define sub-agents with scoped tools and prompts |
| **Custom tools** | Define tools the LLM can invoke (file read, search, etc.) |
| **Skills** | Reusable prompt modules loaded from `SKILL.md` files on disk |
| **MCP servers** | Integrate Model Context Protocol servers for external data |
| **Hooks** | Intercept tool calls, modify prompts, handle errors |
| **Session persistence** | Resume sessions across restarts |
| **BYOK** | Bring your own API keys (OpenAI, Azure, Anthropic) |

### What it does NOT provide (as of v0.2.2)

- **No embeddings API** — the SDK does not expose vector embedding generation
- **No built-in vector store** — no RAG infrastructure included
- **No document indexing** — unlike VS Code's workspace indexing, the SDK doesn't index files itself

---

## How to Build a Knowledge Base Anyway

The SDK doesn't do indexing, but it provides the building blocks. Here are the viable approaches, from simplest to most powerful:

### Approach 1: Skills as Static Knowledge (Simplest)

**How:** Write DOM patterns and Brinell conventions as `SKILL.md` files. The SDK loads them into the agent's context automatically.

```
corpus/
├── skills/
│   ├── brinell-conventions/
│   │   └── SKILL.md          ← Full Brinell control hierarchy, locator strategies, examples
│   ├── exact-online-patterns/
│   │   └── SKILL.md          ← "This site uses .btn-primary as buttons, .form-group as form fields..."
│   ├── exact-online-controls/
│   │   └── SKILL.md          ← Previously generated custom controls (DatePickerControl, etc.)
│   └── site-specific-locators/
│       └── SKILL.md          ← "Prefer aria-label on this site, avoid data-reactid attributes"
```

```csharp
// .NET SDK usage
var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "claude-opus-4-6",
    SkillDirectories = new[] { "./corpus/skills" },
    CustomAgents = new[]
    {
        new AgentConfig
        {
            Name = "analyzer",
            Description = "Analyzes DOM patterns across pages",
            Prompt = "You analyze DOM snapshots to identify reusable UI patterns.",
            Skills = new[] { "brinell-conventions", "exact-online-patterns" }
        },
        new AgentConfig
        {
            Name = "generator",
            Description = "Generates Brinell PageObject code",
            Prompt = "You generate C# PageObject classes for the Brinell framework.",
            Skills = new[] { "brinell-conventions", "exact-online-controls" }
        }
    }
});
```

**Pros:**
- Zero infrastructure — just markdown files on disk
- Skills are eagerly loaded into context (no retrieval step)
- Easy to update — edit the SKILL.md, restart session

**Cons:**
- Static — doesn't automatically learn from new recordings
- All skills load into context at once — could waste tokens on irrelevant patterns
- Manual maintenance — someone has to write/update the SKILL.md files

**Verdict:** Good starting point. Use this for Brinell conventions and stable site patterns.

---

### Approach 2: Custom Tools for Corpus Query (Recommended)

**How:** Define custom tools that let the LLM query the DOM corpus stored in SQLite. The LLM decides what to query based on the task.

```csharp
// Register custom tools that the LLM can call
var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "claude-opus-4-6",
    CustomTools = new[]
    {
        new ToolConfig
        {
            Name = "search_corpus",
            Description = "Search recorded DOM snapshots by CSS selector pattern, element tag, or attribute",
            Parameters = new
            {
                query = "CSS selector or element pattern to search for",
                site = "Site name to search within"
            },
            Handler = async (args) =>
            {
                // Query SQLite corpus
                var results = await corpusDb.SearchElements(args.query, args.site);
                return JsonSerializer.Serialize(results);
            }
        },
        new ToolConfig
        {
            Name = "get_page_snapshot",
            Description = "Get the full DOM snapshot for a specific recorded page",
            Parameters = new { pageId = "ID or URL of the recorded page" },
            Handler = async (args) =>
            {
                var snapshot = await corpusDb.GetSnapshot(args.pageId);
                return snapshot.ToJson();
            }
        },
        new ToolConfig
        {
            Name = "list_recorded_pages",
            Description = "List all recorded pages for a site with their URLs and timestamps",
            Parameters = new { site = "Site name" },
            Handler = async (args) =>
            {
                var pages = await corpusDb.ListPages(args.site);
                return JsonSerializer.Serialize(pages);
            }
        },
        new ToolConfig
        {
            Name = "find_similar_elements",
            Description = "Find elements across all pages that have similar structure to a given element",
            Parameters = new { elementHtml = "HTML snippet of the element to match" },
            Handler = async (args) =>
            {
                var similar = await corpusDb.FindSimilar(args.elementHtml);
                return JsonSerializer.Serialize(similar);
            }
        },
        new ToolConfig
        {
            Name = "get_generated_controls",
            Description = "List all previously generated custom ControlObject classes for a site",
            Parameters = new { site = "Site name" },
            Handler = async (args) =>
            {
                var controls = await controlRegistry.GetControls(args.site);
                return JsonSerializer.Serialize(controls);
            }
        }
    }
});
```

**How the LLM uses it:**

1. User says: "Generate a PageObject for this login page"
2. LLM calls `get_generated_controls("exact-online")` → sees `DatePickerControl`, `AutocompleteControl` already exist
3. LLM calls `search_corpus("input[type=password]", "exact-online")` → sees password fields across 5 pages all use `aria-label`
4. LLM calls `find_similar_elements("<div class='date-picker'>...")` → confirms this is the known DatePickerControl pattern
5. LLM generates the PageObject using existing custom controls and site-consistent locator strategies

**Pros:**
- Dynamic — LLM queries what it needs, doesn't load everything
- Scales to large corpuses — only relevant data enters the context
- LLM decides what's relevant (it's good at this)
- Corpus grows automatically from recordings

**Cons:**
- Need to build the corpus SQLite database and query layer
- LLM may make suboptimal queries (mitigatable with good tool descriptions)
- Each tool call = additional LLM turn (slightly slower)

**Verdict:** This is the recommended approach. Build it.

---

### Approach 3: MCP Server for Corpus (Most Extensible)

**How:** Build a Model Context Protocol server that serves the DOM corpus. The Copilot SDK has built-in MCP support.

```csharp
var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "claude-opus-4-6",
    McpServers = new Dictionary<string, McpServerConfig>
    {
        ["dom-corpus"] = new McpServerConfig
        {
            Command = "dotnet",
            Args = new[] { "run", "--project", "./Brinell.Scraper.McpServer" },
            Tools = new[] { "*" }
        }
    }
});
```

The MCP server exposes the same tools as Approach 2 but as a separate process — reusable across multiple SDK clients, potentially shared across team members.

**Pros:**
- Fully decoupled — corpus server runs independently
- Shareable — multiple users/tools can query the same corpus
- Standard protocol — interoperable with other MCP clients

**Cons:**
- More infrastructure to build and maintain
- Overkill for a single-user desktop tool
- MCP is still relatively new

**Verdict:** Future option. Start with Approach 2 (custom tools), migrate to MCP if you need multi-user or cross-tool sharing.

---

### Approach 4: RAG with External Embeddings (Most Powerful, Most Complex)

**How:** Use an external embeddings API (OpenAI, Azure AI) to embed DOM element descriptions. Store vectors in a local DB. Retrieve relevant context before each LLM call.

```
Recording                    Indexing                      Generation
─────────                    ────────                      ──────────
DOM Snapshot    →    Element descriptions    →    Query: "elements similar
                     ↓                                to this date picker"
                     Embed via OpenAI               ↓
                     embeddings API             Vector search (top-K)
                     ↓                               ↓
                     Store in SQLite-vec         Include in LLM prompt
                     (vector extension)              ↓
                                                Generate PageObject
```

**Implementation:**
- Use `Microsoft.SemanticKernel` or direct OpenAI API for embeddings
- Store vectors in SQLite with the `sqlite-vec` extension
- For each element, embed: `"<input type='email' id='loginEmail' placeholder='Enter email' aria-label='Email address'> inside <form class='login-form'>"`
- At generation time: embed the current page's elements, find similar elements across corpus, include the matches + their previously generated controls in the prompt

**Pros:**
- True semantic search — finds similar elements even with different HTML structure
- Best quality — LLM gets the most relevant context
- Learns naming preferences, locator strategies from past generations

**Cons:**
- External API dependency for embeddings
- Additional cost per embedding call
- Complexity: vector store, embedding pipeline, retrieval logic
- Opus 4.6 / GPT 5.4 with 200K+ tokens may make this unnecessary — you can just send everything

**Verdict:** Probably overkill given large context windows. Only build this if the corpus grows to hundreds of pages per site and custom tool queries become insufficient.

---

## Recommended Architecture

```
┌───────────────────────────────────────────────────────┐
│              Brinell.Scraper                           │
├───────────────────────────────────────────────────────┤
│                                                       │
│  ┌─────────────┐    ┌──────────────────────────────┐ │
│  │   Corpus    │    │      Copilot SDK Session     │ │
│  │   (SQLite)  │◄──►│                              │ │
│  │             │    │  Skills:                      │ │
│  │  Snapshots  │    │   ├ brinell-conventions/     │ │
│  │  Elements   │    │   └ {site}-patterns/         │ │
│  │  Patterns   │    │                              │ │
│  │  Controls   │    │  Custom Tools:               │ │
│  │             │    │   ├ search_corpus()          │ │
│  └─────────────┘    │   ├ get_page_snapshot()      │ │
│                      │   ├ find_similar_elements()  │ │
│                      │   ├ get_generated_controls() │ │
│                      │   └ list_recorded_pages()    │ │
│                      │                              │ │
│                      │  Agents:                     │ │
│                      │   ├ analyzer (cheap model)   │ │
│                      │   └ generator (smart model)  │ │
│                      └──────────────────────────────┘ │
└───────────────────────────────────────────────────────┘
```

### Layer 1: Skills (static knowledge)
- Brinell framework conventions (always loaded)
- Site-specific patterns (updated after each analysis run)

### Layer 2: Custom Tools (dynamic queries)
- LLM queries the SQLite corpus on demand
- Returns only relevant data — efficient token usage
- Corpus grows with each recording session

### Layer 3: Agents (two-model strategy)
- **Analyzer agent** (cheaper model): pattern detection, element frequency, control proposals
- **Generator agent** (smart model): custom control classes, PageObject generation
- Runtime auto-delegates based on the task

---

## Implementation Plan

| Step | What | When |
|------|------|------|
| 1 | Write `brinell-conventions` SKILL.md | Phase 5 (LLM Generation) |
| 2 | Build SQLite corpus store | Phase 4 (DOM Inspection) |
| 3 | Implement custom tools (search, list, find) | Phase 5 (LLM Generation) |
| 4 | Define analyzer + generator agents | Phase 5 (LLM Generation) |
| 5 | Auto-generate site pattern SKILL.md after analysis | Phase 8 (Workflow) |
| 6 | Control registry (previously generated controls) | Phase 7 (Project Output) |
| 7 | Evaluate MCP server extraction | Phase 11 (Polish) |

---

## Copilot SDK .NET Quick Reference

```
Package: GitHub.Copilot.SDK
Install: dotnet add package GitHub.Copilot.SDK
Architecture: App → SDK Client → JSON-RPC → Copilot CLI (bundled)
Auth: GitHub OAuth / BYOK (OpenAI, Azure, Anthropic)
Models: All Copilot CLI models (runtime discoverable)
```

Key features for Scraper:
- **Custom Tools** — let LLM query our corpus DB
- **Skills** — inject Brinell conventions and site patterns as context
- **Custom Agents** — separate analyzer (cheap) from generator (smart)
- **Per-Agent Skills** — analyzer gets pattern skills, generator gets conventions
- **Hooks** — intercept tool calls for logging/metrics
- **Session Persistence** — resume analysis across app restarts
- **BYOK** — use Claude Opus 4.6 / GPT 5.4 directly via API keys

---

## Open Questions

- [ ] Does the Copilot SDK support selecting specific models per agent, or is the model session-wide?
- [ ] What's the token limit for skill content injection? (determines how much corpus context we can pre-load)
- [ ] Custom tool response size limits? (determines how much corpus data we can return per query)
- [ ] Can the SDK run headless / without interactive auth for automated test scenarios?
