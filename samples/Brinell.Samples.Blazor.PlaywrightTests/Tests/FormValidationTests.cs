using Brinell.Samples.Blazor.PlaywrightTests.PageObjects;
using Brinell.Samples.Blazor.PlaywrightTests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.PlaywrightTests.Tests;

/// <summary>
/// Tests for form validation controls on the Validation page.
/// Validates IValidatableControl behavior and validation error display.
/// </summary>
public class FormValidationTests : BlazorPlaywrightTestBase
{
    public FormValidationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task RequiredField_Empty_ShowsValidationError()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // Act - Leave required field empty and trigger validation
        await page.RequiredInput.ClearAsync();
        await page.ValidateButton.ClickAsync();

        // Assert - Use sync methods on controls (they work in async context)
        page.RequiredError.WaitVisible();
        page.RequiredError.AssertTextContains("required");
    }

    [Fact]
    public async Task RequiredField_WithValue_NoError()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // Act - Enter value and validate
        await page.RequiredInput.ClearAndEnterAsync("Test Value");
        await page.ValidateButton.ClickAsync();

        // Assert - Error should not be visible
        page.RequiredError.WaitNotVisible();
    }

    [Fact]
    public async Task EmailField_InvalidFormat_ShowsError()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // Act - Enter invalid email
        await page.EmailInput.ClearAndEnterAsync("invalid-email");
        await page.ValidateButton.ClickAsync();

        // Assert
        page.EmailError.WaitVisible();
        page.EmailError.AssertTextContains("email");
    }

    [Fact]
    public async Task EmailField_ValidFormat_NoError()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // Act - Enter valid email
        await page.EmailInput.ClearAndEnterAsync("test@example.com");
        await page.ValidateButton.ClickAsync();

        // Assert
        page.EmailError.WaitNotVisible();
    }

    [Fact]
    public async Task MinLengthField_TooShort_ShowsError()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // Act - Enter text shorter than minimum
        await page.MinLengthInput.ClearAndEnterAsync("ab");
        await page.ValidateButton.ClickAsync();

        // Assert
        page.MinLengthError.WaitVisible();
        page.MinLengthError.AssertTextContains("5");
    }

    [Fact]
    public async Task ClearButton_ClearsErrorsAndShowsMessage()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // First trigger some errors by validating empty required field
        await page.ValidateButton.ClickAsync();
        page.ValidationSummary.WaitVisible();

        // Act - Clear form
        await page.ClearButton.ClickAsync();

        // Assert - Errors should be cleared and success message shown
        page.ValidationSummary.WaitNotVisible();
    }

    [Fact]
    public async Task SubmitButton_AllValid_ShowsSuccess()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // Fill all required fields with valid data
        await page.RequiredInput.ClearAndEnterAsync("Test Name");
        await page.EmailInput.ClearAndEnterAsync("test@example.com");
        await page.MinLengthInput.ClearAndEnterAsync("hello");
        await page.PasswordInput.ClearAndEnterAsync("password123");
        await page.ConfirmPasswordInput.ClearAndEnterAsync("password123");

        // Act - Submit
        await page.SubmitButton.ClickAsync();

        // Assert - Success message should appear
        page.SuccessMessage.WaitVisible();
        page.SuccessMessage.AssertTextContains("successfully");
    }

    [Fact]
    public async Task PasswordMismatch_ShowsError()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // Act - Enter mismatched passwords
        await page.PasswordInput.ClearAndEnterAsync("password123");
        await page.ConfirmPasswordInput.ClearAndEnterAsync("differentpassword");
        await page.ValidateButton.ClickAsync();

        // Assert
        page.ConfirmPasswordError.WaitVisible();
        page.ConfirmPasswordError.AssertTextContains("match");
    }

    [Fact]
    public async Task RangeField_OutOfRange_ShowsError()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/validation");
        var page = new ValidationPage(Context);
        await page.WaitForDisplayedAsync();

        // Act - Enter value outside range
        await page.RangeInput.ClearAndEnterAsync("150");
        await page.ValidateButton.ClickAsync();

        // Assert
        page.RangeError.WaitVisible();
        page.RangeError.AssertTextContains("between");
    }
}
