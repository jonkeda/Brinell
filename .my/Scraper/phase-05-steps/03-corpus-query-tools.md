# Step 5.3 — Corpus Query Tools

## Objective

Register custom tools with the Copilot SDK that allow the LLM to query the SQLite corpus on demand. The LLM never receives raw data inline — it calls tools to retrieve DOM snapshots, search elements, and inspect the control registry.

## Dependencies

- Step 5.1 (Copilot SDK session with `CustomTools`)
- Phase 4 (`CorpusService` with snapshots and element index)

## Implementation

### CorpusTools static class

```csharp
// Services/CorpusTools.cs
public static class CorpusTools
{
    private static CorpusService _corpusService = null!;
    private static IControlRegistry _controlRegistry = null!;

    public static void Initialize(CorpusService corpusService, IControlRegistry controlRegistry)
    {
        _corpusService = corpusService;
        _controlRegistry = controlRegistry;
    }

    // Tool definitions (registered with Copilot SDK)
    public static readonly ToolDefinition SearchCorpus = new()
    {
        Name = "search_corpus",
        Description = "Full-text search across all stored DOM snapshots. Returns matching elements with page context.",
        Parameters = new
        {
            query = new { type = "string", description = "Search query (tag name, attribute value, text content)" },
            tag = new { type = "string", description = "Optional: filter by HTML tag name", required = false },
            attribute = new { type = "string", description = "Optional: filter by attribute name", required = false }
        },
        Handler = SearchCorpusHandler
    };

    public static readonly ToolDefinition GetPageSnapshot = new()
    {
        Name = "get_page_snapshot",
        Description = "Retrieve the full DOM snapshot for a specific page by its snapshot ID.",
        Parameters = new
        {
            pageId = new { type = "integer", description = "The snapshot ID from list_recorded_pages" }
        },
        Handler = GetPageSnapshotHandler
    };

    public static readonly ToolDefinition FindSimilarElements = new()
    {
        Name = "find_similar_elements",
        Description = "Find elements matching a CSS-like pattern across all pages. Returns frequency and page list.",
        Parameters = new
        {
            selector = new { type = "string", description = "CSS selector or tag.class pattern to match" },
            minCount = new { type = "integer", description = "Minimum occurrences across pages (default: 2)", required = false }
        },
        Handler = FindSimilarElementsHandler
    };

    public static readonly ToolDefinition GetGeneratedControls = new()
    {
        Name = "get_generated_controls",
        Description = "List all previously generated custom controls from the site's control registry.",
        Parameters = new { },
        Handler = GetGeneratedControlsHandler
    };

    public static readonly ToolDefinition ListRecordedPages = new()
    {
        Name = "list_recorded_pages",
        Description = "List all pages in the corpus with URL, title, element counts, and snapshot IDs.",
        Parameters = new { },
        Handler = ListRecordedPagesHandler
    };
}
```

### Tool handlers

```csharp
// search_corpus handler
private static async Task<string> SearchCorpusHandler(ToolCallContext ctx)
{
    var query = ctx.GetString("query");
    var tag = ctx.GetStringOrNull("tag");
    var attribute = ctx.GetStringOrNull("attribute");

    var results = await _corpusService.SearchElementsAsync(
        _corpusService.ActiveSiteId, query);

    // Filter by tag/attribute if provided
    if (tag is not null)
        results = results.Where(e => e.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)).ToList();

    return FormatElementResults(results);
}

// get_page_snapshot handler
private static async Task<string> GetPageSnapshotHandler(ToolCallContext ctx)
{
    var pageId = ctx.GetInt("pageId");
    var snapshot = await _corpusService.GetSnapshotByIdAsync(pageId);

    if (snapshot is null)
        return "No snapshot found with that ID.";

    return FormatSnapshot(snapshot);
}

// find_similar_elements handler
private static async Task<string> FindSimilarElementsHandler(ToolCallContext ctx)
{
    var selector = ctx.GetString("selector");
    var minCount = ctx.GetIntOrDefault("minCount", 2);

    var results = await _corpusService.FindSimilarElementsAsync(
        _corpusService.ActiveSiteId, selector, minCount);

    return FormatSimilarElements(results);
}

// get_generated_controls handler
private static async Task<string> GetGeneratedControlsHandler(ToolCallContext ctx)
{
    var controls = await _controlRegistry.GetAllControlsAsync();

    if (controls.Count == 0)
        return "No custom controls have been generated yet.";

    var sb = new StringBuilder();
    foreach (var ctrl in controls)
    {
        sb.AppendLine($"## {ctrl.Name}");
        sb.AppendLine($"DOM signature: {ctrl.DomSignature}");
        sb.AppendLine($"Namespace: {ctrl.Namespace}");
        sb.AppendLine();
    }
    return sb.ToString();
}

// list_recorded_pages handler
private static async Task<string> ListRecordedPagesHandler(ToolCallContext ctx)
{
    var pages = await _corpusService.ListSnapshotsAsync(_corpusService.ActiveSiteId);

    var sb = new StringBuilder();
    sb.AppendLine("| ID | Page Name | URL | Elements | Captured |");
    sb.AppendLine("|---|---|---|---|---|");
    foreach (var p in pages)
    {
        sb.AppendLine($"| {p.Id} | {p.PageName} | {p.PageUrl} | {p.ElementCount} | {p.CapturedAt:yyyy-MM-dd HH:mm} |");
    }
    return sb.ToString();
}
```

