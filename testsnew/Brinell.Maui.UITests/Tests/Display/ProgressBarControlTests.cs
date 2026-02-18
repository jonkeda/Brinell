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
    /// Note: MAUI ProgressBar is not exposed in the Windows automation tree by WinAppDriver.
    /// This test verifies existence on platforms that support it.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs, Skip = "MAUI ProgressBar is not exposed in the Windows automation tree by WinAppDriver")]
    [Trait("Method", "GetText")]
    public Task ProgressBar_GetValue_ReturnsProgress()
    {
        // Progress bar is bound to slider, so we can read its state
        // Wait for the element to exist on the page before asserting
        Page.VolumeProgress.WaitExists(true);
        Assert.True(Page.VolumeProgress.IsExists());
        return Task.CompletedTask;
    }

    #endregion
}
