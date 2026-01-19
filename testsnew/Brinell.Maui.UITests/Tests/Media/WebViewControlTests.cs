using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Media;

/// <summary>
/// UI tests for MauiWebViewControl verifying web view operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "WebView")]
public class WebViewControlTests
{
    private readonly AppiumFixture _fixture;
    private MediaGalleryPage Page => _fixture.MediaGalleryPage;

    public WebViewControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToMediaGallery();
    }

    #region State Tests

    /// <summary>
    /// Verifies that web view exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task WebView_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.ContentWebView.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that web view is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task WebView_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.ContentWebView.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region URL Tests

    /// <summary>
    /// Verifies GetUrl() returns current URL.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "GetUrl")]
    public Task WebView_GetUrl_ReturnsUrl()
    {
        // Act
        var url = Page.ContentWebView.GetUrl();

        // Assert - URL may be empty initially or have a default
        Assert.NotNull(url);
        return Task.CompletedTask;
    }

    #endregion

    #region Navigation State Tests

    /// <summary>
    /// Verifies CanGoBack() returns navigation state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "CanGoBack")]
    public Task WebView_CanGoBack_ReturnsState()
    {
        // Act
        var canGoBack = Page.ContentWebView.CanGoBack();

        // Assert - either state is valid (nullable bool)
        Assert.True(canGoBack == true || canGoBack == false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies CanGoForward() returns navigation state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "CanGoForward")]
    public Task WebView_CanGoForward_ReturnsState()
    {
        // Act
        var canGoForward = Page.ContentWebView.CanGoForward();

        // Assert - either state is valid (nullable bool)
        Assert.True(canGoForward == true || canGoForward == false);
        return Task.CompletedTask;
    }

    #endregion
}
