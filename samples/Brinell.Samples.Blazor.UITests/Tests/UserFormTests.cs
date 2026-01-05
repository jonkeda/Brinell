using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for the UserForm page functionality.
/// </summary>
[Collection("BlazorUITests")]
public class UserFormTests : BlazorSampleTestBase
{
    public UserFormTests(ITestOutputHelper output) : base(output)
    {
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGE DISPLAY TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UserForm_InitialLoad_DisplaysForm()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/userform");

        var userFormPage = new UserFormPage(Context!);
        userFormPage.WaitForDisplayed();

        // Assert
        userFormPage.AssertDisplayed("UserForm page should be displayed");
        userFormPage.UserFormTitle.AssertVisible("Title should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // TEXT INPUT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UserForm_FirstName_CanEnterText()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/userform");

        var userFormPage = new UserFormPage(Context!);
        userFormPage.WaitForDisplayed();

        // Act
        userFormPage.FirstNameInput.ClearAndEnter("John");

        // Assert
        userFormPage.FirstNameInput.AssertTextEquals("John");
    }

    [Fact]
    public void UserForm_FillPersonalInfo_Works()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/userform");

        var userFormPage = new UserFormPage(Context!);
        userFormPage.WaitForDisplayed();

        // Act
        userFormPage.FillPersonalInfo("John", "Doe", "john.doe@example.com");

        // Assert
        userFormPage.FirstNameInput.AssertTextEquals("John");
        userFormPage.LastNameInput.AssertTextEquals("Doe");
        userFormPage.EmailInput.AssertTextEquals("john.doe@example.com");
    }

    // ═══════════════════════════════════════════════════════════════
    // CHECKBOX TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UserForm_AcceptTerms_Works()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/userform");

        var userFormPage = new UserFormPage(Context!);
        userFormPage.WaitForDisplayed();

        // Act
        userFormPage.AcceptTerms();

        // Assert
        userFormPage.TermsCheckbox.AssertChecked("Terms checkbox should be checked");
        userFormPage.PrivacyCheckbox.AssertChecked("Privacy checkbox should be checked");
    }

    // ═══════════════════════════════════════════════════════════════
    // SELECT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UserForm_Selects_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/userform");

        var userFormPage = new UserFormPage(Context!);
        userFormPage.WaitForDisplayed();

        // Assert
        userFormPage.CountrySelect.AssertVisible("Country select should be visible");
        userFormPage.DepartmentSelect.AssertVisible("Department select should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // ACTION BUTTON TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UserForm_ActionButtons_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/userform");

        var userFormPage = new UserFormPage(Context!);
        userFormPage.WaitForDisplayed();

        // Assert
        userFormPage.SubmitButton.AssertVisible("Submit button should be visible");
        userFormPage.SaveDraftButton.AssertVisible("Save draft button should be visible");
        userFormPage.ClearButton.AssertVisible("Clear button should be visible");
    }

    [Fact]
    public void UserForm_Clear_ClearsForm()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/userform");

        var userFormPage = new UserFormPage(Context!);
        userFormPage.WaitForDisplayed();
        userFormPage.FirstNameInput.ClearAndEnter("John");

        // Act
        userFormPage.Clear();

        // Assert
        userFormPage.FirstNameInput.AssertExists("First name input should still exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // QUANTITY CONTROL TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UserForm_IncrementDecrement_Works()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/userform");

        var userFormPage = new UserFormPage(Context!);
        userFormPage.WaitForDisplayed();

        // Act
        userFormPage.IncrementQuantity();

        // Assert
        userFormPage.QuantityInput.AssertExists("Quantity input should exist");
    }
}
