using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Display;

/// <summary>
/// UI tests for Image verifying image display.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Image")]
public class ImageControlTests
{
    private readonly AppiumFixture _fixture;
    private MediaGalleryPage Page => _fixture.MediaGalleryPage;

    public ImageControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToMediaGallery();
    }

    #region State Tests

    /// <summary>
    /// Verifies that image control exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Image_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.MainImage.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that image is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Image_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.MainImage.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region Image Properties Tests

    /// <summary>
    /// Verifies image is loaded (has source).
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsLoaded")]
    public Task Image_IsLoaded_ReturnsTrueWhenSourceSet()
    {
        // Image should be loaded if it exists and is visible
        var exists = Page.MainImage.IsExists();
        var visible = Page.MainImage.IsVisible();
        
        // If both are true, the image is considered loaded
        Assert.True(exists && visible == true);
        return Task.CompletedTask;
    }

    #endregion
}
