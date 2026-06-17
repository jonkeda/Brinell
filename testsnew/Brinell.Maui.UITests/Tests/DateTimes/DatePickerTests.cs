using System;
using Brinell.Maui.UITests.Pages;
using DateTimeType = System.DateTime;

namespace Brinell.Maui.UITests.Tests.DateTimes;

/// <summary>
/// UI tests for the DatePicker control in the DateTimeTestView.
/// Validates date selection, min/max constraint enforcement, and formatting.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "DatePicker")]
public class DatePickerTests
{
    private readonly MauiFixture _fixture;

    public DatePickerTests(MauiFixture fixture)
    {
        _fixture = fixture;
        // Navigate to DateTime test page if needed
        // For now, assumes the view is accessible via direct route or default navigation

        _fixture.AppShell2.DateTimeContent.Click();
    }

    private DateTimeTestPage GetPage()
    {
        return new(_fixture.Context);
    }

    /// <summary>
    /// Verifies that the DatePicker control exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task DatePicker_IsExists_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestDatePicker.AssertExists();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the DatePicker is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DatePicker_IsVisible_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestDatePicker.AssertVisible();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the DatePicker is enabled.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsEnabled")]
    public Task DatePicker_IsEnabled_ReturnsTrue()
    {
        var page = GetPage();
        // Assert
        page.TestDatePicker.AssertEnabled();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that selecting a date updates the displayed date status.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SetDate")]
    public Task DatePicker_SetDate_UpdatesDisplay()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act & Assert
        page.TestDatePicker.SetDate(DateTimeType.Now.Date.AddDays(5))
            .DateStatusLabel.AssertTextContains("Selected Date");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that an out-of-range date (before minimum) is rejected.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Constraints")]
    public Task DatePicker_DateBeforeMinimum_ShowsValidationError()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);
        var pastDate = DateTimeType.Now.Date.AddDays(-1); // Yesterday (before minimum of today)

        // Act & Assert
        page.TestDatePicker.SetDate(pastDate)
            .StatusLabel.AssertTextContains("before minimum");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that an out-of-range date (after maximum) is rejected.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Constraints")]
    public Task DatePicker_DateAfterMaximum_ShowsValidationError()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);
        var futureDate = DateTimeType.Now.Date.AddDays(31); // 31 days from now (max is 30)

        // Act & Assert
        page.TestDatePicker.SetDate(futureDate)
            .StatusLabel.AssertTextContains("after maximum");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that a valid date within constraints shows success message.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Constraints")]
    public Task DatePicker_DateWithinRange_ShowsSuccess()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);
        var validDate = DateTimeType.Now.Date.AddDays(15); // 15 days from now (within 0-30 range)

        // Act & Assert
        page.TestDatePicker.SetDate(validDate)
            .StatusLabel.AssertTextContains("✓");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the date format displays correctly (e.g., "Monday, January 1, 2025").
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Format")]
    public Task DatePicker_DateFormat_DisplaysCorrectly()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);
        var testDate = DateTimeType.Now.Date.AddDays(10);

        // Act & Assert
        page.TestDatePicker.SetDate(testDate)
            .DateStatusLabel.AssertTextContains(testDate.ToString("dddd, MMMM d, yyyy"));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the Reset button clears the date selection and status.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Reset")]
    public Task DatePicker_Reset_ClearsSelection()
    {
        var page = GetPage();
        // Arrange
        page.IsLoaded(timeoutMs: 5000);

        // Act & Assert
        page.TestDatePicker.SetDate(DateTimeType.Now.Date.AddDays(10))
            .DateStatusLabel.AssertTextContains("Selected Date")
            .ResetButton.Click()
            .StatusLabel.AssertTextContains("Ready");

        return Task.CompletedTask;
    }
}
