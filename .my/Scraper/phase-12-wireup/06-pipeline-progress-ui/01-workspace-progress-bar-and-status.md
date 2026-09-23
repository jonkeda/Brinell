# Step 12.W.6a — Workspace Progress Bar and Status Strip

## Objective

Wire `PipelineOrchestrator`'s `IProgress<PipelineProgress>` reporting into a shared progress strip in `WorkspacePage.xaml`, driven by properties on `WorkspaceViewModel`. While a pipeline is running, all pipeline-trigger buttons across tabs are disabled, and the strip shows real-time stage, item, and percentage information.

## Dependencies

- `PipelineProgress` record already defined (Stage, CurrentItem, Completed, Total, Message).
- `PipelineOrchestrator` accepts `IProgress<PipelineProgress>` in its async methods.
- `WorkspaceViewModel` already holds references to all tab ViewModels.
- `WorkspacePage.xaml` uses a `DockPanel` with a `TabControl`.

## Implementation

### Files

| Action | Path |
|--------|------|
| Modify | `Brinell.Scraper/ViewModels/WorkspaceViewModel.cs` |
| Modify | `Brinell.Scraper/Views/WorkspacePage.xaml` |
| Modify | Each tab VM that invokes pipeline methods (e.g. `AnalyzeTabViewModel`, `GenerateControlsTabViewModel`, `GeneratePagesTabViewModel`) |

### Code sketch

#### WorkspaceViewModel.cs — new properties and progress factory

```csharp
// --- Progress state properties (all notify via ObservableProperty or OnPropertyChanged) ---

[ObservableProperty] private bool _isPipelineRunning;
[ObservableProperty] private string _pipelineStage = string.Empty;
[ObservableProperty] private string _pipelineCurrentItem = string.Empty;
[ObservableProperty] private int _pipelineCompleted;
[ObservableProperty] private int _pipelineTotal;
[ObservableProperty] private string _pipelineMessage = string.Empty;

public double PipelinePercent => PipelineTotal > 0
    ? (double)PipelineCompleted / PipelineTotal * 100
    : 0;

public bool PipelineIsIndeterminate => PipelineTotal == 0;

// Raise PipelinePercent + PipelineIsIndeterminate when Completed/Total change
partial void OnPipelineCompletedChanged(int value) { OnPropertyChanged(nameof(PipelinePercent)); OnPropertyChanged(nameof(PipelineIsIndeterminate)); }
partial void OnPipelineTotalChanged(int value)     { OnPropertyChanged(nameof(PipelinePercent)); OnPropertyChanged(nameof(PipelineIsIndeterminate)); }
```

```csharp
/// <summary>
/// Creates an IProgress that dispatches PipelineProgress updates to the UI thread
/// and sets IsPipelineRunning = true.
/// Called by tab VMs before invoking any pipeline method.
/// </summary>
public IProgress<PipelineProgress> CreatePipelineProgress()
{
    IsPipelineRunning = true;
    PipelineStage = string.Empty;
    PipelineCurrentItem = string.Empty;
    PipelineCompleted = 0;
    PipelineTotal = 0;
    PipelineMessage = string.Empty;

    return new Progress<PipelineProgress>(p =>
    {
        // Progress<T> captures SynchronizationContext at construction,
        // so callbacks already arrive on UI thread when constructed on UI thread.
        PipelineStage = p.Stage;
        PipelineCurrentItem = p.CurrentItem ?? string.Empty;
        PipelineCompleted = p.Completed;
        PipelineTotal = p.Total;
        PipelineMessage = p.Message ?? string.Empty;
    });
}
```

#### WorkspacePage.xaml — progress strip

```xml
<!-- Place AFTER the TabControl inside the DockPanel, docked to Bottom -->
<Border DockPanel.Dock="Bottom"
        Background="{DynamicResource MaterialDesignPaper}"
        BorderBrush="{DynamicResource MaterialDesignDivider}"
        BorderThickness="0,1,0,0"
        Padding="12,6"
        Visibility="{Binding IsPipelineRunning, Converter={StaticResource BooleanToVisibilityConverter}}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <!-- Row 0: Stage + CurrentItem -->
        <TextBlock Grid.Row="0" Grid.Column="0">
            <Run Text="{Binding PipelineStage}" FontWeight="SemiBold" />
            <Run Text=" — " />
            <Run Text="{Binding PipelineCurrentItem}" />
        </TextBlock>

        <!-- Row 0 Col 1: Message -->
        <TextBlock Grid.Row="0" Grid.Column="1"
                   Text="{Binding PipelineMessage}"
                   Foreground="{DynamicResource MaterialDesignBodyLight}"
                   HorizontalAlignment="Right" />

        <!-- Row 1: ProgressBar -->
        <ProgressBar Grid.Row="1" Grid.ColumnSpan="2"
                     Margin="0,4,0,0"
                     Height="4"
                     Minimum="0" Maximum="100"
                     Value="{Binding PipelinePercent, Mode=OneWay}"
                     IsIndeterminate="{Binding PipelineIsIndeterminate, Mode=OneWay}" />
    </Grid>
</Border>
```

#### Tab ViewModels — usage pattern

```csharp
// Inside e.g. AnalyzeTabViewModel.RunAnalyzeAsync()
private async Task RunAnalyzeAsync()
{
    var progress = _workspaceViewModel.CreatePipelineProgress();

    try
    {
        await _orchestrator.AnalyzeForControlObjectsAsync(progress, cancellationToken);
    }
    finally
    {
        _workspaceViewModel.IsPipelineRunning = false;
    }
}
```

#### Tab VM CanExecute guard

```csharp
// In each tab VM, bind command CanExecute to workspace state:
private bool CanRunPipeline() => !_workspaceViewModel.IsPipelineRunning;

// Re-evaluate when IsPipelineRunning changes:
// Subscribe in constructor:
_workspaceViewModel.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(WorkspaceViewModel.IsPipelineRunning))
        RunPipelineCommand.NotifyCanExecuteChanged();
};
```

### Behavior

- When any tab VM calls `CreatePipelineProgress()`, the strip becomes visible and the progress bar starts (indeterminate if Total==0).
- As `PipelineOrchestrator` reports progress, Stage/CurrentItem/Completed/Total/Message update in real time on the UI thread.
- All "Analyze", "Generate Controls", "Generate Pages", and similar buttons across all tabs become disabled (CanExecute → false) while `IsPipelineRunning` is true.
- When the pipeline method completes (success or exception), the tab VM sets `IsPipelineRunning = false`, collapsing the strip and re-enabling buttons.
- `PipelinePercent` is computed; no manual updates needed.

## Checklist

- [ ] Add progress state properties to `WorkspaceViewModel`
- [ ] Add `PipelinePercent` and `PipelineIsIndeterminate` computed properties
- [ ] Implement `CreatePipelineProgress()` returning `IProgress<PipelineProgress>`
- [ ] Add progress strip `Border` to `WorkspacePage.xaml` (docked bottom, collapsed by default)
- [ ] Bind ProgressBar Value/IsIndeterminate and TextBlocks to VM properties
- [ ] Update each tab VM to call `CreatePipelineProgress()` before pipeline invocation
- [ ] Reset `IsPipelineRunning = false` in `finally` blocks of tab VM pipeline calls
- [ ] Wire CanExecute of all pipeline-trigger commands to `!IsPipelineRunning`
- [ ] Subscribe to `WorkspaceViewModel.PropertyChanged` in tab VMs for CanExecute refresh
- [ ] Verify strip collapses immediately when pipeline finishes
