using Brinell.Html;
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

    [Fact]
    public async Task LoginFlow_ValidCredentials_ShowsSuccessMessage_Async()
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
    public async Task LoginFlow_EmptyCredentials_ShowsError_Async()
    {
        await NavigateToPageAsync("/login");
        var page = new LoginPage(Context);

        await page.LoginButton.ClickAsync();

        var validationMessages = Context.FindElements(Locator.ByCss(".text-danger"));
        Assert.NotEmpty(validationMessages);
    }
}