using Brinell.Samples.Shared.Navigation;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Wpf.App.Features.Home.ViewModels;

/// <summary>
/// ViewModel for the Home/Dashboard page.
/// </summary>
public class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private string _welcomeMessage = "Welcome to Brinell Sample Application";
    private string _description = "This sample application demonstrates the Brinell UI Testing Framework capabilities for WPF applications.";

    public HomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    /// <summary>
    /// Gets or sets the welcome message.
    /// </summary>
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    /// <summary>
    /// Gets or sets the description text.
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
}
