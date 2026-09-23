using Brinell.Blazor.UITests.PageObjects;
using Brinell.Blazor.UITests.TestBase;

namespace Brinell.Blazor.UITests.Tests.Controls;

public sealed class TextInputTests : BlazorSampleTestBase
{
    [Fact]
    public void Input_SetText_SetsValue()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.UsernameInput.SetText("testuser");

        Assert.Equal("testuser", page.UsernameInput.GetValue());
    }

    [Fact]
    public void Input_Clear_RemovesText()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.UsernameInput.SetText("some text");
        page.UsernameInput.Clear();

        Assert.Equal(string.Empty, page.UsernameInput.GetValue());
    }

    [Fact]
    public void Input_SetText_ReplacesExistingValue()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.UsernameInput.SetText("old text");
        page.UsernameInput.SetText("new text");

        Assert.Equal("new text", page.UsernameInput.GetValue());
    }

    [Fact]
    public void Input_TypeText_AppendsCharacters()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.UsernameInput.SetText(string.Empty);
        page.UsernameInput.TypeText("sample-user");

        Assert.Equal("sample-user", page.UsernameInput.GetValue());
    }

    [Fact]
    public void Input_IsVisible_ReturnsTrueForVisibleInput()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        Assert.True(page.UsernameInput.IsVisible());
    }

    [Fact]
    public void Input_IsEnabled_ReturnsTrueForEnabledInput()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        Assert.True(page.UsernameInput.IsEnabled());
    }

    [Fact]
    public void Input_GetValue_ReturnsCurrentText()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.UsernameInput.SetText("TestValue");

        Assert.Equal("TestValue", page.UsernameInput.GetValue());
    }

    [Fact]
    public void Input_AssertTextContaining_MatchesPartial()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.UsernameInput.SetText("Hello World");

        Assert.Contains("World", page.UsernameInput.GetValue());
    }
}
