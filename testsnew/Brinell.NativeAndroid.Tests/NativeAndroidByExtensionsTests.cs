namespace Brinell.NativeAndroid.Tests;

public sealed class NativeAndroidByExtensionsTests
{
    [Theory]
    [InlineData(LocatorStrategy.AccessibilityId, "Navigate up", "ByAccessibilityId")]
    [InlineData(LocatorStrategy.ClassName, "android.widget.EditText", "By.ClassName")]
    [InlineData(LocatorStrategy.XPath, "//*[@text='Contacts']", "By.XPath")]
    [InlineData(LocatorStrategy.Text, "Contacts", "By.XPath")]
    [InlineData(LocatorStrategy.ControlType, "android.widget.Button", "By.ClassName")]
    public void ToAndroidBy_Maps_Supported_Locators(LocatorStrategy strategy, string value, string expectedPrefix)
    {
        var by = new Locator(strategy, value).ToAndroidBy();

        var description = by.ToString();
        Assert.Contains(expectedPrefix, description, StringComparison.Ordinal);
        Assert.Contains(value, description, StringComparison.Ordinal);
    }

    /// <summary>
    /// An id is UiAutomator2's own "id" strategy (the resource id), not Selenium's By.Id, which W3C
    /// sends as a CSS selector.
    /// </summary>
    [Theory]
    [InlineData(LocatorStrategy.Id)]
    [InlineData(LocatorStrategy.AutomationId)]
    public void ToAndroidBy_SendsAnId_AsTheResourceIdStrategy(LocatorStrategy strategy)
    {
        var by = new Locator(strategy, "nl.bouw7:id/search").ToAndroidBy();

        Assert.Equal("id", by.Mechanism);
        Assert.Equal("nl.bouw7:id/search", by.Criteria);
    }

    [Theory]
    [InlineData(LocatorStrategy.Css)]
    [InlineData(LocatorStrategy.LinkText)]
    [InlineData(LocatorStrategy.PartialLinkText)]
    public void ToAndroidBy_Rejects_Web_Only_Locators(LocatorStrategy strategy)
    {
        var exception = Assert.Throws<LocatorNotSupportedException>(() => new Locator(strategy, "value").ToAndroidBy());

        Assert.Equal(strategy, exception.Strategy);
        Assert.Equal("Native Android Appium", exception.DriverName);
    }

    [Fact]
    public void ToAndroidBy_Text_Locator_Matches_Text_Or_ContentDescription()
    {
        var description = Locator.ByText("Contacts").ToAndroidBy().ToString();

        Assert.Contains("@text='Contacts'", description);
        Assert.Contains("@content-desc='Contacts'", description);
    }

    /// <summary>
    /// UiAutomator2 roots a view's search document at the view: "." and "./X" are rewritten to
    /// "/*" and "/*/X"; "//X" already stays in the view's subtree.
    /// </summary>
    [Theory]
    [InlineData(".", "/*")]
    [InlineData("./*", "/*/*")]
    [InlineData("./android.widget.TextView[1]", "/*/android.widget.TextView[1]")]
    [InlineData(".//android.widget.TextView", ".//android.widget.TextView")]
    [InlineData("//android.widget.TextView", "//android.widget.TextView")]
    public void ToAndroidChildBy_ScopesAnXPathToTheView(string written, string sent)
        => Assert.Equal(sent, Locator.ByXPath(written).ToAndroidChildBy().Criteria);

    [Fact]
    public void ToAndroidChildBy_FindsTextInTheViewsSubtree()
        => Assert.StartsWith("//*[@content-desc=", Locator.ByText("Save").ToAndroidChildBy().Criteria, StringComparison.Ordinal);

    [Theory]
    [InlineData(LocatorStrategy.Id, "save", "new UiSelector().resourceIdMatches(\".*:id/save\")")]
    [InlineData(LocatorStrategy.Id, "com.app:id/save", "new UiSelector().resourceId(\"com.app:id/save\")")]
    [InlineData(LocatorStrategy.AccessibilityId, "Save \"draft\"", "new UiSelector().description(\"Save \\\"draft\\\"\")")]
    [InlineData(LocatorStrategy.Text, "Save", "new UiSelector().text(\"Save\")")]
    public void ToUiSelector_ExpressesTheLocatorForUiScrollable(LocatorStrategy strategy, string value, string expected)
        => Assert.Equal(expected, new Locator(strategy, value).ToUiSelector());

    [Fact]
    public void ToUiSelector_IsNull_ForAnXPath()
        => Assert.Null(Locator.ByXPath("//*").ToUiSelector());
}