### DOM element formatting

Tools return simplified HTML-like representations:

```csharp
private static string FormatSnapshot(DomSnapshot snapshot)
{
    var sb = new StringBuilder();
    sb.AppendLine($"Page URL: {snapshot.PageUrl}");
    sb.AppendLine($"Page Title: {snapshot.PageTitle}");
    sb.AppendLine($"Element count: {CountElements(snapshot.RootElement)}");
    sb.AppendLine();
    FormatElement(sb, snapshot.RootElement, indent: 0);
    return sb.ToString();
}

private static void FormatElement(StringBuilder sb, DomElement el, int indent)
{
    var pad = new string(' ', indent * 2);

    // Build opening tag with non-null attributes only
    sb.Append($"{pad}<{el.Tag}");
    if (el.Id is not null) sb.Append($" id=\"{el.Id}\"");
    if (el.ClassName is not null) sb.Append($" class=\"{el.ClassName}\"");
    if (el.Name is not null) sb.Append($" name=\"{el.Name}\"");
    if (el.Type is not null) sb.Append($" type=\"{el.Type}\"");
    if (el.DataTestId is not null) sb.Append($" data-testid=\"{el.DataTestId}\"");
    if (el.Role is not null) sb.Append($" role=\"{el.Role}\"");
    if (el.AriaLabel is not null) sb.Append($" aria-label=\"{el.AriaLabel}\"");
    if (el.Placeholder is not null) sb.Append($" placeholder=\"{el.Placeholder}\"");

    if (el.Children.Count == 0)
    {
        if (!string.IsNullOrWhiteSpace(el.TextContent))
            sb.AppendLine($">{el.TextContent}</{el.Tag}>");
        else
            sb.AppendLine(" />");
    }
    else
    {
        sb.AppendLine(">");
        if (!string.IsNullOrWhiteSpace(el.TextContent))
            sb.AppendLine($"{pad}  {el.TextContent}");
        foreach (var child in el.Children)
            FormatElement(sb, child, indent + 1);
        sb.AppendLine($"{pad}</{el.Tag}>");
    }
}
```

### Formatting rules

- Only include attributes that have values (skip null/empty)
- For elements with children, show nested structure with indentation
- Include visible text content as element body
- Strip inline styles and script-related attributes
- Include `<!-- N children omitted -->` for truncated subtrees (>50 children)

## Checklist

- [ ] `CorpusTools` static class with 5 tool definitions
- [ ] `search_corpus` — full-text search with optional tag/attribute filter
- [ ] `get_page_snapshot` — retrieves full DOM by snapshot ID, formatted as HTML-like text
- [ ] `find_similar_elements` — cross-page pattern matching with frequency count
- [ ] `get_generated_controls` — lists custom controls from registry
- [ ] `list_recorded_pages` — markdown table of all pages with IDs, URLs, element counts
- [ ] DOM elements formatted with non-null attributes only
- [ ] Large subtrees truncated with `<!-- N children omitted -->` comment
- [ ] `CorpusTools.Initialize()` called during app startup with service references
