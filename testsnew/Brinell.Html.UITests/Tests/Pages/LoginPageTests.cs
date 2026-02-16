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
        page.PasswordInput.SetText("wrong");
        page.LoginButton.Click();

        page.ErrorMessage.WaitVisible(true);
        page.ErrorMessage.AssertTextContaining("Invalid email or password");
    }
}