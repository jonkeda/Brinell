using Brinell.Maui.UITests.Pages2.TestViewPages;

namespace Brinell.Maui.UITests.Tests2.ControlTests.Buttons;

/// <summary>
/// UI tests for the ImageButton control in the ButtonsTestView.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "ImageButton")]
public class ImageButtonTests
{
    private readonly AppiumFixture _fixture;

    public ImageButtonTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    private ButtonsTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the ImageButton exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task ImageButton_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestImageButton.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the ImageButton is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task ImageButton_IsVisible_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestImageButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the ImageButton is enabled.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsEnabled")]
    public Task ImageButton_IsEnabled_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestImageButton.IsEnabled());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the ImageButton executes its command.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task ImageButton_Tap_ExecutesCommand()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act
        page.TapImageButton()
            .VerifyStatusContains("ImageButton");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the status message is updated when ImageButton is tapped.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task ImageButton_Tap_UpdatesStatus()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);
        var initialStatus = page.GetStatusMessage();

        // Act
        page.TapImageButton();
        var updatedStatus = page.GetStatusMessage();

        // Assert
        Assert.NotEqual(initialStatus, updatedStatus);
        Assert.Contains("ImageButton", updatedStatus);
        return Task.CompletedTask;
    }
}
