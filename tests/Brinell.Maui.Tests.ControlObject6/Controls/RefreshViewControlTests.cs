using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for RefreshViewControl (RV-001 to RV-008).
/// </summary>
[Trait("Category", "Container")]
[Trait("Platform", "MAUI")]
[Trait("Priority", "P2")]
public class RefreshViewControlTests
{
    #region Constructor Tests (RV-001 to RV-002)

    [Fact]
    public void RV001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        // Act
        var refreshView = new TestableRefreshViewControl(context, "pullToRefresh", null);

        // Assert
        refreshView.Locator.Should().NotBeNull();
        refreshView.Locator.Value.Should().Be("pullToRefresh");
        refreshView.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void RV002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);
        var locator = By.AutomationId("myRefreshView");

        // Act
        var refreshView = new TestableRefreshViewControl(context, locator, null);

        // Assert
        refreshView.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (RV-003 to RV-004)

    [Fact]
    public void RV003_IsRefreshing_WhenNotRefreshing_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var refreshView = new TestableRefreshViewControl(context, "pullToRefresh", null);
        refreshView.SetRefreshing(false);

        // Act
        var isRefreshing = refreshView.IsRefreshing();

        // Assert
        isRefreshing.Should().BeFalse();
    }

    [Fact]
    public void RV004_IsRefreshing_WhenRefreshing_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var refreshView = new TestableRefreshViewControl(context, "pullToRefresh", null);
        refreshView.SetRefreshing(true);

        // Act
        var isRefreshing = refreshView.IsRefreshing();

        // Assert
        isRefreshing.Should().BeTrue();
    }

    #endregion

    #region Action Tests (RV-005 to RV-006)

    [Fact]
    public void RV005_Refresh_StartsRefreshing()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var refreshView = new TestableRefreshViewControl(context, "pullToRefresh", null);
        refreshView.SetRefreshing(false);

        // Act
        refreshView.Refresh();

        // Assert
        refreshView.IsRefreshing().Should().BeTrue();
    }

    [Fact]
    public void RV006_WaitRefreshComplete_StopsRefreshing()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var refreshView = new TestableRefreshViewControl(context, "pullToRefresh", null);
        refreshView.SetRefreshing(true);

        // Act
        refreshView.WaitRefreshComplete();

        // Assert
        refreshView.IsRefreshing().Should().BeFalse();
    }

    #endregion

    #region Assertion Tests (RV-007 to RV-008)

    [Fact]
    public void RV007_AssertRefreshing_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var refreshView = new TestableRefreshViewControl(context, "pullToRefresh", null);
        refreshView.SetRefreshing(true);

        // Act & Assert - should not throw
        refreshView.Invoking(rv => rv.AssertRefreshing(true)).Should().NotThrow();
    }

    [Fact]
    public void RV008_AssertRefreshing_WhenMismatch_Throws()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var refreshView = new TestableRefreshViewControl(context, "pullToRefresh", null);
        refreshView.SetRefreshing(false);

        // Act & Assert - should throw
        refreshView.Invoking(rv => rv.AssertRefreshing(true)).Should().Throw<Exception>();
    }

    #endregion
}
