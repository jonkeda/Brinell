# Step 12.W.2 — Edit Site Dialog

## Objective

Wire up the `EditSiteRequested` event so that clicking "Edit" on a site card opens the same `NewSiteDialog` in edit mode, pre-populated with the site's current values, and persists changes via `CorpusDatabase.UpdateSite`.

## Dependencies

- `StartPageViewModel.EditSiteRequested` event (exists, no subscriber)
- `NewSiteDialog` with `IsEditMode` property (created in step 12.W.1)
- `CorpusDatabase.GetAllSites()` → `IReadOnlyList<SiteInfo>`
- `CorpusDatabase.UpdateSite(id, name, startUrl, namespace, outputPath, aliases)`
- `StartPageViewModel.AddOrUpdateSite(SiteInfo)`
- `SiteCardItem.Id` to look up full `SiteInfo`

## Implementation

### Files

| Action | Path |
|--------|------|
| Modify | `Brinell.Scraper/Dialogs/NewSiteDialog.xaml.cs` |
| Modify | `Brinell.Scraper/MainWindow.xaml.cs` |

### Code sketch

**NewSiteDialog.xaml.cs — add populate method**

```csharp
public partial class NewSiteDialog : Window
{
    public bool IsEditMode { get; init; }
    public int EditSiteId { get; private set; }

    public void Populate(SiteInfo site)
    {
        EditSiteId = site.Id;
        Title = "Edit Site";
        NameBox.Text = site.Name;
        UrlBox.Text = site.StartUrl;
        NamespaceBox.Text = site.Namespace;
        OutputPathBox.Text = site.OutputPath;
    }
}
```

**MainWindow.xaml.cs — ShowStartPage addition**

```csharp
private void ShowStartPage()
{
    var vm = _services.GetRequiredService<StartPageViewModel>();
    vm.SiteSelected += OnSiteSelected;
    vm.SettingsRequested += OnSettingsRequested;
    vm.NewSiteRequested += OnNewSiteRequested;
    vm.EditSiteRequested += OnEditSiteRequested;   // ← add
    // ...
}

private void OnEditSiteRequested(SiteCardItem card)
{
    var db = _services.GetRequiredService<CorpusDatabase>();
    var site = db.GetAllSites().First(s => s.Id == card.Id);

    var dlg = new NewSiteDialog { Owner = this, IsEditMode = true };
    dlg.Populate(site);

    if (dlg.ShowDialog() != true) return;

    db.UpdateSite(
        site.Id,
        dlg.SiteName,
        dlg.StartUrl,
        dlg.Namespace,
        dlg.OutputPath,
        aliases: site.Aliases);  // preserve existing aliases

    var updated = db.GetAllSites().First(s => s.Id == site.Id);
    _startVm?.AddOrUpdateSite(updated);
}
```

### Behavior

- Clicking "Edit" on a site card fires `EditSiteRequested(card)`; MainWindow looks up the full `SiteInfo` by `card.Id`.
- `NewSiteDialog` opens with `IsEditMode = true`, title "Edit Site", fields pre-populated.
- Namespace auto-fill from Name is suppressed when `IsEditMode` is true (field already has a value).
- Validation rules are identical to new-site mode (Name required, URL valid).
- On OK: `CorpusDatabase.UpdateSite` persists changes; `AddOrUpdateSite` replaces the card in the observable collection so the UI reflects the new name/URL immediately.
- Aliases are preserved from the existing `SiteInfo` (not editable in this dialog).
- On Cancel or close: no side effects.

## Checklist

- [x] Add `Populate(SiteInfo)` method to `NewSiteDialog`
- [x] Add `EditSiteId` property for tracking which site is being edited
- [x] Set `Title` to "Edit Site" in `Populate`
- [x] Subscribe `EditSiteRequested` in `ShowStartPage`
- [x] Look up full `SiteInfo` via `GetAllSites().First(s => s.Id == card.Id)`
- [x] Call `CorpusDatabase.UpdateSite(...)` on dialog OK
- [x] Preserve existing aliases on update
- [x] Call `_startVm.AddOrUpdateSite(updated)` to refresh the card
