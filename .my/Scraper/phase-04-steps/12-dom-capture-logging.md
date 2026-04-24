# Step 3.5 — DOM Capture Logging

## Objective

Log every DOM snapshot capture with element count, size, timing, and corpus storage details.

## Dependencies

- Step 3.1 (logging framework)

## Implementation

### Logger category

`"Brinell.Scraper.DomCapture"` or `ILogger<DomCaptureService>`

### Capture logging

```csharp
_logger.LogInformation(
    "DOM capture — URL: {Url}, Elements: {ElementCount}, " +
    "Size: {SnapshotSizeBytes} bytes, Elapsed: {ElapsedMs} ms",
    url, elementCount, snapshotSizeBytes, stopwatch.ElapsedMilliseconds);
```

### Corpus storage logging

```csharp
_logger.LogInformation(
    "Corpus store — Site: {SiteName}, Page: {PageName}, IsNew: {IsNewPage}, " +
    "Corpus total pages: {TotalPages}, Corpus total elements: {TotalElements}",
    siteName, pageName, isNewPage, totalPages, totalElements);
```

### Log fields

| Field | Description |
|-------|-------------|
| `Url` | Page URL at capture time |
| `ElementCount` | Total DOM elements in snapshot |
| `SnapshotSizeBytes` | Size of serialized DOM JSON |
| `ElapsedMs` | Time for JS DOM capture + transfer |
| `SiteName` | Which site corpus stored to |
| `IsNewPage` | New page vs re-recording |
| `TotalPages` | Corpus page count after storage |
| `TotalElements` | Corpus total element count after storage |

- `Debug` level: individual element details (tag, id, classes) for selected/inspected elements
- `Error` level: capture failure with URL and partial element count if available

## Checklist

- [ ] Every DOM capture logged with URL, element count, size, elapsed time
- [ ] Corpus storage logged with site name, page name, new vs re-recorded
- [ ] Capture failures logged with exception and partial context
- [ ] Debug-level element details work for inspector selection
