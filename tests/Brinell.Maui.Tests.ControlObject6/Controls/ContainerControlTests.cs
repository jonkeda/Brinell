using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for Frame and Border container controls (FR-001 to FR-004, BO-001 to BO-004).
/// </summary>
[Trait("Category", "Container")]
[Trait("Platform", "MAUI")]
[Trait("Priority", "P2")]
public class ContainerControlTests
{
    #region Frame Constructor Tests (FR-001 to FR-002)

    [Fact]
    public void FR001_Frame_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        // Act
        var frame = new TestableFrameControl(context, "cardFrame", null);

        // Assert
        frame.Locator.Should().NotBeNull();
        frame.Locator.Value.Should().Be("cardFrame");
        frame.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void FR002_Frame_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);
        var locator = By.AutomationId("myFrame");

        // Act
        var frame = new TestableFrameControl(context, locator, null);

        // Assert
        frame.Locator.Should().Be(locator);
    }

    #endregion

    #region Frame Child Count Tests (FR-003 to FR-004)

    [Fact]
    public void FR003_Frame_GetChildCount_ReturnsCount()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var frame = new TestableFrameControl(context, "cardFrame", null);
        frame.SetChildCount(3);

        // Act
        var count = frame.GetChildCount();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public void FR004_Frame_AssertChildCount_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var frame = new TestableFrameControl(context, "cardFrame", null);
        frame.SetChildCount(2);

        // Act & Assert - should not throw
        frame.Invoking(f => f.AssertChildCount(2)).Should().NotThrow();
    }

    #endregion

    #region Border Constructor Tests (BO-001 to BO-002)

    [Fact]
    public void BO001_Border_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        // Act
        var border = new TestableBorderControl(context, "contentBorder", null);

        // Assert
        border.Locator.Should().NotBeNull();
        border.Locator.Value.Should().Be("contentBorder");
        border.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void BO002_Border_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);
        var locator = By.AutomationId("myBorder");

        // Act
        var border = new TestableBorderControl(context, locator, null);

        // Assert
        border.Locator.Should().Be(locator);
    }

    #endregion

    #region Border Child Count Tests (BO-003 to BO-004)

    [Fact]
    public void BO003_Border_GetChildCount_ReturnsCount()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var border = new TestableBorderControl(context, "contentBorder", null);
        border.SetChildCount(1);

        // Act
        var count = border.GetChildCount();

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public void BO004_Border_AssertChildCount_WhenMismatch_Throws()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var border = new TestableBorderControl(context, "contentBorder", null);
        border.SetChildCount(1);

        // Act & Assert - should throw
        border.Invoking(b => b.AssertChildCount(5)).Should().Throw<Exception>();
    }

    #endregion
}
