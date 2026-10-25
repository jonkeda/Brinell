using Bruinell.Html.Playwright;
using Microsoft.Playwright;

namespace Bruinell.Html.Tests;

/// <summary>How a Bruinell locator becomes a Playwright one: the accessible name, and quoting.</summary>
public class LocatorMappingTests
{
    private readonly Mock<ILocator> _root = new();

    [Fact]
    public void AccessibilityId_FindsByTheAccessibleName_Exactly()
    {
        LocatorExtensions.ToPlaywrightLocator(_root.Object, Locator.ByAccessibilityId("Close dialog"));

        _root.Verify(l => l.GetByLabel("Close dialog", It.Is<LocatorGetByLabelOptions?>(o => o != null && o.Exact == true)), Times.Once);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("it's", @"it\'s")]
    [InlineData(@"a\b", @"a\\b")]
    [InlineData(@"end\'", @"end\\\'")]
    public void EscapeForAttribute_EscapesBackslashesThenQuotes(string value, string escaped)
        => Assert.Equal(escaped, LocatorExtensions.EscapeForAttribute(value));

    [Fact]
    public void DataTestId_WithAQuoteAndABackslash_IsOneValidSelector()
    {
        LocatorExtensions.ToPlaywrightLocator(_root.Object, Locator.ByDataTestId(@"O'Brien\row"));

        _root.Verify(l => l.Locator(@"[data-testid='O\'Brien\\row']", It.IsAny<LocatorLocatorOptions?>()), Times.Once);
    }
}
