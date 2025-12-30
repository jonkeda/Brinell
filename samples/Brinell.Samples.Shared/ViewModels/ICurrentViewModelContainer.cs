namespace Brinell.Samples.Shared.ViewModels;

/// <summary>
/// Interface for ViewModels that can host the current navigation content.
/// </summary>
public interface ICurrentViewModelContainer
{
    /// <summary>
    /// Gets or sets the current ViewModel being displayed.
    /// </summary>
    ViewModelBase? CurrentViewModel { get; set; }
}
