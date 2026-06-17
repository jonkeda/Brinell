
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Brinell.Samples.Maui.App;

public partial class App
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // TabbedPage should not be wrapped in NavigationPage
        return new MainWindow();
    }
}
