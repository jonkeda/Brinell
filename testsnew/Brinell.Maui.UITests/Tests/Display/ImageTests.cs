using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Display;

/// <summary>
/// UI tests for the Image control in the DisplayTestView.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "Image")]
public class ImageTests
{
    private readonly MauiFixture _fixture;

    public ImageTests(MauiFixture fixture)
    {
        _fixture = fixture;
        // Navigate to Display tab
        _fixture.AppShell.DisplayTab.Click();
    }

    private DisplayTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the Image exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Image_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestImage.AssertExists();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Image is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Image_IsVisible_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestImage.AssertVisible();
        return Task.CompletedTask;
    }
}
