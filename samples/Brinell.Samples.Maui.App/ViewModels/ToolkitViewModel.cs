using System.Windows.Input;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the Toolkit tab - Expander, nested TabView, Popup demos.
/// </summary>
public class ToolkitViewModel : ViewModelBase
{
    private bool _expander1Expanded;
    private bool _expander2Expanded;
    private bool _expander3Expanded;
    private string _popupResult = "";
    private string _snackbarMessage = "";

    public ToolkitViewModel()
    {
        ShowPopupCommand = new RelayCommand(ShowPopup);
        ShowSnackbarCommand = new RelayCommand(ShowSnackbar);
        ToggleExpanderCommand = new RelayCommand<string>(ToggleExpander);
        ExpandAllCommand = new RelayCommand(ExpandAll);
        CollapseAllCommand = new RelayCommand(CollapseAll);
    }

    #region Expander States

    public bool Expander1Expanded
    {
        get => _expander1Expanded;
        set => SetProperty(ref _expander1Expanded, value);
    }

    public bool Expander2Expanded
    {
        get => _expander2Expanded;
        set => SetProperty(ref _expander2Expanded, value);
    }

    public bool Expander3Expanded
    {
        get => _expander3Expanded;
        set => SetProperty(ref _expander3Expanded, value);
    }

    public ICommand ToggleExpanderCommand { get; }
    public ICommand ExpandAllCommand { get; }
    public ICommand CollapseAllCommand { get; }

    private void ToggleExpander(string? expanderNumber)
    {
        switch (expanderNumber)
        {
            case "1":
                Expander1Expanded = !Expander1Expanded;
                break;
            case "2":
                Expander2Expanded = !Expander2Expanded;
                break;
            case "3":
                Expander3Expanded = !Expander3Expanded;
                break;
        }
    }

    private void ExpandAll()
    {
        Expander1Expanded = true;
        Expander2Expanded = true;
        Expander3Expanded = true;
    }

    private void CollapseAll()
    {
        Expander1Expanded = false;
        Expander2Expanded = false;
        Expander3Expanded = false;
    }

    #endregion

    #region Popup

    public string PopupResult
    {
        get => _popupResult;
        set => SetProperty(ref _popupResult, value);
    }

    public ICommand ShowPopupCommand { get; }

    private void ShowPopup()
    {
        // The actual popup display is handled by the View
        // This command signals that a popup should be shown
        PopupResult = "Popup triggered...";
    }

    public void SetPopupResult(string result)
    {
        PopupResult = $"Popup result: {result}";
    }

    #endregion

    #region Snackbar

    public string SnackbarMessage
    {
        get => _snackbarMessage;
        set => SetProperty(ref _snackbarMessage, value);
    }

    public ICommand ShowSnackbarCommand { get; }

    private void ShowSnackbar()
    {
        // The actual snackbar display is handled by the View
        SnackbarMessage = "Snackbar displayed!";
    }

    #endregion
}
