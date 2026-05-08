# Step 12.W.1 — New Site Dialog

## Objective

Wire up the `NewSiteRequested` event so that clicking "New Site" on the start page opens a modal dialog collecting site details, persists the new site to the database, and updates the start page list.

## Dependencies

- `StartPageViewModel.NewSiteRequested` event (exists, no subscriber)
- `CorpusDatabase.AddSite(name, startUrl, namespace, outputPath, aliases)` → `SiteInfo`
- `StartPageViewModel.AddOrUpdateSite(SiteInfo)` (must accept a SiteInfo and upsert into `Sites` collection)

## Implementation

### Files

| Action | Path                                              |
| ------ | ------------------------------------------------- |
| Create | `Brinell.Scraper/Dialogs/NewSiteDialog.xaml`    |
| Create | `Brinell.Scraper/Dialogs/NewSiteDialog.xaml.cs` |
| Modify | `Brinell.Scraper/MainWindow.xaml.cs`            |

### Code sketch

**NewSiteDialog.xaml** — modal Window, `SizeToContent="WidthAndHeight"`, `ResizeMode="NoResize"`, `WindowStartupLocation="CenterOwner"`.

```xml
<Window x:Class="Brinell.Scraper.Dialogs.NewSiteDialog"
        Title="New Site" Width="480" SizeToContent="Height"
        ResizeMode="NoResize" WindowStartupLocation="CenterOwner">
  <StackPanel Margin="16" Spacing="8">
    <TextBlock Text="Name *" />
    <TextBox x:Name="NameBox" />

    <TextBlock Text="Start URL *" />
    <TextBox x:Name="UrlBox" />
    <TextBlock x:Name="UrlError" Foreground="Red" Visibility="Collapsed"
               Text="Must be a valid absolute URL." />

    <TextBlock Text="Namespace" />
    <TextBox x:Name="NamespaceBox" />

    <TextBlock Text="Output Path" />
    <DockPanel>
      <Button DockPanel.Dock="Right" Content="Browse…" Click="OnBrowse" Margin="4,0,0,0" />
      <TextBox x:Name="OutputPathBox" />
    </DockPanel>

    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,12,0,0">
      <Button Content="OK" Width="80" Click="OnOk" IsDefault="True" />
      <Button Content="Cancel" Width="80" IsCancel="True" Margin="8,0,0,0" />
    </StackPanel>
  </StackPanel>
</Window>
```

**NewSiteDialog.xaml.cs**

```csharp
public partial class NewSiteDialog : Window
{
    public bool IsEditMode { get; init; }

    // Bound result properties
    public string SiteName => NameBox.Text.Trim();
    public string StartUrl => UrlBox.Text.Trim();
    public string Namespace => NamespaceBox.Text.Trim();
    public string OutputPath => OutputPathBox.Text.Trim();

    public NewSiteDialog() => InitializeComponent();

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SiteName))
        {
            NameBox.Focus();
            return;
        }
        if (!Uri.TryCreate(StartUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            UrlError.Visibility = Visibility.Visible;
            UrlBox.Focus();
            return;
        }
        DialogResult = true;
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog();
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            OutputPathBox.Text = dlg.SelectedPath;
    }

    // Auto-fill namespace from name on first edit (only when not in edit mode)
    private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsEditMode && string.IsNullOrEmpty(NamespaceBox.Text))
            NamespaceBox.Text = SanitizeNamespace(NameBox.Text);
    }

    private static string SanitizeNamespace(string name)
        => Regex.Replace(name.Trim(), @"[^A-Za-z0-9_]", "");
}
```

**MainWindow.xaml.cs — ShowStartPage addition**

```csharp
private void ShowStartPage()
{
    var vm = _services.GetRequiredService<StartPageViewModel>();
    vm.SiteSelected += OnSiteSelected;
    vm.SettingsRequested += OnSettingsRequested;
    vm.NewSiteRequested += OnNewSiteRequested;       // ← add
    // ...
}

private void OnNewSiteRequested()
{
    var dlg = new NewSiteDialog { Owner = this };
    if (dlg.ShowDialog() != true) return;

    var db = _services.GetRequiredService<CorpusDatabase>();
    var site = db.AddSite(
        dlg.SiteName,
        dlg.StartUrl,
        dlg.Namespace,
        dlg.OutputPath,
        aliases: Array.Empty<string>());

    _startVm?.AddOrUpdateSite(site);
}
```

### Behavior

- Clicking "New Site" fires `NewSiteRequested`; MainWindow opens `NewSiteDialog` modally.
- OK is disabled until Name and URL pass validation; URL must pass `Uri.TryCreate` with `http`/`https` scheme.
- Namespace auto-fills from sanitized Name on first keystroke (stripped to `[A-Za-z0-9_]`).
- Browse button opens a folder picker for Output Path.
- On OK: `CorpusDatabase.AddSite` persists the row, returns `SiteInfo`; `AddOrUpdateSite` inserts it into the observable `Sites` collection so the UI updates immediately.
- On Cancel or close: no side effects.

## Checklist

- [X] Create `NewSiteDialog.xaml` + code-behind in `Dialogs/` folder
- [X] Validate Name non-empty, URL via `Uri.TryCreate` (absolute, http/https)
- [ ] Auto-fill Namespace from sanitized Name
- [X] Browse button opens `FolderBrowserDialog`
- [X] Subscribe `NewSiteRequested` in `ShowStartPage`
- [X] Call `CorpusDatabase.AddSite(...)` on dialog OK
- [X] Call `_startVm.AddOrUpdateSite(site)` to refresh list
- [X] Verify `AddOrUpdateSite` exists on `StartPageViewModel` (add if missing)
