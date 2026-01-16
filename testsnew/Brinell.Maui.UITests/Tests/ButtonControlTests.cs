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
    [Fact]
    [Trait("Method", "IsExists")]
    public void Button_IsExists_ReturnsTrue()
    {
        // Assert
        Page.IncrementButton.IsExists().Should().BeTrue();
        Page.DecrementButton.IsExists().Should().BeTrue();
        Page.ResetButton.IsExists().Should().BeTrue();
        Page.GreetButton.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that buttons are visible.
    /// </summary>
    [Fact]
    [Trait("Method", "IsVisible")]
    public void Button_IsVisible_ReturnsTrue()
    {
        // Assert
        Page.IncrementButton.IsVisible().Should().BeTrue();
        Page.DecrementButton.IsVisible().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that buttons are enabled.
    /// </summary>
    [Fact]
    [Trait("Method", "IsEnabled")]
    public void Button_IsEnabled_ReturnsTrue()
    {
        // Assert
        Page.IncrementButton.IsEnabled().Should().BeTrue();
        Page.ResetButton.IsEnabled().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that buttons are clickable (visible and enabled).
    /// </summary>
    [Fact]
    [Trait("Method", "IsClickable")]
    public void Button_IsClickable_ReturnsTrue()
    {
        // Assert
        Page.IncrementButton.IsClickable().Should().BeTrue();
        Page.DecrementButton.IsClickable().Should().BeTrue();
    }

    #endregion

    #region Click Tests

    /// <summary>
    /// Clicking the increment button increases the counter.
    /// </summary>
    [Fact]
    [Trait("Method", "Click")]
    public void IncrementButton_Click_IncreasesCounter()
    {
        // Arrange - Reset to known state
        Page.ResetButton.Click();
        Page.CounterLabel.AssertText("Counter: 0");

        // Act
        Page.IncrementButton.Click();

        // Assert
        Page.CounterLabel.AssertText("Counter: 1");
    }

    /// <summary>
    /// Clicking the decrement button decreases the counter.
    /// </summary>
    [Fact]
    [Trait("Method", "Click")]
    public void DecrementButton_Click_DecreasesCounter()
    {
        // Arrange - Reset to known state
        Page.ResetButton.Click();
        Page.CounterLabel.AssertText("Counter: 0");

        // Act
        Page.DecrementButton.Click();

        // Assert
        Page.CounterLabel.AssertText("Counter: -1");
    }

    /// <summary>
    /// Clicking the reset button sets counter to zero.
    /// </summary>
    [Fact]
    [Trait("Method", "Click")]
    public void ResetButton_Click_ResetsCounterToZero()
    {
        // Arrange - Set to non-zero state
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();

        // Act
        Page.ResetButton.Click();

        // Assert
        Page.CounterLabel.AssertText("Counter: 0");
    }

    /// <summary>
    /// Multiple clicks work correctly.
    /// </summary>
    [Fact]
    [Trait("Method", "Click")]
    public void IncrementButton_MultipleClicks_CountsCorrectly()
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
    }

    #endregion

    #region Fluent Chaining Tests

    /// <summary>
    /// Demonstrates fluent chaining with button clicks.
    /// Click() returns the containing scope (MainPage), allowing access to other controls.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentChaining")]
    public void Button_FluentChaining_WorksCorrectly()
    {
        // Fluent chain: reset, then increment twice, then assert
        // Each Click() returns MainPage, allowing access to next control
        Page.ResetButton.Click()
            .IncrementButton.Click()
            .IncrementButton.Click()
            .CounterLabel.AssertText("Counter: 2");
    }

    /// <summary>
    /// Demonstrates assertion chaining on a single button.
    /// All assertion methods return the containing scope for chaining.
    /// </summary>
    [Fact]
    [Trait("Pattern", "AssertionChaining")]
    public void Button_AssertionChaining_WorksCorrectly()
    {
        // Chain assertions on the same control by accessing it again from returned page
        Page.IncrementButton.AssertExists(true)
            .IncrementButton.AssertVisible(true)
            .IncrementButton.AssertEnabled(true)
            .IncrementButton.AssertClickable(true);
    }

    #endregion

    #region Wait Tests

    /// <summary>
    /// WaitExists with timeout for button.
    /// </summary>
    [Fact]
    [Trait("Method", "WaitExists")]
    public void Button_WaitExists_ReturnsTrue()
    {
        // Assert
        var result = Page.IncrementButton.WaitExists(true, timeoutMs: 5000);
        result.Should().BeTrue();
    }

    /// <summary>
    /// WaitClickable with timeout for button.
    /// </summary>
    [Fact]
    [Trait("Method", "WaitClickable")]
    public void Button_WaitClickable_ReturnsTrue()
    {
        // Assert
        var result = Page.IncrementButton.WaitClickable(true, timeoutMs: 5000);
        result.Should().BeTrue();
    }

    #endregion
}
