using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Buttons;

/// <summary>
/// UI tests for the Button control in the ButtonsTestView.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "Button")]
public class ButtonTests
{
    private readonly MauiFixture _fixture;

    public ButtonTests(MauiFixture fixture)
    {
        _fixture = fixture;

        _fixture.AppShell2.ButtonsContent.Click();
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
        page.TestButton.AssertExists();
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
        page.TestButton.AssertVisible();
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
        page.TestButton.AssertEnabled();
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
        // Act
        page.TestButton.Click()
            .StatusLabel.AssertTextContains("Button tapped");

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
        page.TestButton.Click()
            .StatusLabel.AssertTextContains("1 time")
            .TestButton.Click()
            .StatusLabel.AssertTextContains("2 times");

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
        page.TestButton.Click()
            .StatusLabel.AssertTextContains("Button tapped")
            .ResetButton.Click()
            .StatusLabel.AssertTextContains("Ready");

        return Task.CompletedTask;
    }
}
