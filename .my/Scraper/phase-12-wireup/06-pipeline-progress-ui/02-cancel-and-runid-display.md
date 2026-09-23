# Step 12.W.6b — Cancel Pipeline and RunId Display

## Objective

Add a cancellation mechanism and run-correlation label to the pipeline progress strip. `WorkspaceViewModel` owns the `CancellationTokenSource`, exposes a cancel command, shows the current `RunId` (first 8 chars) in the strip, and displays a transient summary message for 3 seconds after completion or cancellation before collapsing the strip.

## Dependencies

- Step 12.W.6a (progress strip and `IsPipelineRunning` properties) must be in place.
- `PipelineOrchestrator` async methods accept a `CancellationToken` parameter.
- Tab VMs already call `CreatePipelineProgress()` from 12.W.6a.

## Implementation

### Files

| Action | Path |
|--------|------|
| Modify | `Brinell.Scraper/ViewModels/WorkspaceViewModel.cs` |
| Modify | `Brinell.Scraper/Views/WorkspacePage.xaml` |
| Modify | Each tab VM that invokes pipeline methods (pass CancellationToken) |

### Code sketch

#### WorkspaceViewModel.cs — CTS, RunId, Cancel, Complete

```csharp
private CancellationTokenSource? _pipelineCts;
private DispatcherTimer? _completionTimer;

[ObservableProperty] private string _pipelineRunId = string.Empty;
[ObservableProperty] private bool _isPipelineComplete;   // true during 3-second summary display

[RelayCommand(CanExecute = nameof(CanCancelPipeline))]
private void CancelPipeline()
{
    _pipelineCts?.Cancel();
}

private bool CanCancelPipeline() => IsPipelineRunning && _pipelineCts is { IsCancellationRequested: false };

partial void OnIsPipelineRunningChanged(bool value)
{
    CancelPipelineCommand.NotifyCanExecuteChanged();
}
```

```csharp
/// <summary>
/// Creates progress + cancellation token pair. Replaces the 12.W.6a overload.
/// </summary>
public (IProgress<PipelineProgress> Progress, CancellationToken Token) CreatePipelineProgress(Guid runId)
{
    _completionTimer?.Stop();
    _pipelineCts?.Dispose();
    _pipelineCts = new CancellationTokenSource();

    IsPipelineRunning = true;
    IsPipelineComplete = false;
    PipelineRunId = runId.ToString("N")[..8];
    PipelineStage = string.Empty;
    PipelineCurrentItem = string.Empty;
    PipelineCompleted = 0;
    PipelineTotal = 0;
    PipelineMessage = string.Empty;

    var progress = new Progress<PipelineProgress>(p =>
    {
        PipelineStage = p.Stage;
        PipelineCurrentItem = p.CurrentItem ?? string.Empty;
        PipelineCompleted = p.Completed;
        PipelineTotal = p.Total;
        PipelineMessage = p.Message ?? string.Empty;
    });

    return (progress, _pipelineCts.Token);
}
```

```csharp
/// <summary>
/// Called by tab VMs after pipeline completes (success, error, or cancellation).
/// Shows a summary in the strip for 3 seconds, then collapses.
/// </summary>
public void CompletePipeline(string summaryMessage)
{
    IsPipelineRunning = false;
    IsPipelineComplete = true;
    PipelineMessage = summaryMessage;
    PipelineStage = string.Empty;
    PipelineCurrentItem = string.Empty;

    _pipelineCts?.Dispose();
    _pipelineCts = null;

    _completionTimer?.Stop();
    _completionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
    _completionTimer.Tick += (_, _) =>
    {
        _completionTimer.Stop();
        IsPipelineComplete = false;
        PipelineMessage = string.Empty;
        PipelineRunId = string.Empty;
    };
    _completionTimer.Start();
}
```

#### WorkspacePage.xaml — updated progress strip

