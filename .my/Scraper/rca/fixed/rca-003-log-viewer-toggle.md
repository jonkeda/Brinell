# RCA-003: Log Viewer Toggle Does Not Work

**Reported:** 2026-04-22
**Severity:** Medium
**Component:** `MainWindow.xaml`, `Views/LogViewerPanel.xaml.cs`

---

## Symptoms

1. **Clicking View → Logs does not show/hide the log panel.** The checkbox toggles visually but the panel doesn't appear or disappear.
2. **Missing "All" log level filter.** There is no way to show all log entries regardless of level.

## Root Cause

### Issue 1 — DataContext Override Breaks Visibility Binding

The `LogViewerPanel` visibility is bound to `IsLogViewerVisible` on the `MainViewModel`:

**File:** `MainWindow.xaml`, line 62
```xml
<views:LogViewerPanel DockPanel.Dock="Bottom" x:Name="LogViewerPanel"
                      Height="180"
                      Visibility="{Binding IsLogViewerVisible, Converter={StaticResource BoolToVisibility}}"/>
```

However, in `MainWindow.OnLoaded`, the panel's DataContext is replaced with `LogViewerViewModel`:

**File:** `MainWindow.xaml.cs`, lines 31–32
```csharp
var logViewerVm = App.Services.GetRequiredService<LogViewerViewModel>();
LogViewerPanel.Initialize(logViewerVm);
```

**File:** `Views/LogViewerPanel.xaml.cs`, line 17
```csharp
DataContext = vm;  // Now LogViewerViewModel, not MainViewModel
```

After `Initialize`, the `Visibility` binding on `LogViewerPanel` resolves `IsLogViewerVisible` against `LogViewerViewModel` — which does **not** have that property. The binding silently fails and the visibility never changes.

The `GridSplitter` below the log panel has the same binding and the same problem — it also inherits the wrong DataContext because its binding source is the panel's parent `DockPanel`, which still has `MainViewModel`, so the splitter binding actually works. But it doesn't matter because the panel itself never toggles.

### Issue 2 — Missing "All" Log Level

The `LogLevels` array in `LogViewerViewModel` starts at `Trace`:

**File:** `ViewModels/LogViewerViewModel.cs`, line 30
```csharp
LogLevels = [LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error];
```

`Trace` effectively shows everything since `FilterByLevel` uses `>=` comparison, but the UX label "Trace" is confusing. Users expect an "All" option.

## Fix

### Issue 1 — Use RelativeSource or ElementName Binding

Change the `Visibility` binding on `LogViewerPanel` to explicitly reference the `MainViewModel` via the Window's DataContext, so it is not affected by the panel's own DataContext override:

```xml
<views:LogViewerPanel DockPanel.Dock="Bottom" x:Name="LogViewerPanel"
                      Height="180"
                      Visibility="{Binding DataContext.IsLogViewerVisible,
                                   RelativeSource={RelativeSource AncestorType=Window},
                                   Converter={StaticResource BoolToVisibility}}"/>
```

Do the same for the `GridSplitter` binding for consistency:
```xml
<GridSplitter DockPanel.Dock="Bottom" Height="4" ResizeDirection="Rows"
              HorizontalAlignment="Stretch"
              Visibility="{Binding DataContext.IsLogViewerVisible,
                           RelativeSource={RelativeSource AncestorType=Window},
                           Converter={StaticResource BoolToVisibility}}"/>
```

### Issue 2 — Add "All" Filter Level

Replace `LogLevel.Trace` with a conceptual "All" option. Since `LogLevel.Trace` (value 0) is the lowest level and the filter uses `>=`, it already shows everything. The simplest fix is purely cosmetic — add a display name override in the ComboBox:

Option A — Keep using `Trace` but display "All":
```xml
<ComboBox ItemsSource="{Binding LogLevels}"
          SelectedItem="{Binding SelectedLogLevel}"
          Width="120" Margin="0,0,8,0">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Converter={StaticResource LogLevelDisplayConverter}}"/>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

Where `LogLevelDisplayConverter` maps `Trace` → "All" and everything else to its name.

Option B — Use `LogLevel.None - 1` or just add a string-based approach. Option A is cleaner.

## Affected Tests

- `LogViewerViewModelTests` — existing tests pass (they test the ViewModel, not the XAML binding)
- **No new unit test needed** — this is a XAML binding issue, testable only via UI testing

## Status

- [ ] LogViewerPanel visibility binding fixed with `RelativeSource`
- [ ] GridSplitter visibility binding fixed with `RelativeSource`
- [ ] "All" filter level added to log viewer
- [ ] Verified toggle shows/hides the log panel
