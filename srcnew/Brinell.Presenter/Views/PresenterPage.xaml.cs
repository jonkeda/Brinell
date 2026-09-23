using Brinell.Presenter.ViewModels;

namespace Brinell.Presenter.Views;

public partial class PresenterPage : ContentPage
{
    public PresenterPage(PresenterShellViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
