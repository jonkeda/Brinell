using Bruinell.Samples.Todo.Core.Services;
using Bruinell.Samples.Todo.Core.ViewModels;

namespace Bruinell.Samples.Todo.App.Pages;

public partial class UserEditPage : ContentPage, IQueryAttributable
{
    private readonly UserEditViewModel _viewModel;

    public UserEditPage(UserEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        PageAutomation.Declare(this);
    }

    /// <summary>Loads the user named by the route's <c>id</c>; without one the page is a new user.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
        => _ = _viewModel.LoadAsync(TodoRoutes.ReadId(query));

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
