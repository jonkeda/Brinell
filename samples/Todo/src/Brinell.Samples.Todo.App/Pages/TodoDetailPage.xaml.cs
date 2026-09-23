using Brinell.Samples.Todo.Core.Services;
using Brinell.Samples.Todo.Core.ViewModels;

namespace Brinell.Samples.Todo.App.Pages;

public partial class TodoDetailPage : ContentPage, IQueryAttributable
{
    private readonly TodoDetailViewModel _viewModel;

    public TodoDetailPage(TodoDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        PageAutomation.Declare(this);
    }

    /// <summary>Loads the todo named by the route's <c>id</c>.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TodoRoutes.ReadId(query) is { } id)
        {
            _ = _viewModel.LoadAsync(id);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnViewAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnViewDisappearing();
    }
}
