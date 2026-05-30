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
    private readonly IUatWorkspaceService _workspaceService;
    private PresenterUatExecutionSession? _activeSession;
    private UatScenarioViewModel? _activeScenario;
    private CancellationTokenSource? _executionCancellation;
    private string _commandCatalogText = string.Empty;
    private string _diagnosticsText = string.Empty;
    private string _discoveryText = string.Empty;
    private int _executionDelayMilliseconds = 250;
    private double _runProgress;
    private string _selectedExecutionMode = "Step";
    private UatScenarioViewModel? _selectedScenario;
    private string _statusSummary = "No workspace loaded";
    private string _stepListText = string.Empty;
    private string _scenarioListText = string.Empty;
    private string _workspaceConfigText = string.Empty;
    private string _workspaceName = "No workspace";
    private string? _workspacePath;
    private string _workspaceSummaryText = string.Empty;
    private bool _isFilesExpanded;
    private bool _isScenariosExpanded = true;
    private bool _isStepsExpanded = true;
    private bool _isDiagnosticsExpanded;
    private bool _isDiscoveryExpanded;
    private bool _isCommandCatalogExpanded;
    private bool _isWorkspaceConfigExpanded;

    public PresenterShellViewModel(
        IUatWorkspaceService workspaceService,
        IUatExecutionService executionService,
        IFolderPickerService folderPickerService)
    {
        _workspaceService = workspaceService;
        _executionService = executionService;
        _folderPickerService = folderPickerService;

        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync);
        ReloadCommand = new RelayCommand(ReloadWorkspace, () => WorkspacePath is not null);
        ValidateCommand = new RelayCommand(ReloadWorkspace, () => WorkspacePath is not null);
        RunSelectedCommand = new AsyncRelayCommand(RunSelectedAsync, () => SelectedScenario is not null);
        RunAllCommand = new AsyncRelayCommand(RunAllAsync, () => Scenarios.Count > 0);
        StopCommand = new RelayCommand(Stop);
        NextStepCommand = new AsyncRelayCommand(NextStepAsync, () => SelectedScenario is not null);
        ToggleFilesCommand = new RelayCommand(() => IsFilesExpanded = !IsFilesExpanded);
        ToggleScenariosCommand = new RelayCommand(() => IsScenariosExpanded = !IsScenariosExpanded);
        ToggleStepsCommand = new RelayCommand(() => IsStepsExpanded = !IsStepsExpanded);
        ToggleWorkspaceConfigCommand = new RelayCommand(() => IsWorkspaceConfigExpanded = !IsWorkspaceConfigExpanded);
        ToggleDiagnosticsCommand = new RelayCommand(() => IsDiagnosticsExpanded = !IsDiagnosticsExpanded);
        ToggleDiscoveryCommand = new RelayCommand(() => IsDiscoveryExpanded = !IsDiscoveryExpanded);
        ToggleCommandCatalogCommand = new RelayCommand(() => IsCommandCatalogExpanded = !IsCommandCatalogExpanded);

        LoadDefaultWorkspace();
    }

    public ObservableCollection<UatFileViewModel> Files { get; } = [];

    public ObservableCollection<UatScenarioViewModel> Scenarios { get; } = [];

    public ObservableCollection<UatStepViewModel> Steps { get; } = [];

    public IReadOnlyList<string> ExecutionModes { get; } = ["Step", "Auto"];

    public ICommand OpenFolderCommand { get; }

    public ICommand ReloadCommand { get; }

    public ICommand ValidateCommand { get; }

    public ICommand RunSelectedCommand { get; }

    public ICommand RunAllCommand { get; }

    public ICommand StopCommand { get; }

    public ICommand NextStepCommand { get; }

    public ICommand ToggleFilesCommand { get; }

    public ICommand ToggleScenariosCommand { get; }

    public ICommand ToggleStepsCommand { get; }

    public ICommand ToggleWorkspaceConfigCommand { get; }

    public ICommand ToggleDiagnosticsCommand { get; }

    public ICommand ToggleDiscoveryCommand { get; }

    public ICommand ToggleCommandCatalogCommand { get; }

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
        private set => SetProperty(ref _workspaceSummaryText, value);
    }

    public string StatusSummary
    {
        get => _statusSummary;
        private set => SetProperty(ref _statusSummary, value);
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

    public string SelectedExecutionMode
    {
        get => _selectedExecutionMode;
        set => SetProperty(ref _selectedExecutionMode, value);
    }

    public int ExecutionDelayMilliseconds
    {
        get => _executionDelayMilliseconds;
        set => SetProperty(ref _executionDelayMilliseconds, Math.Max(0, value));
    }

    public double RunProgress
    {
        get => _runProgress;
        private set => SetProperty(ref _runProgress, value);
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

    public bool IsFilesExpanded
    {
        get => _isFilesExpanded;
        set
        {
            if (SetProperty(ref _isFilesExpanded, value))
            {
                OnPropertyChanged(nameof(FilesExpanderText));
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
                OnPropertyChanged(nameof(ScenariosExpanderText));
            }
        }
    }

    public bool IsStepsExpanded
    {
        get => _isStepsExpanded;
        set
        {
            if (SetProperty(ref _isStepsExpanded, value))
            {
                OnPropertyChanged(nameof(StepsExpanderText));
            }
        }
    }

    public bool IsDiagnosticsExpanded
    {
        get => _isDiagnosticsExpanded;
        set
        {
            if (SetProperty(ref _isDiagnosticsExpanded, value))
            {
                OnPropertyChanged(nameof(DiagnosticsExpanderText));
            }
        }
    }

    public bool IsWorkspaceConfigExpanded
    {
        get => _isWorkspaceConfigExpanded;
        set
        {
            if (SetProperty(ref _isWorkspaceConfigExpanded, value))
            {
                OnPropertyChanged(nameof(WorkspaceConfigExpanderText));
            }
        }
    }

    public bool IsDiscoveryExpanded
    {
        get => _isDiscoveryExpanded;
        set
        {
            if (SetProperty(ref _isDiscoveryExpanded, value))
            {
                OnPropertyChanged(nameof(DiscoveryExpanderText));
            }
        }
    }

    public bool IsCommandCatalogExpanded
    {
        get => _isCommandCatalogExpanded;
        set
        {
            if (SetProperty(ref _isCommandCatalogExpanded, value))
            {
                OnPropertyChanged(nameof(CommandCatalogExpanderText));
            }
        }
    }

    public string FilesExpanderText => IsFilesExpanded ? "Files ^" : "Files v";

    public string ScenariosExpanderText => IsScenariosExpanded ? "Suite ^" : "Suite v";

    public string StepsExpanderText => IsStepsExpanded ? "Steps ^" : "Steps v";

    public string WorkspaceConfigExpanderText => IsWorkspaceConfigExpanded ? "Workspace Config ^" : "Workspace Config v";

    public string DiagnosticsExpanderText => IsDiagnosticsExpanded ? "Diagnostics ^" : "Diagnostics v";

    public string DiscoveryExpanderText => IsDiscoveryExpanded ? "Discovery ^" : "Discovery v";

    public string CommandCatalogExpanderText => IsCommandCatalogExpanded ? "Command Catalog ^" : "Command Catalog v";

    private void LoadDefaultWorkspace()
    {
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

            LoadWorkspace(folder);
        }
        catch (Exception ex)
        {
            StatusSummary = "Failed to open folder";
            DiagnosticsText = ex.Message;
            IsDiagnosticsExpanded = true;
        }
    }

    private void ReloadWorkspace()
    {
        if (WorkspacePath is not null)
        {
            LoadWorkspace(WorkspacePath);
        }
    }

    private void LoadWorkspace(string folderPath)
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
        IsWorkspaceConfigExpanded = result.Config.HasErrors;
        IsDiagnosticsExpanded = result.ErrorCount > 0;
        UpdateScenarioListText();
        StatusSummary = result.ErrorCount == 0
            ? $"Ready: {Files.Count} files, {Scenarios.Count} scenarios, Config: ok, Parse: ok, Bind: ok"
            : $"Needs attention: {result.ErrorCount} diagnostics";
        RunProgress = 0;
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

    private async Task RunSelectedAsync()
    {
        if (SelectedScenario is null || WorkspacePath is null)
        {
            StatusSummary = "No scenario selected";
            return;
        }

        if (SelectedExecutionMode.Equals("Step", StringComparison.OrdinalIgnoreCase))
        {
            await StartStepSessionAsync(SelectedScenario).ConfigureAwait(true);
            return;
        }

        await RunScenarioAutoAsync(SelectedScenario).ConfigureAwait(true);
    }

    private async Task RunAllAsync()
    {
        if (WorkspacePath is null || Scenarios.Count == 0)
        {
            StatusSummary = "No scenarios loaded";
            return;
        }

        var passed = 0;
        foreach (var scenario in Scenarios)
        {
            SelectedScenario = scenario;
            if (!await RunScenarioAutoAsync(scenario).ConfigureAwait(true))
            {
                StatusSummary = $"Failed: {passed}/{Scenarios.Count} scenarios passed";
                return;
            }

            passed++;
        }

        StatusSummary = $"Passed: {passed}/{Scenarios.Count} scenarios";
    }

    private void Stop()
    {
        _executionCancellation?.Cancel();
        StopActiveSession();
        StatusSummary = "Stopped";
    }

    private async Task NextStepAsync()
    {
        if (SelectedScenario is null || WorkspacePath is null)
        {
            StatusSummary = "No scenario selected";
            return;
        }

        if (_activeSession is null || _activeScenario != SelectedScenario)
        {
            await StartStepSessionAsync(SelectedScenario).ConfigureAwait(true);
        }

        if (_activeSession is null)
        {
            return;
        }

        var result = await RunNextStepAsync(_activeSession, SelectedScenario).ConfigureAwait(true);
        if (result.Status is UatStepResultStatus.Failed or UatStepResultStatus.Canceled)
        {
            MarkRemainingSteps("skip");
            FinishActiveSession();
            return;
        }

        if (!_activeSession.HasNext)
        {
            SelectedScenario.Status = "pass";
            StatusSummary = $"Passed: {SelectedScenario.Name}";
            FinishActiveSession();
        }
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
            DiagnosticsText = "Step session ready.";
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
        }
    }

    private async Task<bool> RunScenarioAutoAsync(UatScenarioViewModel scenario)
    {
        if (WorkspacePath is null)
        {
            return false;
        }

        StopActiveSession();
        ResetScenario(scenario);
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

            while (_activeSession.HasNext)
            {
                var result = await RunNextStepAsync(_activeSession, scenario).ConfigureAwait(true);
                if (result.Status is UatStepResultStatus.Failed or UatStepResultStatus.Canceled)
                {
                    MarkRemainingSteps("skip");
                    scenario.Status = result.Status == UatStepResultStatus.Canceled ? "cancel" : "fail";
                    StatusSummary = $"{ScenarioStatusText(result.Status)}: {scenario.Name}: {result.Message}";
                    DiagnosticsText = FormatExecutionDetails(_activeSession, result.Message);
                    return false;
                }

                if (_activeSession.HasNext && ExecutionDelayMilliseconds > 0)
                {
                    await Task.Delay(ExecutionDelayMilliseconds, _executionCancellation.Token).ConfigureAwait(true);
                }
            }

            scenario.Status = "pass";
            StatusSummary = $"Passed: {scenario.Name}";
            DiagnosticsText = FormatExecutionDetails(_activeSession, null);
            return true;
        }
        catch (OperationCanceledException)
        {
            scenario.Status = "cancel";
            StatusSummary = $"Canceled: {scenario.Name}";
            MarkRunningSteps("cancel");
            return false;
        }
        catch (Exception ex)
        {
            scenario.Status = "fail";
            StatusSummary = $"Failed: {scenario.Name}: {ex.Message}";
            DiagnosticsText = ex.Message;
            MarkRunningSteps("fail");
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
        SetStepStatus(index, "run");
        StatusSummary = $"Running: {scenario.Name} ({index + 1}/{scenario.Steps.Count})";

        var cancellationToken = _executionCancellation?.Token ?? CancellationToken.None;
        var result = await Task.Run(
            () => session.RunNextAsync(cancellationToken),
            cancellationToken).ConfigureAwait(true);

        SetStepStatus(index, StepStatusText(result.Status));
        RunProgress = scenario.Steps.Count == 0
            ? 0
            : (double)Math.Min(index + 1, scenario.Steps.Count) / scenario.Steps.Count;

        if (result.Status is UatStepResultStatus.Failed or UatStepResultStatus.Canceled)
        {
            DiagnosticsText = FormatExecutionDetails(session, result.Message);
        }

        return result;
    }

    private void ResetScenario(UatScenarioViewModel scenario)
    {
        scenario.Status = "run";
        foreach (var step in scenario.Steps)
        {
            step.Status = "wait";
        }

        RunProgress = 0;
        UpdateStepListText();
        UpdateScenarioListText();
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
    }

    private void UpdateStepListText()
    {
        StepListText = string.Join(Environment.NewLine, Steps.Select(step => step.DisplayText));
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

    private static string FormatExecutionDetails(PresenterUatExecutionSession session, string? message)
    {
        List<string> lines =
        [
            .. session.StepSession.Results.Select(result =>
                $"{result.Status}: {result.Invocation.Step.Source}: {result.Invocation.CommandId}: {result.Invocation.Step.Text} {result.Message}"),
            "Runtime trace:"
        ];
        lines.AddRange(session.Runner.Context.Diagnostics);
        if (!string.IsNullOrWhiteSpace(message))
        {
            lines.Add("Message:");
            lines.Add(message);
        }

        lines.Add(session.DiscoveryReport);
        lines.Add(session.CommandCatalogReport);
        return string.Join(Environment.NewLine, lines);
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
}
