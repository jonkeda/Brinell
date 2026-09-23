namespace Brinell.Samples.Maui.App;

public partial class NavigationDemoPage : ContentPage
{
    public NavigationDemoPage()
    {
        InitializeComponent();

        // Records like the others if it ever runs, and cannot run - so a test can tell "refused"
        // from "ran and had no visible effect".
        PageToolbarDelete.Command = new Command(() => RecordToolbar("Delete"), () => false);
    }

    /// <summary>
    /// Records a page-toolbar activation.
    /// </summary>
    /// <remarks>
    /// Neither item navigates, on purpose. The only other toolbar item in the sample is "Back",
    /// and a test of a verb that is only ever exercised against navigation cannot tell "the item
    /// was raised" from "the page happened to change".
    /// </remarks>
    private void OnToolbarRefresh(object? sender, EventArgs e) => RecordToolbar("Refresh");

    private void OnToolbarAbout(object? sender, EventArgs e) => RecordToolbar("About");

    private void RecordToolbar(string item)
        => (DemoView.BindingContext as ViewModels.NavigationDemoViewModel)?
            .ToolbarCommand.Execute($"PageToolbar/{item}");

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