```xml
<!-- Visibility: show when running OR during completion summary -->
<Border DockPanel.Dock="Bottom"
        Background="{DynamicResource MaterialDesignPaper}"
        BorderBrush="{DynamicResource MaterialDesignDivider}"
        BorderThickness="0,1,0,0"
        Padding="12,6">
    <Border.Visibility>
        <MultiBinding Converter="{StaticResource BoolOrToVisibilityConverter}">
            <Binding Path="IsPipelineRunning" />
            <Binding Path="IsPipelineComplete" />
        </MultiBinding>
    </Border.Visibility>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto" />  <!-- RunId -->
            <ColumnDefinition Width="*" />     <!-- Stage + Item -->
            <ColumnDefinition Width="Auto" />  <!-- Message -->
            <ColumnDefinition Width="Auto" />  <!-- Cancel button -->
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <!-- RunId label -->
        <TextBlock Grid.Row="0" Grid.Column="0"
                   Margin="0,0,8,0"
                   FontFamily="Consolas"
                   FontSize="11"
                   Foreground="{DynamicResource MaterialDesignBodyLight}"
                   Text="{Binding PipelineRunId, StringFormat='[{0}]'}" />

        <!-- Stage + CurrentItem -->
        <TextBlock Grid.Row="0" Grid.Column="1">
            <Run Text="{Binding PipelineStage}" FontWeight="SemiBold" />
            <Run Text=" — " />
            <Run Text="{Binding PipelineCurrentItem}" />
        </TextBlock>

        <!-- Message -->
        <TextBlock Grid.Row="0" Grid.Column="2"
                   Text="{Binding PipelineMessage}"
                   Foreground="{DynamicResource MaterialDesignBodyLight}"
                   HorizontalAlignment="Right"
                   Margin="8,0" />

        <!-- Cancel button (visible only while running) -->
        <Button Grid.Row="0" Grid.Column="3"
                Command="{Binding CancelPipelineCommand}"
                Content="❌"
                ToolTip="Cancel Pipeline"
                Style="{StaticResource MaterialDesignFlatButton}"
                Padding="4,0"
                FontSize="14"
                Visibility="{Binding IsPipelineRunning, Converter={StaticResource BooleanToVisibilityConverter}}" />

        <!-- ProgressBar (hidden during completion summary) -->
        <ProgressBar Grid.Row="1" Grid.ColumnSpan="4"
                     Margin="0,4,0,0"
                     Height="4"
                     Minimum="0" Maximum="100"
                     Value="{Binding PipelinePercent, Mode=OneWay}"
                     IsIndeterminate="{Binding PipelineIsIndeterminate, Mode=OneWay}"
                     Visibility="{Binding IsPipelineRunning, Converter={StaticResource BooleanToVisibilityConverter}}" />
    </Grid>
</Border>
```

#### Tab VMs — updated usage pattern

```csharp
private async Task RunAnalyzeAsync()
{
    var runId = Guid.NewGuid();
    var (progress, ct) = _workspaceViewModel.CreatePipelineProgress(runId);

    try
    {
        await _orchestrator.AnalyzeForControlObjectsAsync(progress, ct);
        _workspaceViewModel.CompletePipeline($"Completed: {_workspaceViewModel.PipelineCompleted} items analyzed");
    }
    catch (OperationCanceledException)
    {
        _workspaceViewModel.CompletePipeline("Cancelled");
    }
    catch (Exception ex)
    {
        _workspaceViewModel.CompletePipeline($"Failed: {ex.Message}");
    }
}
```

#### BoolOrToVisibilityConverter (if not already available)

```csharp
public class BoolOrToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.OfType<bool>().Any(b => b)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

### Behavior

- `CreatePipelineProgress(runId)` now returns a tuple `(IProgress<PipelineProgress>, CancellationToken)`. Tab VMs pass the token to orchestrator methods.
- The RunId (first 8 hex chars) displays in monospace at the left of the strip, enabling log correlation.
- The ❌ Cancel button is visible only while `IsPipelineRunning` is true. Clicking it calls `_pipelineCts.Cancel()`, which causes the orchestrator to throw `OperationCanceledException`.
- Tab VMs catch `OperationCanceledException` and call `CompletePipeline("Cancelled")`.
- On success, tab VMs call `CompletePipeline(...)` with a descriptive summary (e.g. "Completed: 12 pages generated").
- After `CompletePipeline`, the strip hides the ProgressBar and Cancel button but continues to show RunId + summary message for 3 seconds via `DispatcherTimer`.
- After 3 seconds, the timer fires, resets all progress properties, and collapses the strip entirely.
- `CompletePipeline` disposes the `CancellationTokenSource` to avoid leaks.
- If a new pipeline starts within the 3-second summary window, `CreatePipelineProgress` stops the timer and resets state cleanly.

## Checklist

- [ ] Add `_pipelineCts` field and `PipelineRunId` property to `WorkspaceViewModel`
- [ ] Add `IsPipelineComplete` property for summary display phase
- [ ] Change `CreatePipelineProgress()` signature to accept `Guid runId` and return `(IProgress, CancellationToken)`
- [ ] Implement `CancelPipelineCommand` with CanExecute guard
- [ ] Implement `CompletePipeline(string summaryMessage)` with DispatcherTimer
- [ ] Add `BoolOrToVisibilityConverter` (or equivalent multi-binding converter)
- [ ] Update progress strip XAML: add RunId label, Cancel button, multi-bind visibility
- [ ] Hide ProgressBar row during completion summary phase
- [ ] Update tab VMs to destructure tuple and pass `CancellationToken` to orchestrator
- [ ] Wrap tab VM pipeline calls in try/catch for `OperationCanceledException`
- [ ] Call `CompletePipeline(...)` in all exit paths (success, cancel, error)
- [ ] Verify timer disposes cleanly when new pipeline starts during summary
- [ ] Verify Cancel button disappears immediately on cancel (IsPipelineRunning → false)
