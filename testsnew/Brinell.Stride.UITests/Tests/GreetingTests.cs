using Brinell.Stride.UITests.PageObjects;

namespace Brinell.Stride.UITests.Tests;

public class GreetingTests : StrideUITestBase
{
    public GreetingTests(StrideAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output) { }

    [Fact]
    public void GreetButton_WithName_DisplaysGreeting()
    {
        var mainPage = new MainPage(Context);
        mainPage.AssertLoaded(true);

        mainPage.Greet("Alice");

        mainPage.GreetingDisplay.AssertText("Hello, Alice!");
    }

    [Fact]
    public void GreetButton_WithEmptyName_DisplaysDefaultGreeting()
    {
        var mainPage = new MainPage(Context);
        mainPage.AssertLoaded(true);

        mainPage.NameInput.SetText("");
        mainPage.GreetButton.Click();

        mainPage.GreetingDisplay.AssertText("Hello, World!");
    }

    [Fact]
    public void NameInput_EnterText_DisplaysInField()
    {
        var mainPage = new MainPage(Context);
        mainPage.AssertLoaded(true);

        mainPage.NameInput.SetText("Bob");

        mainPage.NameInput.AssertText("Bob");
    }

    [Fact]
    public void NameInput_ClearAndEnter_ReplacesText()
    {
        var mainPage = new MainPage(Context);
        mainPage.AssertLoaded(true);
        mainPage.NameInput.SetText("FirstName");

        mainPage.NameInput.SetText("NewName");

        mainPage.NameInput.AssertText("NewName");
    }
}
