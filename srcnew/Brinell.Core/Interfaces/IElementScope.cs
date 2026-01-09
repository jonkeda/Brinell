using Brinell.Core.Locators;

namespace Brinell.Core.Interfaces;

/// <summary>
/// Non-generic element scope interface for polymorphic access.
/// </summary>
public interface IElementScope
{
    /// <summary>
    /// Default locator strategy for this scope.
    /// </summary>
    LocatorStrategy DefaultLocatorStrategy { get; }
}

/// <summary>
/// Generic element scope providing typed element finding.
/// TElement is the platform's native element type.
/// </summary>
public interface IElementScope<TElement> : IElementScope
{
    /// <summary>
    /// Try to find a single element. Returns null if not found.
    /// </summary>
    TElement? TryFindElement(Locator locator);
    
    /// <summary>
    /// Find a single element. Throws if not found.
    /// </summary>
    TElement FindElement(Locator locator);
    
    /// <summary>
    /// Find all matching elements.
    /// </summary>
    IReadOnlyList<TElement> FindElements(Locator locator);
}
