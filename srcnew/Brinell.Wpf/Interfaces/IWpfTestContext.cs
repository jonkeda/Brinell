namespace Brinell.Wpf.Interfaces;

/// <summary>
/// WPF test context interface with FlaUI driver access.
/// Combines test context capabilities with WPF element scope.
/// </summary>
public interface IWpfTestContext : ITestContext<IWpfElement>, IWpfElementScope
{
    /// <summary>
    /// Gets the WPF FlaUI driver for operations.
    /// </summary>
    IWpfDriver Driver { get; }

    /// <summary>
    /// Gets this context as the element scope.
    /// Implementation should return 'this'.
    /// </summary>
    new IWpfTestContext Context { get; }
}
