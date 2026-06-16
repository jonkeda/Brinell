using Brinell.Maui.UITests.Pages2.TestViewPages;

namespace Brinell.Maui.UITests.Tests2.ControlTests.Buttons;

/// <summary>
/// UI tests for the Link control in the ButtonsTestView.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Link")]
public class LinkTests
{
    private readonly AppiumFixture _fixture;

    public LinkTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    private ButtonsTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the Link exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Link_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestLinkButton.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Link is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Link_IsVisible_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestLinkButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Link is enabled.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsEnabled")]
    public Task Link_IsEnabled_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        Assert.True(page.TestLinkButton.IsEnabled());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that tapping the Link executes its command.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task Link_Tap_ExecutesCommand()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act
        page.TapLinkButton()
            .VerifyStatusContains("Link");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the status message is updated when Link is tapped.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Tap")]
    public Task Link_Tap_UpdatesStatus()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);
        var initialStatus = page.GetStatusMessage();

        // Act
        page.TapLinkButton();
        var updatedStatus = page.GetStatusMessage();

        // Assert
        Assert.NotEqual(initialStatus, updatedStatus);
        Assert.Contains("Link", updatedStatus);
        return Task.CompletedTask;
    }
}
