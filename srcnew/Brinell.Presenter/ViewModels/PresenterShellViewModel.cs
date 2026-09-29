using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Brinell.Bdd.Preview;
using Brinell.Presenter.Commands;
using Brinell.Presenter.Models;
using Brinell.Presenter.Services;
using Brinell.Uat;

namespace Brinell.Presenter.ViewModels;

public sealed partial class PresenterShellViewModel : ViewModelBase, IStepRowHost
{
    private readonly IBddExecutionService _executionService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IPresenterUserSettingsService _settingsService;
    private readonly List<BddWorkspaceNodeViewModel> _allWorkspaceNodes = [];
    private readonly List<PresenterStepTiming> _stepTimings = [];
    private readonly IBddWorkspaceService _workspaceService;
    private PresenterUatExecutionSession? _activeSession;
    private BddScenarioViewModel? _activeScenario;
    private string _allWorkspaceTreeText = string.Empty;
    private string _autPlacementText = string.Empty;
    private string _commandCatalogText = string.Empty;
    private string _diagnosticsText = string.Empty;
    private string _discoveryText = string.Empty;
    private CancellationTokenSource? _executionCancellation;
    private int _executionDelayMilliseconds = 250;
    private string _executionDelayText = "250";
    private PresenterSpeed _speed;
    private PresenterSpeeds _speeds = new();
    private string _executionTimingText = string.Empty;
    private bool _isRecentFoldersExpanded;
    private bool _isSelectionExpanded;
    private string _recentFoldersText = "No recent folders";
    private double _runProgress;
    private string _scenarioListText = string.Empty;
    private PresenterRunExecutionOptions? _activeRunOptions;
    private string _runScopeText = string.Empty;
    private BddScenarioViewModel? _selectedScenario;
    private string _selectedTab = TreeTabName;
    private BddWorkspaceNodeViewModel? _selectedWorkspaceNode;
    private string _selectedWorkspaceNodeDetailsText = "No selection";
    private string _statusSummary = "No workspace loaded";
    private PresenterTheme _theme = PresenterTheme.System;
    private string _stepListText = string.Empty;
    private string _workspaceConfigText = string.Empty;
    private string _workspaceName = "No workspace";
    private string? _workspacePath;
    private BddWorkspaceNodeViewModel? _workspaceRoot;
    private string _workspaceSummaryText = string.Empty;
    private string _workspaceTreeText = string.Empty;
    private readonly IProjectBuilder _projectBuilder = new MsBuildProjectBuilder();
    private CancellationTokenSource? _buildCancellation;
    private string _buildLogText = string.Empty;
    private string _buildConfiguration = "Debug";
    private string _platform = "windows";
    private bool _isBuilding;
    private bool _isScenariosExpanded = true;
    private bool _isStepsExpanded = true;
    private BddStepRowViewModel? _currentRow;
    private BddCatalogSet? _catalogs;
    private readonly IScenarioPreviewService? _preview;
    private readonly PresenterBreakpointStore _breakpoints;
    private bool _isMoving;
    private readonly IPresenterWindowService? _windowService;
    private PresenterDock _dock;
    private bool _isPresentMode;
    private bool _needsBuild;
    private object? _placementSnapshot;
    private PresenterAppWindow _appWindow;
    private PresenterRunEnd _runEnd;
    private PresenterUatExecutionSession? _endedSession;
    private bool _placedThisRun;

    private const int MaxExecutionDelayMilliseconds = 99999;

    private const string TreeTabName = "Tree";
    private const string ConfigTabName = "Config";
    private const string DiagnosticsTabName = "Diagnostics";
    private const string DiscoveryTabName = "Discovery";
    private const string CommandCatalogTabName = "Command Catalog";
    private const string SettingsTabName = "Settings";

    public PresenterShellViewModel(
        IBddWorkspaceService workspaceService,
        IBddExecutionService executionService,
        IFolderPickerService folderPickerService,
        IPresenterUserSettingsService settingsService,
        IScenarioPreviewService? previewService = null,
        PresenterBreakpointStore? breakpoints = null,
        IPresenterWindowService? windowService = null,
        IPresenterRecorder? recorder = null)
    {
        _workspaceService = workspaceService;
        _windowService = windowService;
        _breakpoints = breakpoints ?? new PresenterBreakpointStore();
        _preview = previewService;
        if (_preview is not null)
        {
            _preview.Ready += RefreshPreview;
            _preview.Message += OnPreviewMessage;
        }

        _executionService = executionService;
        _folderPickerService = folderPickerService;
        _settingsService = settingsService;

        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync);
        ToggleRecentFoldersCommand = new RelayCommand(() => IsRecentFoldersExpanded = !IsRecentFoldersExpanded);
        ReloadCommand = new RelayCommand(ReloadWorkspace, () => WorkspacePath is not null);
        RunCommand = new AsyncRelayCommand(RunAsync, () => !_isMoving);
        StopCommand = new RelayCommand(Stop);
        NextStepCommand = new AsyncRelayCommand(StepAsync, () => WorkspacePath is not null && !_isMoving);
        StepIntoCommand = new AsyncRelayCommand(() => MoveAsync(PresenterMove.Into), () => IsPaused && CurrentRow?.IsCall == true);
        StepOutCommand = new AsyncRelayCommand(() => MoveAsync(PresenterMove.Out), () => IsPaused && CurrentRow?.IsInCall == true);
        RestartCommand = new AsyncRelayCommand(RestartAsync, () => WorkspacePath is not null && !_isMoving);
        ToggleSelectionCommand = new RelayCommand(() => IsSelectionExpanded = !IsSelectionExpanded);
        ShowTreeTabCommand = new RelayCommand(() => SelectedTab = TreeTabName);
        ShowConfigTabCommand = new RelayCommand(() => SelectedTab = ConfigTabName);
        ShowDiagnosticsTabCommand = new RelayCommand(() => SelectedTab = DiagnosticsTabName);
        ShowDiscoveryTabCommand = new RelayCommand(() => SelectedTab = DiscoveryTabName);
        ShowCommandCatalogTabCommand = new RelayCommand(() => SelectedTab = CommandCatalogTabName);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        BuildCommand = new AsyncRelayCommand(BuildWorkspaceProjectAsync, () => !_isBuilding);
        ToggleScenariosCommand = new RelayCommand(() => IsScenariosExpanded = !IsScenariosExpanded);
        ToggleStepsCommand = new RelayCommand(() => IsStepsExpanded = !IsStepsExpanded);
        OpenPreviewCommand = new RelayCommand(OpenPreview, () => _preview is not null);
        ShowPresentModeCommand = new RelayCommand(() => IsPresentMode = true);
        ShowWorkspaceModeCommand = new RelayCommand(() => IsPresentMode = false);
        ToggleDockCommand = new RelayCommand(ToggleDock);
        CycleSpeedCommand = new RelayCommand(CycleSpeed);
        ShowSettingsCommand = new RelayCommand(() => SelectedTab = IsSettingsTabSelected ? TreeTabName : SettingsTabName);

        ConfigEditor = new ConfigEditorViewModel(
            new FixtureInspector(),
            () => _folderPickerService.PickProjectAsync(),
            ReloadWorkspace);

        var settings = _settingsService.Load();
        _theme = settings.Theme;
        _dock = settings.Dock;
        _appWindow = settings.AppWindow;
        _runEnd = settings.RunEnd;
        _speeds = settings.Speeds ?? new PresenterSpeeds();
        _speed = settings.Speed;
        ExecutionDelayMilliseconds = _speeds.Of(_speed);
        InitializeRecording(settings, recorder);
        ApplyTheme();

        if (Application.Current is { } application)
        {
            application.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        RefreshRecentFolders(settings);
        LoadDefaultWorkspace();
    }

    /// <summary>The Config tab's editor for the workspace's two config rows.</summary>
    public ConfigEditorViewModel ConfigEditor { get; }

    public ObservableCollection<BddFileViewModel> Files { get; } = [];

    public ObservableCollection<BddScenarioViewModel> Scenarios { get; } = [];

    public ObservableCollection<BddStepViewModel> Steps { get; } = [];

    /// <summary>The selected scenario's step list: its steps, and a row for each <c>## Step:</c> call.</summary>
    public ObservableCollection<BddStepRowViewModel> StepRows { get; } = [];

    /// <summary>The row the run is paused before, or running; null when no run is on.</summary>
    public BddStepRowViewModel? CurrentRow
    {
        get => _currentRow;
        private set
        {
            var previous = _currentRow;
            if (SetProperty(ref _currentRow, value))
            {
                if (previous is not null)
                {
                    previous.IsCurrent = false;
                }

                if (value is not null)
                {
                    value.IsCurrent = true;
                }

                UpdateStepListText();
            }
        }
    }

    public bool IsScenariosExpanded
    {
        get => _isScenariosExpanded;
        set
        {
            if (SetProperty(ref _isScenariosExpanded, value))
            {
                OnPropertyChanged(nameof(ScenariosExpanderGlyph));
            }
        }
    }

    public string ScenariosExpanderGlyph => IsScenariosExpanded ? PresenterIcons.ChevronUp : PresenterIcons.ChevronDown;

    public bool IsStepsExpanded
    {
        get => _isStepsExpanded;
        set
        {
            if (SetProperty(ref _isStepsExpanded, value))
            {
                OnPropertyChanged(nameof(StepsExpanderGlyph));
            }
        }
    }

    public string StepsExpanderGlyph => IsStepsExpanded ? PresenterIcons.ChevronUp : PresenterIcons.ChevronDown;

    /// <summary>The Steps header: the selected scenario's name.</summary>
    public string StepsHeaderText => SelectedScenario is { } scenario ? $"Steps: {scenario.Name}" : "Steps";

    /// <summary>
    /// Present mode: the scenarios, the steps and the moves, nothing else. Workspace mode adds
    /// opening folders, building, the config and the diagnostics. A workspace that loads cleanly
    /// opens in Present mode; one with problems in Workspace mode.
    /// </summary>
    public bool IsPresentMode
    {
        get => _isPresentMode;
        set
        {
            if (SetProperty(ref _isPresentMode, value))
            {
                OnPropertyChanged(nameof(IsWorkspaceMode));
                if (value)
                {
                    SelectedTab = TreeTabName;
                }
            }
        }
    }

    public bool IsWorkspaceMode => !IsPresentMode;

    public ICommand ShowPresentModeCommand { get; }

    public ICommand ShowWorkspaceModeCommand { get; }

    /// <summary>The side the bar docks to; the app under test is placed in the rest of the screen.</summary>
    public PresenterDock Dock
    {
        get => _dock;
        private set
        {
            if (SetProperty(ref _dock, value))
            {
                OnPropertyChanged(nameof(DockDescription));
            }
        }
    }

    /// <summary>The dock button's tooltip: where it moves the bar.</summary>
    public string DockDescription => Dock == PresenterDock.Left ? "Dock to the right" : "Dock to the left";

