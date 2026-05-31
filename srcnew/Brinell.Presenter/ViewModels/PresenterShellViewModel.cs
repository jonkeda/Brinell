using System.Collections.ObjectModel;
using System.Windows.Input;
using Brinell.Presenter.Commands;
using Brinell.Presenter.Models;
using Brinell.Presenter.Services;
using Brinell.Uat;

namespace Brinell.Presenter.ViewModels;

public sealed class PresenterShellViewModel : ViewModelBase
{
    private readonly IUatExecutionService _executionService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IPresenterUserSettingsService _settingsService;
    private readonly List<UatWorkspaceNodeViewModel> _allWorkspaceNodes = [];
    private readonly List<PresenterStepTiming> _stepTimings = [];
    private readonly IUatWorkspaceService _workspaceService;
    private PresenterUatExecutionSession? _activeSession;
    private UatScenarioViewModel? _activeScenario;
    private string _allWorkspaceTreeText = string.Empty;
    private string _autPlacementText = string.Empty;
    private string _commandCatalogText = string.Empty;
    private string _diagnosticsText = string.Empty;
    private string _discoveryText = string.Empty;
    private CancellationTokenSource? _executionCancellation;
    private int _executionDelayMilliseconds = 250;
    private string _executionDelayText = "250";
    private string _executionTimingText = string.Empty;
    private bool _isRecentFoldersExpanded;
    private bool _isSelectionExpanded = true;
    private string _recentFoldersText = "No recent folders";
    private double _runProgress;
    private string _scenarioListText = string.Empty;
    private PresenterRunExecutionOptions? _activeRunOptions;
    private string _runScopeText = string.Empty;
    private UatScenarioViewModel? _selectedScenario;
    private string _selectedTab = TreeTabName;
    private UatWorkspaceNodeViewModel? _selectedWorkspaceNode;
    private string _selectedWorkspaceNodeDetailsText = "No selection";
    private string _statusSummary = "No workspace loaded";
    private string _stepListText = string.Empty;
    private string _workspaceConfigText = string.Empty;
    private string _workspaceName = "No workspace";
    private string? _workspacePath;
    private UatWorkspaceNodeViewModel? _workspaceRoot;
    private string _workspaceSummaryText = string.Empty;
    private string _workspaceTreeText = string.Empty;

    private const int MaxExecutionDelayMilliseconds = 99999;

    private const string TreeTabName = "Tree";
    private const string ConfigTabName = "Config";
    private const string DiagnosticsTabName = "Diagnostics";
    private const string DiscoveryTabName = "Discovery";
    private const string CommandCatalogTabName = "Command Catalog";

    public PresenterShellViewModel(
        IUatWorkspaceService workspaceService,
        IUatExecutionService executionService,
        IFolderPickerService folderPickerService,
        IPresenterUserSettingsService settingsService)
    {
        _workspaceService = workspaceService;
        _executionService = executionService;
        _folderPickerService = folderPickerService;
        _settingsService = settingsService;

        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync);
        ToggleRecentFoldersCommand = new RelayCommand(() => IsRecentFoldersExpanded = !IsRecentFoldersExpanded);
        ReloadCommand = new RelayCommand(ReloadWorkspace, () => WorkspacePath is not null);
        ValidateCommand = new RelayCommand(ReloadWorkspace, () => WorkspacePath is not null);
        RunCommand = new AsyncRelayCommand(RunSelectedNodeAsync);
        StopCommand = new RelayCommand(Stop);
        NextStepCommand = new AsyncRelayCommand(NextStepAsync, () => WorkspacePath is not null);
        ToggleSelectionCommand = new RelayCommand(() => IsSelectionExpanded = !IsSelectionExpanded);
        ShowTreeTabCommand = new RelayCommand(() => SelectedTab = TreeTabName);
        ShowConfigTabCommand = new RelayCommand(() => SelectedTab = ConfigTabName);
        ShowDiagnosticsTabCommand = new RelayCommand(() => SelectedTab = DiagnosticsTabName);
        ShowDiscoveryTabCommand = new RelayCommand(() => SelectedTab = DiscoveryTabName);
        ShowCommandCatalogTabCommand = new RelayCommand(() => SelectedTab = CommandCatalogTabName);

