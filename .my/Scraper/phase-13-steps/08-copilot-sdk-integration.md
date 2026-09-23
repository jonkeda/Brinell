# Step 13.8 — Copilot SDK Integration & Custom Tools

## Objective

Finalize the `ICopilotService` implementation and the five custom corpus query tools used by the analyzer agent. These were sketched in Phase 5 (steps 5.1, 5.3) — Phase 13 hardens them and wires them into the pipeline.

## Dependencies

- Phase 5.1 (initial `CopilotService` skeleton)
- Phase 5.3 (initial `CorpusTools` sketch)
- Step 13.1, 13.4 (consumers)

## Implementation

### Files

- Update: `Services/CopilotService.cs`
- Update: `Services/CorpusTools.cs`
- New: `Services/CopilotAuthService.cs` (if not present)

### Tools to implement

| Tool name | Purpose | Inputs | Output |
|---|---|---|---|
| `list_recorded_pages` | Enumerate all pages for current site | (none, site comes from session context) | `[{ pageId, pageName, pageUrl, elementCount, lastCapturedAt }]` |
| `get_page_snapshot` | Full DOM tree for a page | `pageId` (or `pageName`) | `{ pageUrl, elements: [...] }` |
| `find_similar_elements` | Query elements by CSS selector across all pages | `selector` | `[{ pageId, xpath, html }]` |
| `get_generated_controls` | List existing ControlObjects with signatures | (none) | `[{ name, domSignature, properties: [...] }]` |
| `search_corpus` | Full-text search across page elements | `query` | `[{ pageId, xpath, snippet }]` |

### Tool implementation pattern

```csharp
public static class CorpusTools
{
    public static CustomTool ListRecordedPages(CorpusService corpus, ISessionContext ctx) =>
        new()
        {
            Name = "list_recorded_pages",
            Description = "List all pages recorded for the current site, with element counts.",
            ParametersSchema = "{}",
            Handler = async (_, ct) =>
            {
                var siteId = ctx.CurrentSiteId ?? throw new InvalidOperationException("No site");
                var pages = await corpus.GetPageSummariesAsync(siteId, ct);
                return JsonSerializer.Serialize(pages);
            }
        };

    public static CustomTool GetPageSnapshot(CorpusService corpus, ISessionContext ctx) => ...;
    public static CustomTool FindSimilarElements(CorpusService corpus, ISessionContext ctx) => ...;
    public static CustomTool GetGeneratedControls(IControlRegistry registry, ISessionContext ctx) => ...;
    public static CustomTool SearchCorpus(CorpusService corpus, ISessionContext ctx) => ...;
}
```

### Session context

```csharp
public interface ISessionContext
{
    long? CurrentSiteId { get; set; }
    string? CurrentSiteSlug { get; set; }
}
```

`ISessionContext` is a scoped service updated by the workspace shell (Step 12.9) when the user opens a site. Tools read from it so the LLM doesn't need to pass `siteId` on every call.

### Updated `CopilotService.InitializeAsync`

```csharp
public async Task InitializeAsync(long siteId, string siteSlug, CancellationToken ct = default)
{
    _sessionContext.CurrentSiteId = siteId;
    _sessionContext.CurrentSiteSlug = siteSlug;
    var token = await _auth.GetTokenAsync();
    _client = new CopilotClient(new CopilotClientOptions { AuthToken = token });
    _session = await _client.CreateSessionAsync(new SessionConfig
    {
        SkillDirectories = new[] { _settings.SkillsRoot },
        CustomAgents = new[]
        {
            new AgentConfig { Name = "analyzer", Model = _settings.AnalyzerModel,
                Skills = new[] { "brinell-conventions" }, ... },
            new AgentConfig { Name = "generator", Model = _settings.GeneratorModel,
                Skills = new[] { "brinell-conventions", $"{siteSlug}-controls" }, ... }
        },
        CustomTools = new[]
        {
            CorpusTools.ListRecordedPages(_corpus, _sessionContext),
            CorpusTools.GetPageSnapshot(_corpus, _sessionContext),
            CorpusTools.FindSimilarElements(_corpus, _sessionContext),
            CorpusTools.GetGeneratedControls(_registry, _sessionContext),
            CorpusTools.SearchCorpus(_corpus, _sessionContext),
        }
    }, ct);
}
```

### Auth

`CopilotAuthService.GetTokenAsync`:

1. Read token from Windows Credential Manager (target `Brinell.Scraper:GitHub`).
2. If missing or 401 on use, raise `AuthRequired` event.
3. Settings tab → "Sign in to GitHub" handles event with OAuth or PAT entry, then stores via Credential Manager.

### Session lifecycle

| Trigger | Action |
|---|---|
| Workspace opened for site | `InitializeAsync(siteId, slug)` — recreate session |
| Workspace closed (back to Start) | Dispose session |
| Site renamed | Reinitialize (skill name changes) |
| Skill files regenerated | No reinit needed — Copilot SDK reloads skill dir on next call |

### DI registration

```csharp
services.AddSingleton<ISessionContext, SessionContext>();
services.AddSingleton<ICopilotAuthService, CopilotAuthService>();
services.AddSingleton<ICopilotService, CopilotService>();
```

## Checklist

- [ ] `ISessionContext` scoped service introduced; updated by workspace shell
- [ ] All five tools implemented and return JSON-serializable results
- [ ] Tools read site from session context, not from LLM-supplied parameters
- [ ] `CopilotService.InitializeAsync` takes siteId+slug and registers per-site generator skill
- [ ] Auth via Windows Credential Manager; auth-required event surfaces in Settings tab
- [ ] Session disposed cleanly on Back-to-Start
- [ ] Logging includes tool name, elapsed time, result size
