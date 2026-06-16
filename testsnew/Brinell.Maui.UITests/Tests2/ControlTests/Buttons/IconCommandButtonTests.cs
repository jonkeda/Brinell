using Brinell.Maui.UITests.Pages2.TestViewPages;

namespace Brinell.Maui.UITests.Tests2.ControlTests.Buttons;

/// <summary>
/// UI tests for the IconCommandButton control in the ButtonsTestView.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "IconCommandButton")]
public class IconCommandButtonTests
{
    private readonly AppiumFixture _fixture;

    public IconCommandButtonTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    private ButtonsTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the IconCommandButton exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task IconCommandButton_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestIconCommandButton.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the IconCommandButton is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task IconCommandButton_IsVisible_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestIconCommandButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the IconCommandButton is enabled.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsEnabled")]
    public Task IconCommandButton_IsEnabled_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestIconCommandButton.IsEnabled());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the IconCommandButton executes its command.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task IconCommandButton_Tap_ExecutesCommand()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act
        page.TapIconCommandButton()
            .VerifyStatusContains("IconCommandButton");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the status message is updated when IconCommandButton is tapped.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task IconCommandButton_Tap_UpdatesStatus()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);
        var initialStatus = page.GetStatusMessage();

        // Act
        page.TapIconCommandButton();
        var updatedStatus = page.GetStatusMessage();

        // Assert
        Assert.NotEqual(initialStatus, updatedStatus);
        Assert.Contains("IconCommandButton", updatedStatus);
        return Task.CompletedTask;
    }
}
