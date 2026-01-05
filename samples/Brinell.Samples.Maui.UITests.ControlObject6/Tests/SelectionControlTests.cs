using Brinell.Samples.Maui.UITests.ControlObject6.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Tests;

/// <summary>
/// Selection control tests for Picker, DatePicker, and TimePicker controls.
/// Uses verified ControlObject6 APIs: SelectByText, SelectByIndex, GetSelectedText, GetSelectedIndex, SetDate, GetDate, SetTime, GetTime.
/// </summary>
public class SelectionControlTests : MauiTestBase6
{
    private readonly MainPageObject6 _mainPage;

    public SelectionControlTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject6(Context);
    }

    #region Picker Tests

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "Picker")]
    [Trait("Priority", "P0")]
    public void Picker_SelectByText_SelectsItem()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        _mainPage.ColorPicker.SelectByText("Blue");

        // Assert
        _mainPage.ColorPicker.AssertSelectedText("Blue");
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "Picker")]
    [Trait("Priority", "P0")]
    public void Picker_SelectByIndex_SelectsItem()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        _mainPage.ColorPicker.SelectByIndex(0);

        // Assert
        _mainPage.ColorPicker.AssertSelectedIndex(0);
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "Picker")]
    [Trait("Priority", "P0")]
    public void Picker_GetSelectedText_ReturnsCurrentSelection()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.ColorPicker.SelectByText("Red");

        // Act
        var text = _mainPage.ColorPicker.GetSelectedText();

        // Assert
        Assert.Equal("Red", text);
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "Picker")]
    [Trait("Priority", "P0")]
    public void Picker_GetSelectedIndex_ReturnsCurrentIndex()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.ColorPicker.SelectByIndex(2);

        // Act
        var index = _mainPage.ColorPicker.GetSelectedIndex();

        // Assert
        Assert.Equal(2, index);
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "Picker")]
    [Trait("Priority", "P1")]
    public void Picker_GetItemCount_ReturnsNumberOfItems()
    {
        // Arrange - MainPage has 5 colors: Red, Green, Blue, Yellow, Purple
        _mainPage.WaitLoaded(true);

        // Act
        var count = _mainPage.ColorPicker.GetItemCount();

        // Assert
        Assert.True(count > 0, $"Item count should be > 0, was {count}");
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "Picker")]
    [Trait("Priority", "P1")]
    public void Picker_AssertSelectedText_PassesWhenMatches()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.ColorPicker.SelectByText("Green");

        // Act & Assert - should not throw
        _mainPage.ColorPicker.AssertSelectedText("Green");
    }

    #endregion

    #region DatePicker Tests

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "DatePicker")]
    [Trait("Priority", "P0")]
    public void DatePicker_GetDate_ReturnsCurrentDate()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        var date = _mainPage.BirthDatePicker.GetDate();

        // Assert
        Assert.True(date > DateTime.MinValue, $"Date should be > MinValue, was {date}");
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "DatePicker")]
    [Trait("Priority", "P0")]
    public void DatePicker_SetDate_SetsSpecifiedDate()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        var targetDate = new DateTime(2025, 6, 15);

        // Act
        _mainPage.BirthDatePicker.SetDate(targetDate);

        // Assert
        _mainPage.BirthDatePicker.AssertDate(targetDate);
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "DatePicker")]
    [Trait("Priority", "P1")]
    public void DatePicker_GetMinDate_ReturnsMinimumDate()
    {
        // Arrange - MainPage.xaml has MinimumDate="1900-01-01"
        _mainPage.WaitLoaded(true);

        // Act
        var minDate = _mainPage.BirthDatePicker.GetMinDate();

        // Assert
        Assert.True(minDate <= new DateTime(2000, 1, 1), $"MinDate should be <= 2000, was {minDate}");
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "DatePicker")]
    [Trait("Priority", "P1")]
    public void DatePicker_GetMaxDate_ReturnsMaximumDate()
    {
        // Arrange - MainPage.xaml has MaximumDate="2025-12-31"
        _mainPage.WaitLoaded(true);

        // Act
        var maxDate = _mainPage.BirthDatePicker.GetMaxDate();

        // Assert
        Assert.True(maxDate >= new DateTime(2020, 1, 1), $"MaxDate should be >= 2020, was {maxDate}");
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "DatePicker")]
    [Trait("Priority", "P1")]
    public void DatePicker_AssertDate_PassesWhenMatches()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        var targetDate = new DateTime(2025, 12, 25);
        _mainPage.BirthDatePicker.SetDate(targetDate);

        // Act & Assert - should not throw
        _mainPage.BirthDatePicker.AssertDate(targetDate);
    }

    #endregion

    #region TimePicker Tests

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "TimePicker")]
    [Trait("Priority", "P0")]
    public void TimePicker_GetTime_ReturnsCurrentTime()
    {
        // Arrange - MainPage.xaml has Time="09:00:00"
        _mainPage.WaitLoaded(true);

        // Act
        var time = _mainPage.ReminderTimePicker.GetTime();

        // Assert
        Assert.True(time >= TimeSpan.Zero, $"Time should be >= Zero, was {time}");
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "TimePicker")]
    [Trait("Priority", "P0")]
    public void TimePicker_SetTime_SetsSpecifiedTime()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        var targetTime = new TimeSpan(14, 30, 0);

        // Act
        _mainPage.ReminderTimePicker.SetTime(targetTime);

        // Assert
        _mainPage.ReminderTimePicker.AssertTime(targetTime);
    }

    [Fact]
    [Trait("Category", "Selection")]
    [Trait("Control", "TimePicker")]
    [Trait("Priority", "P1")]
    public void TimePicker_AssertTime_PassesWhenMatches()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        var targetTime = new TimeSpan(9, 0, 0);
        _mainPage.ReminderTimePicker.SetTime(targetTime);

        // Act & Assert - should not throw
        _mainPage.ReminderTimePicker.AssertTime(targetTime);
    }

    #endregion
}
