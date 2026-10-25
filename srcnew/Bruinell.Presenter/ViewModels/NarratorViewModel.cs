using System.Collections.ObjectModel;
using System.ComponentModel;
using Bruinell.Narrator;

namespace Bruinell.Presenter.ViewModels;

/// <summary>A scene of the open video script: its row, and the scenario it runs.</summary>
public sealed class NarratorSceneViewModel
{
    private readonly List<(NarratorNodeViewModel Row, int Line)> _steps = [];
    private BddScenarioViewModel? _scenario;

    internal NarratorSceneViewModel(int number, BddVideoScene scene, BddVideoScenePlan? plan, NarratorNodeViewModel node)
    {
        Number = number;
        Scene = scene;
        Plan = plan;
        Node = node;
    }

    /// <summary>Its place in the script, from 1.</summary>
    public int Number { get; }

    /// <summary>The scene, as the script writes it.</summary>
    public BddVideoScene Scene { get; }

    /// <summary>The scene as it will run; null when the script has it wrong (its scenario was not found).</summary>
    public BddVideoScenePlan? Plan { get; }

    /// <summary>The scene's row in the tree.</summary>
    public NarratorNodeViewModel Node { get; }

    /// <summary>The workspace's scenario the scene runs; null until the workspace has it.</summary>
    public BddScenarioViewModel? Scenario => _scenario;

    /// <summary>What the scene's kept recording is worth.</summary>
    public BddSceneState State { get; internal set; }

    /// <summary>What a recording of the scene is made from now (<see cref="BddKeptScenes.Hash"/>).</summary>
    public string Hash { get; internal set; } = string.Empty;

    /// <summary>Whether the scene can run: the script has it right and the workspace has its scenario.</summary>
    public bool CanRun => Plan is not null && _scenario is not null;

    internal void AddStep(NarratorNodeViewModel row, int line) => _steps.Add((row, line));

    // The rows follow the scenario's run: the scene its status, each step the status of its line.
    internal void Link(BddScenarioViewModel? scenario)
    {
        if (_scenario is not null)
        {
            _scenario.PropertyChanged -= OnScenarioChanged;
            foreach (var step in _scenario.Steps)
            {
                step.PropertyChanged -= OnStepChanged;
            }
        }

        _scenario = scenario;
        if (scenario is null)
        {
            return;
        }

        scenario.PropertyChanged += OnScenarioChanged;
        foreach (var step in scenario.Steps)
        {
            step.PropertyChanged += OnStepChanged;
        }

        Refresh();
    }

    private void OnScenarioChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BddScenarioViewModel.Status))
        {
            Refresh();
        }
    }

    private void OnStepChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BddStepViewModel.Status))
        {
            Refresh();
        }
    }

    // The scene's row: how its run goes while it runs or has run, else what its recording is worth.
    internal void RefreshStatus()
        => Node.Status = _scenario is { Status: not "wait" } scenario ? scenario.Status : State switch
        {
            BddSceneState.Recorded => "pass",
            BddSceneState.Failed => "fail",
            BddSceneState.OutOfDate => "skip",
            _ => "wait",
        };

    private void Refresh()
    {
        if (_scenario is not { } scenario)
        {
            return;
        }

        RefreshStatus();
        foreach (var (row, line) in _steps)
        {
            // A call's steps share its line: the row fails with any of them, passes with all.
            var statuses = scenario.Steps.Where(step => step.LineNumber == line).Select(step => step.Status).ToList();
            row.Status = statuses.Count == 0 ? "wait"
                : statuses.Contains("fail") ? "fail"
                : statuses.Contains("run") ? "run"
                : statuses.Contains("cancel") ? "cancel"
                : statuses.All(status => status == "pass") ? "pass"
                : statuses.All(status => status == "skip") ? "skip"
                : "wait";
        }
    }
}

/// <summary>
/// Narrator mode: a video script (<c>*.video.md</c>) open as a tree of its scenes, each with the
/// lines said and the steps run, and a checkbox for the scenes to run or record.
/// </summary>
public sealed class NarratorViewModel : ViewModelBase
{
    private string? _scriptPath;
    private string _summary = "No video script open";
    private string _problemsText = string.Empty;
    private string _treeText = string.Empty;
    private bool _updatingChecks;
    private NarratorNodeViewModel? _selectedNode;

    /// <summary>The tree's root rows: the script, when one is open.</summary>
    public ObservableCollection<NarratorNodeViewModel> Roots { get; } = [];

    /// <summary>The scenes of the open script, in order.</summary>
    public IReadOnlyList<NarratorSceneViewModel> Scenes { get; private set; } = [];

