# Step 07a-3 — Update Sidebar XAML

## Objective

Replace the two placeholder `ListView`s in `MainWindow.xaml` with a proper sidebar layout that shows site header, recording indicator, session pages, corpus pages, and controls.

## Current State

```xml
<!-- Sidebar -->
<DockPanel Grid.Column="0"
           Visibility="{Binding HasActiveSite, Converter={StaticResource BoolToVisibility}}">
    <TextBlock DockPanel.Dock="Top" Text="Pages" FontWeight="Bold" Margin="4"/>
    <ListView DockPanel.Dock="Top"
              ItemsSource="{Binding Sidebar.Pages}"
              Height="250"
              Margin="2"/>
    <TextBlock DockPanel.Dock="Top" Text="Controls" FontWeight="Bold" Margin="4"/>
    <ListView ItemsSource="{Binding Sidebar.Controls}"
              Margin="2"/>
</DockPanel>
```

## Changes

Replace the sidebar `DockPanel` contents:

```xml
<!-- Sidebar -->
<DockPanel Grid.Column="0"
           Visibility="{Binding HasActiveSite, Converter={StaticResource BoolToVisibility}}">

    <!-- Site header -->
    <StackPanel DockPanel.Dock="Top" Margin="4">
        <TextBlock Text="{Binding Sidebar.SiteHeader}" FontWeight="Bold" FontSize="13"/>
        <TextBlock Text="{Binding Sidebar.CorpusStats}" Foreground="Gray" FontSize="11" Margin="0,2,0,0"/>
    </StackPanel>

    <!-- Recording indicator (only visible when recording) -->
    <TextBlock DockPanel.Dock="Top" Margin="4,4,4,0"
               Foreground="Red" FontWeight="Bold" FontSize="11"
               Text="{Binding Recording.RecordingStatus}"
               Visibility="{Binding Recording.IsRecording, Converter={StaticResource BoolToVisibility}}"/>

    <!-- Scrollable content -->
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel>

            <!-- This Session section (only visible when recording) -->
            <StackPanel Visibility="{Binding Sidebar.IsRecording, Converter={StaticResource BoolToVisibility}}">
                <TextBlock Text="This Session" FontWeight="SemiBold" FontSize="11"
                           Margin="4,8,4,2" Foreground="#555"/>
                <Separator Margin="4,0"/>
                <ItemsControl ItemsSource="{Binding Sidebar.SessionPages}" Margin="2,0">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <TextBlock Margin="4,1" Cursor="Hand" FontSize="11">
                                <Run Text="{Binding StatusIcon, Mode=OneWay}"/>
                                <Run Text=" "/>
                                <Run Text="{Binding Name, Mode=OneWay}"/>
                            </TextBlock>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>

            <!-- Corpus Pages section -->
            <TextBlock Text="Corpus Pages" FontWeight="SemiBold" FontSize="11"
                       Margin="4,8,4,2" Foreground="#555"/>
            <Separator Margin="4,0"/>
            <ItemsControl ItemsSource="{Binding Sidebar.CorpusPages}" Margin="2,0">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Margin="4,1" Cursor="Hand" FontSize="11">
                            <Run Text="{Binding StatusIcon, Mode=OneWay}"/>
                            <Run Text=" "/>
                            <Run Text="{Binding Name, Mode=OneWay}"/>
                        </TextBlock>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>

            <!-- Placeholder when no corpus pages -->
            <TextBlock Text="No pages recorded yet"
                       Margin="8,4" FontSize="11" Foreground="Gray" FontStyle="Italic">
                <TextBlock.Visibility>
                    <MultiBinding Converter="{StaticResource BoolToVisibility}">
                        <!-- Show only when CorpusPages.Count == 0 -->
                    </MultiBinding>
                </TextBlock.Visibility>
            </TextBlock>

            <!-- Controls section -->
            <TextBlock Text="Controls" FontWeight="SemiBold" FontSize="11"
                       Margin="4,8,4,2" Foreground="#555"/>
            <Separator Margin="4,0"/>
            <ItemsControl ItemsSource="{Binding Sidebar.Controls}" Margin="2,0">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding}" Margin="4,1" FontSize="11"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>

        </StackPanel>
    </ScrollViewer>
</DockPanel>
```

### Design decisions

- **`ItemsControl` over `ListView`**: no selection needed (clicking navigates in Step 07a-6), simpler styling
- **`ScrollViewer` wrapping entire content**: sidebar can get tall with many pages
- **Section headers**: light gray `#555` semi-bold text with `Separator` below, matching Visual Studio tool window style
- **Empty state**: "No pages recorded yet" shown when `CorpusPages` is empty (deferred — need a converter or trigger, exact approach TBD during implementation)
- **Font sizes**: 13px for site name, 11px for everything else (compact sidebar)

### Click-to-navigate (deferred to Step 07a-6)

Page items will get `MouseLeftButtonUp` handlers or a command binding to navigate the browser. Not wired in this step — first get the visual layout working.

## Files Modified

| File | Action |
|------|--------|
| `MainWindow.xaml` | **Edit** — replace sidebar `DockPanel` contents |

## Verification

- Build succeeds
- Sidebar shows site header + corpus stats when site is selected
- "This Session" section hidden when not recording
- "Corpus Pages" section visible (empty for now)
- "Controls" section visible (empty for now)
- Sidebar scrolls when content overflows

## Checklist

- [ ] Site header with name + stats
- [ ] Recording indicator visible only when recording
- [ ] "This Session" section with `SessionPages` binding, visible only when recording
- [ ] "Corpus Pages" section with `CorpusPages` binding, always visible
- [ ] "Controls" section with `Controls` binding, always visible
- [ ] `ScrollViewer` wraps all sidebar content
- [ ] Build succeeds
