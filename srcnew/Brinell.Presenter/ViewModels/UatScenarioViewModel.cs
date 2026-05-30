using Brinell.Presenter.Models;

namespace Brinell.Presenter.ViewModels;

public sealed class UatScenarioViewModel : ViewModelBase
{
    private string _status = "wait";

    public UatScenarioViewModel(UatScenarioLoadResult source)
    {
        Name = source.Name;
        FilePath = source.FilePath;
        Tags = string.Join(", ", source.Tags);
        Steps = source.Steps.Select(step => new UatStepViewModel(step)).ToArray();
    }

    public string Name { get; }

    public string FilePath { get; }

    public string Tags { get; }

    public IReadOnlyList<UatStepViewModel> Steps { get; }

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

    public string StatusIcon => UatStatusPresentation.Icon(Status);

    public string StatusDescription => UatStatusPresentation.Description(Status);

    public Microsoft.Maui.Graphics.Color StatusColor => UatStatusPresentation.Color(Status);

    public string DisplayText => $"{StatusIcon}  {Name}";
}
