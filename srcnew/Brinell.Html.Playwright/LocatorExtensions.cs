using Brinell.Core.Locators;
using Microsoft.Playwright;

namespace Brinell.Html.Playwright;

internal static class LocatorExtensions
{
    public static ILocator ToPlaywrightLocator(PlaywrightTestContext context, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(locator);

        var root = Scope(context.InternalPage, context.InternalFrame, "*");

        if (locator.Parent != null)
        {
            root = ToPlaywrightLocator(root, locator.Parent);
        }

        return ToPlaywrightLocator(root, locator);
    }

    public static ILocator ToPlaywrightLocator(ILocator parent, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(locator);

        var baseLocator = locator.Parent != null
            ? ToPlaywrightLocator(parent, locator.Parent)
            : parent;

        return locator.Strategy switch
        {
            LocatorStrategy.Css => baseLocator.Locator(locator.Value),
            LocatorStrategy.Id => baseLocator.Locator($"#{EscapeCss(locator.Value)}"),
            LocatorStrategy.DataTestId => baseLocator.Locator($"[data-testid='{EscapeForAttribute(locator.Value)}']"),
            LocatorStrategy.DataAutomationId => baseLocator.Locator($"[data-automation-id='{EscapeForAttribute(locator.Value)}']"),
            LocatorStrategy.AutomationId => baseLocator.Locator($"[data-automation-id='{EscapeForAttribute(locator.Value)}'], [id='{EscapeForAttribute(locator.Value)}']"),
            LocatorStrategy.Name => baseLocator.Locator($"[name='{EscapeForAttribute(locator.Value)}']"),
            LocatorStrategy.ClassName => baseLocator.Locator($".{EscapeCss(locator.Value)}"),
            LocatorStrategy.TagName => baseLocator.Locator(locator.Value),
            LocatorStrategy.XPath => baseLocator.Locator($"xpath={locator.Value}"),
            LocatorStrategy.Text => baseLocator.GetByText(locator.Value),
            LocatorStrategy.LinkText => baseLocator.Locator($"a:has-text('{EscapeForSelectorText(locator.Value)}')"),
            LocatorStrategy.PartialLinkText => baseLocator.Locator($"a:has-text('{EscapeForSelectorText(locator.Value)}')"),
            LocatorStrategy.AccessibilityId => baseLocator.GetByRole(AriaRole.Generic, new LocatorGetByRoleOptions { Name = locator.Value }),
            LocatorStrategy.ControlType => baseLocator.Locator(locator.Value),
            _ => baseLocator.Locator(locator.Value)
        };
    }

    private static ILocator Scope(IPage page, IFrame? frame, string selector)
    {
        return frame != null ? frame.Locator(selector) : page.Locator(selector);
    }

    private static string EscapeForAttribute(string value) => value.Replace("'", "\\'");

    private static string EscapeForSelectorText(string value) => value.Replace("'", "\\'");

    private static string EscapeCss(string value)
    {
        return value.Replace("\\", "\\\\").Replace(".", "\\.").Replace("#", "\\#").Replace(":", "\\:");
    }
}
