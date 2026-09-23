namespace Brinell.NativeAndroid.Tests;

public sealed class NativeAndroidByExtensionsTests
{
    [Theory]
    [InlineData(LocatorStrategy.Id, "nl.bouw7:id/search", "By.Id")]
    [InlineData(LocatorStrategy.AutomationId, "nl.bouw7:id/search", "By.Id")]
    [InlineData(LocatorStrategy.AccessibilityId, "Navigate up", "ByAccessibilityId")]
    [InlineData(LocatorStrategy.ClassName, "android.widget.EditText", "By.ClassName")]
    [InlineData(LocatorStrategy.XPath, "//*[@text='Contacts']", "By.XPath")]
    [InlineData(LocatorStrategy.Text, "Contacts", "By.XPath")]
    [InlineData(LocatorStrategy.ControlType, "android.widget.Button", "By.ClassName")]
    public void ToAndroidBy_Maps_Supported_Locators(LocatorStrategy strategy, string value, string expectedPrefix)
    {
        var locator = new Locator(strategy, value);

        var by = locator.ToAndroidBy();

        var description = by.ToString();
        Assert.Contains(expectedPrefix, description, StringComparison.Ordinal);
        Assert.Contains(value, description, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(LocatorStrategy.Css)]
    [InlineData(LocatorStrategy.LinkText)]
    [InlineData(LocatorStrategy.PartialLinkText)]
    public void ToAndroidBy_Rejects_Web_Only_Locators(LocatorStrategy strategy)
    {
        var locator = new Locator(strategy, "value");

        var exception = Assert.Throws<LocatorNotSupportedException>(() => locator.ToAndroidBy());

        Assert.Equal(strategy, exception.Strategy);
        Assert.Equal("Native Android Appium", exception.DriverName);
    }

    [Fact]
    public void ToAndroidBy_Text_Locator_Matches_Text_Or_ContentDescription()
    {
        var by = Locator.ByText("Contacts").ToAndroidBy();

        var description = by.ToString();
        Assert.Contains("@text='Contacts'", description);
        Assert.Contains("@content-desc='Contacts'", description);
    }
}
