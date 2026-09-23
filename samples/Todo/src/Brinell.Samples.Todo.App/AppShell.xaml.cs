using Brinell.Samples.Todo.App.Pages;
using Brinell.Samples.Todo.Core.Services;

namespace Brinell.Samples.Todo.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pushed routes; Shell resolves the pages from the container, view models and all.
        Routing.RegisterRoute(TodoRoutes.Detail, typeof(TodoDetailPage));
        Routing.RegisterRoute(TodoRoutes.Edit, typeof(TodoEditPage));
    }
}
