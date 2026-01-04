using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for CheckBoxControl (CB-001 to CB-015).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
[Trait("Category", "Toggle")]
[Trait("Platform", "MAUI")]
public class CheckBoxControlTests
{
    #region Constructor Tests (CB-001 to CB-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void CB001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Assert
        checkBox.Locator.Should().NotBeNull();
        checkBox.Locator.Value.Should().Be("agreeCheckBox");
        checkBox.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("myCheckBox");

        // Act
        var checkBox = new TestableCheckBoxControl(context, locator, null);

        // Assert
        checkBox.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (CB-003 to CB-008)

    [Fact]
    [Trait("Priority", "P0")]
    public void CB003_IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        var exists = checkBox.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB004_IsExists_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriverWrapper);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        var exists = checkBox.IsExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB005_IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        var visible = checkBox.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB006_IsVisible_WhenElementNotDisplayed_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        var visible = checkBox.IsVisible();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CB007_IsEnabled_WhenElementEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        var enabled = checkBox.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CB008_IsEnabled_WhenElementDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        var enabled = checkBox.IsEnabled();

        // Assert
        enabled.Should().BeFalse();
    }

    #endregion

    #region Toggle State Tests (CB-009 to CB-012)

    [Fact]
    [Trait("Priority", "P0")]
    public void CB009_IsChecked_WhenCheckedAttributeTrue_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("checked")).Returns("true");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        var isChecked = checkBox.IsChecked();

        // Assert
        isChecked.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB010_IsChecked_WhenCheckedAttributeFalse_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("checked")).Returns("false");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        var isChecked = checkBox.IsChecked();

        // Assert
        isChecked.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB011_Check_WhenUnchecked_TogglesElement()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.GetAttribute("checked")).Returns(() => clickCount > 0 ? "true" : "false");
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        checkBox.Check();

        // Assert
        clickCount.Should().Be(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB012_Check_WhenAlreadyChecked_DoesNotToggle()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.GetAttribute("checked")).Returns("true");
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        checkBox.Check();

        // Assert
        clickCount.Should().Be(0);
    }

    #endregion

    #region Toggle Actions (CB-013 to CB-015)

    [Fact]
    [Trait("Priority", "P0")]
    public void CB013_Uncheck_WhenChecked_TogglesElement()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.GetAttribute("checked")).Returns(() => clickCount > 0 ? "false" : "true");
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        checkBox.Uncheck();

        // Assert
        clickCount.Should().Be(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB014_Uncheck_WhenAlreadyUnchecked_DoesNotToggle()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.GetAttribute("checked")).Returns("false");
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        checkBox.Uncheck();

        // Assert
        clickCount.Should().Be(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CB015_Toggle_AlwaysClicksElement()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var checkBox = new TestableCheckBoxControl(context, "agreeCheckBox", null);

        // Act
        checkBox.Toggle();

        // Assert
        clickCount.Should().Be(1);
    }

    #endregion
}
