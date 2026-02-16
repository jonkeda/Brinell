using Brinell.Html.UITests.PageObjects;
using Brinell.Html.UITests.TestBase;

namespace Brinell.Html.UITests.Tests.Scenarios;

public sealed class LoginFlowTests : BlazorSampleTestBase
{
    [Fact]
    public void LoginFlow_ValidCredentials_ShowsSuccessMessage()
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
    public void LoginFlow_EmptyCredentials_ShowsError()
    {
        NavigateToPage("/login");
        var page = new LoginPage(Context);

        page.LoginButton.Click();

        var validationMessages = Context.FindElements(Locator.ByCss(".text-danger"));
        Assert.NotEmpty(validationMessages);
    }
}