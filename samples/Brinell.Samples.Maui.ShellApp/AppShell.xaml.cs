namespace Brinell.Samples.Maui.ShellApp;

public partial class AppShell : Shell
{
    /// <summary>The route a detail page pushes, and the one the stack tests pop.</summary>
    public const string DetailSubRoute = "sub";

    public AppShell()
    {
        InitializeComponent();

        // Pushed rather than declared: a route registered here is not part of the tab or
        // flyout structure, which is what makes it a stack entry to pop.
        Routing.RegisterRoute(DetailSubRoute, typeof(Pages.DetailSubPage));
    }
}