    /// <summary>The script as checked against the feature files; null when none is open.</summary>
    public BddVideoScriptCheck? Check { get; private set; }

    /// <summary>The open script's file; null when none is open.</summary>
    public string? ScriptPath
    {
        get => _scriptPath;
        private set
        {
            if (SetProperty(ref _scriptPath, value))
            {
                OnPropertyChanged(nameof(HasScript));
                OnPropertyChanged(nameof(ScriptName));
            }
        }
    }

    /// <summary>Whether a script is open.</summary>
    public bool HasScript => ScriptPath is not null;

    /// <summary>The open script's file name, or what to do when none is open.</summary>
    public string ScriptName => ScriptPath is null ? "Open a video script" : Path.GetFileName(ScriptPath);

    /// <summary>The suite's folder, found by walking up from the script to its config; null when there is none above it.</summary>
    public string? ConfigDirectory => Check?.ConfigDirectory;

    /// <summary>One line on the open script: its scenes, the checked ones, its problems.</summary>
    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    /// <summary>The script's diagnostics, a line each; empty when it is clean.</summary>
    public string ProblemsText
    {
        get => _problemsText;
        private set
        {
            if (SetProperty(ref _problemsText, value))
            {
                OnPropertyChanged(nameof(HasProblems));
            }
        }
    }

    /// <summary>Whether the script has diagnostics to show.</summary>
    public bool HasProblems => ProblemsText.Length > 0;

    /// <summary>The tree in plain text, a row a line, for the hidden mirror a UI test reads.</summary>
    public string TreeText
    {
        get => _treeText;
        private set => SetProperty(ref _treeText, value);
    }

    /// <summary>Opens a scene's recording; the shell sets it (the system's player).</summary>
    public Action<string>? OpenVideo { get; set; }

    /// <summary>Whether the script's scenes can be stitched: every scene has a recording, of whatever worth.</summary>
    public bool CanStitch => Scenes.Count > 0 && Scenes.All(scene => scene.State != BddSceneState.NotRecorded);

    /// <summary>
    /// Reads each scene's kept recording: its state beside its name, its video to watch. With
    /// <paramref name="resetChecks"/>, the scenes that need recording are the ones checked.
    /// </summary>
    /// <param name="scenesFolder">The script's scenes folder; null when there is none yet.</param>
    /// <param name="voice">The voice setting; the script's own wins.</param>
    /// <param name="beat">The beat setting; the script's own wins.</param>
    /// <param name="resetChecks">Whether to check exactly the scenes that are not recorded and current.</param>
    public void RefreshStates(string? scenesFolder, string? voice, TimeSpan beat, bool resetChecks)
    {
        foreach (var scene in Scenes)
        {
            if (scene.Plan is not { } plan || Check?.Script is not { } script)
            {
                continue;
            }

            scene.Hash = BddKeptScenes.Hash(plan, script.Voice ?? voice, script.Beat ?? beat, script.Instructions);
            scene.State = scenesFolder is null ? BddSceneState.NotRecorded : BddKeptScenes.State(scenesFolder, scene.Number, scene.Hash);
            var recorded = scenesFolder is null || scene.State == BddSceneState.NotRecorded ? null : BddKeptScenes.Recorded(scenesFolder, scene.Number);
            scene.Node.Video = recorded?.Video is { } video ? Path.Combine(BddKeptScenes.SceneFolder(scenesFolder!, scene.Number), video) : null;
            var length = recorded is null ? string.Empty : $" {TimeSpan.FromSeconds(recorded.Duration):m\\:ss}";
            scene.Node.Detail = scene.State switch
            {
                BddSceneState.Recorded => "recorded" + length,
                BddSceneState.Failed => "failed" + length,
                BddSceneState.OutOfDate => "out of date",
                _ => "not recorded",
            } + $", {plan.Scenario.Name}";
            scene.RefreshStatus();
            if (resetChecks && scene.CanRun)
            {
                scene.Node.SetChecked(scene.State != BddSceneState.Recorded);
            }
        }

        OnPropertyChanged(nameof(CanStitch));
        RefreshChecks();
    }

    /// <summary>The scenes whose checkbox is on and that can run, in the script's order.</summary>
    public IReadOnlyList<NarratorSceneViewModel> CheckedScenes => [.. Scenes.Where(scene => scene.Node.IsChecked && scene.CanRun)];

    /// <summary>Raised when the checked scenes change.</summary>
    public event Action? CheckedChanged;

