# Step 1.6 — Status Bar, Loading Indicator, Title Bar, Dev-Tools Toggle

## Objective

Add status bar with corpus stats, loading indicator, title bar with active site name, and a dev-tools toggle.

## Dependencies

- Step 1.4 (navigation events wired)

## Implementation

### Title bar

Shows active site name:

```csharp
Title = $"Brinell Scraper — {ActiveSite.Name}";
```

### Status bar

```xml
<StatusBar DockPanel.Dock="Bottom">
  <StatusBarItem>
    <TextBlock Text="{Binding SiteName}" FontWeight="Bold"/>
  </StatusBarItem>
  <Separator/>
  <StatusBarItem>
    <TextBlock Text="{Binding StatusText}" />
  </StatusBarItem>
  <StatusBarItem>
    <TextBlock Text="{Binding CorpusStats}" />
    <!-- e.g. "12 pages · 34 controls · 2 pending" -->
  </StatusBarItem>
  <StatusBarItem HorizontalAlignment="Right">
    <ProgressBar IsIndeterminate="{Binding IsLoading}" Width="100" Height="14"
                 Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}"/>
  </StatusBarItem>
</StatusBar>
```

### Status bar items

- **`SiteName`** — bold site name on the left
- **`StatusText`** — shows current URL or "Ready" / "Loading..." / error messages
- **`CorpusStats`** — page count, control count, generation progress (e.g. "12 pages · 34 controls · 2 pending")
- **`IsLoading`** — bound to indeterminate `ProgressBar`, toggled by `NavigationStarting` / `NavigationCompleted`

### Dev-tools toggle

```csharp
webView.CoreWebView2.OpenDevToolsWindow();
```

Toggle via a `Button` or `CheckBox` in the toolbar. DevTools opens in a separate window (WebView2 default behavior).

## Checklist

- [ ] Title bar shows "Brinell Scraper — {SiteName}"
- [ ] Status bar shows site name, status text, and corpus stats
- [ ] Loading progress bar appears during navigation
- [ ] Loading progress bar hides on navigation complete
- [ ] Dev-tools toggle opens/closes the DevTools window
- [ ] Status bar shows error message on navigation failure
