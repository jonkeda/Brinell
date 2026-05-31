using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Brinell.Presenter.Commands;
using Microsoft.Maui.Graphics;

namespace Brinell.Presenter.ViewModels;

public sealed class UatWorkspaceNodeViewModel : ViewModelBase
{
    private readonly Action<UatWorkspaceNodeViewModel>? _expansionChanged;
    private bool _isExpanded;

    public UatWorkspaceNodeViewModel(
        string name,
        UatWorkspaceNodeKind kind,
        int depth,
        string? filePath = null,
        UatScenarioViewModel? scenario = null,
        UatStepViewModel? step = null,
        Action<UatWorkspaceNodeViewModel>? expansionChanged = null)
    {
        Name = name;
        Kind = kind;
        Depth = depth;
        FilePath = filePath;
        Scenario = scenario;
        Step = step;
        _expansionChanged = expansionChanged;
        var stableId = SanitizeAutomationId($"{kind}_{name}_{filePath}");
        var toggleId = SanitizeAutomationId($"{kind}_{name}");
        AutomationId = $"WorkspaceNode_{stableId}";
        ToggleAutomationId = $"WorkspaceNodeToggle_{toggleId}";
        ToggleExpansionCommand = new RelayCommand(ToggleExpansion, () => CanExpand);

        if (Scenario is not null)
        {
            Scenario.PropertyChanged += OnChildStatusChanged;
        }

        if (Step is not null)
        {
            Step.PropertyChanged += OnChildStatusChanged;
        }
    }

    public string Name { get; }

    public UatWorkspaceNodeKind Kind { get; }

    public int Depth { get; }

    public double IndentWidth => Depth * 18;

    public string? FilePath { get; }

    public UatScenarioViewModel? Scenario { get; }

    public UatStepViewModel? Step { get; }

    public UatWorkspaceNodeViewModel? Parent { get; private set; }

    public string AutomationId { get; }

    public string ToggleAutomationId { get; }

    public ICommand ToggleExpansionCommand { get; }

    public List<UatWorkspaceNodeViewModel> Children { get; } = [];

    public bool CanExpand => Children.Count > 0;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(ExpansionText));
                OnPropertyChanged(nameof(DisplayText));
                _expansionChanged?.Invoke(this);
            }
        }
    }

    public string ExpansionText => CanExpand ? (IsExpanded ? "v" : ">") : string.Empty;

    public bool IsRunnable => Kind is UatWorkspaceNodeKind.Folder
        or UatWorkspaceNodeKind.MarkdownFile
        or UatWorkspaceNodeKind.Suite
        or UatWorkspaceNodeKind.Scenario
        or UatWorkspaceNodeKind.Step;

    public string Icon => Kind switch
    {
        UatWorkspaceNodeKind.Folder => AggregateStatusIcon() ?? "[F]",
        UatWorkspaceNodeKind.MarkdownFile => AggregateStatusIcon() ?? "[MD]",
        UatWorkspaceNodeKind.WorkflowConfig => "[C]",
        UatWorkspaceNodeKind.File => "[ ]",
        UatWorkspaceNodeKind.Suite => AggregateStatusIcon() ?? "[S]",
        UatWorkspaceNodeKind.Scenario => Scenario?.StatusIcon ?? UatStatusPresentation.Icon("wait"),
        UatWorkspaceNodeKind.Step => Step?.StatusIcon ?? UatStatusPresentation.Icon("wait"),
        _ => "[ ]"
    };

    public string StatusDescription => Kind switch
    {
        UatWorkspaceNodeKind.Scenario => Scenario?.StatusDescription ?? UatStatusPresentation.Description("wait"),
        UatWorkspaceNodeKind.Step => Step?.StatusDescription ?? UatStatusPresentation.Description("wait"),
        UatWorkspaceNodeKind.MarkdownFile => "UAT Markdown file",
        UatWorkspaceNodeKind.WorkflowConfig => "Workflow config",
        UatWorkspaceNodeKind.Suite => "Suite",
        UatWorkspaceNodeKind.Folder => "Folder",
        _ => "File"
    };

    public Color IconColor => Kind switch
    {
        UatWorkspaceNodeKind.Scenario => Scenario?.StatusColor ?? UatStatusPresentation.Color("wait"),
        UatWorkspaceNodeKind.Step => Step?.StatusColor ?? UatStatusPresentation.Color("wait"),
        _ when AggregateStatus() is { } status => UatStatusPresentation.Color(status),
        UatWorkspaceNodeKind.WorkflowConfig => Color.FromArgb("#2563EB"),
        UatWorkspaceNodeKind.MarkdownFile => Color.FromArgb("#0F766E"),
        UatWorkspaceNodeKind.Suite => Color.FromArgb("#7C3AED"),
        UatWorkspaceNodeKind.Folder => Color.FromArgb("#475569"),
        _ => Color.FromArgb("#64748B")
    };

    public string DisplayText => $"{new string(' ', Depth * 2)}{ExpansionText.PadRight(1)} {Icon} {Name}";

    public void AddChild(UatWorkspaceNodeViewModel child)
    {
        child.Parent = this;
        child.PropertyChanged += OnChildStatusChanged;
        Children.Add(child);
        OnPropertyChanged(nameof(CanExpand));
        OnPropertyChanged(nameof(ExpansionText));
        OnPropertyChanged(nameof(DisplayText));
    }

    private void OnChildStatusChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UatScenarioViewModel.Status)
            or nameof(UatScenarioViewModel.StatusIcon)
            or nameof(UatStepViewModel.Status)
            or nameof(UatStepViewModel.StatusIcon)
            or nameof(Icon)
            or nameof(IconColor)
            or nameof(DisplayText))
        {
            OnPropertyChanged(nameof(Icon));
            OnPropertyChanged(nameof(IconColor));
            OnPropertyChanged(nameof(StatusDescription));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    private void ToggleExpansion()
    {
        if (CanExpand)
        {
            IsExpanded = !IsExpanded;
        }
    }

    private string? AggregateStatusIcon()
    {
        var status = AggregateStatus();
        return status is null ? null : UatStatusPresentation.Icon(status);
    }

    private string? AggregateStatus()
    {
        if (Children.Count == 0)
        {
            return null;
        }

        var statuses = EnumerateStatusNodes(this).ToArray();
        if (statuses.Length == 0)
        {
            return null;
        }

        if (statuses.Any(status => status == "fail"))
        {
            return "fail";
        }

        if (statuses.Any(status => status == "run"))
        {
            return "run";
        }

        if (statuses.Any(status => status == "cancel"))
        {
            return "cancel";
        }

        return statuses.All(status => status == "pass") ? "pass" : null;
    }

    private static IEnumerable<string> EnumerateStatusNodes(UatWorkspaceNodeViewModel node)
    {
        foreach (var child in node.Children)
        {
            if (child.Kind == UatWorkspaceNodeKind.Scenario && child.Scenario is not null)
            {
                yield return child.Scenario.Status;
            }

            if (child.Kind == UatWorkspaceNodeKind.Step && child.Step is not null)
            {
                yield return child.Step.Status;
            }

            foreach (var status in EnumerateStatusNodes(child))
            {
                yield return status;
            }
        }
    }

    private static string SanitizeAutomationId(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return builder.ToString().Trim('_');
    }
}
