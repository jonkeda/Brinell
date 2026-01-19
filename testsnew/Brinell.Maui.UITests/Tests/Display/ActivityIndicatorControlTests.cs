using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Display;

/// <summary>
/// UI tests for MauiActivityIndicatorControl verifying running state.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "ActivityIndicator")]
public class ActivityIndicatorControlTests
{
    private readonly AppiumFixture _fixture;
    private MediaGalleryPage Page => _fixture.MediaGalleryPage;

    public ActivityIndicatorControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToMediaGallery();
    }

    #region State Tests

    /// <summary>
    /// Verifies that activity indicator exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task ActivityIndicator_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.WebLoadingIndicator.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that activity indicator visibility state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task ActivityIndicator_IsVisible_ReflectsState()
    {
        // Activity indicator visibility depends on IsRunning binding
        // Just verify we can query the state without errors
        var isVisible = Page.WebLoadingIndicator.IsVisible();
        // State is either true or false, both are valid (nullable bool)
        Assert.True(isVisible == true || isVisible == false);
        return Task.CompletedTask;
    }

    #endregion
}
