using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for TestableStepperControl.
/// Tests cover increment/decrement operations, value manipulation, and step size.
/// Test IDs: ST-001 to ST-012
/// </summary>
public class StepperControlTests
{
    #region Value Operations (ST-001 to ST-003)

    [Fact]
    public void ST001_GetValue_ReturnsCurrentValue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "quantityStepper");

        // Act
        var value = control.GetValue();

        // Assert
        value.Should().Be(0); // Default value for stepper
    }

    [Fact]
    public void ST002_SetValue_UpdatesValue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "quantityStepper");
        control.SetRange(0, 10);

        // Act
        control.SetValue(5);

        // Assert
        control.GetValue().Should().Be(5);
    }

    [Fact]
    public void ST003_GetIncrement_ReturnsStepSize()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetIncrement(2);

        // Act
        var increment = control.GetIncrement();

        // Assert
        increment.Should().Be(2);
    }

    #endregion

    #region Increment Operations (ST-004 to ST-006)

    [Fact]
    public void ST004_Increment_IncreasesValueByStep()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(0, 10);
        control.SetIncrement(1);
        control.SetValue(3);

        // Act
        control.Increment();

        // Assert
        control.GetValue().Should().Be(4);
    }

    [Fact]
    public void ST005_Increment_DoesNotExceedMaximum()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(0, 5);
        control.SetIncrement(2);
        control.SetValue(4);

        // Act
        control.Increment();

        // Assert
        control.GetValue().Should().Be(5); // Clamped to max
    }

    [Fact]
    public void ST006_IncrementBy_IncrementsMultipleTimes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(0, 20);
        control.SetIncrement(2);
        control.SetValue(0);

        // Act
        control.IncrementBy(3);

        // Assert
        control.GetValue().Should().Be(6); // 0 + (2 * 3)
    }

    #endregion

    #region Decrement Operations (ST-007 to ST-009)

    [Fact]
    public void ST007_Decrement_DecreasesValueByStep()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(0, 10);
        control.SetIncrement(1);
        control.SetValue(5);

        // Act
        control.Decrement();

        // Assert
        control.GetValue().Should().Be(4);
    }

    [Fact]
    public void ST008_Decrement_DoesNotGoBelowMinimum()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(0, 10);
        control.SetIncrement(3);
        control.SetValue(2);

        // Act
        control.Decrement();

        // Assert
        control.GetValue().Should().Be(0); // Clamped to min
    }

    [Fact]
    public void ST009_DecrementBy_DecrementsMultipleTimes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(0, 20);
        control.SetIncrement(2);
        control.SetValue(10);

        // Act
        control.DecrementBy(3);

        // Assert
        control.GetValue().Should().Be(4); // 10 - (2 * 3)
    }

    #endregion

    #region Range Operations (ST-010 to ST-012)

    [Fact]
    public void ST010_GetRange_ReturnsMinAndMax()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(1, 100);

        // Act
        var (min, max) = control.GetRange();

        // Assert
        min.Should().Be(1);
        max.Should().Be(100);
    }

    [Fact]
    public void ST011_SetToMinimum_SetsValueToMin()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(5, 25);
        control.SetValue(15);

        // Act
        control.SetToMinimum();

        // Assert
        control.GetValue().Should().Be(5);
    }

    [Fact]
    public void ST012_SetToMaximum_SetsValueToMax()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableStepperControl(context, "stepper");
        control.SetRange(5, 25);
        control.SetValue(15);

        // Act
        control.SetToMaximum();

        // Assert
        control.GetValue().Should().Be(25);
    }

    #endregion
}
