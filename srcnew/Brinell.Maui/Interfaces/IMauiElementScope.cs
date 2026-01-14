using Brinell.Core.Interfaces;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific element scope that provides access to the test context.
/// Extends the generic element scope with IMauiElement as the element type.
/// </summary>
public interface IMauiElementScope : IElementScope<IMauiElement>
{
    /// <summary>
    /// Gets the MAUI test context for this scope.
    /// </summary>
    IMauiTestContext Context { get; }
}
