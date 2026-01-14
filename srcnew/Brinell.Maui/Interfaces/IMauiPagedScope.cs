using Brinell.Core.Interfaces;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific paged scope combining element scope, page access, and test context.
/// Provides controls with element finding, page for fluent chaining, and test context access.
/// </summary>
/// <typeparam name="TPage">The page type for fluent returns.</typeparam>
public interface IMauiPagedScope<TPage> : IPagedScope<TPage, IMauiElement>, IMauiElementScope
    where TPage : IPageObject
{
    // Inherits:
    // - TPage Page { get; }                     from IPagedScope
    // - TryFindElement, FindElement, etc.       from IElementScope<IMauiElement>
    // - IMauiTestContext Context { get; }       from IMauiElementScope
    // - LocatorStrategy DefaultLocatorStrategy  from IElementScope
}
