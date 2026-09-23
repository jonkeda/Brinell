# Step 1.3 — Main Window Layout: Sidebar, Toolbar, Content Area

## Objective

Build the main window shell with a persistent sidebar, toolbar with browser navigation and workflow buttons, and a switchable content area.

## Dependencies

- Step 1.1 (project exists)
- Step 1.2 (MVVM foundation — ViewModelBase, commands, DI)
- NuGet: `Microsoft.Web.WebView2` (latest stable)

## Implementation

XAML structure in `MainWindow.xaml`:

```xml
<DockPanel>
  <!-- Menu bar -->
  <Menu DockPanel.Dock="Top">
    <MenuItem Header="_Site">
      <MenuItem Header="_Manage Controls" Command="{Binding ManageControlsCommand}"/>
      <MenuItem Header="_Browse Corpus" Command="{Binding BrowseCorpusCommand}"/>
      <Separator/>
      <MenuItem Header="_Switch Site..." Command="{Binding SwitchSiteCommand}"/>
    </MenuItem>
  </Menu>

  <!-- Toolbar with navigation + workflow buttons -->
  <ToolBar DockPanel.Dock="Top">
    <Button Command="{Binding GoBackCommand}" Content="◀" ToolTip="Back"/>
    <Button Command="{Binding GoForwardCommand}" Content="▶" ToolTip="Forward"/>
    <Button Command="{Binding RefreshCommand}" Content="↻" ToolTip="Refresh"/>
    <TextBox Text="{Binding AddressUrl, UpdateSourceTrigger=PropertyChanged}"
             KeyDown="AddressBar_KeyDown" MinWidth="400"/>
    <Button Command="{Binding NavigateCommand}" Content="Go"/>
    <Separator/>
    <Button Command="{Binding InspectCommand}" Content="🔍" ToolTip="Inspect"/>
    <Button Command="{Binding RecordCommand}" Content="⏺" ToolTip="Record"/>
    <Button Command="{Binding AnalyzeCommand}" Content="🔬" ToolTip="Analyze"/>
  </ToolBar>

  <!-- StatusBar at bottom (step 1.5) -->
  <StatusBar DockPanel.Dock="Bottom"/>

  <!-- Main body: sidebar + content -->
  <Grid>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="180"/>  <!-- Sidebar -->
      <ColumnDefinition Width="Auto"/>  <!-- Splitter -->
      <ColumnDefinition Width="*"/>    <!-- Content area -->
    </Grid.ColumnDefinitions>

    <!-- Sidebar: corpus state -->
    <DockPanel Grid.Column="0">
      <TextBlock DockPanel.Dock="Top" Text="Pages" FontWeight="Bold" Margin="4"/>
      <ListView DockPanel.Dock="Top" ItemsSource="{Binding Pages}" Height="200">
        <!-- Status icon + page name -->
      </ListView>
      <TextBlock DockPanel.Dock="Top" Text="Controls" FontWeight="Bold" Margin="4"/>
      <ListView ItemsSource="{Binding Controls}">
        <!-- Control name + type -->
      </ListView>
    </DockPanel>

    <GridSplitter Grid.Column="1" Width="4" ResizeBehavior="PreviousAndNext"/>

    <!-- Content area: switches between views -->
    <ContentPresenter Grid.Column="2" Content="{Binding ActiveView}"/>
    <!-- ActiveView switches between: BrowserView, InspectorView,
         RecordingView, AnalysisView, ControlsView,
         GenerationView, CorpusView -->
  </Grid>
</DockPanel>
```

### Key points

- **Sidebar** (180px default, resizable via `GridSplitter`) shows pages with status icons and controls list
- **Toolbar** has two groups: browser nav (Back/Forward/Refresh/Address/Go) and workflow (Inspect/Record/Analyze)
- **Content area** uses `ContentPresenter` bound to `ActiveView` — views are swapped by the ViewModel
- **"Site" menu** provides Manage Controls, Browse Corpus, and Switch Site

### WebView2 initialization

```csharp
await webView.EnsureCoreWebView2Async(environment);
```

Back/Forward/Refresh buttons bound to `CoreWebView2.GoBack()`, `GoForward()`, `Reload()` via commands. Address bar navigates on Enter key press or Go button click.

## Checklist

- [ ] Menu bar with Site menu items
- [ ] Toolbar with all 8 buttons + address bar
- [ ] Sidebar with Pages and Controls list views
- [ ] GridSplitter resizes sidebar
- [ ] ContentPresenter shows active view
- [ ] WebView2 control initializes and renders a page
