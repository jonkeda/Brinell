# Step 12.W.3 — Wire Delete and Export

## Objective

Wire the `Delete(item)` command to remove a page object from the corpus after user confirmation, and wire `Export()` to write all generated page objects to disk as `.cs` files via `CodeOutputService`, then open the output folder in Explorer.

## Dependencies

- `CorpusService.DeletePageObject(snapshotId)`
- `CodeOutputService.WriteProjectAsync(siteId, outputPath, ct)`
- `AppSettings.OutputPath` or per-site `SiteInfo.OutputPath`
- `System.Diagnostics.Process.Start` for opening folder
- `System.Windows.MessageBox` (or abstracted `IDialogService`) for delete confirmation
- `IsBusy` property (from spec 02)

## Implementation

### Files

| Action | Path |
|--------|------|
| Modify | `Brinell.Scraper/ViewModels/PageObjectsTabViewModel.cs` |

### Code sketch

**Delete:**

```csharp
[RelayCommand]
private void Delete(PageObjectListItem item)
{
    if (item is null) return;

    var result = MessageBox.Show(
        $"Delete page object for '{item.PageTitle}'?\nThis removes the generated code from the corpus.",
        "Confirm Delete",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    if (result != MessageBoxResult.Yes) return;

    _corpusService.DeletePageObject(item.SnapshotId);

    // Reset row to NotGenerated state
    item.Status = PageObjectStatus.NotGenerated;
    item.GeneratedAt = null;
    item.MainCode = null;
    item.UsedControlObjects = null;

    // Clear detail pane if this was the selected item
    if (SelectedPageObject == item)
    {
        ControlObjectReferences = new ObservableCollection<string>();
        OnPropertyChanged(nameof(SelectedPageObject));
    }

    StatusMessage = $"Deleted page object for '{item.PageTitle}'.";
}
```

**Export:**

```csharp
[RelayCommand(CanExecute = nameof(CanExport))]
private async Task ExportAsync(CancellationToken ct)
{
    IsBusy = true;
    StatusMessage = "Exporting page objects…";

    try
    {
        var outputPath = GetOutputPath();

        await _codeOutputService.WriteProjectAsync(_siteId, outputPath, ct);

        StatusMessage = $"Exported to {outputPath}";

        // Open folder in Explorer
        Process.Start(new ProcessStartInfo
        {
            FileName = outputPath,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        StatusMessage = $"Export failed: {ex.Message}";
    }
    finally
    {
        IsBusy = false;
    }
}

private bool CanExport() => !IsBusy && PageObjects.Any(p => p.Status == PageObjectStatus.Generated);

private string GetOutputPath()
{
    // Prefer per-site output path, fall back to global AppSettings
    var siteInfo = _corpusService.GetSiteInfo(_siteId);
    var path = siteInfo?.OutputPath ?? _appSettings.OutputPath;

    if (string.IsNullOrWhiteSpace(path))
        throw new InvalidOperationException("No output path configured. Set it in Settings or Site configuration.");

    return path;
}
```

### Behavior

- **Delete**: Shows a WPF `MessageBox` confirmation. On "Yes", calls `CorpusService.DeletePageObject`, then resets the row's status to `NotGenerated` and clears code/references in-place (no full reload needed). If the deleted item was selected, clears the detail pane.
- **Export**: Determines output path from site config or `AppSettings`. Calls `WriteProjectAsync` which writes `.cs` files to disk. On success, opens the output folder in Windows Explorer via `Process.Start` with `UseShellExecute = true`. On failure, surfaces the error in `StatusMessage`.
- **IsBusy guard on Export**: Prevents double-click launching multiple exports. The button disables while export is in progress.
- **CanExport**: Only enabled when at least one page object has `Generated` status (nothing to export otherwise).
- Delete does not require `IsBusy` since it is synchronous and fast.

## Checklist

- [ ] Replace `Delete` stub with confirmation dialog + corpus deletion + row reset
- [ ] Replace `Export` stub with `WriteProjectAsync` call + folder open
- [ ] Add `GetOutputPath()` helper resolving site-level or global output path
- [ ] Guard `ExportAsync` with `IsBusy`
- [ ] Wire `CanExport` to re-evaluate when any row status changes
- [ ] Verify `Process.Start` uses `UseShellExecute = true` (required on .NET 6+)
- [ ] Handle missing output path with clear error message
- [ ] Unit test: Delete resets row state correctly
- [ ] Unit test: Export throws when no output path configured
- [ ] Integration test: Export produces expected `.cs` files on disk
