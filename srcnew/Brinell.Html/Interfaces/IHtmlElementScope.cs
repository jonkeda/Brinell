using Brinell.Core.Exceptions;
using Brinell.Core.Locators;

namespace Brinell.Html.Interfaces;

// R9: Html owns this contract and does not derive from Core's scopes. See .my/html/design.md 4.2.

/// <summary>
/// Anything controls can be declared in: the test context, a page, a container.
/// </summary>
public interface IHtmlElementScope
{
    /// <summary>The test context the scope belongs to.</summary>
    IHtmlTestContext Context { get; }

    /// <summary>How a bare string locator is read.</summary>
    LocatorStrategy DefaultLocatorStrategy { get; }

    /// <summary>The page the scope is on, for naming; <c>null</c> for the context itself.</summary>
    IHtmlPage? Page { get; }

    /// <summary>Whether the scope is ready for its controls.</summary>
    bool IsReady(int? timeoutMs = null);

    /// <summary>Waits until the scope is ready.</summary>
    bool WaitReady(int? timeoutMs = null);

    /// <summary>The first matching element in the scope, or <c>null</c>.</summary>
    IHtmlElement? TryFindElement(Locator locator);

    /// <summary>The first matching element in the scope.</summary>
    /// <exception cref="ElementNotFoundException">Nothing matches.</exception>
    IHtmlElement FindElement(Locator locator);

    /// <summary>Every matching element in the scope.</summary>
    IReadOnlyList<IHtmlElement> FindElements(Locator locator);
}
