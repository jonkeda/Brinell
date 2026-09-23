namespace Brinell.NativeAndroid;

internal static class NativeAndroidByExtensions
{
    public static By ToAndroidBy(this Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => By.Id(locator.Value),
            LocatorStrategy.Id => By.Id(locator.Value),
            LocatorStrategy.AccessibilityId => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.Name => By.Name(locator.Value),
            LocatorStrategy.ClassName => By.ClassName(locator.Value),
            LocatorStrategy.XPath => By.XPath(locator.Value),
            LocatorStrategy.Text => By.XPath(
                $"//*[@text={ToXPathLiteral(locator.Value)} or @content-desc={ToXPathLiteral(locator.Value)}]"),
            LocatorStrategy.TagName => By.ClassName(locator.Value),
            LocatorStrategy.DataTestId => By.XPath(
                $"//*[@resource-id={ToXPathLiteral(locator.Value)} or @content-desc={ToXPathLiteral(locator.Value)}]"),
            LocatorStrategy.DataAutomationId => By.XPath(
                $"//*[@resource-id={ToXPathLiteral(locator.Value)} or @content-desc={ToXPathLiteral(locator.Value)}]"),
            LocatorStrategy.ControlType => By.ClassName(locator.Value),
            _ => throw new LocatorNotSupportedException(
                locator.Strategy,
                "Native Android Appium",
                "Use resource-id, content-desc, text, class name, XPath, or Android UIAutomator helpers instead.")
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
}
