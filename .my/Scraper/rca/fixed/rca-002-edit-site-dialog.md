# RCA-002: Missing Edit Site Functionality

**Reported:** 2026-04-22
**Severity:** Medium
**Component:** `Views/SiteSelectionView.xaml`, `ViewModels/BrowserViewModel.cs` (SiteSelectionViewModel), `Data/CorpusDatabase.cs`

---

## Symptoms

There is no way to edit an existing site's properties (name, start URL, namespace, output path, URL aliases) after creation.

## Root Cause

This is a **missing feature**, not a bug. The Phase 1 implementation only included site creation and selection. No edit dialog, edit command, or database update method was implemented.

The current components:
- `SiteSelectionView.xaml` — has "New Site..." and "Open" buttons but no "Edit" button
- `SiteSelectionViewModel` — has `NewSiteCommand` and `SelectSiteCommand` but no `EditSiteCommand`
- `NewSiteDialog` — hardcoded title "New Site", no mode to pre-populate fields for editing
- `CorpusDatabase` — has `CreateSite()` but no `UpdateSite()` method

## Fix

### 1. Add `UpdateSite` to CorpusDatabase

```csharp
public void UpdateSite(int siteId, string name, string startUrl, string ns, string outputPath, List<string> aliases)
{
    using var conn = CreateConnection();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE Sites SET Name=@name, StartUrl=@url, Namespace=@ns, OutputPath=@path WHERE Id=@id";
    cmd.Parameters.AddWithValue("@id", siteId);
    cmd.Parameters.AddWithValue("@name", name);
    cmd.Parameters.AddWithValue("@url", startUrl);
    cmd.Parameters.AddWithValue("@ns", ns);
    cmd.Parameters.AddWithValue("@path", outputPath);
    cmd.ExecuteNonQuery();
    // Update aliases table separately
}
```

### 2. Convert NewSiteDialog to Support Edit Mode

Rename to `SiteDialog` or add an editing constructor:
- Accept an optional `SiteInfo` parameter
- If provided, pre-populate all fields and change the title to "Edit Site"
- Change the Create button text to "Save" in edit mode
- On save, call `CorpusDatabase.UpdateSite()` instead of `CreateSite()`

### 3. Add Edit Button to SiteSelectionView

```xml
<Button Content="Edit..." Padding="16,8" Margin="0,0,8,0"
        Command="{Binding EditSiteCommand}"
        CommandParameter="{Binding SelectedItem, ElementName=SiteList}"/>
```

### 4. Add EditSiteCommand to SiteSelectionViewModel

```csharp
EditSiteCommand = new RelayCommand<SiteInfo>(site =>
{
    if (site is not null)
        EditSiteRequested?.Invoke(site);
});

public event Action<SiteInfo>? EditSiteRequested;
```

Wire in `SiteSelectionView.xaml.cs` to open the dialog in edit mode and refresh the list on save.

## Status

- [ ] `UpdateSite` method added to CorpusDatabase
- [ ] NewSiteDialog supports edit mode (pre-populated fields, Save button)
- [ ] Edit button added to SiteSelectionView
- [ ] EditSiteCommand added to SiteSelectionViewModel
- [ ] Unit tests for UpdateSite and edit flow
