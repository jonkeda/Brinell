using Brinell.Samples.Maui.UITests.ControlObject6.Pages;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Tests;

/// <summary>
/// Text input control tests using ControlObject6 API.
/// Tests Entry control functionality.
/// </summary>
public class TextInputTests6 : MauiTestBase6
{
    private readonly MainPageObject6 _mainPage;

    public TextInputTests6(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject6(Context);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public void NameEntry_IsVisibleOnPageLoad()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Assert
        _mainPage.NameEntry.AssertVisible(true);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public void NameEntry_Enter_SetsText()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        const string testName = "John Doe";

        // Act
        _mainPage.NameEntry.Enter(testName);

        // Assert
        _mainPage.NameEntry.AssertText(testName);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public void NameEntry_Clear_RemovesText()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NameEntry.Enter("Some text");

        // Act
        _mainPage.NameEntry.Clear();

        // Assert
        _mainPage.NameEntry.AssertTextEmpty(true);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public void NameEntry_ClearAndEnter_ReplacesText()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NameEntry.Enter("Old text");
        const string newText = "New text";

        // Act
        _mainPage.NameEntry.ClearAndEnter(newText);

        // Assert
        _mainPage.NameEntry.AssertText(newText);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public void NameEntry_Append_AddsToExistingText()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NameEntry.Clear();
        _mainPage.NameEntry.Enter("Hello");

        // Act
        _mainPage.NameEntry.Append(" World");

        // Assert
        _mainPage.NameEntry.AssertText("Hello World");
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public void EmailEntry_IsVisibleOnPageLoad()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Assert
        _mainPage.EmailEntry.AssertVisible(true);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public void EmailEntry_AcceptsEmailFormat()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        const string email = "test@example.com";

        // Act
        _mainPage.EmailEntry.Enter(email);

        // Assert
        _mainPage.EmailEntry.AssertText(email);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P0")]
    public void GreetButton_Click_ShowsGreeting()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        const string name = "Alice";

        // Act
        _mainPage.EnterNameAndGreet(name);

        // Assert
        _mainPage.GreetingLabel.AssertTextContains(name);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public void NameEntry_GetText_ReturnsCurrentValue()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        const string testText = "TestValue";
        _mainPage.NameEntry.Enter(testText);

        // Act
        var text = _mainPage.NameEntry.GetText();

        // Assert
        text.Should().Be(testText);
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public void NameEntry_IsEnabled_ReturnsTrue()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Assert
        _mainPage.NameEntry.IsEnabled().Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public void NameEntry_AssertTextContains_MatchesPartial()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NameEntry.Enter("Hello World");

        // Assert
        _mainPage.NameEntry.AssertTextContains("World");
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public void NameEntry_AssertTextStartsWith_MatchesPrefix()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NameEntry.Enter("Hello World");

        // Assert
        _mainPage.NameEntry.AssertTextStartsWith("Hello");
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P1")]
    public void NameEntry_AssertTextEndsWith_MatchesSuffix()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NameEntry.Enter("Hello World");

        // Assert
        _mainPage.NameEntry.AssertTextEndsWith("World");
    }

    [Fact]
    [Trait("Category", "TextInput")]
    [Trait("Priority", "P2")]
    public void NameEntry_AssertTextMatches_MatchesRegex()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.NameEntry.Enter("test123");

        // Assert - matches letters followed by numbers
        _mainPage.NameEntry.AssertTextMatches(@"^[a-z]+\d+$");
    }
}
