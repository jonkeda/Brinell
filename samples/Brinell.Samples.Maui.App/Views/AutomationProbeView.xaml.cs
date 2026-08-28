namespace Brinell.Samples.Maui.App.Views2.TestViews;

public partial class AutomationProbeView : ContentView
{
    public AutomationProbeView()
    {
        InitializeComponent();
    }

    private void OnGoToContainer(object? sender, EventArgs e) => Go("ContainerPage");

    private void OnGoToCollection(object? sender, EventArgs e) => Go("CollectionModulePage");

    private void OnGoToShapes(object? sender, EventArgs e) => Go("ShapesPage");

    private void OnGoToDialogs(object? sender, EventArgs e) => Go("DialogsPage");

    /// <summary>
    /// Navigates by Shell route. These pages have no tab of their own - see the module
    /// navigation comment in the XAML.
    /// </summary>
    private static void Go(string route) => Shell.Current?.GoToAsync(route);
}
