using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for SwitchControl (SW-001 to SW-015).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
[Trait("Category", "Toggle")]
[Trait("Platform", "MAUI")]
public class SwitchControlTests
{
    #region Constructor Tests (SW-001 to SW-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void SW001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Assert
        switchControl.Locator.Should().NotBeNull();
        switchControl.Locator.Value.Should().Be("notificationSwitch");
        switchControl.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("mySwitch");

        // Act
        var switchControl = new TestableSwitchControl(context, locator, null);

        // Assert
        switchControl.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (SW-003 to SW-008)

    [Fact]
    [Trait("Priority", "P0")]
    public void SW003_IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        var exists = switchControl.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW004_IsExists_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriverWrapper);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        var exists = switchControl.IsExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW005_IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        var visible = switchControl.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW006_IsVisible_WhenElementNotDisplayed_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        var visible = switchControl.IsVisible();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void SW007_IsEnabled_WhenElementEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        var enabled = switchControl.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void SW008_IsEnabled_WhenElementDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        var enabled = switchControl.IsEnabled();

        // Assert
        enabled.Should().BeFalse();
    }

    #endregion

    #region Toggle State Tests (SW-009 to SW-012)

    [Fact]
    [Trait("Priority", "P0")]
    public void SW009_IsOn_WhenToggleStateOn_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("checked")).Returns("true");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        var isOn = switchControl.IsChecked();

        // Assert
        isOn.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW010_IsOn_WhenToggleStateOff_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("checked")).Returns("false");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        var isOn = switchControl.IsChecked();

        // Assert
        isOn.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW011_TurnOn_WhenOff_TogglesElement()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.GetAttribute("checked")).Returns(() => clickCount > 0 ? "true" : "false");
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        switchControl.Check();

        // Assert
        clickCount.Should().Be(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW012_TurnOn_WhenAlreadyOn_DoesNotToggle()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.GetAttribute("checked")).Returns("true");
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        switchControl.Check();

        // Assert
        clickCount.Should().Be(0);
    }

    #endregion

    #region Toggle Actions (SW-013 to SW-015)

    [Fact]
    [Trait("Priority", "P0")]
    public void SW013_TurnOff_WhenOn_TogglesElement()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.GetAttribute("checked")).Returns(() => clickCount > 0 ? "false" : "true");
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        switchControl.Uncheck();

        // Assert
        clickCount.Should().Be(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW014_TurnOff_WhenAlreadyOff_DoesNotToggle()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.GetAttribute("checked")).Returns("false");
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        switchControl.Uncheck();

        // Assert
        clickCount.Should().Be(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void SW015_Toggle_AlwaysClicksElement()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var switchControl = new TestableSwitchControl(context, "notificationSwitch", null);

        // Act
        switchControl.Toggle();

        // Assert
        clickCount.Should().Be(1);
    }

    #endregion
}
