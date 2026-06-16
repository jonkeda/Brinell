using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests;

/// <summary>
/// UI tests for MainPage (BasicsView content) demonstrating tab navigation and greeting functionality.
/// Tests run against the Brinell.Samples.Maui.App sample application.
/// </summary>
/// <remarks>
/// Prerequisites:
/// - Appium server running (e.g., on localhost:4723)
/// - Sample app deployed to device/emulator
/// - Correct capabilities configured in test setup
/// 
/// MainPage tests BasicsView content (first tab in TabbedPage), containing:
/// - Name entry for user input
/// - Email entry for email input
/// - Greet button to trigger greeting
/// - Greeting label showing the result
/// - Counter buttons demonstrating state
/// - Toggle/slider/picker controls
/// </remarks>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Page", "MainPage")]
public class MainPageTests
{
    private readonly AppiumFixture _fixture;
    private MainPage Page => _fixture.MainPage;

    public MainPageTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        // Navigate to Basics tab to ensure we're on the right page even when running as part of suite
        _fixture.NavigateToMain();
    }

    #region Basic Control Existence Tests

    /// <summary>
    /// Verifies that key controls exist on MainPage.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void MainPage_KeyControls_Exist()
    {
        // Assert
        Page.NameEntry.AssertExists();
        Page.EmailEntry.AssertExists();
        Page.GreetButton.AssertExists();
        Page.GreetingLabel.AssertExists();
        Page.IncrementButton.AssertExists();
        Page.DecrementButton.AssertExists();
        Page.ResetButton.AssertExists();
    }

    /// <summary>
    /// Verifies that entry controls are visible and enabled.
    /// </summary>
    [Fact]
    [Trait("Method", "IsVisible")]
    public void MainPage_EntryControls_AreVisibleAndEnabled()
    {
        // Assert
        Page.NameEntry.AssertVisible(true);
        Page.NameEntry.AssertEnabled(true);
        Page.EmailEntry.AssertVisible(true);
        Page.EmailEntry.AssertEnabled(true);
    }

    #endregion

    #region Greeting Functionality Tests

    /// <summary>
    /// Enter name and click greet button - verifies greeting message.
    /// </summary>
    [Fact]
    [Trait("Feature", "Greeting")]
    public void MainPage_EnterNameAndGreet_ShowsGreetingMessage()
    {
        // Arrange
        Page.NameEntry.Clear();

        // Act
        Page.NameEntry.Enter("Alice");
        Page.GreetButton.Click();

        // Assert
        Page.GreetingLabel.AssertText("Hello, Alice!");
    }

    /// <summary>
    /// Click greet without entering name shows validation message.
    /// </summary>
    [Fact]
    [Trait("Feature", "Greeting")]
    public void MainPage_GreetWithoutName_ShowsValidationMessage()
    {
        // Arrange
        Page.NameEntry.Clear();

        // Act
        Page.GreetButton.Click();

        // Assert
        Page.GreetingLabel.AssertText("Please enter your name");
    }

    /// <summary>
    /// Greet with different names shows correct personalized greeting.
    /// </summary>
    [Fact]
    [Trait("Feature", "Greeting")]
    public void MainPage_GreetWithDifferentNames_ShowsPersonalizedGreeting()
    {
        // Test multiple names
        var names = new[] { "Bob", "Carol", "Dave" };

        foreach (var name in names)
        {
            // Arrange
            Page.NameEntry.Clear();

            // Act
            Page.NameEntry.Enter(name);
            Page.GreetButton.Click();

            // Assert
            Page.GreetingLabel.AssertText($"Hello, {name}!");
        }
    }

    #endregion

    #region Email Entry Tests

    /// <summary>
    /// Email entry accepts email format text.
    /// </summary>
    [Fact]
    [Trait("Feature", "Email")]
    public void MainPage_EmailEntry_AcceptsEmailFormat()
    {
        // Arrange
        Page.EmailEntry.Clear();

        // Act
        Page.EmailEntry.Enter("test@example.com");

        // Assert
        Page.EmailEntry.AssertText("test@example.com");
    }

    /// <summary>
    /// Email entry placeholder is displayed.
    /// </summary>
    [Fact]
    [Trait("Feature", "Email")]
    public void MainPage_EmailEntry_HasPlaceholder()
    {
        // Arrange
        Page.EmailEntry.Clear();

        // Act - placeholder should be visible when empty
        var placeholder = Page.EmailEntry.GetPlaceholder();

        // Assert
        Assert.False(string.IsNullOrEmpty(placeholder));
    }

    #endregion

    #region Counter Tests

    /// <summary>
    /// Counter increment button click increments counter.
    /// </summary>
    [Fact]
    [Trait("Feature", "Counter")]
    public void MainPage_ClickIncrement_IncrementsCounter()
    {
        // Act - click increment button multiple times
        Page.IncrementButton.Click();
        var firstLabel = Page.CounterLabel.GetText();

        Page.IncrementButton.Click();
        var secondLabel = Page.CounterLabel.GetText();

        // Assert - counter increments
        // Text format: "Counter: N"
        Assert.NotEqual(firstLabel, secondLabel);
    }

    /// <summary>
    /// Counter reset button resets counter to zero.
    /// </summary>
    [Fact]
    [Trait("Feature", "Counter")]
    public void MainPage_ClickReset_ResetsCounter()
    {
        // Arrange - increment counter
        Page.IncrementButton.Click();
        Page.IncrementButton.Click();

        // Act - reset counter
        Page.ResetButton.Click();

        // Assert - counter is back to 0
        Page.CounterLabel.AssertText("Counter: 0");
    }

    #endregion

    #region Placeholder Tests

    /// <summary>
    /// GetPlaceholder retrieves the placeholder text for name entry.
    /// </summary>
    [Fact]
    [Trait("Method", "GetPlaceholder")]
    public void MainPage_NameEntry_GetPlaceholder_ReturnsPlaceholderText()
    {
        // Act
        var placeholder = Page.NameEntry.GetPlaceholder();

        // Assert
        Assert.Equal("Enter your name", placeholder);
    }

    /// <summary>
    /// AssertPlaceholder verifies placeholder text for name entry.
    /// </summary>
    [Fact]
    [Trait("Method", "AssertPlaceholder")]
    public void MainPage_NameEntry_AssertPlaceholder_PassesWithCorrectPlaceholder()
    {
        // Assert (with fluent return)
        Page.NameEntry.AssertPlaceholder("Enter your name");
    }

    #endregion

    #region Fluent Chaining Tests

    /// <summary>
    /// Demonstrates fluent chaining with entry operations on MainPage.
    /// Each operation returns MainPage, allowing access to other controls.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentChaining")]
    public void MainPage_FluentChaining_WorksCorrectly()
    {
        // Fluent chain: clear, enter, assert, then interact with button
        Page.NameEntry.Clear()
            .NameEntry.Enter("Bob")
            .NameEntry.AssertText("Bob")
            .GreetButton.Click()
            .GreetingLabel.AssertText("Hello, Bob!");
    }

    /// <summary>
    /// Demonstrates assertion chaining on MainPage controls.
    /// All assertion methods return the containing scope for chaining.
    /// </summary>
    [Fact]
    [Trait("Pattern", "AssertionChaining")]
    public void MainPage_AssertionChaining_WorksCorrectly()
    {
        Page.NameEntry.AssertExists(true)
            .NameEntry.AssertVisible(true)
            .NameEntry.AssertEnabled(true)
            .NameEntry.AssertPlaceholder("Enter your name");
    }

    #endregion

    #region Wait Tests

    /// <summary>
    /// WaitExists with timeout for MainPage controls.
    /// </summary>
    [Fact]
    [Trait("Method", "WaitExists")]
    public void MainPage_WaitExists_ReturnsTrue()
    {
        // Assert
        var result = Page.NameEntry.WaitExists(true, timeoutMs: 5000);
        Assert.True(result);
    }

    /// <summary>
    /// WaitText with timeout for greeting label.
    /// </summary>
    [Fact]
    [Trait("Method", "WaitText")]
    public void MainPage_WaitText_ReturnsTrueWhenMatches()
    {
        // Arrange
        Page.NameEntry.Clear();
        Page.NameEntry.Enter("WaitTest");
        Page.GreetButton.Click();

        // Act & Assert - wait for greeting to appear
        var result = Page.GreetingLabel.WaitText("Hello, WaitTest!", timeoutMs: 5000);
        Assert.True(result);
    }

    #endregion
}
