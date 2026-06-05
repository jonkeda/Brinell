namespace Brinell.NativeAndroid.Tests;

public sealed class NativeAndroidLocatorTests
{
    [Fact]
    public void ByTextContains_Creates_Text_And_ContentDescription_XPath()
    {
        var locator = NativeAndroidLocator.ByTextContains("Contacts");

        Assert.Equal(LocatorStrategy.XPath, locator.Strategy);
        Assert.Contains("contains(@text, 'Contacts')", locator.Value);
        Assert.Contains("contains(@content-desc, 'Contacts')", locator.Value);
    }

    [Fact]
    public void ByTextOrDescription_Creates_Exact_Text_And_ContentDescription_XPath()
    {
        var locator = NativeAndroidLocator.ByTextOrDescription("Log in");

        Assert.Equal(LocatorStrategy.XPath, locator.Strategy);
        Assert.Contains("@text='Log in'", locator.Value);
        Assert.Contains("@content-desc='Log in'", locator.Value);
    }

    [Fact]
    public void ToXPathLiteral_Handles_Single_And_Double_Quotes()
    {
        var literal = NativeAndroidByExtensions.ToXPathLiteral("Bob's \"Contacts\"");

        Assert.Equal("concat('Bob', \"'\", 's \"Contacts\"')", literal);
    }

    [Fact]
    public void ByResourceIdEndsWith_Uses_ResourceId_Suffix_Match()
    {
        var locator = NativeAndroidLocator.ByResourceIdEndsWith("/contactName");

        Assert.Equal(LocatorStrategy.XPath, locator.Strategy);
        Assert.Contains("@resource-id", locator.Value);
        Assert.Contains("/contactName", locator.Value);
    }
}
