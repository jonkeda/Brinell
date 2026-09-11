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

        // Also to a file when asked. Launched by a test run this app has no console and no
        // debugger, so Debug and stderr both go nowhere - and a crash then reaches the test as
        // an automation tree that simply stopped answering, which looks identical to several
        // completely different faults.
        var crashLog = Environment.GetEnvironmentVariable("BRINELL_APP_CRASH_LOG");
        if (!string.IsNullOrWhiteSpace(crashLog))
        {
            try
            {
                System.IO.File.AppendAllText(
                    crashLog,
                    $"{DateTime.Now:HH:mm:ss.fff} UNHANDLED: {e.Exception}{Environment.NewLine}");
            }
            catch (System.IO.IOException)
            {
                // A crash log that cannot be written must not make the crash worse.
            }
        }
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
