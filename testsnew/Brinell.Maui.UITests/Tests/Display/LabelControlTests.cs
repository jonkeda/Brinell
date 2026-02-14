using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Display;

/// <summary>
/// UI tests for MauiLabelControl verifying text display capabilities.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Label")]
public class LabelControlTests
{
    private readonly AppiumFixture _fixture;
    private MainPage Page => _fixture.MainPage;

    public LabelControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToMain();
    }

    #region State Tests

    /// <summary>
    /// Verifies that label exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Label_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.TitleLabel.IsExists());
        Assert.True(Page.SubtitleLabel.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that label is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Label_IsVisible_ReturnsTrue()
    {
        Page.TitleLabel.ScrollIntoView();

        // Assert
        Assert.True(Page.TitleLabel.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Text Tests

    /// <summary>
    /// Verifies GetText returns correct label text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task Label_GetText_ReturnsExpectedText()
    {
        // Assert
        var text = Page.TitleLabel.GetText();
        Assert.Equal("Brinell MAUI Sample", text);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies AssertText passes with correct text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertText")]
    public Task Label_AssertText_PassesWithCorrectText()
    {
        // Assert - no exception means success
        Page.TitleLabel.AssertText("Brinell MAUI Sample");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies AssertTextContains works for partial match.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertTextContains")]
    public Task Label_AssertTextContains_PassesWithPartialMatch()
    {
        // Assert
        Page.TitleLabel.AssertTextContains("MAUI");
        Page.TitleLabel.AssertTextContains("Brinell");
        return Task.CompletedTask;
    }

    #endregion

    #region Dynamic Label Tests

    /// <summary>
    /// Verifies counter label updates after button click.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task Label_DynamicText_UpdatesAfterAction()
    {
        // Arrange
        Page.ResetButton.Click();
        Page.CounterLabel.AssertText("Counter: 0");

        // Act
        Page.IncrementButton.Click();

        // Assert
        Page.CounterLabel.AssertText("Counter: 1");
        return Task.CompletedTask;
    }

    #endregion

    #region Wait Tests

    /// <summary>
    /// Verifies WaitText works for dynamic content.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "WaitText")]
    public Task Label_WaitText_WaitsForExpectedValue()
    {
        // Arrange
        Page.ResetButton.Click();

        // Act
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();

        // Assert
        var result = Page.CounterLabel.WaitText("Counter: 2", timeoutMs: 5000);
        Assert.True(result);
        return Task.CompletedTask;
    }

    #endregion
}
