namespace Brinell.Wpf.Interfaces;

/// <summary>
/// Page interface extending scope. Pages are scopes that return themselves.
/// Pages are root scopes - they have no Parent property.
/// </summary>
/// <typeparam name="TSelf">The page type itself (self-referencing).</typeparam>
public interface IWpfPage<TSelf> : IWpfScope<TSelf>, IPageObject<IWpfElement>
    where TSelf : IWpfPage<TSelf>
{
    // Inherits:
    // - TSelf Self { get; }               from IWpfScope<TSelf>
    // - Element finding                   from IWpfElementScope
    // - IWpfTestContext Context { get; }   from IWpfElementScope
    // - Page operations                   from IPageObject
}
