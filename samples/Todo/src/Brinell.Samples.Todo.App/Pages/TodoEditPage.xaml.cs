using Brinell.Samples.Todo.Core.Services;
using Brinell.Samples.Todo.Core.ViewModels;

namespace Brinell.Samples.Todo.App.Pages;

public partial class TodoEditPage : ContentPage, IQueryAttributable
{
    private readonly TodoEditViewModel _viewModel;

    public TodoEditPage(TodoEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        PageAutomation.Declare(this);
    }

    /// <summary>Loads the todo named by the route's <c>id</c>; without one the page is a new todo.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
        => _ = _viewModel.LoadAsync(TodoRoutes.ReadId(query));

    /// <summary>The hardware back button (Android) asks about unsaved changes too.</summary>
    protected override bool OnBackButtonPressed()
    {
        _viewModel.CancelCommand.Execute(null);
        return true;
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
