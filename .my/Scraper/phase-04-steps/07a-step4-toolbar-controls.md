# Step 07a-4 — Toolbar Recording Controls

## Objective

Replace the single ⏺ `ToggleButton` with distinct Start / Stop / Pause / Resume buttons that show/hide based on recording state.

## Current State

```xml
<ToggleButton Command="{Binding RecordCommand}" Content="⏺" ToolTip="Record" Padding="6,2"
              IsChecked="{Binding Recording.IsRecording, Mode=OneWay}"/>
```

`RecordCommand` is a `RelayCommand` that calls `ToggleRecording()` (start or stop).

`RecordingViewModel` already has individual commands:
- `StartRecordingCommand` — can execute when `!IsRecording`
- `StopRecordingCommand` — can execute when `IsRecording`
- `PauseRecordingCommand` — can execute when `IsRecording && !IsPaused`
- `ResumeRecordingCommand` — can execute when `IsRecording && IsPaused`

## Changes

### 1. Replace the ToggleButton in `MainWindow.xaml`

Remove:
```xml
<ToggleButton Command="{Binding RecordCommand}" Content="⏺" ToolTip="Record" Padding="6,2"
              IsChecked="{Binding Recording.IsRecording, Mode=OneWay}"/>
```

Replace with:
```xml
<!-- Record: visible when NOT recording -->
<Button Command="{Binding Recording.StartRecordingCommand}"
        Content="⏺" ToolTip="Start Recording" Padding="6,2"
        Visibility="{Binding Recording.IsRecording,
                     Converter={StaticResource BoolToVisibility},
                     ConverterParameter=Invert}"/>

<!-- Stop: visible when recording -->
<Button Command="{Binding Recording.StopRecordingCommand}"
        Content="⏹" ToolTip="Stop Recording" Padding="6,2"
        Visibility="{Binding Recording.IsRecording,
                     Converter={StaticResource BoolToVisibility}}"/>

<!-- Pause: visible when recording AND not paused -->
<Button Command="{Binding Recording.PauseRecordingCommand}"
        Content="⏸" ToolTip="Pause Recording" Padding="6,2"
        Visibility="{Binding Recording.IsPaused,
                     Converter={StaticResource BoolToVisibility},
                     ConverterParameter=Invert}"/>

<!-- Resume: visible when recording AND paused -->
<Button Command="{Binding Recording.ResumeRecordingCommand}"
        Content="▶" ToolTip="Resume Recording" Padding="6,2"
        Visibility="{Binding Recording.IsPaused,
                     Converter={StaticResource BoolToVisibility}}"/>
```

### 2. Add inverted `BoolToVisibility` support

The existing `BoolToVisibilityConverter` may not support an `Invert` parameter. Two options:

**Option A** — Add `ConverterParameter` support to the existing converter:
```csharp
public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
{
    var boolValue = value is true;
    if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        boolValue = !boolValue;
    return boolValue ? Visibility.Visible : Visibility.Collapsed;
}
```

**Option B** — Add a second converter `InverseBoolToVisibilityConverter` in App.xaml resources. Simpler but more XAML resources.

Recommend **Option A** — single converter, consistent usage.

### 3. Pause/Resume visibility logic

The Pause and Resume buttons need dual conditions (recording AND paused/not-paused). Since `PauseRecordingCommand.CanExecute` already encodes this (`IsRecording && !IsPaused`), the simplest approach is:

- Pause/Resume buttons both have `Visibility` bound to `IsRecording` (show when recording)
- But only one is visible at a time — bind to `IsPaused`:
  - Pause: visible when `!IsPaused` (needs Invert)
  - Resume: visible when `IsPaused`
- When not recording, both are hidden because `CanExecute = false` disables them, but they're still visible. Better to also bind their parent visibility to `IsRecording`.

Simplest: wrap Pause+Resume in a `StackPanel` bound to `IsRecording` visibility:

```xml
<!-- Pause/Resume container: visible only when recording -->
<StackPanel Orientation="Horizontal"
            Visibility="{Binding Recording.IsRecording,
                         Converter={StaticResource BoolToVisibility}}">
    <Button Command="{Binding Recording.PauseRecordingCommand}"
            Content="⏸" ToolTip="Pause" Padding="6,2">
        <Button.Visibility>
            <Binding Path="Recording.IsPaused"
                     Converter="{StaticResource BoolToVisibility}"
                     ConverterParameter="Invert"/>
        </Button.Visibility>
    </Button>
    <Button Command="{Binding Recording.ResumeRecordingCommand}"
            Content="▶" ToolTip="Resume" Padding="6,2"
            Visibility="{Binding Recording.IsPaused,
                         Converter={StaticResource BoolToVisibility}}"/>
</StackPanel>
```

### 4. Expose `IsPaused` for binding

`RecordingViewModel.IsPaused` has a `private set` — this is fine for OneWay binding from XAML. No change needed.

### 5. Optionally remove `RecordCommand` from `MainViewModel`

After this step, the toolbar no longer uses `RecordCommand`. It can be removed (or kept for keyboard shortcut binding later). Recommend keeping it for now — no harm.

## Files Modified

| File | Action |
|------|--------|
| `MainWindow.xaml` | **Edit** — replace ToggleButton with Start/Stop/Pause/Resume buttons |
| `Converters/BoolToVisibilityConverter.cs` | **Edit** — add `Invert` parameter support |

## Verification

- Build succeeds
- Before recording: only ⏺ visible
- During recording: ⏹ + ⏸ visible, ⏺ hidden
- When paused: ⏹ + ▶ visible, ⏸ hidden
- After stop: back to ⏺ only

## Checklist

- [ ] ⏺ Start button visible only when not recording
- [ ] ⏹ Stop button visible only when recording
- [ ] ⏸ Pause button visible when recording and not paused
- [ ] ▶ Resume button visible when recording and paused
- [ ] `BoolToVisibilityConverter` supports `Invert` parameter
- [ ] Build succeeds
