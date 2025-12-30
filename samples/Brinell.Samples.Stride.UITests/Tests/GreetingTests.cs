using Brinell.Samples.Stride.UITests.PageObjects;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Stride.UITests;

/// <summary>
/// Tests for the greeting functionality.
/// </summary>
public class GreetingTests : StrideUITestBase
{
    public GreetingTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void GreetButton_WithName_DisplaysGreeting()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        mainPage.Greet("Alice");

        // Assert
        mainPage.GreetingDisplay.AssertTextEquals("Hello, Alice!");
    }

    [Fact]
    public void GreetButton_WithEmptyName_DisplaysDefaultGreeting()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        mainPage.NameInput.Clear();
        mainPage.GreetButton.Click();

        // Assert
        mainPage.GreetingDisplay.WaitText("Hello, World!");
        mainPage.GreetingDisplay.AssertTextEquals("Hello, World!");
    }

    [Fact]
    public void NameInput_EnterText_DisplaysInField()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        mainPage.NameInput.SetText("Bob");

        // Assert
        mainPage.NameInput.WaitText("Bob");
        mainPage.NameInput.AssertTextEquals("Bob");
    }

    [Fact]
    public void NameInput_ClearAndEnter_ReplacesText()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();
        mainPage.NameInput.SetText("FirstName");

        // Act
        mainPage.NameInput.SetText("SecondName");

        // Assert
        mainPage.NameInput.AssertTextEquals("SecondName");
    }
}
