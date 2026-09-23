# Step 12.W.4 — Import & Export

## Objective

Wire `Export` to serialize the current proposals and generated controls to a JSON file via `SaveFileDialog`, and wire `Import` to load a JSON file via `OpenFileDialog`, validate its schema, and merge the data into the current state.

## Dependencies

- `IControlRegistry.GetAllControls()` — for export
- `CorpusService.GetCurrentAnalysisResult(siteId)` — for export of proposals
- `CorpusService.StoreAnalysisResult(siteId, json, isCurrent)` — for import (persist merged proposals)
- `LoadControlObjects(siteId)` — reload after import
- WPF `Microsoft.Win32.SaveFileDialog` / `OpenFileDialog`

## Implementation

### Files

- **Modify**: `ControlObjectsTabViewModel.cs` — replace stub bodies for `Import()` and `Export()`
- **Create** (optional): `ControlObjectsExportModel.cs` — DTO for JSON schema

### Code sketch

```csharp
// ControlObjectsExportModel.cs

public sealed class ControlObjectsExportModel
{
    public int Version { get; set; } = 1;
    public string SiteId { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
    public List<ControlObjectProposal> Proposals { get; set; } = [];
    public List<GeneratedControlExport> GeneratedControls { get; set; } = [];
}

public sealed class GeneratedControlExport
{
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
}
```

```csharp
// ControlObjectsTabViewModel.cs

private void Export()
{
    var dialog = new SaveFileDialog
    {
        Filter = "JSON files (*.json)|*.json",
        DefaultExt = ".json",
        FileName = $"control-objects-{_siteId}-{DateTime.Now:yyyyMMdd}"
    };

    if (dialog.ShowDialog() != true)
        return;

    var generated = _controlRegistry.GetAllControls();
    var analysisResult = _corpusService.GetCurrentAnalysisResult(_siteId);

    var proposals = analysisResult?.Json is not null
        ? JsonSerializer.Deserialize<ControlObjectAnalysisResult>(analysisResult.Json)?.Proposals ?? []
        : [];

    var export = new ControlObjectsExportModel
    {
        Version = 1,
        SiteId = _siteId,
        ExportedAt = DateTime.UtcNow,
        Proposals = proposals,
        GeneratedControls = generated.Select(g => new GeneratedControlExport
        {
            Name = g.Name,
            ClassName = g.ClassName,
            SourceCode = g.SourceCode
        }).ToList()
    };

    var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(dialog.FileName, json);

    _logger.LogInformation("Exported {ProposalCount} proposals and {GenCount} controls to {Path}",
        export.Proposals.Count, export.GeneratedControls.Count, dialog.FileName);
    StatusMessage = $"Exported to {Path.GetFileName(dialog.FileName)}.";
}

private void Import()
{
    var dialog = new OpenFileDialog
    {
        Filter = "JSON files (*.json)|*.json",
        DefaultExt = ".json"
    };

    if (dialog.ShowDialog() != true)
        return;

    try
    {
        var json = File.ReadAllText(dialog.FileName);
        var import = JsonSerializer.Deserialize<ControlObjectsExportModel>(json);

        if (import is null)
        {
            StatusMessage = "Import failed: invalid JSON.";
            return;
        }

        if (import.Version != 1)
        {
            StatusMessage = $"Import failed: unsupported version {import.Version}.";
            _logger.LogWarning("Attempted import of unsupported version {Version}", import.Version);
            return;
        }

        // Store imported proposals as current analysis result
        var analysisJson = JsonSerializer.Serialize(new ControlObjectAnalysisResult
        {
            Proposals = import.Proposals
        });
        _corpusService.StoreAnalysisResult(_siteId, analysisJson, isCurrent: true);

        _logger.LogInformation("Imported {Count} proposals from {Path}",
            import.Proposals.Count, dialog.FileName);
        StatusMessage = $"Imported {import.Proposals.Count} proposals.";

        LoadControlObjects(_siteId);
    }
    catch (JsonException ex)
    {
        StatusMessage = "Import failed: malformed JSON file.";
        _logger.LogError(ex, "Failed to deserialize import file {Path}", dialog.FileName);
    }
}
```

### Behavior

- **Export**:
  - Opens native `SaveFileDialog` with `.json` filter and a default filename including site ID and date.
  - Serializes proposals (from corpus service) and generated controls (from registry) into a versioned JSON structure.
  - Writes indented JSON to selected path.
  - Shows filename in status message on success.
  - Does nothing if dialog is cancelled.

- **Import**:
  - Opens native `OpenFileDialog` with `.json` filter.
  - Reads and deserializes the file.
  - Validates: non-null result, `Version == 1`.
  - Stores imported proposals as the current analysis result (overwrites existing).
  - Reloads `LoadControlObjects` to reflect imported data.
  - Does NOT import generated controls into the registry automatically (proposals only — user must re-generate).
  - Shows error in status message for invalid JSON or unsupported version.
  - Does nothing if dialog is cancelled.

## Checklist

- [ ] `ControlObjectsExportModel` DTO created with `Version`, `SiteId`, `ExportedAt`, `Proposals`, `GeneratedControls`
- [ ] `Export` opens `SaveFileDialog` and writes JSON
- [ ] Export includes both proposals and generated controls
- [ ] `Import` opens `OpenFileDialog` and reads JSON
- [ ] Import validates version field
- [ ] Import calls `StoreAnalysisResult` with imported proposals
- [ ] Import reloads list via `LoadControlObjects`
- [ ] `JsonException` handled gracefully on import
- [ ] Null/empty file handled
- [ ] Dialog cancellation handled (early return)
