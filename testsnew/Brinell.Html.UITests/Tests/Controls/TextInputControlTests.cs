using Brinell.Html;
using Brinell.Html.UITests.PageObjects;
using Brinell.Html.UITests.TestBase;

namespace Brinell.Html.UITests.Tests.Controls;

public sealed class TextInputControlTests : BlazorSampleTestBase
{
    [Fact]
    public void TextInput_SetTextAndGetValue_RoundTripsValue()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.EmailInput.SetText("hello@example.com");

        Assert.Equal("hello@example.com", page.EmailInput.GetValue());
    }

    [Fact]
    public void TextInput_Clear_RemovesText()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.EmailInput.SetText("temp@example.com");
        page.EmailInput.Clear();

        Assert.Equal(string.Empty, page.EmailInput.GetValue());
    }

    [Fact]
    public void TextInput_TypeText_AppendsTypedCharacters()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.UsernameInput.SetText(string.Empty);
        page.UsernameInput.TypeText("sample-user");

        Assert.Equal("sample-user", page.UsernameInput.GetValue());
    }

    [Fact]
    public async Task TextInput_SetTextAndGetValue_RoundTripsValue_Async()
    {
        await NavigateToPageAsync("/login");
        var page = new LoginPage(Context);

        await page.EmailInput.SetTextAsync("hello@example.com");

        Assert.Equal("hello@example.com", await page.EmailInput.GetValueAsync());
    }

    [Fact]
    public async Task TextInput_Clear_RemovesText_Async()
    {
        await NavigateToPageAsync("/login");
        var page = new LoginPage(Context);

        await page.EmailInput.SetTextAsync("temp@example.com");
        await page.EmailInput.ClearAsync();

        Assert.Equal(string.Empty, await page.EmailInput.GetValueAsync());
    }

    [Fact]
    public async Task TextInput_TypeText_AppendsTypedCharacters_Async()
    {
        await NavigateToPageAsync("/login");
        var page = new LoginPage(Context);

        await page.UsernameInput.SetTextAsync(string.Empty);
        await page.UsernameInput.TypeTextAsync("sample-user");

        Assert.Equal("sample-user", await page.UsernameInput.GetValueAsync());
    }
}