    public ICommand ToggleDockCommand { get; }

    /// <summary>Whether the workspace's suite is not built, so its steps cannot be checked or run.</summary>
    public bool NeedsBuild
    {
        get => _needsBuild;
        private set => SetProperty(ref _needsBuild, value);
    }

    /// <summary>Docks the window to the remembered side; called once the window exists.</summary>
    public void ApplyDock() => _windowService?.Dock(Dock);

    private void ToggleDock()
    {
        Dock = Dock == PresenterDock.Left ? PresenterDock.Right : PresenterDock.Left;
        _windowService?.Dock(Dock);
        try
        {
            var settings = _settingsService.Load();
            settings.Dock = Dock;
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            DiagnosticsText = $"Could not save the dock side: {ex.Message}";
        }
    }

    /// <summary>The App window choices, as the Settings page shows them.</summary>
    public IReadOnlyList<string> AppWindowChoices { get; } = [.. AppWindowNames.Values];

    private static IReadOnlyDictionary<PresenterAppWindow, string> AppWindowNames { get; } = new Dictionary<PresenterAppWindow, string>
    {
        [PresenterAppWindow.BesideBar] = "Beside the bar",
        [PresenterAppWindow.MaximizedOtherMonitor] = "Maximized, other monitor",
        [PresenterAppWindow.Maximized] = "Maximized",
        [PresenterAppWindow.WhereItOpens] = "Where it opens",
        [PresenterAppWindow.Background] = "Background",
    };

    /// <summary>How the app under test's window is shown during a run.</summary>
    public PresenterAppWindow AppWindow
    {
        get => _appWindow;
        set
        {
            if (SetProperty(ref _appWindow, value))
            {
                OnPropertyChanged(nameof(AppWindowChoice));
                SaveWindowSettings();
            }
        }
    }

    /// <summary><see cref="AppWindow"/> by its name on the Settings page.</summary>
    public string AppWindowChoice
    {
        get => AppWindowNames[AppWindow];
        set => AppWindow = AppWindowNames.FirstOrDefault(entry => entry.Value == value, new(AppWindow, value)).Key;
    }

    /// <summary>The When a run ends choices, as the Settings page shows them.</summary>
    public IReadOnlyList<string> RunEndChoices { get; } = ["Close the app", "Keep it open"];

    /// <summary>What happens to the app when a run ends.</summary>
    public PresenterRunEnd RunEnd
    {
        get => _runEnd;
        set
        {
            if (SetProperty(ref _runEnd, value))
            {
                OnPropertyChanged(nameof(RunEndChoice));
                SaveWindowSettings();
                if (value == PresenterRunEnd.CloseApp)
                {
                    ReleaseEndedSession();
                }
            }
        }
    }

    /// <summary><see cref="RunEnd"/> by its name on the Settings page.</summary>
    public string RunEndChoice
    {
        get => RunEndChoices[(int)RunEnd];
        set => RunEnd = value == RunEndChoices[1] ? PresenterRunEnd.KeepOpen : PresenterRunEnd.CloseApp;
    }

    /// <summary>Closes what is open: the run, and an app kept open after one. Called when Presenter closes.</summary>
    public void Shutdown()
    {
        StopActiveSession();
        ReleaseEndedSession();
    }

    private void SaveWindowSettings()
    {
        try
        {
            var settings = _settingsService.Load();
            settings.AppWindow = AppWindow;
            settings.RunEnd = RunEnd;
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            DiagnosticsText = $"Could not save the app window settings: {ex.Message}";
        }
    }

    // Before a run starts its app: an app kept open from the last run is closed, the launcher is
    // told how to show the app (BRINELL_AUT_WINDOW, and where: BRINELL_AUT_BOUNDS), and the
    // windows now are noted, to find the app's once it opens.
    private void SnapshotWindows()
    {
        ReleaseEndedSession();
        Dictionary<string, string> environment = new()
        {
            [Brinell.Core.Diagnostics.AutWindow.Variable] = AppWindow == PresenterAppWindow.Background ? "background" : "shown",
        };
        if (_windowService?.TargetBounds(AppWindow, Dock) is { } bounds)
        {
            environment[Brinell.Core.Diagnostics.AutWindow.BoundsVariable] = FormattableString.Invariant($"{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}");
        }

        _executionService.WindowEnvironment = environment;
        _windowService?.SetBarTopmost(AppWindow == PresenterAppWindow.Maximized);
        _placedThisRun = false;
        _placementSnapshot = _windowService?.Snapshot();
    }

    // Shows the app's window as the App window setting says once it has opened; tried again after
    // a step, for an app (a browser) that opens its window late. Presenter keeps the keys.
    private void TryPlaceApp()
    {
        if (_placementSnapshot is null || _windowService?.PlaceApp(_placementSnapshot, Dock, AppWindow) is not { } report)
        {
            _windowService?.ReclaimFocus();
            return;
        }

        _placementSnapshot = null;
        _placedThisRun = true;
        AutPlacementText = report;
    }

    // Keep it open: the ended run's session (its fixture and app) waits here until the next run,
    // Stop with no run on, or Presenter closing.
    private void ReleaseEndedSession()
    {
        _endedSession?.Dispose();
        _endedSession = null;
    }

    public ICommand ToggleScenariosCommand { get; }

    public ICommand ToggleStepsCommand { get; }

    /// <summary>Opens the Scenario Preview for the selected scenario, in a window of its own.</summary>
    public ICommand OpenPreviewCommand { get; }

    /// <summary>
    /// The visible rows, flattened. Still the source of <c>WorkspaceTreeText</c>, which the UAT
    /// suite asserts against; the on-screen tree binds <see cref="WorkspaceRootNodes"/>.
    /// </summary>
    public ObservableCollection<BddWorkspaceNodeViewModel> WorkspaceTreeNodes { get; } = [];

    /// <summary>The workspace root, as the one item the tree control nests everything under.</summary>
    public ObservableCollection<BddWorkspaceNodeViewModel> WorkspaceRootNodes { get; } = [];

    public ObservableCollection<RecentFolderViewModel> RecentFolders { get; } = [];

    public ICommand OpenFolderCommand { get; }

    public ICommand ToggleRecentFoldersCommand { get; }

    public ICommand ReloadCommand { get; }

    public ICommand RunCommand { get; }

    public ICommand StopCommand { get; }

    /// <summary>Step: runs the row the run is paused before; with no run on, starts one paused before its first step.</summary>
    public ICommand NextStepCommand { get; }

    /// <summary>At a <c>## Step:</c> call, goes inside it without running anything.</summary>
    public ICommand StepIntoCommand { get; }

    /// <summary>Inside a call, runs the rest of it and pauses after it.</summary>
    public ICommand StepOutCommand { get; }

    /// <summary>Starts the scenario again, paused before its first step.</summary>
    public ICommand RestartCommand { get; }

    /// <summary>Whether a run is paused before a row, waiting for a move.</summary>
    public bool IsPaused => _activeSession is not null && !_isMoving && CurrentRow is not null;

    public ICommand ToggleSelectionCommand { get; }

    public ICommand ShowTreeTabCommand { get; }

    public ICommand ShowConfigTabCommand { get; }

    public ICommand ShowDiagnosticsTabCommand { get; }

    public ICommand ShowDiscoveryTabCommand { get; }

    public ICommand ShowCommandCatalogTabCommand { get; }

    public ICommand ToggleThemeCommand { get; }

    /// <summary>Builds the workspace's project, streaming MSBuild's own output.</summary>
    public ICommand BuildCommand { get; }

    /// <summary>The build configurations offered in the toolbar.</summary>
    public IReadOnlyList<string> BuildConfigurations { get; } = ["Debug", "Release"];

    /// <summary>
    /// The platforms offered in the toolbar. Setting one sets <c>APPIUM_PLATFORM</c>, which the
    /// fixture reads before resolving its own app binary.
    /// </summary>
    /// <remarks>
    /// Narrowed to what the fixture declares through <c>[UatPlatforms]</c> when it declares
    /// anything: offering a platform the fixture has no binary for only produces a throw later.
    /// </remarks>
    public IReadOnlyList<string> Platforms
    {
        get
        {
            var declared = ConfigEditor.DeclaredPlatforms;
            return declared.Count == 0
                ? AllPlatforms
                : [.. AllPlatforms.Where(platform => declared.Any(name =>
                    name.Equals(platform, StringComparison.OrdinalIgnoreCase)))];
        }
    }

    private static IReadOnlyList<string> AllPlatforms { get; } = ["windows", "android", "ios"];

    /// <summary>The configuration the Build button uses.</summary>
    public string BuildConfiguration
    {
        get => _buildConfiguration;
        set
        {
            if (SetProperty(ref _buildConfiguration, value))
            {
                SaveWorkspacePreferences();
            }
        }
    }

    /// <summary>The platform a run targets.</summary>
    public string Platform
    {
        get => _platform;
        set
        {
            if (SetProperty(ref _platform, value))
            {
                Environment.SetEnvironmentVariable("APPIUM_PLATFORM", value);
                SaveWorkspacePreferences();
            }
        }
    }

    /// <summary>What the last build printed.</summary>
    public string BuildLogText
    {
        get => _buildLogText;
        private set => SetProperty(ref _buildLogText, value);
    }

    public string WorkspaceName
    {
        get => _workspaceName;
        private set => SetProperty(ref _workspaceName, value);
    }

    public string? WorkspacePath
    {
        get => _workspacePath;
        private set => SetProperty(ref _workspacePath, value);
    }

    public string WorkspaceSummaryText
    {
        get => _workspaceSummaryText;
        private set
        {
            if (SetProperty(ref _workspaceSummaryText, value))
            {
                RefreshSelectedNodeDetails();
            }
        }
    }

    public string StatusSummary
    {
        get => _statusSummary;
        private set
        {
            if (SetProperty(ref _statusSummary, value))
            {
                RefreshSelectedNodeDetails();
            }
        }
    }

    public string DiagnosticsText
    {
        get => _diagnosticsText;
        private set => SetProperty(ref _diagnosticsText, value);
    }

    public string DiscoveryText
    {
        get => _discoveryText;
        private set => SetProperty(ref _discoveryText, value);
    }

    public string CommandCatalogText
    {
        get => _commandCatalogText;
        private set => SetProperty(ref _commandCatalogText, value);
    }

    public string ScenarioListText
    {
        get => _scenarioListText;
        private set => SetProperty(ref _scenarioListText, value);
    }

    public string WorkspaceConfigText
    {
        get => _workspaceConfigText;
        private set => SetProperty(ref _workspaceConfigText, value);
    }

    public string StepListText
    {
        get => _stepListText;
        private set => SetProperty(ref _stepListText, value);
    }

