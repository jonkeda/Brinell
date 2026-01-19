using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests;

/// <summary>
/// Example UI tests demonstrating Button control testing patterns.
/// These tests run against the Brinell.Samples.Maui.App sample application.
/// </summary>
/// <remarks>
/// Prerequisites:
/// - Appium server running (e.g., on localhost:4723)
/// - Sample app deployed to device/emulator
/// - Correct capabilities configured in test setup
/// </remarks>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Button")]
public class ButtonControlTests
{
    private readonly AppiumFixture _fixture;
    private MainPage Page => _fixture.MainPage;

    public ButtonControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    #region Button State Tests

    /// <summary>
    /// Verifies that buttons exist on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Button_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.IncrementButton.IsExists());
        Assert.True(Page.DecrementButton.IsExists());
        Assert.True(Page.ResetButton.IsExists());
        Assert.True(Page.GreetButton.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that buttons are visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Button_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.IncrementButton.IsVisible());
        Assert.True(Page.DecrementButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that buttons are enabled.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsEnabled")]
    public Task Button_IsEnabled_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.IncrementButton.IsEnabled());
        Assert.True(Page.ResetButton.IsEnabled());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that buttons are clickable (visible and enabled).
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsClickable")]
    public Task Button_IsClickable_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.IncrementButton.IsClickable());
        Assert.True(Page.DecrementButton.IsClickable());
        return Task.CompletedTask;
    }

    #endregion

    #region Click Tests

    /// <summary>
    /// Clicking the increment button increases the counter.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task IncrementButton_Click_IncreasesCounter()
    {
        // Arrange - Reset to known state
        Page.ResetButton.Click();
        Page.CounterLabel.AssertText("Counter: 0");

        // Act
        Page.IncrementButton.Click();

        // Assert
        Page.CounterLabel.AssertText("Counter: 1");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clicking the decrement button decreases the counter.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task DecrementButton_Click_DecreasesCounter()
    {
        // Arrange - Reset to known state
        Page.ResetButton.Click();
        Page.CounterLabel.AssertText("Counter: 0");

        // Act
        Page.DecrementButton.Click();

        // Assert
        Page.CounterLabel.AssertText("Counter: -1");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clicking the reset button sets counter to zero.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task ResetButton_Click_ResetsCounterToZero()
    {
        // Arrange - Set to non-zero state
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();

        // Act
        Page.ResetButton.Click();

        // Assert
        Page.CounterLabel.AssertText("Counter: 0");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Multiple clicks work correctly.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task IncrementButton_MultipleClicks_CountsCorrectly()
    {
        // Arrange
        Page.ResetButton.Click();

        // Act - Click 5 times
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();

        // Assert
        Page.CounterLabel.AssertText("Counter: 5");
        return Task.CompletedTask;
    }

    #endregion

    #region Fluent Chaining Tests

    /// <summary>
    /// Demonstrates fluent chaining with button clicks.
    /// Click() returns the containing scope (MainPage), allowing access to other controls.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "FluentChaining")]
    public Task Button_FluentChaining_WorksCorrectly()
    {
        // Fluent chain: reset, then increment twice, then assert
        // Each Click() returns MainPage, allowing access to next control
        Page.ResetButton.Click()
            .IncrementButton.Click()
            .IncrementButton.Click()
            .CounterLabel.AssertText("Counter: 2");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Demonstrates assertion chaining on a single button.
    /// All assertion methods return the containing scope for chaining.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "AssertionChaining")]
    public Task Button_AssertionChaining_WorksCorrectly()
    {
        // Chain assertions on the same control by accessing it again from returned page
        Page.IncrementButton.AssertExists(true)
            .IncrementButton.AssertVisible(true)
            .IncrementButton.AssertEnabled(true)
            .IncrementButton.AssertClickable(true);
        return Task.CompletedTask;
    }

    #endregion

    #region Wait Tests

    /// <summary>
    /// WaitExists with timeout for button.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "WaitExists")]
    public Task Button_WaitExists_ReturnsTrue()
    {
        // Assert
        var result = Page.IncrementButton.WaitExists(true, timeoutMs: 5000);
        Assert.True(result);
        return Task.CompletedTask;
    }

    /// <summary>
    /// WaitClickable with timeout for button.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "WaitClickable")]
    public Task Button_WaitClickable_ReturnsTrue()
    {
        // Assert
        var result = Page.IncrementButton.WaitClickable(true, timeoutMs: 5000);
        Assert.True(result);
        return Task.CompletedTask;
    }

    #endregion
}
