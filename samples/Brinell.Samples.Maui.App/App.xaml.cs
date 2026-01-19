namespace Brinell.Samples.Maui.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // TabbedPage should not be wrapped in NavigationPage
        return new Window(new MainPage());
    }
}
