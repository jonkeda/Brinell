using Brinell.Samples.Blazor.PlaywrightTests.PageObjects;
using Brinell.Samples.Blazor.PlaywrightTests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.PlaywrightTests.Tests;

/// <summary>
/// Tests for Link controls using Playwright.
/// </summary>
public class LinkTests : BlazorPlaywrightTestBase
{
    public LinkTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Link_GetHref_ReturnsCorrectUrl()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var href = await page.InternalLink.GetHrefAsync();

        // Assert
        Assert.Equal("/counter", href);
    }

    [Fact]
    public async Task Link_ExternalLink_HasCorrectTarget()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var target = await page.ExternalLink.GetTargetAsync();

        // Assert
        Assert.Equal("_blank", target);
    }

    [Fact]
    public async Task Link_ExternalLink_OpensInNewTab()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var opensInNewTab = await page.ExternalLink.OpensInNewTabAsync();

        // Assert
        Assert.True(opensInNewTab, "External link should open in new tab.");
    }

    [Fact]
    public async Task Link_InternalLink_DoesNotOpenInNewTab()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var opensInNewTab = await page.InternalLink.OpensInNewTabAsync();

        // Assert
        Assert.False(opensInNewTab, "Internal link should not open in new tab.");
    }

    [Fact]
    public async Task Link_GetText_ReturnsLinkText()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var text = await page.InternalLink.GetTextAsync();

        // Assert
        Assert.Equal("Go to Counter", text);
    }

    [Fact]
    public async Task Link_Click_NavigatesToPage()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        await page.InternalLink.ClickAsync();
        await WaitForBlazorReadyAsync();

        // Assert - we should now be on the counter page
        var counterPage = new CounterPage(Context);
        var isDisplayed = await counterPage.IsDisplayedAsync();
        Assert.True(isDisplayed, "Should navigate to counter page.");
    }

    [Fact]
    public async Task Link_IsVisible_ReturnsTrue()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var isVisible = await page.InternalLink.IsVisibleAsync();

        // Assert
        Assert.True(isVisible, "Internal link should be visible.");
    }

    [Fact]
    public async Task Link_DownloadLink_HasDownloadAttribute()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var href = await page.DownloadLink.GetHrefAsync();
        var text = await page.DownloadLink.GetTextAsync();

        // Assert
        Assert.Contains("sample.pdf", href ?? "");
        Assert.Equal("Download Sample PDF", text);
    }
}
