# RCA-012: White Space Gaps When Resizing Inspector Panel

**Reported:** 2026-04-22
**Severity:** Low
**Component:** `MainWindow.xaml`

---

## Symptoms

When dragging the `GridSplitter` between the browser content area and the inspector panel, white space appears on the left and/or right sides of the inspector. The panel doesn't fill its column properly during or after resize.

## Root Cause

The content area grid has three columns:

```xml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>        <!-- browser content -->
    <ColumnDefinition Width="Auto"/>     <!-- splitter -->
    <ColumnDefinition Width="Auto"/>     <!-- inspector panel -->
</Grid.ColumnDefinitions>
```

The inspector column (column 2) is `Width="Auto"`, meaning it sizes to its content. The `DockPanel` inside has a **fixed** `Width="300"`:

```xml
<DockPanel Grid.Column="2" Width="300" ...>
```

The `GridSplitter` uses `ResizeBehavior="PreviousAndNext"`, which resizes column 1 (the splitter's own `Auto` column) and column 2. But since column 2 is `Auto` and the `DockPanel` has a fixed width, the splitter can only push the panel around — it can't actually resize it. This creates gaps between the column boundary and the panel content.

**The fix:** Change the inspector column to a fixed or proportional width and remove the fixed `Width` from the `DockPanel`. The `DockPanel` should stretch to fill its column.

## Fix

```xml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="Auto"/>
    <ColumnDefinition Width="300"/>   <!-- fixed initial width, resizable via splitter -->
</Grid.ColumnDefinitions>
```

And remove `Width="300"` from the `DockPanel`, replacing it with default stretch behavior:

```xml
<DockPanel Grid.Column="2"
           Visibility="...">
```

The `GridSplitter` with `ResizeBehavior="PreviousAndNext"` then correctly resizes the `*` column and the `300` column. The `DockPanel` fills whatever width the column has — no gaps.

## Status

- [ ] Inspector column changed from `Auto` to fixed `300`
- [ ] `DockPanel` `Width="300"` removed
- [ ] No white space gaps when resizing
