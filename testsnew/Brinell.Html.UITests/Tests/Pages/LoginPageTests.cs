using Brinell.Html;
using Brinell.Html.UITests.PageObjects;
using Brinell.Html.UITests.TestBase;

namespace Brinell.Html.UITests.Tests.Pages;

public sealed class LoginPageTests : BlazorSampleTestBase
{
    [Fact]
    public void Login_ValidCredentials_ShowsSuccessMessage()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.EmailInput.SetText("test@example.com");
        page.PasswordInput.SetText("password123");
        page.LoginButton.Click();

        page.SuccessMessage.WaitVisible(true);
        page.SuccessMessage.AssertTextContaining("Login successful");
    }

    [Fact]
    public void Login_InvalidCredentials_ShowsErrorMessage()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.EmailInput.SetText("invalid@example.com");
        page.PasswordInput.SetText("wrongpw");
        page.LoginButton.Click();

        page.ErrorMessage.WaitVisible(true);
        page.ErrorMessage.AssertTextContaining("Invalid email or password");
    }

    [Fact]
    public async Task Login_ValidCredentials_ShowsSuccessMessage_Async()
    {
        await NavigateToPageAsync("/login");
        var page = new LoginPage(Context);

        await page.EmailInput.SetTextAsync("test@example.com");
        await page.PasswordInput.SetTextAsync("password123");
        await page.LoginButton.ClickAsync();

        await page.SuccessMessage.WaitVisibleAsync(true);
        await page.SuccessMessage.AssertTextContainingAsync("Login successful");
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsErrorMessage_Async()
    {
        await NavigateToPageAsync("/login");
        var page = new LoginPage(Context);

        await page.EmailInput.SetTextAsync("invalid@example.com");
        await page.PasswordInput.SetTextAsync("wrongpw");
        await page.LoginButton.ClickAsync();

        await page.ErrorMessage.WaitVisibleAsync(true);
        await page.ErrorMessage.AssertTextContainingAsync("Invalid email or password");
    }
}