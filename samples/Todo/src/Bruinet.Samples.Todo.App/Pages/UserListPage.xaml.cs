using Bruinet.Samples.Todo.Core.ViewModels;

namespace Bruinet.Samples.Todo.App.Pages;

public partial class UserListPage : ContentPage
{
    private readonly UserListViewModel _viewModel;

    public UserListPage(UserListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        PageAutomation.Declare(this);
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
