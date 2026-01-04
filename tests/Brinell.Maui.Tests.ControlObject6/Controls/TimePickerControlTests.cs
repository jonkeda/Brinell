using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for TestableTimePickerControl.
/// Tests cover time operations, range validation, and picker interactions.
/// Test IDs: TP-001 to TP-012
/// </summary>
public class TimePickerControlTests
{
    #region Time Get/Set Operations (TP-001 to TP-004)

    [Fact]
    public void TP001_GetTime_ReturnsCurrentTime()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "alarmTimePicker");

        // Act
        var time = control.GetTime();

        // Assert
        time.Should().Be(TimeSpan.FromHours(12)); // Default value
    }

    [Fact]
    public void TP002_SetTime_UpdatesTime()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "alarmTimePicker");
        var newTime = new TimeSpan(14, 30, 0); // 2:30 PM

        // Act
        control.SetTime(newTime);

        // Assert
        control.GetTime().Should().Be(newTime);
    }

    [Fact]
    public void TP003_SetTime_WithNull_DoesNothing()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        var originalTime = control.GetTime();

        // Act
        control.SetTime(null);

        // Assert
        control.GetTime().Should().Be(originalTime);
    }

    [Fact]
    public void TP004_WaitTime_WhenMatches_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        var targetTime = new TimeSpan(9, 30, 0);
        control.SetTime(targetTime);

        // Act
        var result = control.WaitTime(targetTime);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Time Range (TP-005 to TP-007)

    [Fact]
    public void TP005_GetMinTime_ReturnsMinimumTime()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        var minTime = new TimeSpan(8, 0, 0);
        control.SetTimeRange(minTime, new TimeSpan(18, 0, 0));

        // Act
        var result = control.GetMinTime();

        // Assert
        result.Should().Be(minTime);
    }

    [Fact]
    public void TP006_GetMaxTime_ReturnsMaximumTime()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        var maxTime = new TimeSpan(18, 0, 0);
        control.SetTimeRange(new TimeSpan(8, 0, 0), maxTime);

        // Act
        var result = control.GetMaxTime();

        // Assert
        result.Should().Be(maxTime);
    }

    [Fact]
    public void TP007_AssertTimeInRange_WhenWithinRange_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        control.SetTime(new TimeSpan(12, 0, 0)); // Noon

        // Act & Assert - Should not throw
        control.AssertTimeInRange(new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0));
    }

    #endregion

    #region Assertion Tests (TP-008 to TP-010)

    [Fact]
    public void TP008_AssertTime_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        var testTime = new TimeSpan(15, 45, 0);
        control.SetTime(testTime);

        // Act & Assert - Should not throw
        control.AssertTime(testTime);
    }

    [Fact]
    public void TP009_AssertTime_WhenMismatch_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        control.SetTime(new TimeSpan(9, 0, 0));

        // Act & Assert
        var action = () => control.AssertTime(new TimeSpan(17, 0, 0));
        action.Should().Throw<AssertionException>()
            .WithMessage("*Expected time*");
    }

    [Fact]
    public void TP010_AssertTimeInRange_WhenOutOfRange_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        control.SetTime(new TimeSpan(6, 0, 0)); // 6 AM - outside range

        // Act & Assert
        var action = () => control.AssertTimeInRange(new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0));
        action.Should().Throw<AssertionException>()
            .WithMessage("*less than minimum*");
    }

    #endregion

    #region Picker Operations (TP-011 to TP-012)

    [Fact]
    public void TP011_OpenPicker_SetsPickerToOpen()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");

        // Act
        control.OpenPicker();

        // Assert
        control.IsPickerOpen().Should().BeTrue();
    }

    [Fact]
    public void TP012_ClosePicker_SetsPickerToClosed()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableTimePickerControl(context, "timePicker");
        control.OpenPicker();

        // Act
        control.ClosePicker();

        // Assert
        control.IsPickerOpen().Should().BeFalse();
    }

    #endregion
}
