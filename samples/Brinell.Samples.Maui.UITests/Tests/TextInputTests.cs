using Brinell.Samples.Maui.UITests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.Tests;

/// <summary>
/// Tests for text input controls on MainPage.
/// </summary>
public class TextInputTests : MauiTestBase
{
    private readonly MainPageObject _mainPage;

    public TextInputTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject(Context);
    }

    [Fact]
    public void NameEntry_EnterText_ShowsValue()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Act
        _mainPage.NameEntry.SetText("John Doe");

        // Assert
        _mainPage.NameEntry.AssertTextEquals("John Doe");
    }

    [Fact]
    public void EmailEntry_EnterEmail_ShowsValue()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Act
        _mainPage.EmailEntry.SetText("john@example.com");

        // Assert
        _mainPage.EmailEntry.AssertTextEquals("john@example.com");
    }

    [Fact]
    public void GreetButton_WithName_ShowsGreeting()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.NameEntry.SetText("Alice");

        // Act
        _mainPage.GreetButton.Tap();

        // Assert
        _mainPage.GreetingLabel.AssertTextContains("Hello, Alice!");
    }

    [Fact]
    public void GreetButton_WithoutName_ShowsError()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.NameEntry.Clear();

        // Act
        _mainPage.GreetButton.Tap();

        // Assert
        _mainPage.GreetingLabel.AssertTextContains("Please enter your name");
    }

    [Fact]
    public void MessageEditor_EnterMultilineText_ShowsValue()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollDown(200);

        // Act
        _mainPage.MessageEditor.SetText("Line 1\nLine 2\nLine 3");

        // Assert
        _mainPage.MessageEditor.AssertTextContains("Line 1");
    }
}