    public string WorkspaceTreeText
    {
        get => _workspaceTreeText;
        private set => SetProperty(ref _workspaceTreeText, value);
    }

    public string AllWorkspaceTreeText
    {
        get => _allWorkspaceTreeText;
        private set => SetProperty(ref _allWorkspaceTreeText, value);
    }

    public string RecentFoldersText
    {
        get => _recentFoldersText;
        private set => SetProperty(ref _recentFoldersText, value);
    }

    public string AutPlacementText
    {
        get => _autPlacementText;
        private set => SetProperty(ref _autPlacementText, value);
    }

    public bool IsRecentFoldersExpanded
    {
        get => _isRecentFoldersExpanded;
        set
        {
            if (SetProperty(ref _isRecentFoldersExpanded, value))
            {
                OnPropertyChanged(nameof(RecentFoldersGlyph));
            }
        }
    }

    public string RecentFoldersGlyph => IsRecentFoldersExpanded
        ? PresenterIcons.ChevronUp
        : PresenterIcons.ChevronDown;

    public string ExecutionTimingText
    {
        get => _executionTimingText;
        private set => SetProperty(ref _executionTimingText, value);
    }

    public string RunScopeText
    {
        get => _runScopeText;
        private set => SetProperty(ref _runScopeText, value);
    }

    /// <summary>The pause between steps, in milliseconds: the current speed's.</summary>
    public string ExecutionDelayText
    {
        get => _executionDelayText;
        private set
        {
            if (SetProperty(ref _executionDelayText, value))
            {
                _executionDelayMilliseconds = ParseDelayMilliseconds(value);
                OnPropertyChanged(nameof(ExecutionDelayMilliseconds));
            }
        }
    }

    public int ExecutionDelayMilliseconds
    {
        get => _executionDelayMilliseconds;
        private set
        {
            var clamped = Math.Clamp(value, 0, MaxExecutionDelayMilliseconds);
            if (SetProperty(ref _executionDelayMilliseconds, clamped))
            {
                SetProperty(ref _executionDelayText, clamped.ToString(CultureInfo.InvariantCulture), nameof(ExecutionDelayText));
            }
        }
    }

    /// <summary>The run's pace; the Speed button cycles Fast, Normal and Slow.</summary>
    public PresenterSpeed Speed
    {
        get => _speed;
        private set
        {
            if (SetProperty(ref _speed, value))
            {
                ApplySpeed();
            }
        }
    }

    /// <summary>The Speed button's gauge: high, medium or low.</summary>
    public string SpeedGlyph => Speed switch
    {
        PresenterSpeed.Fast => PresenterIcons.SpeedFast,
        PresenterSpeed.Slow => PresenterIcons.SpeedSlow,
        _ => PresenterIcons.SpeedNormal,
    };

    /// <summary>The Speed button's tooltip: the speed, its pause, and what a click chooses.</summary>
    public string SpeedDescription => $"Speed: {Speed}, {_speeds.Of(Speed)} ms between steps. Click for {NextSpeed(Speed)}.";

    public ICommand CycleSpeedCommand { get; }

    /// <summary>The Settings page's pause of each speed, in milliseconds.</summary>
    public string FastMillisecondsText
    {
        get => _speeds.Fast.ToString(CultureInfo.InvariantCulture);
        set => SetSpeedMilliseconds(value, milliseconds => _speeds.Fast = milliseconds);
    }

    /// <inheritdoc cref="FastMillisecondsText"/>
    public string NormalMillisecondsText
    {
        get => _speeds.Normal.ToString(CultureInfo.InvariantCulture);
        set => SetSpeedMilliseconds(value, milliseconds => _speeds.Normal = milliseconds);
    }

    /// <inheritdoc cref="FastMillisecondsText"/>
    public string SlowMillisecondsText
    {
        get => _speeds.Slow.ToString(CultureInfo.InvariantCulture);
        set => SetSpeedMilliseconds(value, milliseconds => _speeds.Slow = milliseconds);
    }

    private static PresenterSpeed NextSpeed(PresenterSpeed speed) => speed switch
    {
        PresenterSpeed.Fast => PresenterSpeed.Normal,
        PresenterSpeed.Normal => PresenterSpeed.Slow,
        _ => PresenterSpeed.Fast,
    };

    private void CycleSpeed()
    {
        Speed = NextSpeed(Speed);
        SaveSpeeds();
    }

    private void ApplySpeed()
    {
        ExecutionDelayMilliseconds = _speeds.Of(Speed);
        OnPropertyChanged(nameof(SpeedGlyph));
        OnPropertyChanged(nameof(SpeedDescription));
    }

