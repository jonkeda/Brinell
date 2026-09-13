namespace Brinell.Samples.Maui.App;

public partial class NavigationDemoPage : ContentPage
{
    public NavigationDemoPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Records a page-menu activation, the same way every other surface in the demo does.
    /// </summary>
    /// <remarks>
    /// These two items did nothing at all before, because nothing could reach them: the
    /// navigation probe reports that MenuBarItem and MenuFlyoutItem are addressable by neither
    /// AutomationId nor name on Windows. A handler on an unreachable control is untestable, so
    /// there was no reason to write one. The InvokeMenuItem verb is what makes it worth having.
    /// </remarks>
    private void OnMenuNew(object? sender, EventArgs e) => Record("File/New");

    private void OnMenuExit(object? sender, EventArgs e) => Record("File/Exit");

    private void Record(string action)
        => (DemoView.BindingContext as ViewModels.NavigationDemoViewModel)?
            .MenuCommand.Execute(action);
}
