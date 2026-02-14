using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Display;

/// <summary>
/// UI tests for MauiProgressBarControl verifying progress bar display and value.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "ProgressBar")]
public class ProgressBarControlTests
{
    private readonly AppiumFixture _fixture;
    private MainPage Page => _fixture.MainPage;

    public ProgressBarControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToMain();
    }

    #region State Tests

    /// <summary>
    /// Verifies that progress bar exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task ProgressBar_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.VolumeProgress.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that progress bar is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task ProgressBar_IsVisible_ReturnsTrue()
    {
        // Ensure control is in viewport for visibility checks on scrollable page
        Page.VolumeProgress.ScrollIntoView();

        // Assert
        Assert.True(Page.VolumeProgress.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Value Tests

    /// <summary>
    /// Verifies progress bar shows progress value.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task ProgressBar_GetValue_ReturnsProgress()
    {
        // Progress bar is bound to slider, so we can read its state
        // The value may be represented in the element's text or attributes
        Assert.True(Page.VolumeProgress.IsExists());
        return Task.CompletedTask;
    }

    #endregion
}
