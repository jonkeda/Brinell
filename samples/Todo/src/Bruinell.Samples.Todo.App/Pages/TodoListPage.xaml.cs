using Bruinell.Samples.Todo.Core.ViewModels;
using Bruinell.Samples.Todo.Infrastructure;

namespace Bruinell.Samples.Todo.App.Pages;

public partial class TodoListPage : ContentPage
{
    private readonly TodoListViewModel _viewModel;

    public TodoListPage(TodoListViewModel viewModel, TodoTestReset testReset)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        PageAutomation.Declare(this);

        // Here, not at startup: the launch settings a test run passes exist once the first page does.
        testReset.Start();
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
