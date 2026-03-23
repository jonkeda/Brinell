using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Shared.Navigation;

/// <summary>
/// Navigation service interface for managing view transitions.
/// Platform-agnostic interface - implementations are platform-specific.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets or sets the container that hosts the current ViewModel.
    /// </summary>
    ICurrentViewModelContainer Container { get; set; }

    /// <summary>
    /// Gets the current navigation route name.
    /// </summary>
    string? CurrentRoute { get; }

    /// <summary>
    /// Gets whether back navigation is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navigate to a specific route by name.
    /// </summary>
    Task NavigateToAsync(string route);

    /// <summary>
    /// Navigate to a specific ViewModel directly.
    /// </summary>
    Task NavigateToAsync(ParentViewModel viewModel);

    /// <summary>
    /// Navigate back to the previous route.
    /// </summary>
    Task GoBackAsync();
}
