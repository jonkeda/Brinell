using AngleSharp.Dom;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;

namespace Brinell.Html.Bunit;

/// <summary>A Brinell locator as a query on the rendered DOM: CSS where it can be, a text filter where not.</summary>
internal static class BunitLocators
{
    /// <summary>
    /// The elements <paramref name="locator"/> finds, its parents first. <paramref name="select"/> runs a CSS
    /// query in the outermost scope: the rendered nodes, or one element's descendants.
    /// </summary>
    public static IReadOnlyList<IElement> FindAll(Func<string, IEnumerable<IElement>> select, Locator locator)
    {
        if (locator.Parent is not { } parent)
        {
            return Query(select, locator).ToList();
        }

        var found = new List<IElement>();
        foreach (var scope in FindAll(select, parent))
        {
            foreach (var element in Query(css => scope.QuerySelectorAll(css), locator))
            {
                if (!found.Contains(element))
                {
                    found.Add(element);
                }
            }
        }

        return found;
    }

    private static IEnumerable<IElement> Query(Func<string, IEnumerable<IElement>> select, Locator locator) => locator.Strategy switch
    {
        LocatorStrategy.Text => ByText(select("*"), locator.Value),
        LocatorStrategy.LinkText or LocatorStrategy.PartialLinkText => select("a").Where(link => Holds(link, locator.Value)),
        LocatorStrategy.XPath => throw new LocatorNotSupportedException(locator.Strategy, "bUnit", "Use a test id or CSS."),
        LocatorStrategy.AccessibilityId => throw new LocatorNotSupportedException(locator.Strategy, "bUnit", "The accessible name needs a browser; use a test id."),
        _ => select(ToCss(locator)),
    };

    private static string ToCss(Locator locator) => locator.Strategy switch
    {
        LocatorStrategy.Id => $"[id='{Escape(locator.Value)}']",
        LocatorStrategy.DataTestId => $"[data-testid='{Escape(locator.Value)}']",
        LocatorStrategy.DataAutomationId => $"[data-automation-id='{Escape(locator.Value)}']",
        LocatorStrategy.AutomationId => $"[data-automation-id='{Escape(locator.Value)}'], [id='{Escape(locator.Value)}']",
        LocatorStrategy.Name => $"[name='{Escape(locator.Value)}']",
        LocatorStrategy.ClassName => $"[class~='{Escape(locator.Value)}']",
        _ => locator.Value,
    };

    // As Playwright's text locator: the innermost elements whose text holds the value, any case.
    private static IEnumerable<IElement> ByText(IEnumerable<IElement> candidates, string text)
        => candidates.Where(element => Holds(element, text) && !element.Children.Any(child => Holds(child, text)));

    private static bool Holds(IElement element, string text)
        => Normalize(element.TextContent).Contains(Normalize(text), StringComparison.OrdinalIgnoreCase);

    internal static string Normalize(string? text)
        => string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'");
}