        RefreshRecentFolders(_settingsService.Load());
        LoadDefaultWorkspace();
    }

    public ObservableCollection<UatFileViewModel> Files { get; } = [];

    public ObservableCollection<UatScenarioViewModel> Scenarios { get; } = [];

    public ObservableCollection<UatStepViewModel> Steps { get; } = [];

    public ObservableCollection<UatWorkspaceNodeViewModel> WorkspaceTreeNodes { get; } = [];

    public ObservableCollection<RecentFolderViewModel> RecentFolders { get; } = [];

    public ICommand OpenFolderCommand { get; }

    public ICommand ToggleRecentFoldersCommand { get; }

    public ICommand ReloadCommand { get; }

    public ICommand ValidateCommand { get; }

    public ICommand RunCommand { get; }

    public ICommand StopCommand { get; }

    public ICommand NextStepCommand { get; }

    public ICommand ToggleSelectionCommand { get; }

    public ICommand ShowTreeTabCommand { get; }

    public ICommand ShowConfigTabCommand { get; }

    public ICommand ShowDiagnosticsTabCommand { get; }

    public ICommand ShowDiscoveryTabCommand { get; }

    public ICommand ShowCommandCatalogTabCommand { get; }

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
                OnPropertyChanged(nameof(RecentFoldersButtonText));
            }
        }
    }

    public string RecentFoldersButtonText => IsRecentFoldersExpanded ? "^" : "v";

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

    public string ExecutionDelayText
    {
        get => _executionDelayText;
        set
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
        set
        {
            var clamped = Math.Clamp(value, 0, MaxExecutionDelayMilliseconds);
            if (SetProperty(ref _executionDelayMilliseconds, clamped))
            {
                var text = clamped.ToString();
                if (!string.Equals(_executionDelayText, text, StringComparison.Ordinal))
                {
                    SetProperty(ref _executionDelayText, text, nameof(ExecutionDelayText));
                }
            }
        }
    }

    public double RunProgress
    {
        get => _runProgress;
        private set => SetProperty(ref _runProgress, value);
    }

    public UatWorkspaceNodeViewModel? SelectedWorkspaceNode
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

    public UatScenarioViewModel? SelectedScenario
    {
        get => _selectedScenario;
        set
        {
            if (SetProperty(ref _selectedScenario, value))
            {
                StopActiveSession();
                LoadSteps(value);
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
                OnPropertyChanged(nameof(TreeTabText));
                OnPropertyChanged(nameof(ConfigTabText));
                OnPropertyChanged(nameof(DiagnosticsTabText));
                OnPropertyChanged(nameof(DiscoveryTabText));
                OnPropertyChanged(nameof(CommandCatalogTabText));
            }
        }
    }

    public bool IsTreeTabSelected => SelectedTab == TreeTabName;

    public bool IsConfigTabSelected => SelectedTab == ConfigTabName;

    public bool IsDiagnosticsTabSelected => SelectedTab == DiagnosticsTabName;

    public bool IsDiscoveryTabSelected => SelectedTab == DiscoveryTabName;

    public bool IsCommandCatalogTabSelected => SelectedTab == CommandCatalogTabName;

    public string TreeTabText => IsTreeTabSelected ? "[Tree]" : "Tree";

    public string ConfigTabText => IsConfigTabSelected ? "[Config]" : "Config";

    public string DiagnosticsTabText => IsDiagnosticsTabSelected ? "[Diagnostics]" : "Diagnostics";

    public string DiscoveryTabText => IsDiscoveryTabSelected ? "[Discovery]" : "Discovery";

    public string CommandCatalogTabText => IsCommandCatalogTabSelected ? "[Command Catalog]" : "Command Catalog";

    public bool IsSelectionExpanded
    {
        get => _isSelectionExpanded;
        set
        {
            if (SetProperty(ref _isSelectionExpanded, value))
            {
                OnPropertyChanged(nameof(SelectionExpanderText));
            }
        }
    }

    public string SelectionExpanderText => IsSelectionExpanded ? "Selection ^" : "Selection v";

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
            DiagnosticsText = ex.Message;
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
        WorkspacePath = result.FolderPath;
        WorkspaceName = result.WorkspaceName;

        Files.Clear();
        foreach (var file in result.Files)
        {
            Files.Add(new UatFileViewModel(file));
        }

        Scenarios.Clear();
        foreach (var scenario in result.Scenarios)
        {
            Scenarios.Add(new UatScenarioViewModel(scenario));
        }

        SelectedScenario = Scenarios.FirstOrDefault();
        DiagnosticsText = string.Join(Environment.NewLine, result.Diagnostics);
        DiscoveryText = result.DiscoveryReport;
        CommandCatalogText = result.CommandCatalogReport;
        WorkspaceSummaryText = $"{result.Config.Summary}  {Files.Count} files  {Scenarios.Count} scenarios";
        WorkspaceConfigText = FormatWorkspaceConfig(result.Config);
        BuildWorkspaceTree(result);
        UpdateScenarioListText();
        AutPlacementText = string.Empty;
        StatusSummary = result.ErrorCount == 0
            ? $"Ready: {Files.Count} files, {Scenarios.Count} scenarios, Config: ok, Parse: ok, Bind: ok"
            : $"Needs attention: {result.ErrorCount} diagnostics";
        SelectedTab = result.ErrorCount == 0 ? TreeTabName : DiagnosticsTabName;
        if (recordRecent)
        {
            RefreshRecentFolders(_settingsService.RecordOpenedFolder(result.FolderPath));
            IsRecentFoldersExpanded = false;
        }

        RunProgress = 0;
    }

    private void BuildWorkspaceTree(UatWorkspaceLoadResult result)
    {
        _allWorkspaceNodes.Clear();
        WorkspaceTreeNodes.Clear();
        _workspaceRoot = null;

        var root = new UatWorkspaceNodeViewModel(
            result.WorkspaceName,
            UatWorkspaceNodeKind.Folder,
            0,
            result.FolderPath,
            expansionChanged: OnWorkspaceNodeExpansionChanged);

        var folderNodes = new Dictionary<string, UatWorkspaceNodeViewModel>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.GetFullPath(result.FolderPath)] = root
        };

        var fileNodes = new Dictionary<string, UatWorkspaceNodeViewModel>(StringComparer.OrdinalIgnoreCase);
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
            var fileNode = new UatWorkspaceNodeViewModel(
                fileName,
                kind,
                parentNode.Depth + 1,
                fullFilePath,
                expansionChanged: OnWorkspaceNodeExpansionChanged);

            parentNode.AddChild(fileNode);
            fileNodes[fullFilePath] = fileNode;

            if (kind == UatWorkspaceNodeKind.MarkdownFile
                && scenariosByFile.TryGetValue(fullFilePath, out var fileScenarios))
            {
                var suiteName = fileScenarios.FirstOrDefault()?.SuiteName
                    ?? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName));
                var suiteNode = new UatWorkspaceNodeViewModel(
                    suiteName,
                    UatWorkspaceNodeKind.Suite,
                    fileNode.Depth + 1,
                    fullFilePath,
                    expansionChanged: OnWorkspaceNodeExpansionChanged);

                fileNode.AddChild(suiteNode);
                foreach (var scenario in fileScenarios)
                {
                    var scenarioNode = new UatWorkspaceNodeViewModel(
                        scenario.Name,
                        UatWorkspaceNodeKind.Scenario,
                        suiteNode.Depth + 1,
                        fullFilePath,
                        scenario,
                        expansionChanged: OnWorkspaceNodeExpansionChanged);

                    suiteNode.AddChild(scenarioNode);
                    foreach (var step in scenario.Steps)
                    {
                        scenarioNode.AddChild(new UatWorkspaceNodeViewModel(
                            step.Text,
                            UatWorkspaceNodeKind.Step,
                            scenarioNode.Depth + 1,
                            fullFilePath,
                            scenario,
                            step,
                            OnWorkspaceNodeExpansionChanged));
                    }
                }
            }
            else if (filesByPath.TryGetValue(fullFilePath, out var loadedFile)
                     && !string.IsNullOrWhiteSpace(loadedFile.Diagnostics))
            {
                fileNode.AddChild(new UatWorkspaceNodeViewModel(
                    "Diagnostics available",
                    UatWorkspaceNodeKind.File,
                    fileNode.Depth + 1,
                    fullFilePath,
                    expansionChanged: OnWorkspaceNodeExpansionChanged));
            }
        }

        SortTree(root);
        ApplyInitialExpansion(root);
        AddAllNode(root);
        var preferredScenario = SelectedScenario ?? Scenarios.FirstOrDefault();
        var preferredNode = _allWorkspaceNodes.FirstOrDefault(
                                node => node.Kind == UatWorkspaceNodeKind.Scenario
                                        && ReferenceEquals(node.Scenario, preferredScenario))
                            ?? _allWorkspaceNodes.FirstOrDefault(node => node.Kind == UatWorkspaceNodeKind.Scenario)
                            ?? root;
        ExpandAncestors(preferredNode);
        _workspaceRoot = root;
        RefreshVisibleWorkspaceTree();
        SelectedWorkspaceNode = preferredNode;

        void AddAllNode(UatWorkspaceNodeViewModel node)
        {
            _allWorkspaceNodes.Add(node);
            foreach (var child in node.Children)
            {
                AddAllNode(child);
            }
        }
    }

    private void OnWorkspaceNodeExpansionChanged(UatWorkspaceNodeViewModel node)
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

        void AddVisibleNode(UatWorkspaceNodeViewModel node)
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

    private static void ApplyInitialExpansion(UatWorkspaceNodeViewModel root)
    {
        root.IsExpanded = true;
        foreach (var child in root.Children)
        {
            child.IsExpanded = child.Kind != UatWorkspaceNodeKind.Folder || CountMarkdownDescendants(child) <= 1;
            CollapseDescendants(child);
        }
    }

    private static void CollapseDescendants(UatWorkspaceNodeViewModel node)
    {
        foreach (var child in node.Children)
        {
            child.IsExpanded = false;
            CollapseDescendants(child);
        }
    }

    private static void ExpandAncestors(UatWorkspaceNodeViewModel node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    private static int CountMarkdownDescendants(UatWorkspaceNodeViewModel node)
    {
        var count = node.Kind is UatWorkspaceNodeKind.MarkdownFile or UatWorkspaceNodeKind.WorkflowConfig ? 1 : 0;
        return count + node.Children.Sum(CountMarkdownDescendants);
    }

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
            if (!await RunScenarioAutoAsync(scenario, options).ConfigureAwait(true))
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

    private void Stop()
    {
        _executionCancellation?.Cancel();
        StopActiveSession();
        StatusSummary = "Stopped";
    }

    private async Task NextStepAsync()
    {
        if (WorkspacePath is null)
        {
            StatusSummary = "No workspace loaded";
            return;
        }

        var scenario = _activeScenario
                       ?? GetRunnableScenarios(SelectedWorkspaceNode).FirstOrDefault()
                       ?? SelectedScenario;
        if (scenario is null)
        {
            StatusSummary = "No runnable scenario below selection";
            return;
        }

        if (_activeSession is null && !ReferenceEquals(SelectedScenario, scenario))
        {
            SelectedScenario = scenario;
        }

        if (_activeSession is null || _activeScenario != scenario)
        {
            await StartStepSessionAsync(scenario).ConfigureAwait(true);
        }

        if (_activeSession is null)
        {
            return;
        }

        var result = await RunNextStepAsync(_activeSession, scenario).ConfigureAwait(true);
        if (result.Status is UatStepResultStatus.Failed or UatStepResultStatus.Canceled)
        {
            MarkRemainingSteps("skip");
            scenario.Status = result.Status == UatStepResultStatus.Canceled ? "cancel" : "fail";
            StatusSummary = $"{ScenarioStatusText(result.Status)}: {scenario.Name}: {result.Message}";
            DiagnosticsText = FormatExecutionDetails(_activeSession, result.Message);
            RefreshSelectedNodeDetails();
            FinishActiveSession();
            return;
        }

        if (!_activeSession.HasNext)
        {
            scenario.Status = "pass";
            StatusSummary = $"Passed: {scenario.Name}";
            DiagnosticsText = FormatExecutionDetails(_activeSession, null);
            FinishActiveSession();
        }
        else
        {
            StatusSummary = $"Ready: {scenario.Name}. Tap Next.";
        }

        RefreshSelectedNodeDetails();
    }

    private async Task StartStepSessionAsync(UatScenarioViewModel scenario)
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
            _activeSession = await _executionService
                .CreateSessionAsync(WorkspacePath, scenario.FilePath, scenario.Name, _executionCancellation.Token)
                .ConfigureAwait(true);
            _activeScenario = scenario;
            DiscoveryText = _activeSession.DiscoveryReport;
            CommandCatalogText = _activeSession.CommandCatalogReport;
            AutPlacementText = _activeSession.AutPlacementReport;
            DiagnosticsText = string.Join(
                Environment.NewLine,
                "Step session ready.",
                AutPlacementText);
            StatusSummary = $"Ready: {scenario.Name}. Tap Next.";
        }
        catch (Exception ex)
        {
            scenario.Status = "fail";
            StatusSummary = $"Failed to start: {scenario.Name}";
            DiagnosticsText = ex.Message;
            FinishActiveSession();
        }
        finally
        {
            UpdateScenarioListText();
            RefreshSelectedNodeDetails();
        }
    }

    private async Task<bool> RunScenarioAutoAsync(
        UatScenarioViewModel scenario,
        PresenterRunExecutionOptions options)
    {
        if (WorkspacePath is null)
        {
            return false;
        }

        StopActiveSession();
        ResetScenario(scenario, clearTimings: false);
        _executionCancellation = new CancellationTokenSource();
        StatusSummary = $"Starting: {scenario.Name}";
        scenario.Status = "run";
        UpdateScenarioListText();

        try
        {
            _activeSession = await _executionService
                .CreateSessionAsync(WorkspacePath, scenario.FilePath, scenario.Name, _executionCancellation.Token)
                .ConfigureAwait(true);
            _activeScenario = scenario;
            DiscoveryText = _activeSession.DiscoveryReport;
            CommandCatalogText = _activeSession.CommandCatalogReport;
            AutPlacementText = _activeSession.AutPlacementReport;

            while (_activeSession.HasNext)
            {
                var result = await RunNextStepAsync(_activeSession, scenario).ConfigureAwait(true);
                if (result.Status is UatStepResultStatus.Failed or UatStepResultStatus.Canceled)
                {
                    MarkRemainingSteps("skip");
                    scenario.Status = result.Status == UatStepResultStatus.Canceled ? "cancel" : "fail";
                    StatusSummary = $"{ScenarioStatusText(result.Status)}: {scenario.Name}: {result.Message}";
                    DiagnosticsText = FormatExecutionDetails(_activeSession, result.Message);
                    RefreshSelectedNodeDetails();
                    return false;
                }

                await WaitBeforeNextStepAsync(_activeSession, scenario, options).ConfigureAwait(true);
            }

            scenario.Status = "pass";
            StatusSummary = $"Passed: {scenario.Name}";
            DiagnosticsText = FormatExecutionDetails(_activeSession, null);
            RefreshSelectedNodeDetails();
            return true;
        }
        catch (OperationCanceledException)
        {
            scenario.Status = "cancel";
            StatusSummary = $"Canceled: {scenario.Name}";
            MarkRunningSteps("cancel");
            RefreshSelectedNodeDetails();
            return false;
        }
        catch (Exception ex)
        {
            scenario.Status = "fail";
            StatusSummary = $"Failed: {scenario.Name}: {ex.Message}";
            DiagnosticsText = ex.Message;
            MarkRunningSteps("fail");
            RefreshSelectedNodeDetails();
            return false;
        }
        finally
        {
            UpdateScenarioListText();
            FinishActiveSession();
        }
    }

    private async Task<UatStepResult> RunNextStepAsync(
        PresenterUatExecutionSession session,
        UatScenarioViewModel scenario)
    {
        var index = session.CompletedStepCount;
        var startedAt = DateTimeOffset.Now;
        var stepText = GetScenarioStepText(scenario, index);
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

        SetStepStatus(index, StepStatusText(result.Status));
        RunProgress = scenario.Steps.Count == 0
            ? 0
            : (double)Math.Min(index + 1, scenario.Steps.Count) / scenario.Steps.Count;

        if (result.Status is UatStepResultStatus.Failed or UatStepResultStatus.Canceled)
        {
            DiagnosticsText = FormatExecutionDetails(session, result.Message);
        }

        RefreshSelectedNodeDetails();
        return result;
    }

    private async Task WaitBeforeNextStepAsync(
        PresenterUatExecutionSession session,
        UatScenarioViewModel scenario,
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

    private static string GetScenarioStepText(UatScenarioViewModel scenario, int index)
    {
        return index >= 0 && index < scenario.Steps.Count
            ? scenario.Steps[index].Text
            : scenario.Name;
    }

    private void LoadSteps(UatScenarioViewModel? scenario)
    {
        Steps.Clear();
        if (scenario is not null)
        {
            foreach (var step in scenario.Steps)
            {
                Steps.Add(step);
            }
        }

        UpdateStepListText();
        RunProgress = 0;
    }

    private void ResetScenario(UatScenarioViewModel scenario, bool clearTimings = true)
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
        StepListText = string.Join(Environment.NewLine, Steps.Select(step => step.DisplayText));
        UpdateWorkspaceTreeText();
        UpdateAllWorkspaceTreeText();
    }

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

    private IEnumerable<UatScenarioViewModel> GetRunnableScenarios(UatWorkspaceNodeViewModel? node)
    {
        if (node is null)
        {
            return [];
        }

        if (node.Kind == UatWorkspaceNodeKind.Step)
        {
            return node.Scenario is null ? [] : [node.Scenario];
        }

        if (node.Kind == UatWorkspaceNodeKind.Scenario)
        {
            return node.Scenario is null ? [] : [node.Scenario];
        }

        return EnumerateDescendants(node)
            .Where(candidate => candidate.Kind == UatWorkspaceNodeKind.Scenario && candidate.Scenario is not null)
            .Select(candidate => candidate.Scenario!)
            .Distinct()
            .ToArray();
    }

    private PresenterRunExecutionOptions CaptureRunExecutionOptions(
        UatWorkspaceNodeViewModel? selectedNode,
        IReadOnlyCollection<UatScenarioViewModel> scenarios)
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

    private string FormatSelectedNodeDetails(UatWorkspaceNodeViewModel? node)
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

    private static IEnumerable<UatWorkspaceNodeViewModel> EnumerateDescendants(UatWorkspaceNodeViewModel node)
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

    private static UatWorkspaceNodeKind GetFileNodeKind(string fileName)
    {
        if (fileName.Equals("uat.config.md", StringComparison.OrdinalIgnoreCase))
        {
            return UatWorkspaceNodeKind.WorkflowConfig;
        }

        return fileName.EndsWith(".uat.md", StringComparison.OrdinalIgnoreCase)
            ? UatWorkspaceNodeKind.MarkdownFile
            : UatWorkspaceNodeKind.MarkdownFile;
    }

    private UatWorkspaceNodeViewModel EnsureFolderNode(
        string folderPath,
        string rootPath,
        UatWorkspaceNodeViewModel root,
        Dictionary<string, UatWorkspaceNodeViewModel> folderNodes)
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
        var node = new UatWorkspaceNodeViewModel(
            folderName,
            UatWorkspaceNodeKind.Folder,
            parentNode.Depth + 1,
            fullFolderPath,
            expansionChanged: OnWorkspaceNodeExpansionChanged);
        parentNode.AddChild(node);
        folderNodes[fullFolderPath] = node;
        return node;
    }

    private static void SortTree(UatWorkspaceNodeViewModel node)
    {
        node.Children.Sort(CompareNodes);
        foreach (var child in node.Children)
        {
            SortTree(child);
        }
    }

    private static int CompareNodes(UatWorkspaceNodeViewModel left, UatWorkspaceNodeViewModel right)
    {
        if (left.Kind == UatWorkspaceNodeKind.Folder && right.Kind == UatWorkspaceNodeKind.Folder)
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

    private static int IsExpectedFailuresFolder(UatWorkspaceNodeViewModel node)
    {
        return node.Name.Equals("ExpectedFailures", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private static int NodeRank(UatWorkspaceNodeKind kind)
    {
        return kind switch
        {
            UatWorkspaceNodeKind.WorkflowConfig => 0,
            UatWorkspaceNodeKind.Folder => 1,
            UatWorkspaceNodeKind.MarkdownFile => 2,
            UatWorkspaceNodeKind.Suite => 3,
            UatWorkspaceNodeKind.Scenario => 4,
            UatWorkspaceNodeKind.Step => 5,
            _ => 6
        };
    }

    private void StopActiveSession()
    {
        _executionCancellation?.Cancel();
        _executionCancellation?.Dispose();
        _executionCancellation = null;
        _activeSession?.Dispose();
        _activeSession = null;
        _activeScenario = null;
    }

    private void FinishActiveSession()
    {
        _executionCancellation?.Dispose();
        _executionCancellation = null;
        _activeSession?.Dispose();
        _activeSession = null;
        _activeScenario = null;
    }

    private static string StepStatusText(UatStepResultStatus status)
    {
        return status switch
        {
            UatStepResultStatus.Passed => "pass",
            UatStepResultStatus.Failed => "fail",
            UatStepResultStatus.Skipped => "skip",
            UatStepResultStatus.Canceled => "cancel",
            UatStepResultStatus.Running => "run",
            _ => "wait"
        };
    }

    private static string ScenarioStatusText(UatStepResultStatus status)
    {
        return status switch
        {
            UatStepResultStatus.Canceled => "Canceled",
            UatStepResultStatus.Failed => "Failed",
            _ => "Stopped"
        };
    }

    private string FormatExecutionDetails(PresenterUatExecutionSession session, string? message)
    {
        var options = _activeRunOptions;
        List<string> lines =
        [
            .. session.StepSession.Results.Select(result =>
                $"{result.Status}: {result.Invocation.Step.Source}: {result.Invocation.CommandId}: {result.Invocation.Step.Text} {result.Message}"),
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

    private static string FormatWorkspaceConfig(UatWorkspaceConfigLoadResult config)
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
