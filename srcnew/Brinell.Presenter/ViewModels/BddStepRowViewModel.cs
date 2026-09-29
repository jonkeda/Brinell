using System.ComponentModel;
using Microsoft.Maui.Graphics;

namespace Brinell.Presenter.ViewModels;

/// <summary>
/// One row of the step list: a step, or a <c>## Step:</c> call over the steps it runs. A call is a
/// place of its own to pause at, as in the engine's stepping (AD-022): Step runs it whole, Into
/// goes inside without running anything.
/// </summary>
public sealed class BddStepRowViewModel : ViewModelBase
{
    private readonly IReadOnlyList<BddStepViewModel> _steps;
    private bool _isCurrent;
    private bool _hasBreakpoint;

    private BddStepRowViewModel(int index, IReadOnlyList<BddStepViewModel> steps, int firstStep, bool isCall, string text)
    {
        Index = index;
        _steps = steps;
        FirstStep = firstStep;
        IsCall = isCall;
        Text = text;
        foreach (var step in steps)
        {
            step.PropertyChanged += OnStepChanged;
        }
    }

    /// <summary>A step's row.</summary>
    /// <param name="index">The row's position in the list.</param>
    /// <param name="step">The step.</param>
    /// <param name="stepIndex">The step's position in the scenario's run.</param>
    public static BddStepRowViewModel ForStep(int index, BddStepViewModel step, int stepIndex)
        => new(index, [step], stepIndex, isCall: false, step.Text);

    /// <summary>A call's row, over the steps it runs.</summary>
    /// <param name="index">The row's position in the list.</param>
    /// <param name="callText">The call as written in the scenario.</param>
    /// <param name="steps">The steps it runs.</param>
    /// <param name="firstStep">The first of them, in the scenario's run.</param>
    public static BddStepRowViewModel ForCall(int index, string callText, IReadOnlyList<BddStepViewModel> steps, int firstStep)
        => new(index, steps, firstStep, isCall: true, callText);

    /// <summary>The row's position in the list.</summary>
    public int Index { get; }

    /// <summary>The first step the row runs, as its position in the scenario's run.</summary>
    public int FirstStep { get; }

    /// <summary>The position after the last step the row runs.</summary>
    public int EndStep => FirstStep + _steps.Count;

    /// <summary>Whether this is a <c>## Step:</c> call's row.</summary>
    public bool IsCall { get; }

    /// <summary>Whether this is a step inside a call.</summary>
    public bool IsInCall => !IsCall && _steps[0].CallText is not null;

    /// <summary>The step's or the call's text, in full: the tooltip.</summary>
    public string Text { get; }

    /// <summary>The text without the indent it carries under a call.</summary>
    public string Label => Text.TrimStart();

    /// <summary>A step inside a call is indented under it.</summary>
    public Thickness Indent => new(IsInCall ? 16 + 8 * ((Text.Length - Label.Length) / 2 - 1) : 0, 0, 0, 0);

    /// <summary>The line of the step, or of the call, in the scenario file.</summary>
    public int LineNumber => _steps[0].LineNumber;

    /// <summary>Whether a breakpoint can be set here: a line of the scenario file itself, not of a step file.</summary>
    public bool CanBreak => !IsInCall;

    public string AutomationId => $"StepRow_{Index}";

    /// <summary>A call's status is its steps': failed if one failed, passed when all passed.</summary>
    public string Status
    {
        get
        {
            if (!IsCall)
            {
                return _steps[0].Status;
            }

            var statuses = _steps.Select(step => step.Status).ToArray();
            return statuses.Contains("fail") ? "fail"
                : statuses.Contains("run") ? "run"
                : statuses.Contains("cancel") ? "cancel"
                : statuses.All(status => status == "pass") ? "pass"
                : statuses.All(status => status == "skip") ? "skip"
                : statuses.Any(status => status == "pass") ? "run"
                : "wait";
        }
    }

    public string StatusGlyph => BddStatusPresentation.Glyph(Status);

    public string StatusDescription => BddStatusPresentation.Description(Status);

    public Color StatusColor => BddStatusPresentation.Color(Status);

    /// <summary>What a failed step said; null otherwise.</summary>
    public string? Message => _steps.Select(step => step.Message).FirstOrDefault(message => message is not null);

    /// <summary>The full text and, after a failure, why: the row's tooltip.</summary>
    public string Tooltip => Message is { } message ? $"{Label}{Environment.NewLine}{message}" : Label;

    /// <summary>Whether the run is paused here: the next thing it does is this row.</summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }

    /// <summary>Whether a step breakpoint is set on this row's line.</summary>
    public bool HasBreakpoint
    {
        get => _hasBreakpoint;
        set
        {
            if (SetProperty(ref _hasBreakpoint, value))
            {
                OnPropertyChanged(nameof(BreakpointGlyph));
            }
        }
    }

    /// <summary>A dot when a breakpoint is set.</summary>
    public string BreakpointGlyph => HasBreakpoint ? "●" : string.Empty;

    /// <summary>The ASCII line for the step list's text mirror.</summary>
    public string DisplayText => $"{(IsCurrent ? ">" : " ")} {BddStatusPresentation.Icon(Status)}  {Text}";

    /// <summary>Re-reads <see cref="StatusColor"/> against the current theme.</summary>
    public void RefreshThemeColors() => OnPropertyChanged(nameof(StatusColor));

    private void OnStepChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BddStepViewModel.Status) or nameof(BddStepViewModel.Message))
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusGlyph));
            OnPropertyChanged(nameof(StatusDescription));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(Tooltip));
        }
    }
}
