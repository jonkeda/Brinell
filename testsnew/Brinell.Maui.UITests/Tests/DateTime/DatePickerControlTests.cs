using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.DateTime;

/// <summary>
/// UI tests for DatePicker verifying date selection operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "DatePicker")]
public class DatePickerControlTests
{
    private readonly AppiumFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public DatePickerControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that date picker exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task DatePicker_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.BirthDatePicker.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that date picker is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DatePicker_IsVisible_ReturnsTrue()
    {
        Page.BirthDatePicker.ScrollIntoView();

        // Assert
        Assert.True(Page.BirthDatePicker.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region Date Value Tests

    /// <summary>
    /// Verifies GetDate() returns current date value.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetDate")]
    public Task DatePicker_GetDate_ReturnsDate()
    {
        // Act
        var date = Page.BirthDatePicker.GetDate();

        // Assert - date should be within valid range
        Assert.True(date.HasValue, "Date should not be null");
        Assert.True(date!.Value.Year >= 1900 && date.Value.Year <= 2010);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies SetDate() changes the date.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "SetDate")]
    public Task DatePicker_SetDate_ChangesDate()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Arrange
        var targetDate = new System.DateTime(1990, 6, 15);

        // Act
        Page.BirthDatePicker.SetDate(targetDate);

        // Assert
        var date = Page.BirthDatePicker.GetDate();
        Assert.True(date.HasValue, "Date should not be null after set");
        Assert.Equal(1990, date!.Value.Year);
        Assert.Equal(6, date.Value.Month);
        Assert.Equal(15, date.Value.Day);
        return Task.CompletedTask;
    }

    #endregion

    #region Assertion Tests

    /// <summary>
    /// Verifies AssertDate passes with correct date.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "AssertDate")]
    public Task DatePicker_AssertDate_PassesWithCorrectDate()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Arrange
        var targetDate = new System.DateTime(1985, 3, 20);
        Page.BirthDatePicker.SetDate(targetDate);

        // Assert - no exception means success
        Page.BirthDatePicker.AssertDate(targetDate);
        return Task.CompletedTask;
    }

    #endregion

    #region Boundary Tests

    /// <summary>
    /// Verifies GetMinimumDate() returns configured minimum.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetMinimumDate")]
    public Task DatePicker_GetMinimumDate_ReturnsMin()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Act
        var minDate = Page.BirthDatePicker.GetMinimumDate();

        // Assert - configured min is 1900-01-01
        Assert.True(minDate.HasValue, "MinimumDate should be available");
        Assert.Equal(1900, minDate!.Value.Year);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies GetMaximumDate() returns configured maximum.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetMaximumDate")]
    public Task DatePicker_GetMaximumDate_ReturnsMax()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Act
        var maxDate = Page.BirthDatePicker.GetMaximumDate();

        // Assert - configured max is 2010-12-31
        Assert.True(maxDate.HasValue, "MaximumDate should be available");
        Assert.Equal(2010, maxDate!.Value.Year);
        return Task.CompletedTask;
    }

    #endregion
}
