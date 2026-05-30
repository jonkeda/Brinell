using Brinell.Presenter.Services;
using Brinell.Presenter.ViewModels;
using Brinell.Presenter.Views;

namespace Brinell.Presenter;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var viewModel = new PresenterShellViewModel(
            new UatWorkspaceService(),
            new UatExecutionService(),
            new FolderPickerService());
        return new Window(new PresenterPage(viewModel))
        {
            Title = "Brinell Presenter"
        };
    }
}
