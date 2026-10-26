using System.Windows.Input;
using Bruinet.Presenter.Commands;
using Microsoft.Maui.Graphics;

namespace Bruinet.Presenter.ViewModels;

/// <summary>What a row of the Narrator tree is.</summary>
public enum NarratorNodeKind
{
    /// <summary>The video script.</summary>
    Script,

    /// <summary>A scene: one scenario.</summary>
    Scene,

    /// <summary>What is said before a scene's first step, or after its last.</summary>
    Line,

    /// <summary>A step of the scene's scenario, with what is said before it.</summary>
    Step,
}

/// <summary>A row of the Narrator tree: the script, a scene, a spoken line or a step.</summary>
public sealed class NarratorNodeViewModel : ViewModelBase
{
    private readonly Action<NarratorNodeViewModel>? _checkedChanged;
    private bool _isExpanded;
    private bool _isChecked;
    private bool _isCheckEnabled = true;
    private string _status = "wait";
    private string _detail = string.Empty;
    private string? _video;

    /// <summary>Creates a row.</summary>
    /// <param name="kind">What the row is.</param>
    /// <param name="name">Its text.</param>
    /// <param name="key">Its place, for its automation ids: <c>1</c> for scene 1, <c>1_3</c> for its third row.</param>
    /// <param name="checkedChanged">Called when its checkbox changes; null for a row without one.</param>
    public NarratorNodeViewModel(NarratorNodeKind kind, string name, string key, Action<NarratorNodeViewModel>? checkedChanged = null)
    {
        Kind = kind;
        Name = name;
        _checkedChanged = checkedChanged;
        AutomationId = $"Narrator{kind}_{key}";
        ToggleAutomationId = $"Narrator{kind}Toggle_{key}";
        CheckAutomationId = $"Narrator{kind}Check_{key}";
        ToggleExpansionCommand = new RelayCommand(() => IsExpanded = !IsExpanded, () => CanExpand);
    }

    /// <summary>What the row is.</summary>
    public NarratorNodeKind Kind { get; }

    /// <summary>The row's text: the script's title, the scene's number and name, the step or the line.</summary>
    public string Name { get; }

    /// <summary>Under a step: what is said before it, or that it runs silently. Under a line: intro or outro.</summary>
    public string Note { get; init; } = string.Empty;

    /// <summary>Whether <see cref="Note"/> has anything to show.</summary>
    public bool HasNote => Note.Length > 0;

    /// <summary>What is wrong with the row in the script (a <c>VID0xx</c> diagnostic); empty when nothing is.</summary>
    public string Problem { get; init; } = string.Empty;

    /// <summary>Whether <see cref="Problem"/> has anything to show.</summary>
    public bool HasProblem => Problem.Length > 0;

    /// <summary>Beside the name: a scene's scenario and state, the script's scene count.</summary>
    public string Detail
    {
        get => _detail;
        set
        {
            if (SetProperty(ref _detail, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>A scene's own recording, to watch before any stitch; null when it has none.</summary>
    public string? Video
    {
        get => _video;
        set
        {
            if (SetProperty(ref _video, value))
            {
                OnPropertyChanged(nameof(CanWatch));
            }
        }
    }

    /// <summary>Whether the row has a recording to watch.</summary>
    public bool CanWatch => Video is not null;

    /// <summary>Opens the scene's recording.</summary>
    public ICommand? WatchCommand { get; init; }

    /// <summary>The id of the row's watch button.</summary>
    public string WatchAutomationId => AutomationId.Replace(Kind.ToString() + "_", Kind + "Watch_", StringComparison.Ordinal);

    /// <summary>The row's id for UI Automation.</summary>
    public string AutomationId { get; }

    /// <summary>The id of the row's expand button.</summary>
    public string ToggleAutomationId { get; }

    /// <summary>The id of the row's checkbox.</summary>
    public string CheckAutomationId { get; }

    /// <summary>The rows under this one.</summary>
    public List<NarratorNodeViewModel> Children { get; } = [];

    /// <summary>Whether the row has rows under it.</summary>
    public bool CanExpand => Children.Count > 0;

    /// <summary>Expands or collapses the row.</summary>
    public ICommand ToggleExpansionCommand { get; }

    /// <summary>Whether the rows under this one show.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(ExpansionGlyph));
                OnPropertyChanged(nameof(ExpansionDescription));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>The chevron on the expand button.</summary>
    public string ExpansionGlyph => CanExpand ? (IsExpanded ? PresenterIcons.ChevronDown : PresenterIcons.ChevronRight) : string.Empty;

    /// <summary>The expand button's accessible name: the state, which is all a UI Automation test can read of it.</summary>
    public string ExpansionDescription => CanExpand ? (IsExpanded ? "Expanded" : "Collapsed") : "Leaf";

    /// <summary>Whether the row has a checkbox: the script (all scenes) and each scene.</summary>
    public bool HasCheckBox => Kind is NarratorNodeKind.Script or NarratorNodeKind.Scene;

    /// <summary>Whether the scene is run and recorded; on the script, whether every scene is.</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value))
            {
                OnPropertyChanged(nameof(DisplayText));
                _checkedChanged?.Invoke(this);
            }
        }
    }

    /// <summary>Whether the checkbox can be changed: not for a scene the script has wrong.</summary>
    public bool IsCheckEnabled
    {
        get => _isCheckEnabled;
        set => SetProperty(ref _isCheckEnabled, value);
    }

    /// <summary>Sets the checkbox without telling the owner: the owner's own update.</summary>
    /// <param name="value">The new state.</param>
    internal void SetChecked(bool value)
    {
        if (SetProperty(ref _isChecked, value, nameof(IsChecked)))
        {
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>The row's run status: <c>wait</c>, <c>run</c>, <c>pass</c>, <c>fail</c>, <c>skip</c> or <c>cancel</c>.</summary>
    public string Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IconGlyph));
                OnPropertyChanged(nameof(IconColor));
                OnPropertyChanged(nameof(StatusDescription));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>The row's icon: its status, or a speech mark for a spoken line.</summary>
    public string IconGlyph => Kind == NarratorNodeKind.Line ? PresenterIcons.Say : BddStatusPresentation.Glyph(Status);

    /// <summary>The icon's colour.</summary>
    public Color IconColor => BddStatusPresentation.Color(Kind == NarratorNodeKind.Line ? "wait" : Status);

    /// <summary>The icon's accessible name.</summary>
    public string StatusDescription => Kind == NarratorNodeKind.Line ? "Said" : BddStatusPresentation.Description(Status);

    /// <summary>The row in plain text, for the tree's hidden mirror: no icon font.</summary>
    public string DisplayText
    {
        get
        {
            var expansion = CanExpand ? (IsExpanded ? "v " : "> ") : string.Empty;
            var check = HasCheckBox ? (IsChecked ? "[x] " : "[ ] ") : string.Empty;
            var icon = Kind == NarratorNodeKind.Line ? "\"" : BddStatusPresentation.Icon(Status);
            var detail = Detail.Length > 0 ? $"  ({Detail})" : string.Empty;
            var note = Note.Length > 0 ? $"  - {Note}" : string.Empty;
            var problem = Problem.Length > 0 ? $"  ! {Problem}" : string.Empty;
            return $"{expansion}{check}{icon} {Name}{detail}{note}{problem}";
        }
    }
}
