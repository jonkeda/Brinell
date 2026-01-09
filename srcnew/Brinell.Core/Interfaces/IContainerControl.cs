using Brinell.Core.Locators;

namespace Brinell.Core.Interfaces;

/// <summary>
/// Non-generic container interface.
/// Containers are controls that scope element searches to their bounds.
/// </summary>
public interface IContainerControl : IControlObject, IElementScope
{
    /// <summary>
    /// Root element of this container for scoped searches.
    /// </summary>
    object ContainerRoot { get; }
}

/// <summary>
/// Generic container interface with typed element finding.
/// TElement is the platform's native element type.
/// </summary>
public interface IContainerControl<TElement> : IContainerControl, IElementScope<TElement>
{
    /// <summary>
    /// Typed root element for scoped searches.
    /// </summary>
    new TElement ContainerRoot { get; }
    
    // Inherits from IElementScope<TElement>:
    // TElement? TryFindElement(Locator locator);
    // TElement FindElement(Locator locator);
    // IReadOnlyList<TElement> FindElements(Locator locator);
}
