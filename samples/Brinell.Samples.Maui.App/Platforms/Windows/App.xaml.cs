using Microsoft.UI.Xaml;

namespace Brinell.Samples.Maui.App.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        this.UnhandledException += OnUnhandledException;
        this.InitializeComponent();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {e.Exception}");
        Console.Error.WriteLine($"UNHANDLED EXCEPTION: {e.Exception}");
    }

    protected override MauiApp CreateMauiApp()
    {
        try
        {
            return MauiProgram.CreateMauiApp();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in CreateMauiApp: {ex}");
            Console.Error.WriteLine($"ERROR in CreateMauiApp: {ex}");
            throw;
        }
    }
}
