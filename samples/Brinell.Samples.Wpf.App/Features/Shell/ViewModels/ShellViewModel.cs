using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.Navigation;
using Brinell.Samples.Shared.ViewModels;
using Brinell.Samples.Wpf.App.Infrastructure.Navigation;

namespace Brinell.Samples.Wpf.App.Features.Shell.ViewModels;

/// <summary>
/// ViewModel for the main shell/window.
/// </summary>
public class ShellViewModel : ViewModelBase, ICurrentViewModelContainer
{
    private readonly INavigationService _navigationService;
    private ViewModelBase? _currentViewModel;
    private string _title = "Brinell Sample Application";

    public ShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _navigationService.Container = this;

        // Navigation commands
        NavigateHomeCommand = new RelayCommand(() => NavigateTo(NavigationRoutes.Home));
        NavigateLoginCommand = new RelayCommand(() => NavigateTo(NavigationRoutes.Login));
        NavigateFormsCommand = new RelayCommand(() => NavigateTo(NavigationRoutes.Forms));
        NavigateDataGridCommand = new RelayCommand(() => NavigateTo(NavigationRoutes.DataGrid));

        // Navigate to home by default
        _ = _navigationService.NavigateToAsync(NavigationRoutes.Home);
    }

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <inheritdoc/>
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    /// <summary>
    /// Gets the navigation service.
    /// </summary>
    public INavigationService NavigationService => _navigationService;

    /// <summary>
    /// Command to navigate to Home.
    /// </summary>
    public RelayCommand NavigateHomeCommand { get; }

    /// <summary>
    /// Command to navigate to Login.
    /// </summary>
    public RelayCommand NavigateLoginCommand { get; }

    /// <summary>
    /// Command to navigate to Forms.
    /// </summary>
    public RelayCommand NavigateFormsCommand { get; }

    /// <summary>
    /// Command to navigate to DataGrid.
    /// </summary>
    public RelayCommand NavigateDataGridCommand { get; }

    private void NavigateTo(string route)
    {
        _ = _navigationService.NavigateToAsync(route);
    }
}
