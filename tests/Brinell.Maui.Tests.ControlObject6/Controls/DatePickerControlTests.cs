using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for TestableDatePickerControl.
/// Tests cover date operations, range validation, and picker interactions.
/// Test IDs: DP-001 to DP-015
/// </summary>
public class DatePickerControlTests
{
    #region Date Get/Set Operations (DP-001 to DP-004)

    [Fact]
    public void DP001_GetDate_ReturnsCurrentDate()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "birthDatePicker");

        // Act
        var date = control.GetDate();

        // Assert
        date.Should().Be(DateTime.Today); // Default value
    }

    [Fact]
    public void DP002_SetDate_UpdatesDate()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "birthDatePicker");
        var newDate = new DateTime(2024, 6, 15);

        // Act
        control.SetDate(newDate);

        // Assert
        control.GetDate().Should().Be(newDate.Date);
    }

    [Fact]
    public void DP003_SetDate_WithNull_DoesNothing()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        var originalDate = control.GetDate();

        // Act
        control.SetDate(null);

        // Assert
        control.GetDate().Should().Be(originalDate);
    }

    [Fact]
    public void DP004_WaitDate_WhenMatches_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        var targetDate = new DateTime(2024, 3, 21);
        control.SetDate(targetDate);

        // Act
        var result = control.WaitDate(targetDate);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Date Range (DP-005 to DP-007)

    [Fact]
    public void DP005_GetMinDate_ReturnsMinimumDate()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        var minDate = new DateTime(2020, 1, 1);
        control.SetDateRange(minDate, new DateTime(2030, 12, 31));

        // Act
        var result = control.GetMinDate();

        // Assert
        result.Should().Be(minDate);
    }

    [Fact]
    public void DP006_GetMaxDate_ReturnsMaximumDate()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        var maxDate = new DateTime(2030, 12, 31);
        control.SetDateRange(new DateTime(2020, 1, 1), maxDate);

        // Act
        var result = control.GetMaxDate();

        // Assert
        result.Should().Be(maxDate);
    }

    [Fact]
    public void DP007_AssertDateInRange_WhenWithinRange_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        control.SetDate(new DateTime(2024, 6, 15));

        // Act & Assert - Should not throw
        control.AssertDateInRange(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
    }

    #endregion

    #region Assertion Tests (DP-008 to DP-011)

    [Fact]
    public void DP008_AssertDate_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        var testDate = new DateTime(2024, 7, 4);
        control.SetDate(testDate);

        // Act & Assert - Should not throw
        control.AssertDate(testDate);
    }

    [Fact]
    public void DP009_AssertDate_WhenMismatch_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        control.SetDate(new DateTime(2024, 1, 1));

        // Act & Assert
        var action = () => control.AssertDate(new DateTime(2024, 12, 25));
        action.Should().Throw<AssertionException>()
            .WithMessage("*Expected date*");
    }

    [Fact]
    public void DP010_AssertDateInRange_WhenBelowMin_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        control.SetDate(new DateTime(2020, 1, 1));

        // Act & Assert
        var action = () => control.AssertDateInRange(new DateTime(2022, 1, 1), new DateTime(2025, 12, 31));
        action.Should().Throw<AssertionException>()
            .WithMessage("*less than minimum*");
    }

    [Fact]
    public void DP011_AssertDateInRange_WhenAboveMax_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        control.SetDate(new DateTime(2030, 1, 1));

        // Act & Assert
        var action = () => control.AssertDateInRange(new DateTime(2022, 1, 1), new DateTime(2025, 12, 31));
        action.Should().Throw<AssertionException>()
            .WithMessage("*greater than maximum*");
    }

    #endregion

    #region Picker Operations (DP-012 to DP-015)

    [Fact]
    public void DP012_IsPickerOpen_WhenClosed_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");

        // Act
        var isOpen = control.IsPickerOpen();

        // Assert
        isOpen.Should().BeFalse();
    }

    [Fact]
    public void DP013_OpenPicker_SetPickerToOpen()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");

        // Act
        control.OpenPicker();

        // Assert
        control.IsPickerOpen().Should().BeTrue();
    }

    [Fact]
    public void DP014_ClosePicker_SetsPickerToClosed()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");
        control.OpenPicker();

        // Act
        control.ClosePicker();

        // Assert
        control.IsPickerOpen().Should().BeFalse();
    }

    [Fact]
    public void DP015_GetFormat_ReturnsDateFormat()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableDatePickerControl(context, "datePicker");

        // Act
        var format = control.GetFormat();

        // Assert
        format.Should().Be("d");
    }

    #endregion
}
