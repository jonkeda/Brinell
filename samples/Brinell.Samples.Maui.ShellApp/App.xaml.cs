namespace Brinell.Samples.Maui.ShellApp;

public partial class App
{
    /// <summary>
    /// The native window title. Automation attaches to the app by this string, so it must
    /// differ from the other sample app's title or a driver could attach to the wrong window.
    /// </summary>
    public const string WindowTitle = "Brinell Shell Sample";

    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Title = WindowTitle;
        return window;
    }
}
