namespace Brinell.Wpf.Interfaces;

/// <summary>
/// WPF-specific element scope that provides access to the test context.
/// Extends the generic element scope with IWpfElement as the element type.
/// </summary>
public interface IWpfElementScope : IElementScope<IWpfElement>
{
    /// <summary>
    /// Gets the WPF test context for this scope.
    /// </summary>
    IWpfTestContext Context { get; }
}
