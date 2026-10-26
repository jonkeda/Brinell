using Bruinet.Presenter.Models;

namespace Bruinet.Presenter.ViewModels;

public sealed class BddScenarioViewModel : ViewModelBase
{
    private string _status = "wait";

    public BddScenarioViewModel(BddScenarioLoadResult source)
    {
        Name = source.Name;
        SuiteName = source.SuiteName;
        FilePath = source.FilePath;
        LineNumber = source.LineNumber;
        Tags = string.Join(", ", source.Tags);
        Steps = source.Steps.Select(step => new BddStepViewModel(step)).ToArray();
        Rows = BuildRows(Steps);
    }

    public string Name { get; }

    public string SuiteName { get; }

    public string FilePath { get; }

    /// <summary>The scenario's heading line in its file; 0 when not known.</summary>
    public int LineNumber { get; }

    public string Tags { get; }

    public IReadOnlyList<BddStepViewModel> Steps { get; }

    /// <summary>The step list's rows: each step, with a row for each <c>## Step:</c> call above its steps.</summary>
    public IReadOnlyList<BddStepRowViewModel> Rows { get; }

    public string Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusDescription));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    public string StatusIcon => BddStatusPresentation.Icon(Status);

    public string StatusDescription => BddStatusPresentation.Description(Status);

    public Microsoft.Maui.Graphics.Color StatusColor => BddStatusPresentation.Color(Status);

    public string DisplayText => $"{StatusIcon}  {Name}";

    // A call's steps share its text and line (the call's); a new call starts where either changes.
    private static IReadOnlyList<BddStepRowViewModel> BuildRows(IReadOnlyList<BddStepViewModel> steps)
    {
        List<BddStepRowViewModel> rows = [];
        for (var index = 0; index < steps.Count; index++)
        {
            if (steps[index].CallText is { } call && (index == 0 || !SameCall(steps[index - 1], steps[index])))
            {
                var end = index;
                while (end < steps.Count && SameCall(steps[end], steps[index]))
                {
                    end++;
                }

                rows.Add(BddStepRowViewModel.ForCall(rows.Count, call, [.. steps.Skip(index).Take(end - index)], index));
            }

            rows.Add(BddStepRowViewModel.ForStep(rows.Count, steps[index], index));
        }

        return rows;
    }

    private static bool SameCall(BddStepViewModel left, BddStepViewModel right)
        => left.CallText is not null && left.CallText == right.CallText && left.LineNumber == right.LineNumber;
}
