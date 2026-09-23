using System.Windows.Input;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the dialogs module test view.
/// </summary>
/// <remarks>
/// The dialogs themselves are raised from the code-behind, because DisplayAlert and
/// DisplayPromptAsync are Page methods. This holds only the observable result, so tests
/// assert on a label rather than on the transient popup.
/// </remarks>
public class DialogsViewModel : ParentViewModel
{
    private const string NoResult = "none";

    private string _lastResult = NoResult;

    public DialogsViewModel()
    {
        ResetCommand = new RelayCommand(() => LastResult = NoResult);
    }

    /// <summary>The outcome of the most recent dialog, or "none".</summary>
    public string LastResult
    {
        get => _lastResult;
        private set => SetProperty(ref _lastResult, value);
    }

    /// <summary>Restores the initial state.</summary>
    public ICommand ResetCommand { get; }

    /// <summary>Records a dialog outcome. Called from the view's code-behind.</summary>
    public void Record(string result) => LastResult = result;
}
