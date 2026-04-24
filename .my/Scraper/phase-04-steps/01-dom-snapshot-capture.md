# Step 4.1 — Inject JS to Capture DOM Snapshot

## Objective

Inject JavaScript into WebView2 to capture a full DOM snapshot on demand — the foundation for inspection, recording, and analysis.

## Dependencies

- Phase 1 (WebView2 browser shell)
- Phase 3 (logging)

## Implementation

### DomElement model

```csharp
public sealed class DomElement
{
    public string Tag { get; init; } = "";
    public string? Id { get; init; }
    public string? ClassName { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public string? DataTestId { get; init; }
    public string? Role { get; init; }
    public string? AriaLabel { get; init; }
    public string? Placeholder { get; init; }
    public string? TextContent { get; init; }
    public BoundingBox? BoundingBox { get; init; }
    public List<DomElement> Children { get; init; } = [];
}

public sealed record BoundingBox(double X, double Y, double Width, double Height);
```

### DomSnapshot model

```csharp
public sealed class DomSnapshot
{
    public string SiteName { get; set; } = "";
    public string PageName { get; set; } = "";
    public string PageUrl { get; init; } = "";
    public string PageTitle { get; init; } = "";
    public DateTimeOffset CapturedAt { get; init; }
    public DomElement RootElement { get; init; } = new();
    public List<DomElement> SelectedElements { get; init; } = [];
}
```

### DomCaptureService

```csharp
public sealed class DomCaptureService
{
    private readonly ILogger<DomCaptureService> _logger;

    public async Task<DomSnapshot> CaptureAsync(CoreWebView2 webView)
    {
        var json = await webView.ExecuteScriptAsync(DomCaptureScript);
        return JsonSerializer.Deserialize<DomSnapshot>(json, _jsonOptions)!;
    }
}
```

### Injected JavaScript

Captures per element: `tag`, `id`, `class`, `name`, `type`, `data-testid`, `role`, `aria-label`, `aria-labelledby`, `aria-describedby`, `placeholder`, `value`, `href`, `src`, visible text content, bounding box (`getBoundingClientRect()`).

```javascript
(function() {
    function captureElement(el) {
        const rect = el.getBoundingClientRect();
        return {
            tag: el.tagName.toLowerCase(),
            id: el.id || null,
            className: el.className || null,
            name: el.getAttribute('name'),
            type: el.getAttribute('type'),
            dataTestId: el.getAttribute('data-testid'),
            role: el.getAttribute('role'),
            ariaLabel: el.getAttribute('aria-label'),
            placeholder: el.getAttribute('placeholder'),
            textContent: el.childNodes.length === 1 && el.childNodes[0].nodeType === 3
                ? el.textContent.trim().substring(0, 200) : null,
            boundingBox: { x: rect.x, y: rect.y, width: rect.width, height: rect.height },
            children: Array.from(el.children).map(captureElement)
        };
    }
    return JSON.stringify(captureElement(document.documentElement));
})();
```

## Checklist

- [ ] `DomElement` and `DomSnapshot` models created
- [ ] `DomCaptureService` registered in DI
- [ ] JS capture script extracts all specified attributes
- [ ] Bounding box captured for overlay positioning
- [ ] Text content truncated to 200 chars
- [ ] Capture completes in < 2 seconds for pages with up to 5,000 elements
