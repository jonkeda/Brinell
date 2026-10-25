using Bruinell.Samples.Todo.App.Pages;
using Bruinell.Samples.Todo.Core.Services;

namespace Bruinell.Samples.Todo.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pushed routes; Shell resolves the pages from the container, view models and all.
        Routing.RegisterRoute(TodoRoutes.Detail, typeof(TodoDetailPage));
        Routing.RegisterRoute(TodoRoutes.Edit, typeof(TodoEditPage));
        Routing.RegisterRoute(UserRoutes.List, typeof(UserListPage));
        Routing.RegisterRoute(UserRoutes.Edit, typeof(UserEditPage));
    }
}
