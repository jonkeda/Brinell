namespace Brinell.Samples.Todo.App.WinUI;

/// <summary>The WinUI entry point.</summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        UnhandledException += OnUnhandledException;
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // To a file when a test run asks: launched by a test the app has no console or debugger,
        // and a crash otherwise reaches the test only as an automation tree that stopped answering.
        var crashLog = Environment.GetEnvironmentVariable("BRINELL_APP_CRASH_LOG");
        if (string.IsNullOrWhiteSpace(crashLog))
        {
            return;
        }

        try
        {
            File.AppendAllText(crashLog, $"{DateTime.Now:HH:mm:ss.fff} UNHANDLED: {e.Exception}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // A crash log that cannot be written must not make the crash worse.
        }
    }
}
