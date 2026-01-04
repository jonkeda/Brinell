using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for LabelControl (LC-001 to LC-012).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
[Trait("Category", "Clickable")]
[Trait("Platform", "MAUI")]
public class LabelControlTests
{
    #region Constructor Tests (LC-001 to LC-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void LC001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Assert
        label.Locator.Should().NotBeNull();
        label.Locator.Value.Should().Be("titleLabel");
        label.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LC002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("myLabel");

        // Act
        var label = new TestableLabelControl(context, locator, null);

        // Assert
        label.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (LC-003 to LC-008)

    [Fact]
    [Trait("Priority", "P0")]
    public void LC003_IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act
        var exists = label.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LC004_IsExists_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriverWrapper);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act
        var exists = label.IsExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LC005_IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act
        var visible = label.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LC006_IsVisible_WhenElementNotDisplayed_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act
        var visible = label.IsVisible();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LC007_IsEnabled_WhenElementEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act
        var enabled = label.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LC008_IsEnabled_WhenElementDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act
        var enabled = label.IsEnabled();

        // Assert
        enabled.Should().BeFalse();
    }

    #endregion

    #region Text Tests (LC-009 to LC-012)

    [Fact]
    [Trait("Priority", "P0")]
    public void LC009_GetText_ReturnsElementText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Hello World");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act
        var text = label.GetText();

        // Assert
        text.Should().Be("Hello World");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LC010_AssertText_WithMatchingText_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Expected Text");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act & Assert - should not throw
        Action act = () => label.AssertText("Expected Text");
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LC011_AssertTextContains_WithPartialMatch_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Hello World");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act & Assert - should not throw
        Action act = () => label.AssertTextContains("World");
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LC012_AssertTextStartsWith_WithMatchingPrefix_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Hello World");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var label = new TestableLabelControl(context, "titleLabel", null);

        // Act & Assert - should not throw
        Action act = () => label.AssertTextStartsWith("Hello");
        act.Should().NotThrow();
    }

    #endregion
}
