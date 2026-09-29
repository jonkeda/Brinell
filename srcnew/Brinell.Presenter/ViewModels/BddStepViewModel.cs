using Brinell.Presenter.Models;

namespace Brinell.Presenter.ViewModels;

public sealed class BddStepViewModel : ViewModelBase
{
    private string _status;
    private string? _message;

    public BddStepViewModel(BddStepLoadResult source)
    {
        _status = source.Status;
        Text = source.Text;
        CommandId = source.CommandId;
        LineNumber = source.LineNumber;
        CallText = source.CallText;
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

    /// <summary>What the step's run said: a failed step's message; null before it runs.</summary>
    public string? Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    /// <summary>The step's text; a callable phrase's step is indented under its call.</summary>
    public string Text { get; }

    public string CommandId { get; }

    /// <summary>Its line in the scenario file; for a callable phrase's step, the call's.</summary>
    public int LineNumber { get; }

    /// <summary>For a callable phrase's step, the call as written in the scenario; null otherwise.</summary>
    public string? CallText { get; }

    public string StatusIcon => BddStatusPresentation.Icon(Status);

    public string StatusDescription => BddStatusPresentation.Description(Status);

    public Microsoft.Maui.Graphics.Color StatusColor => BddStatusPresentation.Color(Status);

    public string DisplayText => $"{StatusIcon}  {Text}";
}
