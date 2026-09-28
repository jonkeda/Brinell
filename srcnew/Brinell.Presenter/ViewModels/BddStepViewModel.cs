using Brinell.Presenter.Models;

namespace Brinell.Presenter.ViewModels;

public sealed class BddStepViewModel : ViewModelBase
{
    private string _status;

    public BddStepViewModel(BddStepLoadResult source)
    {
        _status = source.Status;
        Text = source.Text;
        CommandId = source.CommandId;
        LineNumber = source.LineNumber;
    }

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

    public string Text { get; }

    public string CommandId { get; }

    public int LineNumber { get; }

    public string StatusIcon => BddStatusPresentation.Icon(Status);

    public string StatusDescription => BddStatusPresentation.Description(Status);

    public Microsoft.Maui.Graphics.Color StatusColor => BddStatusPresentation.Color(Status);

    public string DisplayText => $"{StatusIcon}  {Text}";
}
