namespace Brinell.Core.Interfaces;

/// <summary>
/// Represents an element scope that provides access to its owning page.
/// Used by controls to get both element-finding capability and page for fluent chaining.
/// </summary>
/// <typeparam name="TPage">The page type for fluent returns.</typeparam>
/// <typeparam name="TElement">The platform's native element type.</typeparam>
public interface IPagedScope<TPage, TElement> : IElementScope<TElement>
    where TPage : IPageObject
{
    /// <summary>
    /// Gets the page that owns this scope.
    /// For pages: returns 'this' (the page itself).
    /// For containers: returns the parent page, not the container.
    /// </summary>
    TPage Page { get; }
}
