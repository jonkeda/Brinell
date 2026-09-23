# Step 07a-5 — Red Border on Browser During Recording

## Objective

Add a visual red border around the browser area when recording is active, providing a clear at-a-glance indicator.

## Current State

The browser is hosted in a plain `ContentControl`:
```xml
<ContentControl Grid.Column="0" x:Name="ContentArea"/>
```

The `BrowserView` is set into `ContentArea.Content` from code-behind in `MainWindow.xaml.cs`.

## Changes

### 1. Wrap `ContentControl` in a `Border` with a `DataTrigger`

```xml
<Border x:Name="BrowserBorder" Grid.Column="0">
    <Border.Style>
        <Style TargetType="Border">
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="BorderBrush" Value="Transparent"/>
            <Style.Triggers>
                <DataTrigger Binding="{Binding DataContext.Recording.IsRecording,
                             RelativeSource={RelativeSource AncestorType=Window}}" Value="True">
                    <Setter Property="BorderThickness" Value="3"/>
                    <Setter Property="BorderBrush" Value="Red"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
    <ContentControl x:Name="ContentArea"/>
</Border>
```

### Design notes

- 3px border — visible but not intrusive
- `Transparent` default — no layout shift when recording starts/stops (border space is reserved but invisible)
- Uses `DataTrigger` instead of a converter — cleaner for boolean-to-visual property mapping
- The `ContentControl` stays inside the `Border`, so `ContentArea.Content = browserView` in code-behind still works

### Alternative: no layout shift

If 3px border reservation is undesirable when not recording, use `BorderThickness="0"` default (no space reserved) and accept a 3px layout shift on record start. In practice this is barely noticeable.

## Files Modified

| File | Action |
|------|--------|
| `MainWindow.xaml` | **Edit** — wrap `ContentControl` in a `Border` with recording trigger |

## Verification

- Build succeeds
- Not recording: no border visible around browser
- Start recording: red 3px border appears
- Stop recording: border disappears

## Checklist

- [ ] `ContentControl` wrapped in `Border` with `DataTrigger`
- [ ] Red border visible only when `Recording.IsRecording == true`
- [ ] No layout issues when toggling recording
- [ ] Build succeeds
