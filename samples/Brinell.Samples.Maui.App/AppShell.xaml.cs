namespace Brinell.Samples.Maui.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterModuleRoutes();
    }

    /// <summary>
    /// Registers routes for the module pages reached from <see cref="ModulesPage"/>.
    /// </summary>
    /// <remarks>
    /// These pages have no ShellContent of their own: only 9 Shell tabs are reachable on
    /// Windows, and a tenth pushes the rest into an overflow menu where automation cannot
    /// click them. Registering them as routes keeps them navigable without consuming a tab.
    /// </remarks>
    private static void RegisterModuleRoutes()
    {
        Routing.RegisterRoute(nameof(ContainerPage), typeof(ContainerPage));
        Routing.RegisterRoute(nameof(CollectionModulePage), typeof(CollectionModulePage));
        Routing.RegisterRoute(nameof(ShapesPage), typeof(ShapesPage));
        Routing.RegisterRoute(nameof(DialogsPage), typeof(DialogsPage));
    }
}
