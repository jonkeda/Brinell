using Brinell.Samples.Blazor.PlaywrightTests.PageObjects;
using Brinell.Samples.Blazor.PlaywrightTests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.PlaywrightTests.Tests;

/// <summary>
/// Tests for Select controls using Playwright.
/// </summary>
public class SelectTests : BlazorPlaywrightTestBase
{
    public SelectTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Select_InitialState_NoCountrySelected()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var selectedValue = await page.CountrySelect.GetSelectedValueAsync();

        // Assert
        Assert.Equal("", selectedValue);
    }

    [Fact]
    public async Task Select_SelectByValue_SelectsCorrectOption()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        await page.CountrySelect.SelectByValueAsync("uk");

        // Assert
        var selectedValue = await page.CountrySelect.GetSelectedValueAsync();
        Assert.Equal("uk", selectedValue);
    }

    [Fact]
    public async Task Select_SelectByText_SelectsCorrectOption()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        await page.CountrySelect.SelectByTextAsync("Germany");

        // Assert
        var selectedText = await page.CountrySelect.GetSelectedTextAsync();
        Assert.Equal("Germany", selectedText);
    }

    [Fact]
    public async Task Select_SelectByIndex_SelectsCorrectOption()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act - select index 1 (United States, since 0 is the placeholder)
        await page.CountrySelect.SelectByIndexAsync(1);

        // Assert
        var selectedValue = await page.CountrySelect.GetSelectedValueAsync();
        Assert.Equal("us", selectedValue);
    }

    [Fact]
    public async Task Select_GetItems_ReturnsAllOptions()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var items = await page.CountrySelect.GetItemsAsync();

        // Assert
        Assert.Equal(6, items.Count); // Placeholder + 5 countries
        Assert.Contains("United States", items);
        Assert.Contains("United Kingdom", items);
        Assert.Contains("Germany", items);
    }

    [Fact]
    public async Task Select_HasOption_ReturnsTrueForExistingOption()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var hasOption = await page.CountrySelect.HasOptionAsync("jp");

        // Assert
        Assert.True(hasOption, "Country select should have 'jp' option.");
    }

    [Fact]
    public async Task Select_HasOption_ReturnsFalseForNonExistingOption()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var hasOption = await page.CountrySelect.HasOptionAsync("xx");

        // Assert
        Assert.False(hasOption, "Country select should not have 'xx' option.");
    }

    [Fact]
    public async Task Select_StatusUpdates_WhenSelected()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        await page.CountrySelect.SelectByValueAsync("fr");

        // Wait for Blazor to update
        await Task.Delay(100);

        // Assert
        var status = await page.SelectStatus.GetTextAsync();
        Assert.Contains("fr", status);
    }
}
