namespace Brinell.Samples.Maui.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Wrap MainPage in NavigationPage to enable navigation to TabbedPageDemoPage
        return new Window(new NavigationPage(new MainPage()));
    }
}
