using Brinell.Core.Interfaces;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// Page interface extending scope. Pages are scopes that return themselves.
/// Pages are root scopes - they have no Parent property.
/// </summary>
/// <typeparam name="TSelf">The page type itself (self-referencing).</typeparam>
public interface IMauiPage<TSelf> : IMauiScope<TSelf>, IPageObject<IMauiElement>
    where TSelf : IMauiPage<TSelf>
{
    // Inherits:
    // - TSelf Self { get; }                from IMauiScope<TSelf>
    // - Element finding                    from IMauiElementScope
    // - IMauiTestContext Context { get; }  from IMauiElementScope
    // - Page operations                    from IPageObject
    // NO Parent property - pages are the root of the hierarchy
}
