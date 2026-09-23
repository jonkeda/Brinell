# Phase 1 — WPF Shell & Embedded Browser

## Goal

Create the foundational WPF application with a site selector start screen, persistent sidebar, and embedded WebView2 browser. The app starts with a site corpus picker where the user creates or selects a site corpus before browsing. A sidebar shows corpus state (pages, controls, stats) alongside the browser and other switchable views.

## Tasks

### 1.1 — Create `Brinell.Scraper` WPF Project (.NET 10)

**Implementation Details:**

- Create a new WPF Application project targeting `net10.0-windows`
- Project file (`Brinell.Scraper.csproj`):
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <OutputType>WinExe</OutputType>
      <TargetFramework>net10.0-windows</TargetFramework>
      <UseWPF>true</UseWPF>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>
  </Project>
  ```
- Project structure:
  ```
  Brinell.Scraper/
    App.xaml / App.xaml.cs
    MainWindow.xaml / MainWindow.xaml.cs
    ViewModels/
    Views/
    Services/
    Models/
    Converters/
    Resources/
    Data/          # Corpus SQLite access, repositories
    Corpus/        # Corpus services (page tracking, control registry)
  ```
- `App.xaml.cs` — application entry point, DI container bootstrap (step 1.2)
- `MainWindow.xaml` — top-level shell with menu bar, content area, status bar

---

### 1.2 — Main Window Layout: Sidebar, Toolbar, Content Area

**Implementation Details:**

- NuGet package: `Microsoft.Web.WebView2` (latest stable)
- XAML structure in `MainWindow.xaml`:
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

    <!-- StatusBar at bottom (task 1.5) -->
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
- WebView2 initialization in code-behind or via ViewModel:
  ```csharp
  await webView.EnsureCoreWebView2Async(environment);
  ```
- Back/Forward/Refresh buttons bound to `CoreWebView2.GoBack()`, `GoForward()`, `Reload()` via commands
- Address bar `TextBox` navigates on Enter key press or Go button click

---

### 1.3 — Navigation Support

**Implementation Details:**

- Subscribe to WebView2 navigation events:
  ```csharp
  webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
  webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
  webView.CoreWebView2.SourceChanged += OnSourceChanged;
  ```
- `NavigationStarting` — update status bar ("Navigating to ..."), show loading indicator, validate URL
- `NavigationCompleted` — hide loading indicator, update status bar with result, handle navigation errors (`e.IsSuccess`, `e.WebErrorStatus`)
- `SourceChanged` — sync address bar `TextBox` with the current URL so it reflects redirects and in-page navigations
- User browses to target sites (Exact Online, Synergy, etc.) by typing URL or following links

---

### 1.4 — Cookie / Session Persistence

**Implementation Details:**

- Create a `CoreWebView2Environment` with a custom user data folder so cookies, localStorage, and session state survive app restarts:
  ```csharp
  var userDataFolder = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "Brinell.Scraper", "WebView2Data");

  var environment = await CoreWebView2Environment.CreateAsync(
      browserExecutableFolder: null,
      userDataFolder: userDataFolder);

  await webView.EnsureCoreWebView2Async(environment);
  ```
- The user data folder persists cookies, cache, IndexedDB, etc. — user stays logged into Exact Online / Synergy across sessions
- On application exit, do **not** clear the user data folder (preserve sessions)
- Optionally expose a "Clear Session Data" menu item that deletes the user data folder contents

---

### 1.5 — Status Bar, Loading Indicator, Title Bar, Dev-Tools Toggle

**Implementation Details:**

- **Title bar** — shows active site name: `"Brinell Scraper — {SiteName}"`
  ```csharp
  Title = $"Brinell Scraper — {ActiveSite.Name}";
  ```
- **Status bar** at the bottom of `MainWindow`:
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
- `StatusText` — shows current URL or "Ready" / "Loading..." / error messages
- `CorpusStats` — shows page count, control count, generation progress
- `IsLoading` — bound to an indeterminate `ProgressBar`, toggled by `NavigationStarting` / `NavigationCompleted`
- **"Site" menu** in the menu bar (see task 1.2) with items: Manage Controls, Browse Corpus, Switch Site
- **Dev-tools toggle** button in toolbar or menu:
  ```csharp
  webView.CoreWebView2.OpenDevToolsWindow();
  ```
- Toggle via a `Button` or `CheckBox` in the toolbar; DevTools opens in a separate window (WebView2 default behavior)

---

### 1.6 — Start Screen / Site Selector

**Implementation Details:**

- On launch, show a **site selector** view (either a separate `Window` or the initial `ActiveView` in `MainWindow`) instead of going directly to the browser
- The site selector displays:
  - A list of existing site corpuses (name, URL, last-opened date, page/control counts)
  - A **"New Site"** button that opens a dialog
- **New Site dialog** collects:
  - **Site name** — display name (e.g. "Exact Online")
  - **Start URL** — the base URL to open in the browser
  - **URL aliases** — additional URL patterns for regional variants (e.g. `start.exactonline.nl`, `start.exactonline.be`, `start.exactonline.de`) that share the same control set
  - **Namespace** — C# namespace for generated code (e.g. `ExactOnline`)
  - **Output path** — where generated Brinell page objects are written
- Per-site settings are stored in the **corpus SQLite database** (`Data/` folder)
- After selecting or creating a site, the app transitions to the main window layout (sidebar + browser)
- A "Switch Site" menu item (Site → Switch Site) returns to the site selector

---

## Acceptance Criteria

- [ ] Application launches with a **site selector** showing existing corpuses and a "New Site" button
- [ ] User can create a new site corpus (name, start URL, aliases, namespace, output path)
- [ ] After selecting a site, main window shows **sidebar** (pages/controls lists) and **browser**
- [ ] Title bar shows active site name ("Brinell Scraper — {SiteName}")
- [ ] Sidebar (180px, resizable via GridSplitter) displays pages with status icons and controls list
- [ ] Toolbar includes browser nav (Back/Forward/Refresh/Address/Go) and workflow buttons (Inspect/Record/Analyze)
- [ ] Content area switches between views: BrowserView, InspectorView, RecordingView, AnalysisView, ControlsView, GenerationView, CorpusView
- [ ] "Site" menu provides Manage Controls, Browse Corpus, and Switch Site actions
- [ ] User can type a URL in the address bar and press Enter or click Go to navigate
- [ ] Back, Forward, and Refresh buttons work correctly
- [ ] Address bar updates to reflect the current URL after navigation and redirects
- [ ] Cookies and sessions persist — logging into Exact Online survives an app restart
- [ ] Status bar shows site name, current URL, corpus stats (page count, control count), and loading state
- [ ] Loading indicator (progress bar) appears during navigation and hides on completion
- [ ] Dev-tools can be toggled open/closed
- [ ] Navigation errors are handled gracefully (status bar shows error, no crash)

## Dependencies

| Dependency | Purpose |
|---|---|
| .NET 10 SDK | Target framework (`net10.0-windows`) |
| `Microsoft.Web.WebView2` NuGet | Embedded Chromium browser control for WPF |
| `Microsoft.Data.Sqlite` NuGet | Corpus storage (site settings, pages, controls) |
| WebView2 Runtime | Chromium runtime required on end-user machines (auto-installed with Evergreen or bundled Fixed Version) |

---

## Unit Test Plan

> Full test details in [unittest-roadmap.md](unittest-roadmap.md)

### Testable Components (~62 tests)

| Component | Tests | Strategy |
|-----------|-------|----------|
| `ViewModelBase` | 9 | SetProperty change tracking, equality, null handling |
| `RelayCommand` / `RelayCommand<T>` | 8 | Execute, CanExecute, parameter passing, event firing |
| `AsyncRelayCommand` / `AsyncRelayCommand<T>` | 7 | Async execution, IsRunning, re-entry prevention, cancellation |
| `MainViewModel` | 12 | Site selection flow, command states, property updates, events |
| `BrowserViewModel` | 10 | Navigation events, address sync, history state, command states |
| `SiteSelectionViewModel` | 6 | Site loading, selection events, empty state |
| `BoolToVisibilityConverter` | 4 | true/false → Visible/Collapsed for both converters |
| `CorpusDatabase` | 6 | SQLite CRUD, table creation, alias storage |

### Not Unit-Tested (UI-dependent)

- `MainWindow.xaml.cs` — code-behind with WebView2 wiring
- `BrowserView.xaml.cs` — WebView2 initialization and events
- `SiteSelectionView.xaml.cs` — dialog interactions
- `NewSiteDialog.xaml.cs` — form validation and dialog result

### Test Infrastructure

- **Framework:** xUnit
- **Mocking:** NSubstitute for `ILogger<T>`, `CorpusDatabase`
- **Database:** In-memory SQLite (`Data Source=:memory:`) for `CorpusDatabase` tests
