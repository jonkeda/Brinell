using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for the Validation page form validation functionality.
/// </summary>
[Collection("BlazorUITests")]
public class ValidationTests : BlazorSampleTestBase
{
    public ValidationTests(ITestOutputHelper output) : base(output)
    {
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGE DISPLAY TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Validation_InitialLoad_DisplaysForm()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/validation");

        var validationPage = new ValidationPage(Context!);
        validationPage.WaitForDisplayed();

        // Assert
        validationPage.AssertDisplayed("Validation page should be displayed");
        validationPage.ValidationTitle.AssertVisible("Title should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // REQUIRED FIELD TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Validation_RequiredField_ShowsError_WhenEmpty()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/validation");

        var validationPage = new ValidationPage(Context!);
        validationPage.WaitForDisplayed();

        // Act - Click validate without filling required field
        validationPage.Validate();

        // Assert
        validationPage.RequiredError.AssertVisible("Required error should be visible");
    }

    [Fact]
    public void Validation_RequiredField_AcceptsInput()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/validation");

        var validationPage = new ValidationPage(Context!);
        validationPage.WaitForDisplayed();

        // Act
        validationPage.FillRequiredField("Valid input");

        // Assert
        validationPage.RequiredInput.AssertTextEquals("Valid input");
    }

    // ═══════════════════════════════════════════════════════════════
    // EMAIL FIELD TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Validation_EmailField_ValidatesFormat()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/validation");

        var validationPage = new ValidationPage(Context!);
        validationPage.WaitForDisplayed();

        // Act - Enter invalid email
        validationPage.FillEmailField("invalid-email");
        validationPage.Validate();

        // Assert
        validationPage.EmailError.AssertVisible("Email error should be visible for invalid format");
    }

    [Fact]
    public void Validation_EmailField_AcceptsValidEmail()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/validation");

        var validationPage = new ValidationPage(Context!);
        validationPage.WaitForDisplayed();

        // Act
        validationPage.FillEmailField("test@example.com");

        // Assert
        validationPage.EmailInput.AssertTextEquals("test@example.com");
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM ACTION TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Validation_ClearButton_ClearsForm()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/validation");

        var validationPage = new ValidationPage(Context!);
        validationPage.WaitForDisplayed();
        validationPage.FillRequiredField("Some text");

        // Act
        validationPage.Clear();

        // Assert
        validationPage.RequiredInput.AssertExists("Required input should still exist");
    }

    [Fact]
    public void Validation_SubmitButton_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/validation");

        var validationPage = new ValidationPage(Context!);
        validationPage.WaitForDisplayed();

        // Assert
        validationPage.SubmitButton.AssertVisible("Submit button should be visible");
        validationPage.ValidateButton.AssertVisible("Validate button should be visible");
        validationPage.ClearButton.AssertVisible("Clear button should be visible");
    }
}
