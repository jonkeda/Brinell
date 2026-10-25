using System.Windows;
using Bruinell.Samples.Wpf.App.Features.Shell.ViewModels;
using Bruinell.Samples.Wpf.App.Infrastructure.Navigation;

namespace Bruinell.Samples.Wpf.App;

/// <summary>
/// Application entry point and service container.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the navigation service instance.
    /// </summary>
    public static NavigationService NavigationService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        NavigationService = new NavigationService();
    }
}
