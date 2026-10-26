using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Bruinet.NativeAndroid.Appium;

/// <summary>How a Bruinet locator becomes a UiAutomator2 selector.</summary>
internal static class NativeAndroidByExtensions
{
    /// <summary>The selector for <paramref name="locator"/>, searching the whole screen.</summary>
    /// <exception cref="LocatorNotSupportedException">A web-only strategy (CSS, link text).</exception>
    public static By ToAndroidBy(this Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return locator.Strategy switch
        {
            // Selenium's By.Id is a CSS selector under W3C; UiAutomator2's own "id" strategy is the
            // resource id, with or without the package prefix (MAUI found the same, LocatorExtensions).
            LocatorStrategy.AutomationId or LocatorStrategy.Id => new ResourceIdBy(locator.Value),
            LocatorStrategy.AccessibilityId => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.Name or LocatorStrategy.Text => By.XPath(TextOrDescriptionPath(locator.Value)),
            LocatorStrategy.ClassName or LocatorStrategy.TagName or LocatorStrategy.ControlType => By.ClassName(locator.Value),
            LocatorStrategy.XPath => By.XPath(locator.Value),
            LocatorStrategy.DataTestId or LocatorStrategy.DataAutomationId => By.XPath(
                $"//*[@resource-id={ToXPathLiteral(locator.Value)} or @content-desc={ToXPathLiteral(locator.Value)}]"),
            _ => throw new LocatorNotSupportedException(
                locator.Strategy,
                "Native Android Appium",
                "Use resource-id, content-desc, text, class name, XPath, or Android UIAutomator helpers instead.")
        };
    }

    /// <summary>
    /// The selector for a search under a view. UiAutomator2 evaluates it in a document whose root
    /// element is the view itself, from the document node (measured on the emulator, plan step 2.1):
    /// <c>//X</c> stays in the view's subtree, but <c>./*</c> is the view itself and <c>.</c> is
    /// nothing. So the view is <c>/*</c> and its children are <c>/*/*</c>.
    /// </summary>
    public static By ToAndroidChildBy(this Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return locator.Strategy switch
        {
            LocatorStrategy.Name or LocatorStrategy.Text => By.XPath(TextOrDescriptionPath(locator.Value)),
            LocatorStrategy.XPath => By.XPath(ScopedXPath(locator.Value)),
            _ => locator.ToAndroidBy(),
        };
    }

    // "." is the view itself and "./X" a child of it, as a page object author writes them.
    private static string ScopedXPath(string xpath)
        => xpath == "." ? "/*"
            : xpath.StartsWith("./", StringComparison.Ordinal) && !xpath.StartsWith(".//", StringComparison.Ordinal) ? "/*" + xpath[1..]
            : xpath;

    /// <summary>
    /// The locator as a UiAutomator <c>UiSelector</c> expression, for <c>UiScrollable.scrollIntoView</c>;
    /// null for a locator a UiSelector cannot express (an XPath).
    /// </summary>
    public static string? ToUiSelector(this Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var value = JavaString(locator.Value);
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId or LocatorStrategy.Id => locator.Value.Contains(":id/", StringComparison.Ordinal)
                ? $"new UiSelector().resourceId({value})"
                : $"new UiSelector().resourceIdMatches({JavaString(".*:id/" + System.Text.RegularExpressions.Regex.Escape(locator.Value))})",
            LocatorStrategy.AccessibilityId => $"new UiSelector().description({value})",
            LocatorStrategy.Name or LocatorStrategy.Text => $"new UiSelector().text({value})",
            LocatorStrategy.ClassName or LocatorStrategy.TagName or LocatorStrategy.ControlType => $"new UiSelector().className({value})",
            _ => null,
        };
    }

    public static string ToXPathLiteral(string value)
    {
        if (!value.Contains('\''))
        {
            return $"'{value}'";
        }

        if (!value.Contains('"'))
        {
            return $"\"{value}\"";
        }

        var parts = value.Split('\'');
        return "concat(" + string.Join(", \"'\", ", parts.Select(part => $"'{part}'")) + ")";
    }

    // A view's accessible name: its content description when it has one, its text otherwise.
    private static string TextOrDescriptionPath(string value)
    {
        var literal = ToXPathLiteral(value);
        return $"//*[@content-desc={literal} or @text={literal}]";
    }

    private static string JavaString(string value)
        => "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private sealed class ResourceIdBy(string id) : By("id", id);
}
