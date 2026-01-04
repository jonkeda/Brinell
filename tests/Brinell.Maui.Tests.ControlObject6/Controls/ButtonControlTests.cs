using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for ButtonControl (BC-001 to BC-008).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
public class ButtonControlTests
{
    [Fact]
    public void Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Assert
        button.Locator.Should().NotBeNull();
        button.Locator.Value.Should().Be("submitBtn");
        button.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("myButton");

        // Act
        var button = new TestableButtonControl(context, locator, null);

        // Assert
        button.Locator.Should().Be(locator);
    }

    [Fact]
    public void BC003_Click_CallsElementClick()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Act
        button.Click();

        // Assert
        mockElement.Verify(e => e.Click(), Times.Once);
    }

    [Fact]
    public void IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Act
        var exists = button.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void IsExists_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriverWrapper);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Act
        var exists = button.IsExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Act
        var visible = button.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    public void IsVisible_WhenElementNotDisplayed_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Act
        var visible = button.IsVisible();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WhenElementEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Act
        var enabled = button.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenElementDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Act
        var enabled = button.IsEnabled();

        // Assert
        enabled.Should().BeFalse();
    }

    [Fact]
    public void GetText_ReturnsElementText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Click Me");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var button = new TestableButtonControl(context, "submitBtn", null);

        // Act
        var text = button.GetText();

        // Assert
        text.Should().Be("Click Me");
    }
}
