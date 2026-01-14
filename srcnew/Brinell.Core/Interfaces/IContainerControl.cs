namespace Brinell.Core.Interfaces;

/// <summary>
/// Generic container interface with typed element finding.
/// TElement is the platform's native element type.
/// </summary>
public interface IContainerControl<TElement> : IElementScope<TElement>
{
    /// <summary>
    /// Typed root element for scoped searches.
    /// </summary>
    TElement ContainerRoot { get; }
    
    // Inherits from IElementScope<TElement>:
    // TElement? TryFindElement(Locator locator);
    // TElement FindElement(Locator locator);
    // IReadOnlyList<TElement> FindElements(Locator locator);
}
