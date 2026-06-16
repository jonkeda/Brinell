using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Text;

/// <summary>
/// Example UI tests demonstrating Entry control testing patterns.
/// These tests run against the Brinell.Samples.Maui.App sample application.
/// </summary>
/// <remarks>
/// Prerequisites:
/// - Maui server running (e.g., on localhost:4723)
/// - Sample app deployed to device/emulator
/// - Correct capabilities configured in test setup
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "Entry")]
public class EntryControlTests
{
    private readonly MauiFixture _fixture;
    private MainPage Page => _fixture.MainPage;

    public EntryControlTests(MauiFixture fixture)
    {
        _fixture = fixture;
        // Navigate to the Basics tab where entry controls are located
        _fixture.NavigateToMain();
    }

    #region Entry State Tests

    /// <summary>
    /// Verifies that entry controls exist on the page.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void Entry_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.NameEntry.IsExists());
        Assert.True(Page.EmailEntry.IsExists());
    }

    /// <summary>
    /// Verifies that entry controls are visible.
    /// </summary>
    [Fact]
    [Trait("Method", "IsVisible")]
    public void Entry_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.NameEntry.IsVisible());
        Assert.True(Page.EmailEntry.IsVisible());
    }

    /// <summary>
    /// Verifies that entry controls are enabled.
    /// </summary>
    [Fact]
    [Trait("Method", "IsEnabled")]
    public void Entry_IsEnabled_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.NameEntry.IsEnabled());
        Assert.True(Page.EmailEntry.IsEnabled());
    }

    #endregion

    #region Text Input Tests

    /// <summary>
    /// Enter text into entry control and verify it was entered.
    /// </summary>
    [Fact]
    [Trait("Method", "Enter")]
    public void Entry_Enter_SetsText()
    {
        // Arrange
        Page.NameEntry.Clear();

        // Act
        Page.NameEntry.Enter("John Doe");

        // Assert
        Assert.Equal("John Doe", Page.NameEntry.GetText());
    }

    /// <summary>
    /// SetText replaces existing text.
    /// </summary>
    [Fact]
    [Trait("Method", "SetText")]
    public void Entry_SetText_ReplacesExistingText()
    {
        // Arrange
        Page.NameEntry.SetText("Initial Text");

        // Act
        Page.NameEntry.SetText("Replaced Text");

        // Assert
        Assert.Equal("Replaced Text", Page.NameEntry.GetText());
    }

    /// <summary>
    /// Clear removes all text from entry.
    /// </summary>
    [Fact]
    [Trait("Method", "Clear")]
    public void Entry_Clear_RemovesAllText()
    {
        // Arrange
        Page.NameEntry.SetText("Some text to clear");

        // Act
        Page.NameEntry.Clear();

        // Assert
        Assert.True(string.IsNullOrEmpty(Page.NameEntry.GetText()));
    }

    /// <summary>
    /// GetText retrieves the current text value.
    /// </summary>
    [Fact]
    [Trait("Method", "GetText")]
    public void Entry_GetText_ReturnsCurrentText()
    {
        // Arrange
        const string expectedText = "Test Input";
        Page.NameEntry.SetText(expectedText);

        // Act
        var actualText = Page.NameEntry.GetText();

        // Assert
        Assert.Equal(expectedText, actualText);
    }

    #endregion

    #region Placeholder Tests

    /// <summary>
    /// GetPlaceholder retrieves the placeholder text.
    /// </summary>
    [Fact]
    [Trait("Method", "GetPlaceholder")]
    public void Entry_GetPlaceholder_ReturnsPlaceholderText()
    {
        // Act
        var placeholder = Page.NameEntry.GetPlaceholder();

        // Assert
        Assert.Equal("Enter your name", placeholder);
    }

    /// <summary>
    /// AssertPlaceholder verifies placeholder text.
    /// </summary>
    [Fact]
    [Trait("Method", "AssertPlaceholder")]
    public void Entry_AssertPlaceholder_PassesWithCorrectPlaceholder()
    {
        // Assert (with fluent return)
        Page.NameEntry.AssertPlaceholder("Enter your name");
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Enter name and click greet button - verifies greeting message.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Integration")]
    public void Entry_EnterNameAndGreet_ShowsGreetingMessage()
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
    [Trait("Pattern", "Integration")]
    public void Entry_GreetWithoutName_ShowsValidationMessage()
    {
        // Arrange
        Page.NameEntry.Clear();

        // Act
        Page.GreetButton.Click();

        // Assert
        Page.GreetingLabel.AssertText("Please enter your name");
    }

    /// <summary>
    /// Email entry accepts email format text.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Integration")]
    public void EmailEntry_EnterEmail_AcceptsEmailFormat()
    {
        // Arrange
        Page.EmailEntry.Clear();

        // Act
        Page.EmailEntry.Enter("test@example.com");

        // Assert
        Page.EmailEntry.AssertText("test@example.com");
    }

    #endregion

    #region Fluent Chaining Tests

    /// <summary>
    /// Demonstrates fluent chaining with entry operations.
    /// Each operation returns MainPage, allowing access to other controls.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentChaining")]
    public void Entry_FluentChaining_WorksCorrectly()
    {
        // Fluent chain: clear, enter, assert, then interact with button
        Page.NameEntry.Clear()
            .NameEntry.Enter("Bob")
            .NameEntry.AssertText("Bob")
            .GreetButton.Click()
            .GreetingLabel.AssertText("Hello, Bob!");
    }

    /// <summary>
    /// Demonstrates assertion chaining on entry.
    /// All assertion methods return the containing scope for chaining.
    /// </summary>
    [Fact]
    [Trait("Pattern", "AssertionChaining")]
    public void Entry_AssertionChaining_WorksCorrectly()
    {
        Page.NameEntry.AssertExists(true)
            .NameEntry.AssertVisible(true)
            .NameEntry.AssertEnabled(true)
            .NameEntry.AssertPlaceholder("Enter your name");
    }

    /// <summary>
    /// Demonstrates using nullable skip pattern.
    /// </summary>
    [Fact]
    [Trait("Pattern", "NullableSkip")]
    public void Entry_NullableSkip_SkipsWhenNull()
    {
        string? nullText = null;
        string? nullPlaceholder = null;

        // These should skip (no-op) when parameter is null
        // Each returns MainPage, so we access the control again
        Page.NameEntry.Enter(nullText)           // Skipped
            .NameEntry.AssertText(nullText)      // Skipped
            .NameEntry.AssertPlaceholder(nullPlaceholder); // Skipped
    }

    #endregion

    #region Wait Tests

    /// <summary>
    /// WaitExists with timeout for entry.
    /// </summary>
    [Fact]
    [Trait("Method", "WaitExists")]
    public void Entry_WaitExists_ReturnsTrue()
    {
        // Assert
        var result = Page.NameEntry.WaitExists(true, timeoutMs: 5000);
        Assert.True(result);
    }

    /// <summary>
    /// WaitText with timeout for entry.
    /// </summary>
    [Fact]
    [Trait("Method", "WaitText")]
    public void Entry_WaitText_ReturnsTrueWhenMatches()
    {
        // Arrange
        Page.NameEntry.SetText("WaitTest");

        // Assert
        var result = Page.NameEntry.WaitText("WaitTest", timeoutMs: 5000);
        Assert.True(result);
    }

    #endregion
}