    // A value that is not a number of 0 to 99999 ms is ignored; the field shows it until it is fixed.
    private void SetSpeedMilliseconds(string value, Action<int> set)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds) || milliseconds > MaxExecutionDelayMilliseconds)
        {
            return;
        }

        set(milliseconds);
        ApplySpeed();
        SaveSpeeds();
    }

    private void SaveSpeeds()
    {
        try
        {
            var settings = _settingsService.Load();
            settings.Speed = Speed;
            settings.Speeds = _speeds;
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            DiagnosticsText = $"Could not save the speeds: {ex.Message}";
        }
    }

    public double RunProgress
    {
        get => _runProgress;
        private set => SetProperty(ref _runProgress, value);
    }

    public BddWorkspaceNodeViewModel? SelectedWorkspaceNode
    {
        get => _selectedWorkspaceNode;
        set
        {
            if (SetProperty(ref _selectedWorkspaceNode, value))
            {
                var scenario = value?.Scenario ?? value?.Parent?.Scenario;
                if (scenario is not null && !ReferenceEquals(SelectedScenario, scenario))
                {
                    SelectedScenario = scenario;
                }

                RefreshSelectedNodeDetails();
            }
        }
    }

    public string SelectedWorkspaceNodeDetailsText
    {
        get => _selectedWorkspaceNodeDetailsText;
        private set => SetProperty(ref _selectedWorkspaceNodeDetailsText, value);
    }

    public BddScenarioViewModel? SelectedScenario
    {
        get => _selectedScenario;
        set
        {
            if (SetProperty(ref _selectedScenario, value))
            {
                StopActiveSession();
                LoadSteps(value);
                OnPropertyChanged(nameof(StepsHeaderText));
                OnPropertyChanged(nameof(SelectedScenarioName));
                OnPropertyChanged(nameof(SelectedScenarioTags));
            }
        }
    }

    public string SelectedScenarioName => SelectedScenario?.Name ?? "No scenario selected";

    public string SelectedScenarioTags => SelectedScenario?.Tags ?? string.Empty;

    public string SelectedTab
    {
        get => _selectedTab;
        private set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                OnPropertyChanged(nameof(IsTreeTabSelected));
                OnPropertyChanged(nameof(IsConfigTabSelected));
                OnPropertyChanged(nameof(IsDiagnosticsTabSelected));
                OnPropertyChanged(nameof(IsDiscoveryTabSelected));
                OnPropertyChanged(nameof(IsCommandCatalogTabSelected));
                OnPropertyChanged(nameof(IsSettingsTabSelected));
            }
        }
    }

    public bool IsTreeTabSelected => SelectedTab == TreeTabName;

    public bool IsConfigTabSelected => SelectedTab == ConfigTabName;

    public bool IsDiagnosticsTabSelected => SelectedTab == DiagnosticsTabName;

    public bool IsDiscoveryTabSelected => SelectedTab == DiscoveryTabName;

    public bool IsCommandCatalogTabSelected => SelectedTab == CommandCatalogTabName;

    /// <summary>Whether the Settings page shows; its button opens and closes it, in either mode.</summary>
    public bool IsSettingsTabSelected => SelectedTab == SettingsTabName;

    public ICommand ShowSettingsCommand { get; }

    public bool IsSelectionExpanded
    {
        get => _isSelectionExpanded;
        set
        {
            if (SetProperty(ref _isSelectionExpanded, value))
            {
                OnPropertyChanged(nameof(SelectionExpanderGlyph));
            }
        }
    }

    /// <summary>The header caption, now static - the chevron moved to <see cref="SelectionExpanderGlyph"/>.</summary>
    public string SelectionExpanderText => "Selection";

    public string SelectionExpanderGlyph => IsSelectionExpanded
        ? PresenterIcons.ChevronUp
        : PresenterIcons.ChevronDown;

    /// <summary>
    /// The persisted choice. <see cref="PresenterTheme.System" /> until the user presses
    /// the toggle for the first time.
    /// </summary>
    public PresenterTheme CurrentTheme
    {
        get => _theme;
        private set
        {
            if (SetProperty(ref _theme, value))
            {
                RaiseThemeProperties();
            }
        }
    }

    /// <summary>True when what is on screen right now is the dark theme.</summary>
    public bool IsDarkThemeActive => _theme switch
    {
        PresenterTheme.Dark => true,
        PresenterTheme.Light => false,
        _ => Application.Current?.RequestedTheme == AppTheme.Dark
    };

    /// <summary>
    /// The glyph on the toggle button. It names the destination, not the current state,
    /// so a two-state button needs no separate state indicator. WP2 replaces these two
    /// characters with Segoe Fluent codepoints.
    /// </summary>
    public string ThemeGlyph => IsDarkThemeActive ? PresenterIcons.ThemeLight : PresenterIcons.ThemeDark;

    /// <summary>
    /// Tooltip and UI Automation name for the toggle. An icon-only button is still
    /// findable and still named because of this string.
    /// </summary>
    public string ThemeDescription => IsDarkThemeActive ? "Switch to light theme" : "Switch to dark theme";

    /// <summary>
    /// Two-state toggle: it flips between light and dark, taking the current effective
    /// theme as the starting point, and never returns to <see cref="PresenterTheme.System" />
    /// once pressed. A tri-state cycle would need a visible state indicator the top row
    /// has no room for.
    /// </summary>
    private void ToggleTheme()
    {
        CurrentTheme = IsDarkThemeActive ? PresenterTheme.Light : PresenterTheme.Dark;
        ApplyTheme();
        PersistTheme();
    }

    private void ApplyTheme()
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        application.UserAppTheme = _theme switch
        {
            PresenterTheme.Light => AppTheme.Light,
            PresenterTheme.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    private void PersistTheme()
    {
        try
        {
            var settings = _settingsService.Load();
            settings.Theme = _theme;
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            DiagnosticsText = $"Could not save the theme choice: {ex.Message}";
        }
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        RaiseThemeProperties();
    }

    /// <summary>
    /// XAML colours ride AppThemeBinding and repaint themselves. The C#-side glyph
    /// colours do not, so every node has to be told to re-read <c>IconColor</c>.
    /// </summary>
    private void RaiseThemeProperties()
    {
        OnPropertyChanged(nameof(IsDarkThemeActive));
        OnPropertyChanged(nameof(ThemeGlyph));
        OnPropertyChanged(nameof(ThemeDescription));

        foreach (var node in _allWorkspaceNodes)
        {
            node.RefreshThemeColors();
        }

        foreach (var row in StepRows)
        {
            row.RefreshThemeColors();
        }

        RefreshPreview();
    }

    private void LoadDefaultWorkspace()
    {
        var settings = _settingsService.Load();
        var startupFolder = FindStartupFolder(settings);
        RefreshRecentFolders(settings);
        if (startupFolder is not null)
        {
            LoadWorkspace(startupFolder);
            return;
        }

        var folder = _workspaceService.FindDefaultWorkspace();
        if (folder is null)
        {
            StatusSummary = "Sample workspace not found";
            DiagnosticsText = "Could not find testsnew/Brinell.Maui.Uat.Tests from the application folder.";
            WorkspaceSummaryText = "No workspace config";
            return;
        }

        LoadWorkspace(folder);
    }

    private async Task OpenFolderAsync()
    {
        try
        {
            var folder = await _folderPickerService.PickFolderAsync().ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(folder))
            {
                StatusSummary = WorkspacePath is null
                    ? "No workspace selected"
                    : "Open folder canceled";
                return;
            }

            LoadWorkspace(folder, recordRecent: true);
        }
        catch (Exception ex)
        {
            StatusSummary = "Failed to open folder";
            DiagnosticsText = Describe(ex);
            SelectedTab = DiagnosticsTabName;
        }
    }

    private void ReloadWorkspace()
    {
        if (WorkspacePath is not null)
        {
            LoadWorkspace(WorkspacePath);
        }
    }

    private void OpenRecentFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            StatusSummary = "Recent folder not found";
            RefreshRecentFolders(_settingsService.Load());
            return;
        }

        LoadWorkspace(folderPath, recordRecent: true);
    }

    private string? FindStartupFolder(PresenterUserSettings settings)
    {
        var candidates = new[] { settings.LastOpenedFolder }
            .Concat(settings.RecentFolders)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existing = candidates.FirstOrDefault(Directory.Exists);
        var existingRecentFolders = settings.RecentFolders
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        var changed = !settings.RecentFolders.SequenceEqual(existingRecentFolders, StringComparer.OrdinalIgnoreCase);
        if (existing is not null && !string.Equals(settings.LastOpenedFolder, existing, StringComparison.OrdinalIgnoreCase))
        {
            settings.LastOpenedFolder = existing;
            changed = true;
        }

        if (settings.LastOpenedFolder is not null && !Directory.Exists(settings.LastOpenedFolder))
        {
            settings.LastOpenedFolder = existing;
            changed = true;
        }

        if (changed)
        {
            settings.RecentFolders = existingRecentFolders;
            _settingsService.Save(settings);
        }

        return existing;
    }

    private void RefreshRecentFolders(PresenterUserSettings settings)
    {
        RecentFolders.Clear();
        foreach (var (folder, index) in settings.RecentFolders
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Select(Path.GetFullPath)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Take(10)
                     .Select((folder, index) => (folder, index)))
        {
            RecentFolders.Add(new RecentFolderViewModel(folder, index, OpenRecentFolder));
        }

        RecentFoldersText = RecentFolders.Count == 0
            ? "No recent folders"
            : string.Join(Environment.NewLine, RecentFolders.Select(folder => folder.AutomationText));
    }

    private void LoadWorkspace(string folderPath, bool recordRecent = false)
    {
        StopActiveSession();

        var result = _workspaceService.LoadFolder(folderPath);
        var sameWorkspace = WorkspacePath is not null && string.Equals(
            Path.GetFullPath(WorkspacePath), Path.GetFullPath(result.FolderPath), StringComparison.OrdinalIgnoreCase);
        if (!sameWorkspace)
        {
            ReleaseEndedSession();
        }

        _catalogs = result.Catalogs;
        NeedsBuild = result.Config.ConfigExists && result.Catalogs is null;
        WorkspacePath = result.FolderPath;
        WorkspaceName = result.WorkspaceName;

        Files.Clear();
        foreach (var file in result.Files)
        {
            Files.Add(new BddFileViewModel(file));
        }

        Scenarios.Clear();
        foreach (var scenario in result.Scenarios)
        {
            Scenarios.Add(new BddScenarioViewModel(scenario));
        }

        SelectedScenario = Scenarios.FirstOrDefault();
        DiagnosticsText = string.Join(Environment.NewLine, result.Diagnostics);
        DiscoveryText = result.DiscoveryReport;
        CommandCatalogText = result.CommandCatalogReport;
        WorkspaceSummaryText = $"{result.Config.Summary}  {Files.Count} files  {Scenarios.Count} scenarios";
        WorkspaceConfigText = FormatWorkspaceConfig(result.Config);
        ConfigEditor.Load(result.FolderPath, result.Config);
        _projectPath = result.Config.Project?.ResolvedPath;
        ApplyWorkspacePreferences(result.FolderPath);
        OnPropertyChanged(nameof(Platforms));
        BuildWorkspaceTree(result);
        UpdateScenarioListText();
        AutPlacementText = string.Empty;
        StatusSummary = result.ErrorCount == 0
            ? $"Ready: {Files.Count} files, {Scenarios.Count} scenarios, Config: ok, Parse: ok, Bind: ok"
            : $"Needs attention: {result.ErrorCount} diagnostics";
        // A new workspace opens in Present mode when it is clean; a reload keeps the mode unless
        // something is now wrong, which only Workspace mode shows.
        if (result.ErrorCount > 0)
        {
            IsPresentMode = false;
        }
        else if (!sameWorkspace)
        {
            IsPresentMode = true;
        }

        SelectedTab = result.ErrorCount == 0 ? TreeTabName : DiagnosticsTabName;
        if (recordRecent)
        {
            RefreshRecentFolders(_settingsService.RecordOpenedFolder(result.FolderPath));
            IsRecentFoldersExpanded = false;
        }

        RunProgress = 0;
    }

    private void BuildWorkspaceTree(BddWorkspaceLoadResult result)
    {
        // A reload rebuilds every node, so where you were is carried across by NodePath.
        // Saving the config reloads, and a save that drops your place is worse than no reload.
        var expandedBefore = _allWorkspaceNodes
            .Where(node => node.IsExpanded)
            .Select(node => node.NodePath)
            .ToHashSet(StringComparer.Ordinal);
        var selectedBefore = SelectedWorkspaceNode?.NodePath;

        _allWorkspaceNodes.Clear();
        WorkspaceTreeNodes.Clear();
        WorkspaceRootNodes.Clear();
        _workspaceRoot = null;

        var root = new BddWorkspaceNodeViewModel(
            result.WorkspaceName,
            BddWorkspaceNodeKind.Folder,
            0,
            result.FolderPath,
            expansionChanged: OnWorkspaceNodeExpansionChanged);

        var folderNodes = new Dictionary<string, BddWorkspaceNodeViewModel>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.GetFullPath(result.FolderPath)] = root
        };

        var fileNodes = new Dictionary<string, BddWorkspaceNodeViewModel>(StringComparer.OrdinalIgnoreCase);
        var filesByPath = Files.ToDictionary(file => Path.GetFullPath(file.FilePath), StringComparer.OrdinalIgnoreCase);
        var scenariosByFile = Scenarios
            .GroupBy(scenario => Path.GetFullPath(scenario.FilePath), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in EnumerateWorkspaceFiles(result.FolderPath))
        {
            var fullFilePath = Path.GetFullPath(filePath);
            var parentFolderPath = Path.GetDirectoryName(fullFilePath) ?? Path.GetFullPath(result.FolderPath);
            var parentNode = EnsureFolderNode(parentFolderPath, result.FolderPath, root, folderNodes);
            var fileName = Path.GetFileName(fullFilePath);
            var kind = GetFileNodeKind(fileName);
            var fileNode = new BddWorkspaceNodeViewModel(
                fileName,
                kind,
                parentNode.Depth + 1,
                fullFilePath,
                expansionChanged: OnWorkspaceNodeExpansionChanged);

            parentNode.AddChild(fileNode);
            fileNodes[fullFilePath] = fileNode;

            if (kind == BddWorkspaceNodeKind.MarkdownFile
                && scenariosByFile.TryGetValue(fullFilePath, out var fileScenarios))
            {
                var suiteName = fileScenarios.FirstOrDefault()?.SuiteName
                    ?? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName));
                var suiteNode = new BddWorkspaceNodeViewModel(
                    suiteName,
                    BddWorkspaceNodeKind.Suite,
                    fileNode.Depth + 1,
                    fullFilePath,
                    expansionChanged: OnWorkspaceNodeExpansionChanged);

                fileNode.AddChild(suiteNode);
                foreach (var scenario in fileScenarios)
                {
                    var scenarioNode = new BddWorkspaceNodeViewModel(
                        scenario.Name,
                        BddWorkspaceNodeKind.Scenario,
                        suiteNode.Depth + 1,
                        fullFilePath,
                        scenario,
                        expansionChanged: OnWorkspaceNodeExpansionChanged);

                    // The steps are in the step list, not the tree.
                    suiteNode.AddChild(scenarioNode);
                }
            }
            else if (filesByPath.TryGetValue(fullFilePath, out var loadedFile)
                     && !string.IsNullOrWhiteSpace(loadedFile.Diagnostics))
            {
                fileNode.AddChild(new BddWorkspaceNodeViewModel(
                    "Diagnostics available",
                    BddWorkspaceNodeKind.File,
                    fileNode.Depth + 1,
                    fullFilePath,
                    expansionChanged: OnWorkspaceNodeExpansionChanged));
            }
        }

        SortTree(root);
        ApplyInitialExpansion(root);
        AddAllNode(root);
        RestoreExpansion(expandedBefore);
        var preferredScenario = SelectedScenario ?? Scenarios.FirstOrDefault();
        var preferredNode = FindByNodePath(selectedBefore)
                            ?? _allWorkspaceNodes.FirstOrDefault(
                                node => node.Kind == BddWorkspaceNodeKind.Scenario
                                        && ReferenceEquals(node.Scenario, preferredScenario))
                            ?? _allWorkspaceNodes.FirstOrDefault(node => node.Kind == BddWorkspaceNodeKind.Scenario)
                            ?? root;
        ExpandAncestors(preferredNode);
        _workspaceRoot = root;
        // Added once expansion is settled, so the tree control's nodes bind the final state.
        WorkspaceRootNodes.Add(root);
        RefreshVisibleWorkspaceTree();
        SelectedWorkspaceNode = preferredNode;

        void AddAllNode(BddWorkspaceNodeViewModel node)
        {
            _allWorkspaceNodes.Add(node);
            foreach (var child in node.Children)
            {
                AddAllNode(child);
            }
        }
    }

    /// <summary>
    /// Builds the workspace's project and reloads, so the fixture list and the Pages assembly
    /// reflect what was just built.
    /// </summary>
    /// <remarks>
    /// The build's own output is shown verbatim: NETSDK1147 names the missing workload and the
    /// command that installs it, which is better than anything this could paraphrase.
    /// </remarks>
    private async Task BuildWorkspaceProjectAsync()
    {
        var project = _projectPath;
        if (string.IsNullOrWhiteSpace(project))
        {
            BuildLogText = "This workspace has no Runtime Project to build.";
            return;
        }

        // A run maps the Pages assembly; the session's load context releases it on dispose,
        // and building over a mapped file would fail.
        StopActiveSession();
        ReleaseEndedSession();

        _isBuilding = true;
        (BuildCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        _buildCancellation?.Dispose();
        _buildCancellation = new CancellationTokenSource();
        BuildLogText = $"Building {Path.GetFileName(project)}...";

        try
        {
            var result = await _projectBuilder.BuildAsync(
                project,
                null,
                BuildConfiguration,
                line => BuildLogText += Environment.NewLine + line,
                _buildCancellation.Token).ConfigureAwait(true);

            BuildLogText = result.Output.Length == 0
                ? result.Succeeded ? "Build succeeded." : "Build failed."
                : result.Output;

            if (result.Succeeded)
            {
                ReloadWorkspace();
            }
        }
        finally
        {
            _isBuilding = false;
            (BuildCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    /// <summary>The resolved project path of the loaded workspace, when it has one.</summary>
    private string? _projectPath;

    /// <summary>Remembers this workspace's configuration and platform for next time.</summary>
    private void SaveWorkspacePreferences()
    {
        if (WorkspacePath is not { Length: > 0 } workspace)
        {
            return;
        }

        var settings = _settingsService.Load();
        settings.WorkspacePreferences[workspace] = new PresenterWorkspacePreferences
        {
            Configuration = BuildConfiguration,
            Platform = Platform
        };
        _settingsService.Save(settings);
    }

    /// <summary>Applies the configuration and platform this workspace was last run as.</summary>
    private void ApplyWorkspacePreferences(string workspacePath)
    {
        var settings = _settingsService.Load();
        if (!settings.WorkspacePreferences.TryGetValue(workspacePath, out var preferences))
        {
            return;
        }

        SetProperty(ref _buildConfiguration, preferences.Configuration, nameof(BuildConfiguration));
        SetProperty(ref _platform, preferences.Platform, nameof(Platform));
        Environment.SetEnvironmentVariable("APPIUM_PLATFORM", preferences.Platform);
    }

    /// <summary>
    /// The message to show for a failure, with reflection's wrapper removed.
    /// </summary>
    /// <remarks>
    /// Constructing a fixture goes through reflection, so anything it throws arrives wrapped in
    /// a <see cref="System.Reflection.TargetInvocationException" /> whose own message is
    /// "Exception has been thrown by the target of an invocation" — which says nothing about
    /// what went wrong. The inner exception is the reason; the wrapper is an implementation
    /// detail of how the fixture was created.
    /// </remarks>
    /// <param name="exception">The failure.</param>
    /// <returns>The innermost message, with the outer ones kept when they add anything.</returns>
    private static string Describe(Exception exception)
    {
        List<string> messages = [];
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is System.Reflection.TargetInvocationException)
            {
                continue;
            }

            if (!messages.Contains(current.Message, StringComparer.Ordinal))
            {
                messages.Add(current.Message);
            }
        }

        return messages.Count == 0 ? exception.Message : string.Join(Environment.NewLine, messages);
    }

    /// <summary>Re-expands the nodes that were expanded before a reload.</summary>
    /// <param name="expandedPaths">The <see cref="BddWorkspaceNodeViewModel.NodePath" />s to expand.</param>
    private void RestoreExpansion(IReadOnlySet<string> expandedPaths)
    {
        if (expandedPaths.Count == 0)
        {
            return;
        }

        foreach (var node in _allWorkspaceNodes.Where(node =>
                     node.CanExpand && expandedPaths.Contains(node.NodePath)))
        {
            node.IsExpanded = true;
        }
    }

    /// <summary>
    /// The node at a path, or the nearest surviving ancestor when that node is gone.
    /// </summary>
    /// <param name="nodePath">The path to find, or null.</param>
    /// <returns>The node, an ancestor of it, or null.</returns>
    private BddWorkspaceNodeViewModel? FindByNodePath(string? nodePath)
    {
        if (string.IsNullOrEmpty(nodePath))
        {
            return null;
        }

        var candidate = nodePath;
        while (candidate.Length > 0)
        {
            var match = _allWorkspaceNodes.FirstOrDefault(node =>
                node.NodePath.Equals(candidate, StringComparison.Ordinal));
            if (match is not null)
            {
                return match;
            }

            var lastSeparator = candidate.LastIndexOf('/');
            if (lastSeparator < 0)
            {
                return null;
            }

            candidate = candidate[..lastSeparator];
        }

        return null;
    }

    private void OnWorkspaceNodeExpansionChanged(BddWorkspaceNodeViewModel node)
    {
        if (_workspaceRoot is not null)
        {
            RefreshVisibleWorkspaceTree();
            RefreshSelectedNodeDetails();
        }
    }

    private void RefreshVisibleWorkspaceTree()
    {
        WorkspaceTreeNodes.Clear();
        if (_workspaceRoot is not null)
        {
            AddVisibleNode(_workspaceRoot);
        }

        UpdateWorkspaceTreeText();
        UpdateAllWorkspaceTreeText();

        void AddVisibleNode(BddWorkspaceNodeViewModel node)
        {
            WorkspaceTreeNodes.Add(node);
            if (!node.IsExpanded)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                AddVisibleNode(child);
            }
        }
    }

    private static void ApplyInitialExpansion(BddWorkspaceNodeViewModel root)
    {
        root.IsExpanded = true;
        foreach (var child in root.Children)
        {
            child.IsExpanded = child.Kind != BddWorkspaceNodeKind.Folder || CountMarkdownDescendants(child) <= 1;
            CollapseDescendants(child);
        }
    }

    private static void CollapseDescendants(BddWorkspaceNodeViewModel node)
    {
        foreach (var child in node.Children)
        {
            child.IsExpanded = false;
            CollapseDescendants(child);
        }
    }

    private static void ExpandAncestors(BddWorkspaceNodeViewModel node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    private static int CountMarkdownDescendants(BddWorkspaceNodeViewModel node)
    {
        var count = node.Kind is BddWorkspaceNodeKind.MarkdownFile or BddWorkspaceNodeKind.WorkflowConfig ? 1 : 0;
        return count + node.Children.Sum(CountMarkdownDescendants);
    }

    // Run (F5): on from where a paused run is, else the selection's scenarios from the start.
    private Task RunAsync()
        => IsPaused ? MoveAsync(PresenterMove.Play) : RunSelectedNodeAsync();

    private async Task RunSelectedNodeAsync()
    {
        if (WorkspacePath is null)
        {
            StatusSummary = "No workspace loaded";
            return;
        }

        var scenarios = GetRunnableScenarios(SelectedWorkspaceNode).ToArray();
        if (scenarios.Length == 0)
        {
            StatusSummary = "No runnable scenarios below selection";
            return;
        }

        var options = CaptureRunExecutionOptions(SelectedWorkspaceNode, scenarios);
        _activeRunOptions = options;
        _stepTimings.Clear();
        RunScopeText = FormatRunScope(options);
        ExecutionTimingText = FormatExecutionTiming(options);
        DiagnosticsText = FormatRunStartedDetails(options);
        StatusSummary = $"Starting: {options.SelectedNodeName}";
        RefreshSelectedNodeDetails();

        var passed = 0;
        foreach (var scenario in scenarios)
        {
            SelectedScenario = scenario;
            var outcome = await RunScenarioAutoAsync(scenario, options).ConfigureAwait(true);
            if (outcome == RunOutcome.Paused)
            {
                // A breakpoint: the run waits there; Run goes on with this scenario only.
                return;
            }

            if (outcome != RunOutcome.Passed)
            {
                StatusSummary = $"Failed: {passed}/{scenarios.Length} scenarios passed";
                RefreshSelectedNodeDetails();
                return;
            }

            passed++;
        }

        StatusSummary = $"Passed: {passed}/{scenarios.Length} scenarios";
        DiagnosticsText = FormatRunCompletedDetails(options);
        ExecutionTimingText = FormatExecutionTiming(options);
        RefreshSelectedNodeDetails();
    }

    // Stop ends the run; with no run on, it closes an app kept open after one.
    private void Stop()
    {
        if (_activeSession is null)
        {
            var closed = _endedSession is not null;
            ReleaseEndedSession();
            StatusSummary = closed ? "Closed the app" : "Stopped";
            RaiseStepping();
            return;
        }

        _executionCancellation?.Cancel();
        FinishActiveSession();
        StatusSummary = "Stopped";
        RaiseStepping();
    }

    // Step (Space, Right, PageDown): with no run on, starts one paused before its first row.
    private Task StepAsync()
        => _activeSession is null ? StartPausedAsync(null) : MoveAsync(PresenterMove.Step);

    private async Task RestartAsync()
    {
        var scenario = _activeScenario ?? SelectedScenario;
        StopActiveSession();
        await StartPausedAsync(scenario).ConfigureAwait(true);
    }

    // Starts a run of the scenario (or the selection's first) and pauses before its first row,
    // so what is about to happen can be said before it happens.
    private async Task StartPausedAsync(BddScenarioViewModel? scenario)
    {
        if (WorkspacePath is null)
        {
            StatusSummary = "No workspace loaded";
            return;
        }

        scenario ??= GetRunnableScenarios(SelectedWorkspaceNode).FirstOrDefault() ?? SelectedScenario;
        if (scenario is null)
        {
            StatusSummary = "No runnable scenario below selection";
            return;
        }

        if (!ReferenceEquals(SelectedScenario, scenario))
        {
            SelectedScenario = scenario;
        }

        await StartStepSessionAsync(scenario).ConfigureAwait(true);
        if (_activeSession is not null && StepRows.Count > 0)
        {
            Pause(StepRows[0]);
        }
    }

    private async Task StartStepSessionAsync(BddScenarioViewModel scenario)
    {
        if (WorkspacePath is null)
        {
            return;
        }

        StopActiveSession();
        ResetScenario(scenario);
        _executionCancellation = new CancellationTokenSource();
        StatusSummary = $"Starting: {scenario.Name}";

        try
        {
            SnapshotWindows();
            _activeSession = await _executionService
                .CreateSessionAsync(WorkspacePath, scenario.FilePath, scenario.Name, _executionCancellation.Token)
                .ConfigureAwait(true);
            _activeScenario = scenario;
            TryPlaceApp();
            await StartRecordingAsync(_activeSession, scenario).ConfigureAwait(true);
            DiscoveryText = _activeSession.DiscoveryReport;
            CommandCatalogText = _activeSession.CommandCatalogReport;
            AutPlacementText = _placedThisRun ? AutPlacementText : _activeSession.AutPlacementReport;
            DiagnosticsText = string.Join(
                Environment.NewLine,
                "Step session ready.",
                AutPlacementText);
        }
        catch (Exception ex)
        {
            scenario.Status = "fail";
            StatusSummary = $"Failed to start: {scenario.Name}";
            DiagnosticsText = Describe(ex);
            FinishActiveSession();
        }
        finally
        {
            UpdateScenarioListText();
            RefreshSelectedNodeDetails();
        }
    }

    private async Task<RunOutcome> RunScenarioAutoAsync(
        BddScenarioViewModel scenario,
        PresenterRunExecutionOptions options)
    {
        if (WorkspacePath is null)
        {
            return RunOutcome.Failed;
        }

        StopActiveSession();
        ResetScenario(scenario, clearTimings: false);
        _executionCancellation = new CancellationTokenSource();
        StatusSummary = $"Starting: {scenario.Name}";
        scenario.Status = "run";
        UpdateScenarioListText();
        _isMoving = true;
        RaiseStepping();

        try
        {
            SnapshotWindows();
            _activeSession = await _executionService
                .CreateSessionAsync(WorkspacePath, scenario.FilePath, scenario.Name, _executionCancellation.Token)
                .ConfigureAwait(true);
            _activeScenario = scenario;
            TryPlaceApp();
            await StartRecordingAsync(_activeSession, scenario).ConfigureAwait(true);
            DiscoveryText = _activeSession.DiscoveryReport;
            CommandCatalogText = _activeSession.CommandCatalogReport;
            AutPlacementText = _placedThisRun ? AutPlacementText : _activeSession.AutPlacementReport;
            RefreshBreakpoints(scenario);
            return await AdvanceAsync(_activeSession, scenario, new StepPlan(int.MaxValue, true, null), -1, options, checkFirst: true)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            scenario.Status = "cancel";
            StatusSummary = $"Canceled: {scenario.Name}";
            MarkRunningSteps("cancel");
            FinishActiveSession();
            RefreshSelectedNodeDetails();
            return RunOutcome.Stopped;
        }
        catch (Exception ex)
        {
            scenario.Status = "fail";
            StatusSummary = $"Failed: {scenario.Name}: {Describe(ex)}";
            DiagnosticsText = Describe(ex);
            MarkRunningSteps("fail");
            FinishActiveSession();
            RefreshSelectedNodeDetails();
            return RunOutcome.Failed;
        }
        finally
        {
            _isMoving = false;
            RaiseStepping();
            UpdateScenarioListText();
        }
    }

    // One move from the row the run is paused before (P2 of the Presenter update).
    private async Task MoveAsync(PresenterMove move, BddStepRowViewModel? until = null, BddStepRowViewModel? from = null)
    {
        if (_activeSession is not { } session || _activeScenario is not { } scenario || _isMoving || CurrentRow is not { } at)
        {
            return;
        }

        var start = at.Index;
        if (from is not null)
        {
            // Run from here: the rows before it are skipped, not run; the app must already be there.
            while (session.HasNext && session.CompletedStepCount < from.FirstStep)
            {
                var index = session.CompletedStepCount;
                session.SkipNext("Skipped by Run from here.");
                SetStepStatus(index, "skip");
            }

            start = from.Index;
            move = PresenterMove.Play;
        }

        var plan = until is not null
            ? PresenterStepping.PlanUntil(StepRows, start, until.Index)
            : PresenterStepping.Plan(StepRows, start, move);
        if (plan is not { } planned)
        {
            return;
        }

        _isMoving = true;
        RaiseStepping();
        _executionCancellation ??= new CancellationTokenSource();
        PresenterRunExecutionOptions? pacing = null;
        if (planned.CheckBreaks)
        {
            RefreshBreakpoints(scenario);
            pacing = CaptureRunExecutionOptions(SelectedWorkspaceNode, [scenario]);
            _activeRunOptions ??= pacing;
        }

        try
        {
            // A move from a pause runs the paused row first, so its own breakpoint does not stop it again.
            await AdvanceAsync(session, scenario, planned, start, pacing, checkFirst: from is not null).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            scenario.Status = "cancel";
            StatusSummary = $"Canceled: {scenario.Name}";
            MarkRunningSteps("cancel");
            FinishActiveSession();
        }
        finally
        {
            _isMoving = false;
            RaiseStepping();
            UpdateScenarioListText();
            RefreshSelectedNodeDetails();
        }
    }

    // Runs steps until the plan's count, a breakpoint, a failure or the end; then pauses, or ends the run.
    private async Task<RunOutcome> AdvanceAsync(
        PresenterUatExecutionSession session,
        BddScenarioViewModel scenario,
        StepPlan plan,
        int fromRow,
        PresenterRunExecutionOptions? pacing,
        bool checkFirst)
    {
        var first = true;
        while (session.HasNext && session.CompletedStepCount < plan.Until)
        {
            var step = session.CompletedStepCount;
            if (plan.CheckBreaks && (checkFirst || !first) && PresenterStepping.BreakBefore(StepRows, step) is { } breakRow)
            {
                Pause(breakRow, "Breakpoint");
                return RunOutcome.Paused;
            }

            first = false;
            var result = await RunNextStepAsync(session, scenario).ConfigureAwait(true);
            if (!ReferenceEquals(_activeSession, session))
            {
                return RunOutcome.Stopped;
            }

            if (result.Status is BddStepResultStatus.Failed or BddStepResultStatus.Canceled)
            {
                MarkRemainingSteps("skip");
                scenario.Status = result.Status == BddStepResultStatus.Canceled ? "cancel" : "fail";
                StatusSummary = $"{ScenarioStatusText(result.Status)}: {scenario.Name}: {result.Message}";
                DiagnosticsText = FormatExecutionDetails(session, result.Message);
                FinishActiveSession();
                return RunOutcome.Failed;
            }

            if (pacing is not null)
            {
                await WaitBeforeNextStepAsync(session, scenario, pacing).ConfigureAwait(true);
                if (!ReferenceEquals(_activeSession, session))
                {
                    return RunOutcome.Stopped;
                }
            }
        }

        if (!session.HasNext)
        {
            scenario.Status = "pass";
            StatusSummary = $"Passed: {scenario.Name}";
            DiagnosticsText = FormatExecutionDetails(session, null);
            FinishActiveSession();
            return RunOutcome.Passed;
        }

        var stop = plan.StopRow ?? PresenterStepping.PositionAfter(StepRows, session.CompletedStepCount, fromRow);
        Pause(StepRows[Math.Min(stop, StepRows.Count - 1)]);
        return RunOutcome.Paused;
    }

    // The run waits before a row: the row is highlighted, and the moves say what they will do.
    private void Pause(BddStepRowViewModel row, string reason = "Paused")
    {
        CurrentRow = row;
        StatusSummary = $"{reason} before: {row.Label}";
        RaiseStepping();
        RefreshPreview();
    }

    private void RaiseStepping()
    {
        OnPropertyChanged(nameof(IsPaused));
        foreach (var command in new[] { RunCommand, NextStepCommand, StepIntoCommand, StepOutCommand, RestartCommand })
        {
            (command as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        foreach (var row in StepRows)
        {
            row.RefreshMenu();
        }
    }

    // The shared breakpoints file, read again before a run goes on, so one set in an IDE counts.
    private void RefreshBreakpoints(BddScenarioViewModel scenario)
    {
        var lines = _breakpoints.Lines(scenario.FilePath);
        foreach (var row in scenario.Rows)
        {
            row.HasBreakpoint = row.CanBreak && lines.Contains(row.LineNumber);
        }
    }

    /// <inheritdoc />
    public bool CanRunUntil(BddStepRowViewModel row)
        => WorkspacePath is not null && !_isMoving && (_activeSession is null || (CurrentRow is { } at && row.Index > at.Index));

    /// <inheritdoc />
    public bool CanRunFrom(BddStepRowViewModel row)
        => IsPaused && CurrentRow is { } at && row.Index > at.Index && !row.IsInCall;

    /// <inheritdoc />
    public async void RunUntil(BddStepRowViewModel row)
    {
        if (_activeSession is null)
        {
            await StartPausedAsync(SelectedScenario).ConfigureAwait(true);
        }

        if (CurrentRow is { } at && row.Index > at.Index)
        {
            await MoveAsync(PresenterMove.Step, until: row).ConfigureAwait(true);
        }
    }

    /// <inheritdoc />
    public async void RunFrom(BddStepRowViewModel row)
    {
        if (CanRunFrom(row))
        {
            await MoveAsync(PresenterMove.Play, from: row).ConfigureAwait(true);
        }
    }

    /// <inheritdoc />
    public void ToggleBreakpoint(BddStepRowViewModel row)
    {
        if (row.CanBreak && SelectedScenario is { } scenario)
        {
            row.HasBreakpoint = _breakpoints.Toggle(scenario.FilePath, row.LineNumber);
        }
    }

    /// <summary>
    /// The keys, for a keyboard or a presentation clicker (which sends PageDown and PageUp).
    /// Space, Right and PageDown step; Down goes into a call, Up out of it; F5 runs; Escape
    /// stops; Ctrl+R restarts.
    /// </summary>
    /// <param name="key">The key's name, as Windows gives it (<c>Space</c>, <c>PageDown</c>, <c>F5</c> ...).</param>
    /// <param name="control">Whether Ctrl is down.</param>
    /// <returns>Whether the key was one of these.</returns>
    public bool HandleKey(string key, bool control)
    {
        var command = (key, control) switch
        {
            ("Space" or "Right" or "PageDown", false) => NextStepCommand,
            ("Down", false) => StepIntoCommand,
            ("Up", false) => StepOutCommand,
            ("F5", false) => RunCommand,
            ("Escape", false) => StopCommand,
            ("R", true) => RestartCommand,
            _ => null,
        };
        if (command is null)
        {
            return false;
        }

        if (command.CanExecute(null))
        {
            command.Execute(null);
        }

        return true;
    }

    // The preview's paused bar sends the same moves.
    private void OnPreviewMessage(string type, System.Text.Json.JsonElement message)
    {
        if (type != "debug" || !message.TryGetProperty("move", out var move))
        {
            return;
        }

        var command = move.GetString() switch
        {
            "continue" => RunCommand,
            "over" => NextStepCommand,
            "into" => StepIntoCommand,
            "out" => StepOutCommand,
            "stop" => StopCommand,
            _ => null,
        };
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }

    private enum RunOutcome
    {
        Passed,
        Failed,
        Paused,
        Stopped,
    }

    private async Task<BddStepResult> RunNextStepAsync(
        PresenterUatExecutionSession session,
        BddScenarioViewModel scenario)
    {
        var index = session.CompletedStepCount;
        var startedAt = DateTimeOffset.Now;
        var stepText = GetScenarioStepText(scenario, index);
        CurrentRow = RowOfStep(index);
        SetStepStatus(index, "run");
        StatusSummary = stepText;
        RefreshSelectedNodeDetails();
        await Task.Yield();

        var cancellationToken = _executionCancellation?.Token ?? CancellationToken.None;
        var result = await Task.Run(
            () => session.RunNextAsync(cancellationToken),
            cancellationToken).ConfigureAwait(true);

        var completedAt = DateTimeOffset.Now;
        _stepTimings.Add(new PresenterStepTiming(
            index + 1,
            stepText,
            startedAt,
            completedAt));
        ExecutionTimingText = FormatExecutionTiming(_activeRunOptions);

        if (index >= 0 && index < Steps.Count)
        {
            Steps[index].Message = result.Status == BddStepResultStatus.Passed ? null : result.Message;
        }

        SetStepStatus(index, StepStatusText(result.Status));
        TryPlaceApp();
        RunProgress = scenario.Steps.Count == 0
            ? 0
            : (double)Math.Min(index + 1, scenario.Steps.Count) / scenario.Steps.Count;

        if (result.Status is BddStepResultStatus.Failed or BddStepResultStatus.Canceled)
        {
            DiagnosticsText = FormatExecutionDetails(session, result.Message);
        }

        RefreshSelectedNodeDetails();
        return result;
    }

    private async Task WaitBeforeNextStepAsync(
        PresenterUatExecutionSession session,
        BddScenarioViewModel scenario,
        PresenterRunExecutionOptions options)
    {
        if (!session.HasNext)
        {
            return;
        }

        var delayMilliseconds = options.EffectiveDelayMilliseconds;
        if (delayMilliseconds <= 0)
        {
            return;
        }

        if (_stepTimings.Count > 0)
        {
            var timing = _stepTimings[^1];
            timing.DelayAfterMilliseconds = delayMilliseconds;
            timing.WaitStartedAt = DateTimeOffset.Now;
        }

        StatusSummary = GetScenarioStepText(scenario, session.CompletedStepCount);
        DiagnosticsText = FormatExecutionDetails(session, null);
        ExecutionTimingText = FormatExecutionTiming(options);
        RefreshSelectedNodeDetails();
        await Task.Delay(delayMilliseconds, _executionCancellation?.Token ?? CancellationToken.None).ConfigureAwait(true);

        if (_stepTimings.Count > 0)
        {
            _stepTimings[^1].WaitCompletedAt = DateTimeOffset.Now;
            ExecutionTimingText = FormatExecutionTiming(options);
        }
    }

    private static string GetScenarioStepText(BddScenarioViewModel scenario, int index)
    {
        return index >= 0 && index < scenario.Steps.Count
            ? scenario.Steps[index].Text
            : scenario.Name;
    }

    private void LoadSteps(BddScenarioViewModel? scenario)
    {
        CurrentRow = null;
        Steps.Clear();
        StepRows.Clear();
        if (scenario is not null)
        {
            foreach (var step in scenario.Steps)
            {
                Steps.Add(step);
            }

            RefreshBreakpoints(scenario);
            foreach (var row in scenario.Rows)
            {
                row.Host = this;
                StepRows.Add(row);
            }
        }

        UpdateStepListText();
        RunProgress = 0;
    }

    private void ResetScenario(BddScenarioViewModel scenario, bool clearTimings = true)
    {
        if (clearTimings)
        {
            _activeRunOptions = null;
            _stepTimings.Clear();
            ExecutionTimingText = string.Empty;
            RunScopeText = string.Empty;
        }

        scenario.Status = "run";
        foreach (var step in scenario.Steps)
        {
            step.Status = "wait";
            step.Message = null;
        }

        RunProgress = 0;
        UpdateStepListText();
        UpdateScenarioListText();
        RefreshSelectedNodeDetails();
    }

    private void SetStepStatus(int index, string status)
    {
        if (index >= 0 && index < Steps.Count)
        {
            Steps[index].Status = status;
            UpdateStepListText();
        }
    }

    private void MarkRemainingSteps(string status)
    {
        foreach (var step in Steps.Where(step => step.Status is "wait" or "run"))
        {
            step.Status = status;
        }

        UpdateStepListText();
    }

    private void MarkRunningSteps(string status)
    {
        foreach (var step in Steps.Where(step => step.Status == "run"))
        {
            step.Status = status;
        }

        UpdateStepListText();
    }

    private void UpdateScenarioListText()
    {
        ScenarioListText = string.Join(
            Environment.NewLine,
            Scenarios.Select(scenario => scenario.DisplayText));
        UpdateWorkspaceTreeText();
        UpdateAllWorkspaceTreeText();
    }

    private void UpdateStepListText()
    {
        StepListText = string.Join(Environment.NewLine, StepRows.Select(row => row.DisplayText));
        UpdateWorkspaceTreeText();
        UpdateAllWorkspaceTreeText();
        RefreshPreview();
    }

    // The row a step of the run shows on: its own, never its call's.
    private BddStepRowViewModel? RowOfStep(int stepIndex)
        => StepRows.FirstOrDefault(row => !row.IsCall && row.FirstStep == stepIndex);

    private void OpenPreview()
    {
        _preview?.Open();
        RefreshPreview();
    }

    // The preview follows the selected scenario and its run: each step's status, and the current row.
    private void RefreshPreview()
    {
        if (_preview is not { IsOpen: true } preview || SelectedScenario is not { } scenario)
        {
            return;
        }

        preview.Show(new ScenarioPreviewState(
            scenario.FilePath,
            scenario.Name,
            scenario.LineNumber,
            ScenarioRun(scenario),
            CurrentRow?.LineNumber,
            _catalogs,
            IsDarkThemeActive,
            WorkspaceName,
            IsPaused && CurrentRow is { } paused
                ? new ScenarioPreviewPause(paused.Label, paused.LineNumber, paused.IsCall, paused.IsInCall)
                : null));
    }

    // The run so far, as the preview reads a report: the steps that ran, by the line they are written on.
    private static BddScenarioRun? ScenarioRun(BddScenarioViewModel scenario)
    {
        var ran = scenario.Steps.Where(step => step.Status is "pass" or "fail" or "skip" or "cancel").ToList();
        if (ran.Count == 0)
        {
            return null;
        }

        var status = scenario.Status switch
        {
            "pass" => "passed",
            "fail" => "failed",
            "cancel" => "canceled",
            _ => "running",
        };
        return new BddScenarioRun(
            scenario.Name,
            status,
            0,
            null,
            [.. ran.Select(step => new BddStepRun(step.LineNumber, null, null, PreviewStatus(step.Status), step.Message))],
            DateTimeOffset.Now);
    }

    private static string PreviewStatus(string status) => status switch
    {
        "pass" => "Passed",
        "fail" => "Failed",
        "cancel" => "Canceled",
        _ => "Skipped",
    };

    private void UpdateWorkspaceTreeText()
    {
        WorkspaceTreeText = string.Join(Environment.NewLine, WorkspaceTreeNodes.Select(node => node.DisplayText));
    }

    private void UpdateAllWorkspaceTreeText()
    {
        AllWorkspaceTreeText = string.Join(Environment.NewLine, _allWorkspaceNodes.Select(node => node.DisplayText));
    }

    private void RefreshSelectedNodeDetails()
    {
        SelectedWorkspaceNodeDetailsText = FormatSelectedNodeDetails(SelectedWorkspaceNode);
    }

    private IEnumerable<BddScenarioViewModel> GetRunnableScenarios(BddWorkspaceNodeViewModel? node)
    {
        if (node is null)
        {
            return [];
        }

        if (node.Kind == BddWorkspaceNodeKind.Step)
        {
            return node.Scenario is null ? [] : [node.Scenario];
        }

        if (node.Kind == BddWorkspaceNodeKind.Scenario)
        {
            return node.Scenario is null ? [] : [node.Scenario];
        }

        return EnumerateDescendants(node)
            .Where(candidate => candidate.Kind == BddWorkspaceNodeKind.Scenario && candidate.Scenario is not null)
            .Select(candidate => candidate.Scenario!)
            .Distinct()
            .ToArray();
    }

    private PresenterRunExecutionOptions CaptureRunExecutionOptions(
        BddWorkspaceNodeViewModel? selectedNode,
        IReadOnlyCollection<BddScenarioViewModel> scenarios)
    {
        var effectiveDelay = ParseDelayMilliseconds(ExecutionDelayText);
        ExecutionDelayMilliseconds = effectiveDelay;

        return new PresenterRunExecutionOptions(
            selectedNode?.Kind.ToString() ?? "None",
            selectedNode?.Name ?? "No selection",
            scenarios.Count,
            scenarios.Sum(scenario => scenario.Steps.Count),
            effectiveDelay,
            DateTimeOffset.Now);
    }

    private static int ParseDelayMilliseconds(string? value)
    {
        if (!int.TryParse(value, out var parsed))
        {
            return 0;
        }

        return Math.Clamp(parsed, 0, MaxExecutionDelayMilliseconds);
    }

    private string FormatSelectedNodeDetails(BddWorkspaceNodeViewModel? node)
    {
        if (node is null)
        {
            return "No selection";
        }

        List<string> lines =
        [
            $"Type: {node.Kind}",
            $"Name: {node.Name}",
            $"Workspace: {WorkspaceSummaryText}",
            $"Status: {StatusSummary}"
        ];

        if (!string.IsNullOrWhiteSpace(node.FilePath))
        {
            lines.Add($"Path: {FormatDisplayPath(node.FilePath)}");
        }

        if (node.Scenario is not null)
        {
            lines.Add($"Suite: {node.Scenario.SuiteName}");
            lines.Add($"Scenario status: {node.Scenario.StatusDescription}");
            lines.Add($"Steps: {node.Scenario.Steps.Count}");
            if (!string.IsNullOrWhiteSpace(node.Scenario.Tags))
            {
                lines.Add($"Tags: {node.Scenario.Tags}");
            }
        }

        if (node.Step is not null)
        {
            lines.Add($"Step status: {node.Step.StatusDescription}");
            lines.Add($"Command: {node.Step.CommandId}");
            lines.Add($"Line: {node.Step.LineNumber}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string FormatDisplayPath(string path)
    {
        if (WorkspacePath is null)
        {
            return path;
        }

        var fullPath = Path.GetFullPath(path);
        var workspace = Path.GetFullPath(WorkspacePath);
        return fullPath.StartsWith(workspace, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(workspace, fullPath)
            : path;
    }

    private static IEnumerable<BddWorkspaceNodeViewModel> EnumerateDescendants(BddWorkspaceNodeViewModel node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var descendant in EnumerateDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<string> EnumerateWorkspaceFiles(string folderPath)
    {
        return Directory
            .EnumerateFiles(folderPath, "*.md", SearchOption.AllDirectories)
            .Where(path => !HasIgnoredSegment(path))
            .OrderBy(path => Path.GetRelativePath(folderPath, path), StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasIgnoredSegment(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                                       || segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static BddWorkspaceNodeKind GetFileNodeKind(string fileName)
    {
        if (BddFileFormat.IsConfigFileName(fileName))
        {
            return BddWorkspaceNodeKind.WorkflowConfig;
        }

        return fileName.EndsWith(BddFileFormat.FeatureExtension, StringComparison.OrdinalIgnoreCase)
            ? BddWorkspaceNodeKind.MarkdownFile
            : BddWorkspaceNodeKind.MarkdownFile;
    }

    private BddWorkspaceNodeViewModel EnsureFolderNode(
        string folderPath,
        string rootPath,
        BddWorkspaceNodeViewModel root,
        Dictionary<string, BddWorkspaceNodeViewModel> folderNodes)
    {
        var fullFolderPath = Path.GetFullPath(folderPath);
        if (folderNodes.TryGetValue(fullFolderPath, out var existing))
        {
            return existing;
        }

        var fullRootPath = Path.GetFullPath(rootPath);
        var parentFolderPath = Path.GetDirectoryName(fullFolderPath);
        var parentNode = parentFolderPath is null
                         || fullFolderPath.Equals(fullRootPath, StringComparison.OrdinalIgnoreCase)
                         || !fullFolderPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase)
            ? root
            : EnsureFolderNode(parentFolderPath, fullRootPath, root, folderNodes);

        var folderName = Path.GetFileName(fullFolderPath);
        var node = new BddWorkspaceNodeViewModel(
            folderName,
            BddWorkspaceNodeKind.Folder,
            parentNode.Depth + 1,
            fullFolderPath,
            expansionChanged: OnWorkspaceNodeExpansionChanged);
        parentNode.AddChild(node);
        folderNodes[fullFolderPath] = node;
        return node;
    }

    private static void SortTree(BddWorkspaceNodeViewModel node)
    {
        node.Children.Sort(CompareNodes);
        foreach (var child in node.Children)
        {
            SortTree(child);
        }
    }

    private static int CompareNodes(BddWorkspaceNodeViewModel left, BddWorkspaceNodeViewModel right)
    {
        if (left.Kind == BddWorkspaceNodeKind.Folder && right.Kind == BddWorkspaceNodeKind.Folder)
        {
            var expectedFailureComparison = IsExpectedFailuresFolder(left)
                .CompareTo(IsExpectedFailuresFolder(right));
            if (expectedFailureComparison != 0)
            {
                return expectedFailureComparison;
            }
        }

        var rankComparison = NodeRank(left.Kind).CompareTo(NodeRank(right.Kind));
        return rankComparison != 0
            ? rankComparison
            : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static int IsExpectedFailuresFolder(BddWorkspaceNodeViewModel node)
    {
        return node.Name.Equals("ExpectedFailures", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private static int NodeRank(BddWorkspaceNodeKind kind)
    {
        return kind switch
        {
            BddWorkspaceNodeKind.WorkflowConfig => 0,
            BddWorkspaceNodeKind.Folder => 1,
            BddWorkspaceNodeKind.MarkdownFile => 2,
            BddWorkspaceNodeKind.Suite => 3,
            BddWorkspaceNodeKind.Scenario => 4,
            BddWorkspaceNodeKind.Step => 5,
            _ => 6
        };
    }

    private void StopActiveSession()
    {
        FinishRecording();
        CurrentRow = null;
        _executionCancellation?.Cancel();
        _executionCancellation?.Dispose();
        _executionCancellation = null;
        _activeSession?.Dispose();
        _activeSession = null;
        _activeScenario = null;
        _windowService?.SetBarTopmost(false);
    }

    // A run ended (passed, failed or stopped): its app is closed, or kept open as the setting says.
    private void FinishActiveSession()
    {
        FinishRecording();
        CurrentRow = null;
        _executionCancellation?.Dispose();
        _executionCancellation = null;
        if (RunEnd == PresenterRunEnd.KeepOpen && _activeSession is not null)
        {
            ReleaseEndedSession();
            _endedSession = _activeSession;
        }
        else
        {
            _activeSession?.Dispose();
        }

        _activeSession = null;
        _activeScenario = null;
        _windowService?.SetBarTopmost(false);
    }

    private static string StepStatusText(BddStepResultStatus status)
    {
        return status switch
        {
            BddStepResultStatus.Passed => "pass",
            BddStepResultStatus.Failed => "fail",
            BddStepResultStatus.Skipped => "skip",
            BddStepResultStatus.Canceled => "cancel",
            BddStepResultStatus.Running => "run",
            _ => "wait"
        };
    }

    private static string ScenarioStatusText(BddStepResultStatus status)
    {
        return status switch
        {
            BddStepResultStatus.Canceled => "Canceled",
            BddStepResultStatus.Failed => "Failed",
            _ => "Stopped"
        };
    }

    private string FormatExecutionDetails(PresenterUatExecutionSession session, string? message)
    {
        var options = _activeRunOptions;
        List<string> lines =
        [
            .. BddDiagnosticsFormatter.FormatSteps(session.StepSession.Results),
            "Runtime trace:"
        ];
        if (!string.IsNullOrWhiteSpace(session.AutPlacementReport))
        {
            lines.Add(session.AutPlacementReport);
        }

        lines.AddRange(session.Runner.Context.Diagnostics);
        if (options is not null)
        {
            lines.Add("Run scope:");
            lines.AddRange(FormatRunScopeLines(options));
        }

        lines.Add("Execution timing:");
        lines.Add($"Effective delay: {(options?.EffectiveDelayMilliseconds ?? ExecutionDelayMilliseconds)} ms");
        lines.AddRange(_stepTimings.Select(FormatStepTiming));
        if (!string.IsNullOrWhiteSpace(message))
        {
            lines.Add("Message:");
            lines.Add(message);
        }

        lines.Add(session.DiscoveryReport);
        lines.Add(session.CommandCatalogReport);
        return string.Join(Environment.NewLine, lines);
    }

    private string FormatRunStartedDetails(PresenterRunExecutionOptions options)
    {
        return string.Join(
            Environment.NewLine,
            [
                "Run started.",
                "Run scope:",
                .. FormatRunScopeLines(options),
                "Execution timing:",
                $"Effective delay: {options.EffectiveDelayMilliseconds} ms"
            ]);
    }

    private string FormatRunCompletedDetails(PresenterRunExecutionOptions options)
    {
        return string.Join(
            Environment.NewLine,
            [
                "Run completed.",
                "Run scope:",
                .. FormatRunScopeLines(options),
                "Execution timing:",
                $"Effective delay: {options.EffectiveDelayMilliseconds} ms",
                AutPlacementText,
                .. _stepTimings.Select(FormatStepTiming)
            ]);
    }

    private static string FormatRunScope(PresenterRunExecutionOptions options)
    {
        return string.Join(Environment.NewLine, FormatRunScopeLines(options));
    }

    private static IEnumerable<string> FormatRunScopeLines(PresenterRunExecutionOptions options)
    {
        yield return $"Selected node kind: {options.SelectedNodeKind}";
        yield return $"Selected node name: {options.SelectedNodeName}";
        yield return $"Scenario count: {options.ScenarioCount}";
        yield return $"Step count: {options.StepCount}";
        yield return $"Effective delay: {options.EffectiveDelayMilliseconds} ms";
        yield return $"Run started: {options.StartedAt:HH:mm:ss.fff}";
    }

    private string FormatExecutionTiming(PresenterRunExecutionOptions? options)
    {
        List<string> lines =
        [
            $"Effective delay: {(options?.EffectiveDelayMilliseconds ?? ExecutionDelayMilliseconds)} ms"
        ];
        lines.AddRange(_stepTimings.Select(FormatStepTiming));
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatStepTiming(PresenterStepTiming timing)
    {
        var duration = Math.Max(0, (int)Math.Round((timing.CompletedAt - timing.StartedAt).TotalMilliseconds));
        var waitText = timing.WaitStartedAt is null
            ? $"delay after {timing.DelayAfterMilliseconds} ms"
            : $"wait {timing.WaitStartedAt:HH:mm:ss.fff} -> {timing.WaitCompletedAt?.ToString("HH:mm:ss.fff") ?? "pending"} ({timing.DelayAfterMilliseconds} ms)";
        return $"- Step {timing.StepNumber}: {timing.StartedAt:HH:mm:ss.fff} -> {timing.CompletedAt:HH:mm:ss.fff} ({duration} ms), {waitText}: {timing.StepText}";
    }

    private static string FormatWorkspaceConfig(BddWorkspaceConfigLoadResult config)
    {
        List<string> lines =
        [
            $"Config: {(config.ConfigExists ? config.ConfigPath : "missing")}",
            $"Target: {ValueOrMissing(config.Target)}",
            $"Fixture: {ValueOrMissing(config.Fixture)}",
            $"AppPath: {ValueOrMissing(config.AppPath)}",
            $"Resolved AppPath: {ValueOrMissing(config.ResolvedAppPath)}",
            $"AppPath Exists: {config.AppPathExists}",
            $"WorkingDirectory: {ValueOrMissing(config.WorkingDirectory)}",
            $"Resolved WorkingDirectory: {ValueOrMissing(config.ResolvedWorkingDirectory)}",
            $"WorkingDirectory Exists: {config.WorkingDirectoryExists}",
            "Assemblies:"
        ];

        lines.AddRange(config.Assemblies.Select(assembly =>
            $"- {assembly.Kind}: {assembly.Assembly} -> {(assembly.Exists ? "ok" : "missing")} {assembly.ResolvedPath}"));

        if (config.Diagnostics.Count > 0)
        {
            lines.Add("Diagnostics:");
            lines.AddRange(config.Diagnostics);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string ValueOrMissing(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(missing)" : value;
    }

    private sealed record PresenterRunExecutionOptions(
        string SelectedNodeKind,
        string SelectedNodeName,
        int ScenarioCount,
        int StepCount,
        int EffectiveDelayMilliseconds,
        DateTimeOffset StartedAt);

    private sealed class PresenterStepTiming
    {
        public PresenterStepTiming(
            int stepNumber,
            string stepText,
            DateTimeOffset startedAt,
            DateTimeOffset completedAt)
        {
            StepNumber = stepNumber;
            StepText = stepText;
            StartedAt = startedAt;
            CompletedAt = completedAt;
        }

        public int StepNumber { get; }

        public string StepText { get; }

        public DateTimeOffset StartedAt { get; }

        public DateTimeOffset CompletedAt { get; }

        public int DelayAfterMilliseconds { get; set; }

        public DateTimeOffset? WaitStartedAt { get; set; }

        public DateTimeOffset? WaitCompletedAt { get; set; }
    }
}
