namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI test context interface with Appium driver access.
/// Combines test context capabilities with MAUI element scope.
/// </summary>
public interface IMauiTestContext : ITestContext<IMauiElement>, IMauiElementScope
{
    /// <summary>
    /// Gets the wrapped Appium driver for operations.
    /// </summary>
    IMauiDriver Driver { get; }
    
    /// <summary>
    /// Gets this context as the element scope.
    /// Implementation should return 'this'.
    /// </summary>
    new IMauiTestContext Context { get; }
}
