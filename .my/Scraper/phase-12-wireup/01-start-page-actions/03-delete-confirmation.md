# Step 12.W.3 — Delete Confirmation

## Objective

Wire up the `DeleteSiteConfirmRequested` event so that when the user clicks "Delete" on a site card, a confirmation message box is shown. The VM already handles the database deletion and list removal — this step only provides the missing confirmation callback.

## Dependencies

- `StartPageViewModel.DeleteSiteConfirmRequested` — `Func<SiteCardItem, bool>?` (exists, no subscriber)
- VM logic: if delegate returns `true`, calls `_db.DeleteSite(card.Id)` and `Sites.Remove(card)` (already implemented)

## Implementation

### Files

| Action | Path |
|--------|------|
| Modify | `Brinell.Scraper/MainWindow.xaml.cs` |

### Code sketch

**MainWindow.xaml.cs — ShowStartPage addition**

```csharp
private void ShowStartPage()
{
    var vm = _services.GetRequiredService<StartPageViewModel>();
    vm.SiteSelected += OnSiteSelected;
    vm.SettingsRequested += OnSettingsRequested;
    vm.NewSiteRequested += OnNewSiteRequested;
    vm.EditSiteRequested += OnEditSiteRequested;
    vm.DeleteSiteConfirmRequested += OnDeleteSiteConfirmRequested;  // ← add
    // ...
}

private bool OnDeleteSiteConfirmRequested(SiteCardItem card)
{
    var result = MessageBox.Show(
        $"Delete \"{card.Name}\" and all its data?",
        "Confirm Delete",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    return result == MessageBoxResult.Yes;
}
```

### Behavior

- Clicking "Delete" on a site card invokes `DeleteSiteConfirmRequested` on the VM.
- MainWindow shows a `MessageBox` with the site name, Yes/No buttons, and a warning icon.
- If the user clicks **Yes**: delegate returns `true`; VM proceeds to call `_db.DeleteSite(card.Id)` and removes the card from `Sites`.
- If the user clicks **No** or closes the dialog: delegate returns `false`; no changes occur.
- No additional database or UI code needed — the VM already owns the deletion logic.

## Checklist

- [x] Subscribe `DeleteSiteConfirmRequested` in `ShowStartPage`
- [x] Implement `OnDeleteSiteConfirmRequested` returning `bool`
- [x] MessageBox displays site name, "Confirm Delete" title, YesNo buttons, Warning icon
- [x] Return `true` only when `MessageBoxResult.Yes`
- [x] Verify VM delete logic fires after `true` (no changes needed in VM)
