# Step 3.3 — In-App Log Viewer Panel

## Objective

Collapsible bottom panel showing real-time log entries with level filtering.

## Dependencies

- Step 3.1 (logging framework)

## Implementation

### LogEntry model

```csharp
public record LogEntry(DateTime Timestamp, LogLevel Level, string Source, string Category, string Message);
```

`Source` identifies the subsystem: `Browser`, `DomCapture`, `Corpus`, `Analyzer`, `Generator`, `Llm`, `Navigation`.

### InAppLogProvider

Implements `ILoggerProvider` — creates loggers that push `LogEntry` to a shared `ObservableCollection<LogEntry>`.

### LogViewerPanel.xaml

```xml
<DockPanel>
  <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="4">
    <TextBlock Text="Level:" VerticalAlignment="Center" Margin="0,0,4,0"/>
    <ComboBox ItemsSource="{Binding LogLevels}"
              SelectedItem="{Binding SelectedLogLevel}" Width="120"/>
    <Button Content="Clear" Command="{Binding ClearLogsCommand}" Margin="8,0,0,0"/>
  </StackPanel>

  <ListBox ItemsSource="{Binding FilteredLogEntries}"
           VirtualizingPanel.IsVirtualizing="True">
    <ListBox.ItemTemplate>
      <DataTemplate>
        <StackPanel Orientation="Horizontal">
          <TextBlock Text="{Binding Timestamp, StringFormat=HH:mm:ss.fff}" Width="90" Foreground="Gray"/>
          <TextBlock Text="{Binding Level}" Width="60"
                     Foreground="{Binding Level, Converter={StaticResource LogLevelToBrush}}"/>
          <TextBlock Text="{Binding Source}" Width="120" Foreground="DarkCyan"/>
          <TextBlock Text="{Binding Message}" TextTrimming="CharacterEllipsis"/>
        </StackPanel>
      </DataTemplate>
    </ListBox.ItemTemplate>
  </ListBox>
</DockPanel>
```

### Behavior

- `FilteredLogEntries` — `ICollectionView` filtered by `SelectedLogLevel` (show entries at or above selected level)
- ComboBox options: `Debug`, `Information`, `Warning`, `Error`
- Virtualization for performance with large volumes
- Auto-scroll to bottom on new entries (optional toggle)
- Toggle via `View → Logs` or status bar click
- Resizable via horizontal `GridSplitter` (drag top edge)

## Checklist

- [ ] Log viewer panel renders in collapsible bottom area
- [ ] Entries appear in real time as logs are written
- [ ] Level filter dropdown works (e.g. selecting Warning hides Info/Debug)
- [ ] Clear button empties the viewer
- [ ] Virtualization handles 10,000+ entries smoothly
- [ ] Auto-scroll follows latest entry
