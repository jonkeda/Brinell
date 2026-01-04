using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for EntryControl (EC-001 to EC-032).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
public class EntryControlTests
{
    [Fact]
    public void Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var entry = new TestableEntryControl(context, "usernameInput", null);

        // Assert
        entry.Locator.Should().NotBeNull();
        entry.Locator.Value.Should().Be("usernameInput");
        entry.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("myInput");

        // Act
        var entry = new TestableEntryControl(context, locator, null);

        // Assert
        entry.Locator.Should().Be(locator);
    }

    #region Text Input Operations (EC-010 to EC-016)

    [Fact]
    public void EC010_Enter_WithNull_DoesNothing()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var entry = new TestableEntryControl(context, "input", null);

        // Act & Assert - should not throw
        Action act = () => entry.Enter(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void EC011_Enter_WithText_ClearsAndSendsKeys()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        entry.Enter("hello");

        // Assert
        mockElement.Verify(e => e.Clear(), Times.Once);
        mockElement.Verify(e => e.SendKeys("hello"), Times.Once);
    }

    [Fact]
    public void EC012_Clear_ClearsElement()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        entry.Clear();

        // Assert
        mockElement.Verify(e => e.Clear(), Times.Once);
    }

    [Fact]
    public void EC013_ClearAndEnter_WithNull_OnlyClears()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        entry.ClearAndEnter(null);

        // Assert
        mockElement.Verify(e => e.Clear(), Times.Once);
        mockElement.Verify(e => e.SendKeys(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void EC014_ClearAndEnter_WithText_ClearsAndTypes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        entry.ClearAndEnter("world");

        // Assert
        mockElement.Verify(e => e.Clear(), Times.Once);
        mockElement.Verify(e => e.SendKeys("world"), Times.Once);
    }

    [Fact]
    public void EC015_Append_WithNull_DoesNothing()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var entry = new TestableEntryControl(context, "input", null);

        // Act & Assert - should not throw
        Action act = () => entry.Append(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void EC016_Append_WithText_TypesWithoutClearing()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        entry.Append("appended");

        // Assert
        mockElement.Verify(e => e.Clear(), Times.Never);
        mockElement.Verify(e => e.SendKeys("appended"), Times.Once);
    }

    #endregion

    #region State Methods

    [Fact]
    public void IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        var exists = entry.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        var visible = entry.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    public void GetText_ReturnsElementText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Input Value");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        var text = entry.GetText();

        // Assert
        text.Should().Be("Input Value");
    }

    [Fact]
    public void GetTextLength_ReturnsCorrectLength()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Hello");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var entry = new TestableEntryControl(context, "input", null);

        // Act
        var length = entry.GetTextLength();

        // Assert
        length.Should().Be(5);
    }

    #endregion
}
