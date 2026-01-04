using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for TestableSliderControl.
/// Tests cover value operations, range, percentage, and slider-specific actions.
/// Test IDs: SL-001 to SL-015
/// </summary>
public class SliderControlTests
{
    #region Value Operations (SL-001 to SL-004)

    [Fact]
    public void SL001_GetValue_ReturnsCurrentValue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "volumeSlider");

        // Act
        var value = control.GetValue();

        // Assert
        value.Should().Be(50); // Default value
    }

    [Fact]
    public void SL002_SetValue_UpdatesValue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "volumeSlider");

        // Act
        control.SetValue(75);

        // Assert
        control.GetValue().Should().Be(75);
    }

    [Fact]
    public void SL003_SetValue_ClampsToMaximum()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "volumeSlider");
        control.SetRange(0, 100);

        // Act
        control.SetValue(150); // Above max

        // Assert
        control.GetValue().Should().Be(100);
    }

    [Fact]
    public void SL004_SetValue_ClampsToMinimum()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "volumeSlider");
        control.SetRange(0, 100);

        // Act
        control.SetValue(-20); // Below min

        // Assert
        control.GetValue().Should().Be(0);
    }

    #endregion

    #region Range Operations (SL-005 to SL-007)

    [Fact]
    public void SL005_GetMinimum_ReturnsMinimumValue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(10, 200);

        // Act
        var minimum = control.GetMinimum();

        // Assert
        minimum.Should().Be(10);
    }

    [Fact]
    public void SL006_GetMaximum_ReturnsMaximumValue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(10, 200);

        // Act
        var maximum = control.GetMaximum();

        // Assert
        maximum.Should().Be(200);
    }

    [Fact]
    public void SL007_GetRange_ReturnsBothMinAndMax()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(25, 75);

        // Act
        var (min, max) = control.GetRange();

        // Assert
        min.Should().Be(25);
        max.Should().Be(75);
    }

    #endregion

    #region Percentage Operations (SL-008 to SL-009)

    [Fact]
    public void SL008_GetValuePercent_ReturnsCorrectPercentage()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(0, 100);
        control.SetValue(50);

        // Act
        var percent = control.GetValuePercent();

        // Assert
        percent.Should().Be(0.5);
    }

    [Fact]
    public void SL009_SetValuePercent_SetsValueFromPercentage()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(0, 100);

        // Act
        control.SetValuePercent(0.25);

        // Assert
        control.GetValue().Should().Be(25);
    }

    #endregion

    #region Step Actions (SL-010 to SL-011)

    [Fact]
    public void SL010_Increase_IncreasesValueByStep()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(0, 100);
        control.SetValue(50);

        // Act
        control.Increase();

        // Assert
        control.GetValue().Should().Be(60); // Default step is (max-min)/10 = 10
    }

    [Fact]
    public void SL011_Decrease_DecreasesValueByStep()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(0, 100);
        control.SetValue(50);

        // Act
        control.Decrease();

        // Assert
        control.GetValue().Should().Be(40); // Default step is (max-min)/10 = 10
    }

    #endregion

    #region Slider-Specific Methods (SL-012 to SL-013)

    [Fact]
    public void SL012_SlideToPercent_SetsValueToPercentage()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(0, 100);

        // Act
        control.SlideToPercent(75); // 75%

        // Assert
        control.GetValue().Should().Be(75);
    }

    [Fact]
    public void SL013_SlideLeft_DecreasesValue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(0, 100);
        control.SetValue(50);

        // Act
        control.SlideLeft();

        // Assert
        control.GetValue().Should().BeLessThan(50);
    }

    #endregion

    #region Min/Max Operations (SL-014 to SL-015)

    [Fact]
    public void SL014_SetToMinimum_SetsValueToMin()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(10, 90);
        control.SetValue(50);

        // Act
        control.SetToMinimum();

        // Assert
        control.GetValue().Should().Be(10);
    }

    [Fact]
    public void SL015_SetToMaximum_SetsValueToMax()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableSliderControl(context, "slider");
        control.SetRange(10, 90);
        control.SetValue(50);

        // Act
        control.SetToMaximum();

        // Assert
        control.GetValue().Should().Be(90);
    }

    #endregion
}
