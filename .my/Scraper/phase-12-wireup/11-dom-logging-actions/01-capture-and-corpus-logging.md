# Step 12.W.11 — Wire DOM Capture & Corpus Lifecycle Logging

## Objective

Wire structured logging into the DOM capture and corpus storage paths so that every capture is logged with timing/size metrics, and corpus open/create/store events are tracked.

## Dependencies

- `ILogger<DomCaptureService>` — injected logger
- `ILogger<CorpusService>` — injected logger
- `Stopwatch` for elapsed timing
- Logging framework from phase 3

## Implementation

### Files

| File | Action |
|------|--------|
| `Services/DomCaptureService.cs` | Add logging around `CaptureAsync` |
| `Services/CorpusService.cs` | Add logging in `StoreSnapshotAsync`, `CreateSiteAsync`, site open |

### Code sketch

**DomCaptureService.cs:**

```csharp
public async Task<DomSnapshot> CaptureAsync(CoreWebView2 webView)
{
    var sw = Stopwatch.StartNew();

    var json = await webView.ExecuteScriptAsync(DomCaptureScript);
    var snapshot = JsonSerializer.Deserialize<DomSnapshot>(json, _jsonOptions)!;

    sw.Stop();
    var elementCount = CountElements(snapshot.RootElement);
    var sizeBytes = Encoding.UTF8.GetByteCount(json);

    _logger.LogInformation(
        "DOM capture — URL: {Url}, Elements: {ElementCount}, Size: {SnapshotSizeBytes} bytes, Elapsed: {ElapsedMs} ms",
        snapshot.PageUrl, elementCount, sizeBytes, sw.ElapsedMilliseconds);

    return snapshot;
}
```

Error path:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "DOM capture failed — URL: {Url}", currentUrl);
    throw;
}
```

**CorpusService.cs — StoreSnapshotAsync:**

```csharp
public async Task StoreSnapshotAsync(string siteId, DomSnapshot snapshot)
{
    var isNewPage = !await PageExistsAsync(siteId, snapshot.PageName);

    // ... store logic ...

    var totalPages = await GetPageCountAsync(siteId);
    var totalElements = await GetTotalElementCountAsync(siteId);

    _logger.LogInformation(
        "Corpus store — Site: {SiteName}, Page: {PageName}, IsNew: {IsNewPage}, " +
        "Corpus total pages: {TotalPages}, Corpus total elements: {TotalElements}",
        snapshot.SiteName, snapshot.PageName, isNewPage, totalPages, totalElements);
}
```

**CorpusService.cs — site lifecycle:**

```csharp
public async Task<SiteInfo> CreateSiteAsync(string name, string startUrl, string ns, string outputPath)
{
    // ... create logic ...

    _logger.LogInformation("Corpus created — Site: {SiteName}, StartUrl: {StartUrl}", name, startUrl);
    return site;
}

public async Task<SiteInfo?> OpenSiteAsync(string siteId)
{
    var site = await GetSiteAsync(siteId);
    if (site is null) return null;

    var pageCount = await GetPageCountAsync(siteId);
    var lastRecorded = await GetLastRecordedDateAsync(siteId);

    _logger.LogInformation(
        "Corpus opened — Site: {SiteName}, Pages: {PageCount}, Last recorded: {LastRecordingDate}",
        site.Name, pageCount, lastRecorded);

    return site;
}
```

### Log levels

| Level | Usage |
|-------|-------|
| `Information` | Every capture, store, open/create event |
| `Debug` | Individual element details during inspection/selection |
| `Error` | Capture failures, deserialization errors |

## Checklist

- [ ] Every DOM capture logged with URL, element count, size, elapsed time
- [ ] Corpus storage logged with site name, page name, new vs re-recorded
- [ ] Corpus open/create logged with site name, page count, last recording date
- [ ] Capture failures logged at Error level with exception context
- [ ] Debug-level element details available for inspector selection
