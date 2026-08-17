using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Display;

/// <summary>
/// UI tests for the Label control in the DisplayTestView.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "Label")]
public class LabelTests
{
    private readonly MauiFixture _fixture;

    public LabelTests(MauiFixture fixture)
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
    /// Verifies that the Label exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Label_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestLabel.AssertExists();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Label is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Label_IsVisible_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestLabel.AssertVisible();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Label displays text correctly.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task Label_DisplaysText_ReturnsExpectedText()
    {
        var page = GetPage();
        // Assert
        page.TestLabel.AssertTextContains("test label");
        return Task.CompletedTask;
    }
}
