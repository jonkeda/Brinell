using Brinell.Maui.UITests.Pages2.TestViewPages;

namespace Brinell.Maui.UITests.Tests2.ControlTests.Buttons;

/// <summary>
/// UI tests for the RoundButton control in the ButtonsTestView.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "RoundButton")]
public class RoundButtonTests
{
    private readonly AppiumFixture _fixture;

    public RoundButtonTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    private ButtonsTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the RoundButton exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task RoundButton_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestRoundButton.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the RoundButton is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task RoundButton_IsVisible_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestRoundButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the RoundButton is enabled.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsEnabled")]
    public Task RoundButton_IsEnabled_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestRoundButton.IsEnabled());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the RoundButton executes its command.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task RoundButton_Tap_ExecutesCommand()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act
        page.TapRoundButton()
            .VerifyStatusContains("RoundButton");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the status message is updated when RoundButton is tapped.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task RoundButton_Tap_UpdatesStatus()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);
        var initialStatus = page.GetStatusMessage();

        // Act
        page.TapRoundButton();
        var updatedStatus = page.GetStatusMessage();

        // Assert
        Assert.NotEqual(initialStatus, updatedStatus);
        Assert.Contains("RoundButton", updatedStatus);
        return Task.CompletedTask;
    }
}
