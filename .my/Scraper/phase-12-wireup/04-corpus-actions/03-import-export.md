# Step 12.W.4c — Import & Export

## Objective

Wire the `Export()`, `ExportPage()`, and `Import()` stubs in `CorpusTabViewModel` to serialize/deserialize the corpus (or a single page) as a versioned JSON file, using `SaveFileDialog` / `OpenFileDialog` for file selection and `CorpusService` for data access. Large imports display progress feedback.

## Dependencies

- `CorpusService.ListSnapshots(siteId)` — returns all snapshots for a site
- `CorpusService.GetSnapshotById(id)` — returns full snapshot including HTML
- `CorpusService.AddSnapshot(siteId, pageName, pageUrl, html, capturedAt)` — inserts a snapshot
- `CorpusTabViewModel.Pages` — current page collection
- `CorpusTabViewModel.SelectedPage` — currently selected page
- `CorpusTabViewModel._siteId` — current site identifier
- WPF `Microsoft.Win32.SaveFileDialog` / `OpenFileDialog`

## Implementation

### Files

| File | Action |
|------|--------|
| `CorpusTabViewModel.cs` | Implement `Export()`, `ExportPage()`, `Import()` |
| `CorpusExportModel.cs` | New — POCO for JSON schema |

### Code sketch

**CorpusExportModel.cs** — JSON schema:

```csharp
public sealed class CorpusExportModel
{
    public int Version { get; init; } = 1;
    public string SiteId { get; init; } = string.Empty;
    public DateTime ExportedAt { get; init; }
    public List<ExportedPage> Pages { get; init; } = [];
}

public sealed class ExportedPage
{
    public string PageName { get; init; } = string.Empty;
    public string PageUrl { get; init; } = string.Empty;
    public List<ExportedSnapshot> Snapshots { get; init; } = [];
}

public sealed class ExportedSnapshot
{
    public string Id { get; init; } = string.Empty;
    public DateTime CapturedAt { get; init; }
    public string Html { get; init; } = string.Empty;
    public int ElementCount { get; init; }
}
```

**CorpusTabViewModel.cs** — `Export()`:

```csharp
private void Export()
{
    var dialog = new SaveFileDialog
    {
        Filter = "JSON files (*.json)|*.json",
        FileName = $"{_siteId}-corpus-export.json"
    };

    if (dialog.ShowDialog() != true) return;

    var model = BuildExportModel(Pages);
    var json = JsonSerializer.Serialize(model, _jsonOptions);
    File.WriteAllText(dialog.FileName, json);

    _logger.LogInformation("Exported {Count} pages to {Path}.", model.Pages.Count, dialog.FileName);
}
```

**CorpusTabViewModel.cs** — `ExportPage()`:

```csharp
private void ExportPage()
{
    if (SelectedPage is null) return;

    var dialog = new SaveFileDialog
    {
        Filter = "JSON files (*.json)|*.json",
        FileName = $"{SelectedPage.PageName}-export.json"
    };

    if (dialog.ShowDialog() != true) return;

    var model = BuildExportModel(new[] { SelectedPage });
    var json = JsonSerializer.Serialize(model, _jsonOptions);
    File.WriteAllText(dialog.FileName, json);

    _logger.LogInformation("Exported page '{Page}' to {Path}.", SelectedPage.PageName, dialog.FileName);
}
```

**CorpusTabViewModel.cs** — shared builder:

```csharp
private CorpusExportModel BuildExportModel(IEnumerable<PageItemViewModel> pages)
{
    var exportedPages = new List<ExportedPage>();

    foreach (var page in pages)
    {
        var snapshots = _corpusService.ListSnapshots(_siteId)
            .Where(s => s.PageName == page.PageName)
            .Select(s =>
            {
                var full = _corpusService.GetSnapshotById(s.Id);
                return new ExportedSnapshot
                {
                    Id = s.Id,
                    CapturedAt = full.CapturedAt,
                    Html = full.Html,
                    ElementCount = full.ElementCount
                };
            })
            .ToList();

        exportedPages.Add(new ExportedPage
        {
            PageName = page.PageName,
            PageUrl = page.PageUrl,
            Snapshots = snapshots
        });
    }

    return new CorpusExportModel
    {
        SiteId = _siteId,
        ExportedAt = DateTime.UtcNow,
        Pages = exportedPages
    };
}
```

**CorpusTabViewModel.cs** — `Import()`:

```csharp
private async Task Import()
{
    var dialog = new OpenFileDialog
    {
        Filter = "JSON files (*.json)|*.json"
    };

    if (dialog.ShowDialog() != true) return;

    var json = await File.ReadAllTextAsync(dialog.FileName);
    var model = JsonSerializer.Deserialize<CorpusExportModel>(json, _jsonOptions);

    if (model is null || model.Version != 1)
    {
        _logger.LogWarning("Unsupported or invalid export file.");
        return;
    }

    IsBusy = true;
    var total = model.Pages.Sum(p => p.Snapshots.Count);
    var current = 0;

    try
    {
        foreach (var page in model.Pages)
        {
            foreach (var snapshot in page.Snapshots)
            {
                _corpusService.AddSnapshot(
                    _siteId,
                    page.PageName,
                    page.PageUrl,
                    snapshot.Html,
                    snapshot.CapturedAt);

                current++;
                ImportProgress = (double)current / total * 100;
            }
        }

        _logger.LogInformation("Imported {Count} snapshots from {Path}.", total, dialog.FileName);

        // Reload page list
        await LoadPagesAsync(CancellationToken.None);
    }
    finally
    {
        IsBusy = false;
        ImportProgress = 0;
    }
}
```

**CorpusTabViewModel.cs** — progress property:

```csharp
private double _importProgress;
public double ImportProgress
{
    get => _importProgress;
    private set => SetProperty(ref _importProgress, value);
}
```

**JSON options (field)**:

```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

### Behavior

- **Export**: Opens `SaveFileDialog`, serializes all pages + their full snapshot HTML to a single JSON file. File name defaults to `{siteId}-corpus-export.json`.
- **ExportPage**: Same as Export but only for `SelectedPage`. No-op if nothing selected.
- **Import**: Opens `OpenFileDialog`, deserializes JSON, validates version field. For each snapshot in the file, calls `AddSnapshot`. Progress is reported via `ImportProgress` (0–100). After completion, reloads the page list. `IsBusy` is true for the duration.
- Unsupported version numbers are rejected with a log warning.
- Large exports/imports operate synchronously on the corpus service (assumed fast for local storage); progress is for UI feedback only.

## Checklist

- [ ] `CorpusExportModel.cs` created with `Version`, `SiteId`, `ExportedAt`, `Pages` structure
- [ ] `Export()` uses `SaveFileDialog`, serializes all pages via `BuildExportModel`
- [ ] `ExportPage()` serializes only `SelectedPage`; no-op when null
- [ ] `Import()` uses `OpenFileDialog`, validates version, imports all snapshots
- [ ] `ImportProgress` property (0–100) bound to progress bar in UI
- [ ] `IsBusy = true` during import
- [ ] Page list reloaded after import completes
- [ ] `_jsonOptions` uses `WriteIndented` + `CamelCase`
- [ ] Invalid/unsupported files logged and rejected gracefully
