using Brinell.Maui.UITests.Pages2.TestViewPages;

namespace Brinell.Maui.UITests.Tests2.ControlTests.Buttons;

/// <summary>
/// UI tests for the Button control in the ButtonsTestView.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Button")]
public class ButtonTests
{
    private readonly AppiumFixture _fixture;

    public ButtonTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    private ButtonsTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the Button exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Button_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestButton.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Button is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Button_IsVisible_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Button is enabled.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsEnabled")]
    public Task Button_IsEnabled_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestButton.IsEnabled());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the Button executes its command.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task Button_Tap_ExecutesCommand()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act
        page.TapButton()
            .VerifyStatusContains("Button tapped");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the Button multiple times increments the tap count.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task Button_MultipleTaps_IncrementsCount()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act
        page.TapButton()
            .VerifyStatusContains("1 time")
            .TapButton()
            .VerifyStatusContains("2 times");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the Reset button clears the status.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Reset")]
    public Task Button_Reset_ClearsStatus()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act
        page.TapButton()
            .VerifyStatusContains("Button tapped")
            .Reset()
            .VerifyStatusContains("Ready");

        return Task.CompletedTask;
    }
}
