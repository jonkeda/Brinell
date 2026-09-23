namespace Brinell.NativeAndroid;

/// <summary>
/// Android-specific locator helpers for common UiAutomator attributes.
/// </summary>
public static class NativeAndroidLocator
{
    public static Locator ByResourceId(string resourceId) => Locator.ById(resourceId);

    public static Locator ByContentDescription(string contentDescription) => Locator.ByAccessibilityId(contentDescription);

    public static Locator ByClass(string className) => Locator.ByClassName(className);

    public static Locator ByText(string text) => Locator.ByText(text);

    public static Locator ByTextContains(string text)
    {
        var literal = NativeAndroidByExtensions.ToXPathLiteral(text);
        return Locator.ByXPath($"//*[contains(@text, {literal}) or contains(@content-desc, {literal})]");
    }

    public static Locator ByTextOrDescription(string value)
    {
        var literal = NativeAndroidByExtensions.ToXPathLiteral(value);
        return Locator.ByXPath($"//*[@text={literal} or @content-desc={literal}]");
    }

    public static Locator ByResourceIdEndsWith(string resourceIdSuffix)
    {
        var literal = NativeAndroidByExtensions.ToXPathLiteral(resourceIdSuffix);
        return Locator.ByXPath($"//*[substring(@resource-id, string-length(@resource-id) - string-length({literal}) + 1) = {literal}]");
    }

    public static Locator ByAndroidXPath(string xpath) => Locator.ByXPath(xpath);
}
