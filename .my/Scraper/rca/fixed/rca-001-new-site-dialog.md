# RCA-001: New Site Dialog Issues

**Reported:** 2026-04-22
**Severity:** High
**Component:** `Views/SiteSelectionView.xaml.cs`, `Views/NewSiteDialog.xaml`, `ViewModels/BrowserViewModel.cs` (SiteSelectionViewModel)

---

## Symptoms

1. **No browse button on Output Path** — the Output Path field is a plain TextBox with no folder picker.
2. **Cancel button opens multiple dialogs** — clicking Cancel on the New Site dialog doesn't close cleanly; instead, multiple dialog windows appear.
3. **Empty "New Site" dialog opens after creating a site** — after filling in the form and clicking Create, the site is added and the URL opens, but then a blank New Site dialog appears on top.

## Root Cause

### Issue 1 — Missing Browse Button

The `NewSiteDialog.xaml` only has a `TextBox` for Output Path. There is no `Button` wired to a `FolderBrowserDialog` or `OpenFolderDialog`.

**File:** `Views/NewSiteDialog.xaml`, line 33
```xml
<TextBox Grid.Row="3" Grid.Column="1" x:Name="OutputPathBox" Margin="0,0,0,8"/>
```

### Issues 2 & 3 — Duplicate Event Subscription

The root cause of both the multiple-dialog and empty-dialog issues is the same: **`SiteSelectionView.OnLoaded` subscribes to `NewSiteRequested` every time the control is loaded, without unsubscribing.**

**File:** `Views/SiteSelectionView.xaml.cs`, lines 17–21
```csharp
private void OnLoaded(object sender, RoutedEventArgs e)
{
    if (DataContext is SiteSelectionViewModel vm)
    {
        vm.NewSiteRequested += ShowNewSiteDialog;  // ← adds a new handler EVERY load
    }
}
```

The `SiteSelectionView` instance is cached in `MainWindow._siteSelectionView`. Each time the user switches to the site selector (via `ShowSiteSelector()`), the view is set as `ContentArea.Content`, which triggers `Loaded` again. After N visits to the site selector, `ShowNewSiteDialog` is subscribed N times.

When the user clicks "New Site...":
1. `NewSiteRequested` fires → all N handlers execute
2. Handler 1: opens dialog → user fills in data → clicks Create → `vm.AddSite()` → `SiteSelected` fires → browser view loads
3. Handlers 2..N: each opens a **new empty** `NewSiteDialog` on top of the browser view
4. Pressing Cancel on each of these closes them one at a time, making it look like Cancel "doesn't work"

## Fix

### Issue 1 — Add Browse Button

Add a `Button` next to the `OutputPathBox` in `NewSiteDialog.xaml` and wire it to `System.Windows.Forms.FolderBrowserDialog` (or `Microsoft.Win32.OpenFolderDialog` on .NET 8+) in the code-behind.

```xml
<Grid Grid.Row="3" Grid.Column="1" Margin="0,0,0,8">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <TextBox Grid.Column="0" x:Name="OutputPathBox"/>
    <Button Grid.Column="1" Content="Browse..." Margin="4,0,0,0" Padding="8,2"
            Click="OnBrowseOutputPath"/>
</Grid>
```

```csharp
private void OnBrowseOutputPath(object sender, RoutedEventArgs e)
{
    var dialog = new Microsoft.Win32.OpenFolderDialog
    {
        Title = "Select output folder"
    };
    if (dialog.ShowDialog(this) == true)
        OutputPathBox.Text = dialog.FolderName;
}
```

### Issues 2 & 3 — Change Event to Action Property

The preferred fix is to change `NewSiteRequested` from a C# `event` to a plain `Action` property. An `event` uses `+=` which accumulates handlers; an `Action` property uses `=` which replaces the previous value — making duplicate subscriptions impossible by design.

**In `SiteSelectionViewModel`:**
```csharp
// Before (event — allows += accumulation)
public event Action? NewSiteRequested;

// After (Action property — = replaces, no duplicates possible)
public Action? NewSiteRequested { get; set; }
```

**In `SiteSelectionView.xaml.cs`:**
```csharp
private void OnLoaded(object sender, RoutedEventArgs e)
{
    if (DataContext is SiteSelectionViewModel vm)
    {
        vm.NewSiteRequested = ShowNewSiteDialog;  // = not +=
    }
}
```

**In `SiteSelectionViewModel.NewSiteCommand`:**
```csharp
NewSiteCommand = new RelayCommand(() => NewSiteRequested?.Invoke());
```
No change needed — `?.Invoke()` works the same on both `event` and `Action`.

## Affected Tests

- `SiteSelectionViewModelTests.NewSiteCommand_FiresNewSiteRequested` — existing test passes (only tests the event fires once, not subscription duplication)
- **New test needed:** Verify that multiple `Loaded` events do not cause multiple dialog opens

## Status

- [ ] Browse button added to Output Path
- [ ] Duplicate subscription fixed
- [ ] Verified Cancel closes dialog in single action
- [ ] Verified Create adds site without spawning empty dialog
