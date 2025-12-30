using Brinell.Samples.Shared.Navigation;
using Brinell.Samples.Shared.ViewModels;
using Brinell.Samples.Wpf.App.Features.Home.ViewModels;
using Brinell.Samples.Wpf.App.Features.Login.ViewModels;

namespace Brinell.Samples.Wpf.App.Infrastructure.Navigation;

/// <summary>
/// WPF implementation of the navigation service.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly Stack<ViewModelBase> _navigationStack = new();
    private ICurrentViewModelContainer _container = null!;

    /// <inheritdoc/>
    public ICurrentViewModelContainer Container
    {
        get => _container;
        set => _container = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public string? CurrentRoute { get; private set; }

    /// <inheritdoc/>
    public bool CanGoBack => _navigationStack.Count > 1;

    /// <inheritdoc/>
    public Task NavigateToAsync(string route)
    {
        var viewModel = CreateViewModelForRoute(route);
        if (viewModel != null)
        {
            CurrentRoute = route;
            NavigateToViewModel(viewModel);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task NavigateToAsync(ViewModelBase viewModel)
    {
        NavigateToViewModel(viewModel);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task GoBackAsync()
    {
        if (!CanGoBack)
            return Task.CompletedTask;

        // Remove current
        var current = _navigationStack.Pop();
        current.OnViewDisappearing();

        // Show previous
        var previous = _navigationStack.Peek();
        previous.OnViewAppearing();
        Container.CurrentViewModel = previous;

        return Task.CompletedTask;
    }

    private void NavigateToViewModel(ViewModelBase viewModel)
    {
        // Notify current view it's disappearing
        if (_navigationStack.Count > 0)
        {
            var current = _navigationStack.Peek();
            current.OnViewDisappearing();
        }

        // Push new view model
        _navigationStack.Push(viewModel);
        viewModel.OnViewAppearing();
        Container.CurrentViewModel = viewModel;
    }

    private ViewModelBase? CreateViewModelForRoute(string route)
    {
        return route switch
        {
            NavigationRoutes.Home => new HomeViewModel(this),
            NavigationRoutes.Login => new LoginViewModel(this),
            NavigationRoutes.Forms => new HomeViewModel(this), // Placeholder
            NavigationRoutes.DataGrid => new HomeViewModel(this), // Placeholder
            _ => null
        };
    }
}
