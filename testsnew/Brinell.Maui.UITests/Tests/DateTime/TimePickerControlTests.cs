using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.DateTime;

/// <summary>
/// UI tests for TimePicker verifying time selection operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "TimePicker")]
public class TimePickerControlTests
{
    private readonly AppiumFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public TimePickerControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that time picker exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task TimePicker_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.PreferredTimePicker.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that time picker is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task TimePicker_IsVisible_ReturnsTrue()
    {
        Page.PreferredTimePicker.ScrollIntoView();

        // Assert
        Assert.True(Page.PreferredTimePicker.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region Time Value Tests

    /// <summary>
    /// Verifies GetTime() returns current time value.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetTime")]
    public Task TimePicker_GetTime_ReturnsTime()
    {
        // Act
        var time = Page.PreferredTimePicker.GetTime();

        // Assert - time should be valid TimeSpan
        Assert.True(time.HasValue, "Time should not be null");
        Assert.True(time!.Value.Hours >= 0 && time.Value.Hours <= 23);
        Assert.True(time.Value.Minutes >= 0 && time.Value.Minutes <= 59);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies SetTime() changes the time.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "SetTime")]
    public Task TimePicker_SetTime_ChangesTime()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Arrange
        var targetTime = new TimeSpan(14, 30, 0); // 2:30 PM

        // Act
        Page.PreferredTimePicker.SetTime(targetTime);

        // Assert
        var time = Page.PreferredTimePicker.GetTime();
        Assert.True(time.HasValue, "Time should not be null after set");
        Assert.Equal(14, time!.Value.Hours);
        Assert.Equal(30, time.Value.Minutes);
        return Task.CompletedTask;
    }

    #endregion

    #region Component Tests

    /// <summary>
    /// Verifies GetHours() returns current hours.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "GetHours")]
    public Task TimePicker_GetHours_ReturnsHours()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Arrange
        Page.PreferredTimePicker.SetTime(new TimeSpan(10, 0, 0));

        // Act
        var hours = Page.PreferredTimePicker.GetHours();

        // Assert
        Assert.Equal(10, hours);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies GetMinutes() returns current minutes.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "GetMinutes")]
    public Task TimePicker_GetMinutes_ReturnsMinutes()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Arrange
        Page.PreferredTimePicker.SetTime(new TimeSpan(0, 45, 0));

        // Act
        var minutes = Page.PreferredTimePicker.GetMinutes();

        // Assert
        Assert.Equal(45, minutes);
        return Task.CompletedTask;
    }

    #endregion

    #region Assertion Tests

    /// <summary>
    /// Verifies AssertTime passes with correct time.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "AssertTime")]
    public Task TimePicker_AssertTime_PassesWithCorrectTime()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Arrange
        var targetTime = new TimeSpan(9, 15, 0);
        Page.PreferredTimePicker.SetTime(targetTime);

        // Assert - no exception means success
        Page.PreferredTimePicker.AssertTime(targetTime);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies AssertTime with tolerance passes for close times.
    /// </summary>
    [Fact(Timeout = TestConstants.ShortTestTimeoutMs)]
    [Trait("Method", "AssertTime")]
    public Task TimePicker_AssertTime_PassesWithTolerance()
    {
        if (OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Arrange
        var targetTime = new TimeSpan(16, 0, 0);
        Page.PreferredTimePicker.SetTime(targetTime);

        // Assert - with tolerance, close times should pass
        var slightlyDifferent = new TimeSpan(16, 0, 30); // 30 seconds off
        Page.PreferredTimePicker.AssertTime(slightlyDifferent, toleranceSeconds: 60);
        return Task.CompletedTask;
    }

    #endregion
}
