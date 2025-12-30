using Brinell.Samples.Blazor.PlaywrightTests.PageObjects;
using Brinell.Samples.Blazor.PlaywrightTests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.PlaywrightTests.Tests;

/// <summary>
/// Tests for CheckBox controls using Playwright.
/// </summary>
public class CheckBoxTests : BlazorPlaywrightTestBase
{
    public CheckBoxTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task CheckBox_InitialState_TermsUnchecked()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var isChecked = await page.TermsCheckbox.IsCheckedAsync();

        // Assert
        Assert.False(isChecked, "Terms checkbox should be unchecked initially.");
    }

    [Fact]
    public async Task CheckBox_InitialState_NewsletterChecked()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var isChecked = await page.NewsletterCheckbox.IsCheckedAsync();

        // Assert
        Assert.True(isChecked, "Newsletter checkbox should be checked initially.");
    }

    [Fact]
    public async Task CheckBox_Check_SetsCheckedState()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        await page.TermsCheckbox.CheckAsync();

        // Assert
        var isChecked = await page.TermsCheckbox.IsCheckedAsync();
        Assert.True(isChecked, "Terms checkbox should be checked after calling Check().");
    }

    [Fact]
    public async Task CheckBox_Uncheck_ClearsCheckedState()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        await page.NewsletterCheckbox.UncheckAsync();

        // Assert
        var isChecked = await page.NewsletterCheckbox.IsCheckedAsync();
        Assert.False(isChecked, "Newsletter checkbox should be unchecked after calling Uncheck().");
    }

    [Fact]
    public async Task CheckBox_Toggle_FlipsState()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Get initial state
        var initialState = await page.TermsCheckbox.IsCheckedAsync();

        // Act
        await page.TermsCheckbox.ToggleAsync();

        // Assert
        var newState = await page.TermsCheckbox.IsCheckedAsync();
        Assert.NotEqual(initialState, newState);
    }

    [Fact]
    public async Task CheckBox_Disabled_IsNotEnabled()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        var isEnabled = await page.DisabledCheckbox.IsEnabledAsync();

        // Assert
        Assert.False(isEnabled, "Disabled checkbox should not be enabled.");
    }

    [Fact]
    public async Task CheckBox_StatusUpdates_WhenChecked()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/form-controls");
        await WaitForBlazorReadyAsync();
        var page = new FormControlsPage(Context);
        await page.WaitForDisplayedAsync();

        // Act
        await page.TermsCheckbox.CheckAsync();

        // Wait for Blazor to update
        await Task.Delay(100);

        // Assert
        var status = await page.CheckboxStatus.GetTextAsync();
        Assert.Contains("Accepted", status);
    }
}