    /// <summary>Raised when a row is selected, with the scene it is of; null for the script's row.</summary>
    public event Action<NarratorSceneViewModel?>? SceneSelected;

    /// <summary>The selected row of the tree.</summary>
    public NarratorNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                SceneSelected?.Invoke(SceneOf(value));
            }
        }
    }

    /// <summary>The scene a row is of: the scene's own row, or one under it.</summary>
    /// <param name="node">A row of the tree.</param>
    public NarratorSceneViewModel? SceneOf(NarratorNodeViewModel? node)
        => node is null ? null : Scenes.FirstOrDefault(scene => ReferenceEquals(scene.Node, node) || scene.Node.Children.Contains(node));

    /// <summary>The scene that runs a scenario of the workspace; null when none does.</summary>
    /// <param name="scenario">A scenario.</param>
    public NarratorSceneViewModel? SceneOf(BddScenarioViewModel? scenario)
        => scenario is null ? null : Scenes.FirstOrDefault(scene => ReferenceEquals(scene.Scenario, scenario));

    /// <summary>Opens a video script: reads it, finds its suite by walking up to the config, and checks it.</summary>
    /// <param name="path">The <c>*.video.md</c> file.</param>
    public void Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = Path.GetFullPath(path);
        var check = BddVideoScriptCheck.Check(File.ReadAllText(path), path);
        Check = check;
        ScriptPath = path;
        OnPropertyChanged(nameof(ConfigDirectory));
        Build(check);
    }

    /// <summary>Closes the script.</summary>
    public void Close()
    {
        foreach (var scene in Scenes)
        {
            scene.Link(null);
        }

        Check = null;
        ScriptPath = null;
        Scenes = [];
        Roots.Clear();
        OnPropertyChanged(nameof(ConfigDirectory));
        ProblemsText = string.Empty;
        Summary = "No video script open";
        TreeText = string.Empty;
    }

    /// <summary>Points each scene at the workspace's scenario it runs, so its rows follow the run.</summary>
    /// <param name="scenarios">The loaded workspace's scenarios.</param>
    public void Link(IEnumerable<BddScenarioViewModel> scenarios)
    {
        var all = scenarios.ToList();
        foreach (var scene in Scenes)
        {
            var found = scene.Plan is { } plan
                ? all.FirstOrDefault(candidate =>
                    string.Equals(Path.GetFullPath(candidate.FilePath), plan.FeatureFile, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(candidate.Name, plan.Scenario.Name, StringComparison.Ordinal))
                : null;
            scene.Link(found);
            scene.Node.IsCheckEnabled = scene.CanRun;
            scene.Node.SetChecked(scene.CanRun);
        }

        RefreshChecks();
    }

    private void Build(BddVideoScriptCheck check)
    {
        foreach (var scene in Scenes)
        {
            scene.Link(null);
        }

        Roots.Clear();
        var problems = check.Diagnostics.Select(diagnostic => $"{diagnostic.Severity} {diagnostic.Code} line {diagnostic.Location.LineNumber}: {diagnostic.Message}").ToList();
        ProblemsText = string.Join(Environment.NewLine, problems);
        if (check.Script is not { } script)
        {
            Scenes = [];
            Summary = "The script cannot be read";
            TreeText = string.Empty;
            return;
        }

        var root = new NarratorNodeViewModel(NarratorNodeKind.Script, script.Title, "Root", OnChecked) { IsExpanded = true };
        List<NarratorSceneViewModel> scenes = [];
        for (var index = 0; index < script.Scenes.Count; index++)
        {
            var written = script.Scenes[index];
            var next = index + 1 < script.Scenes.Count ? script.Scenes[index + 1].Line : int.MaxValue;
            var plan = check.Scenes.FirstOrDefault(candidate => ReferenceEquals(candidate.Scene, written));
            var wrong = check.Diagnostics
                .Where(diagnostic => diagnostic.Location.LineNumber >= written.Line && diagnostic.Location.LineNumber < next)
                .ToList();
            var number = index + 1;
            NarratorNodeViewModel node = null!;
            node = new NarratorNodeViewModel(NarratorNodeKind.Scene, $"{number} {written.Name}", number.ToString(System.Globalization.CultureInfo.InvariantCulture), OnChecked)
            {
                WatchCommand = new Commands.RelayCommand(() =>
                {
                    if (node.Video is { } video)
                    {
                        OpenVideo?.Invoke(video);
                    }
                }),
                Problem = string.Join("; ", wrong.Where(diagnostic => !IsOfALine(written, diagnostic.Location.LineNumber)).Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")),
            };
            node.Detail = plan?.Scenario.Name ?? "no scenario";
            var scene = new NarratorSceneViewModel(number, written, plan, node);
            AddRows(scene, wrong);
            root.Children.Add(node);
            scenes.Add(scene);
        }

        Scenes = scenes;
        root.PropertyChanged += OnRowChanged;
        Roots.Add(root);
        foreach (var scene in scenes)
        {
            // Until the workspace has its scenario a scene cannot run; Link turns the checkbox on.
            scene.Node.IsCheckEnabled = false;
        }

        RefreshChecks();
    }

    private static bool IsOfALine(BddVideoScene scene, int line) => scene.Lines.Any(row => row.Line == line);

    // Under a scene: its intro, each step with what is said before it, its outro.
    private void AddRows(NarratorSceneViewModel scene, List<BddDiagnostic> wrong)
    {
        var row = 0;
        string Key() => $"{scene.Number}_{++row}";
        if (scene.Scene.Intro is { Length: > 0 } intro)
        {
            scene.Node.Children.Add(new NarratorNodeViewModel(NarratorNodeKind.Line, intro, Key()) { Note = "intro" });
        }

        if (scene.Plan is { } plan)
        {
            foreach (var step in plan.Steps)
            {
                var say = plan.Says.TryGetValue(step.Source.LineNumber, out var line) ? line.Say : null;
                var node = new NarratorNodeViewModel(NarratorNodeKind.Step, BddVideoScriptCheck.Written(step), Key()) { Note = say ?? "silent" };
                scene.Node.Children.Add(node);
                scene.AddStep(node, step.Source.LineNumber);
            }
        }

        // A Say row that names no step of the scenario: shown, so it can be put right.
        foreach (var line in scene.Scene.Lines)
        {
            var problem = string.Join("; ", wrong.Where(diagnostic => diagnostic.Location.LineNumber == line.Line && diagnostic.Severity == BddDiagnosticSeverity.Error).Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}"));
            if (problem.Length > 0)
            {
                scene.Node.Children.Add(new NarratorNodeViewModel(NarratorNodeKind.Step, line.Step, Key()) { Note = line.Say, Problem = problem, Status = "fail" });
            }
        }

        if (scene.Scene.Outro is { Length: > 0 } outro)
        {
            scene.Node.Children.Add(new NarratorNodeViewModel(NarratorNodeKind.Line, outro, Key()) { Note = "outro" });
        }

        foreach (var child in scene.Node.Children)
        {
            child.PropertyChanged += OnRowChanged;
        }

        scene.Node.PropertyChanged += OnRowChanged;
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NarratorNodeViewModel.DisplayText))
        {
            RefreshTreeText();
        }
    }

    // The script's checkbox checks or clears every scene; a scene's updates the script's.
    private void OnChecked(NarratorNodeViewModel node)
    {
        if (_updatingChecks)
        {
            return;
        }

        if (node.Kind == NarratorNodeKind.Script)
        {
            _updatingChecks = true;
            foreach (var scene in Scenes.Where(scene => scene.CanRun))
            {
                scene.Node.SetChecked(node.IsChecked);
            }

            _updatingChecks = false;
        }

        RefreshChecks();
        CheckedChanged?.Invoke();
    }

    private void RefreshChecks()
    {
        var runnable = Scenes.Where(scene => scene.CanRun).ToList();
        if (Roots.FirstOrDefault() is { } root)
        {
            root.SetChecked(runnable.Count > 0 && runnable.All(scene => scene.Node.IsChecked));
            root.IsCheckEnabled = runnable.Count > 0;
            root.Detail = $"{Scenes.Count} scenes";
        }

        var problems = Check?.Diagnostics.Count(diagnostic => diagnostic.Severity == BddDiagnosticSeverity.Error) ?? 0;
        var recordedCount = Scenes.Count(scene => scene.State == BddSceneState.Recorded);
        Summary = $"{Scenes.Count} scenes, {CheckedScenes.Count} checked"
            + (recordedCount > 0 ? $", {recordedCount} recorded" : string.Empty)
            + (problems > 0 ? $", {problems} problems" : string.Empty);
        RefreshTreeText();
    }

    private void RefreshTreeText()
    {
        List<string> lines = [];
        void Add(NarratorNodeViewModel node, int depth)
        {
            lines.Add(new string(' ', depth * 2) + node.DisplayText);
            if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                {
                    Add(child, depth + 1);
                }
            }
        }

        foreach (var root in Roots)
        {
            Add(root, 0);
        }

        TreeText = string.Join(Environment.NewLine, lines);
    }
}
