using Bruinet.Core.Exceptions;
using Bruinet.Core.Locators;
using Bruinet.Core.Scopes;

namespace Bruinet.Html.Interfaces;

// R9: Html owns this contract and does not derive from Core's scopes. See AD-010.

/// <summary>
/// Anything controls can be declared in: the test context, a page, a container, a frame.
/// </summary>
public interface IHtmlElementScope
{
    /// <summary>The test context the scope belongs to.</summary>
    IHtmlTestContext Context { get; }

    /// <summary>How a bare string locator is read.</summary>
    LocatorStrategy DefaultLocatorStrategy { get; }

    /// <summary>The page the scope is on, for naming; <c>null</c> for the context itself.</summary>
    IHtmlPage? Page { get; }

    /// <summary>
    /// One readiness probe: the scope answers for itself, after asking its parent (R4). One
    /// attempt; never waits.
    /// </summary>
    ScopeReadiness ProbeReadiness();

    /// <summary>Whether the scope is ready now.</summary>
    bool IsReady() => ProbeReadiness().IsReady;

    /// <summary>Waits until the scope is ready, as one call.</summary>
    bool WaitReady(int? timeoutMs = null);

    /// <summary>The first matching element in the scope, or <c>null</c>.</summary>
    IHtmlElement? TryFindElement(Locator locator);

    /// <summary>The first matching element in the scope.</summary>
    /// <exception cref="ElementNotFoundException">Nothing matches.</exception>
    IHtmlElement FindElement(Locator locator);

    /// <summary>Every matching element in the scope.</summary>
    IReadOnlyList<IHtmlElement> FindElements(Locator locator);
}
