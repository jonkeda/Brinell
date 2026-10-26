using Bruinet.Blazor.UITests.PageObjects;
using Bruinet.Blazor.UITests.TestBase;

namespace Bruinet.Blazor.UITests.Tests.Pages;

// The page object the Scraper made from a recording (AD-027): its locators find the form.
public sealed class UserFormPageTests : BlazorSampleTestBase
{
    [Fact]
    public void UserForm_FillPersonalInformation_KeepsTheValues()
    {
        NavigateToPage("/userform");
        var page = new UserFormPage(Context);

        page.Title.AssertText("User Registration");
        page.FirstName.SetText("Ada");
        page.LastName.SetText("Lovelace");
        page.Email.SetText("ada@example.com");

        page.FirstName.AssertValue("Ada");
        page.LastName.AssertValue("Lovelace");
        page.Email.AssertValue("ada@example.com");
    }

    [Fact]
    public void UserForm_ChooseOptions_ShowsThemChosen()
    {
        NavigateToPage("/userform");
        var page = new UserFormPage(Context);

        page.Terms.Check();
        page.Professional.Select();
        page.Country.SelectByText("Canada");

        page.Terms.AssertChecked();
        page.Professional.AssertChecked();
        page.Basic.AssertChecked(false);
        page.Country.AssertSelectedText("Canada");
    }
}
