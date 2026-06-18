using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Display;

/// <summary>
/// UI tests for the ActivityIndicator control in the DisplayTestView.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "ActivityIndicator")]
public class ActivityIndicatorTests
{
    private readonly MauiFixture _fixture;

    public ActivityIndicatorTests(MauiFixture fixture)
    {
        _fixture = fixture;
        // Navigate to Display tab
        _fixture.AppShell2.DisplayContent.Click();
    }

    private DisplayTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the ActivityIndicator exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task ActivityIndicator_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestActivityIndicator.AssertExists();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the ActivityIndicator is visible when running.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task ActivityIndicator_IsVisible_WhenRunning_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestActivityIndicator.AssertVisible();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that toggling ActivityIndicator changes its running state.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Toggle")]
    public Task ActivityIndicator_Toggle_ChangesRunningState()
    {
        var page = GetPage();
        // Act - Toggle the activity indicator off
        page.ToggleActivityButton.Click()
            .StatusLabel.AssertTextContains("Stopped");

        // Act - Toggle the activity indicator back on
        page.ToggleActivityButton.Click()
            .StatusLabel.AssertTextContains("Running");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that resetting the view restarts the ActivityIndicator.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Reset")]
    public Task ActivityIndicator_Reset_RestartsIndicator()
    {
        var page = GetPage();

        // Act - Stop the indicator
        page.ToggleActivityButton.Click()
            .StatusLabel.AssertTextContains("Stopped")
            // Reset it back to initial state
            .ResetButton.Click()
            .StatusLabel.AssertTextContains("Ready");

        return Task.CompletedTask;
    }
}
