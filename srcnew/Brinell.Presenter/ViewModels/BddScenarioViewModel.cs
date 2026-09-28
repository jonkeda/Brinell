using Brinell.Presenter.Models;

namespace Brinell.Presenter.ViewModels;

public sealed class BddScenarioViewModel : ViewModelBase
{
    private string _status = "wait";

    public BddScenarioViewModel(BddScenarioLoadResult source)
    {
        Name = source.Name;
        SuiteName = source.SuiteName;
        FilePath = source.FilePath;
        Tags = string.Join(", ", source.Tags);
        Steps = source.Steps.Select(step => new BddStepViewModel(step)).ToArray();
    }

    public string Name { get; }

    public string SuiteName { get; }

    public string FilePath { get; }

    public string Tags { get; }

    public IReadOnlyList<BddStepViewModel> Steps { get; }

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
}
