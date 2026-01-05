using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for TabbedPageControl and TabBarControl (TP-001 to TP-012, TB-001 to TB-012).
/// </summary>
[Trait("Category", "Navigation")]
[Trait("Platform", "MAUI")]
[Trait("Priority", "P2")]
public class TabControlTests
{
    #region TabbedPage Constructor Tests (TP-001 to TP-002)

    [Fact]
    public void TP001_TabbedPage_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        // Act
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);

        // Assert
        tabbedPage.Locator.Should().NotBeNull();
        tabbedPage.Locator.Value.Should().Be("mainTabbedPage");
        tabbedPage.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void TP002_TabbedPage_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);
        var locator = By.AutomationId("myTabs");

        // Act
        var tabbedPage = new TestableTabbedPageControl(context, locator, null);

        // Assert
        tabbedPage.Locator.Should().Be(locator);
    }

    #endregion

    #region TabbedPage Tab Count Tests (TP-003 to TP-004)

    [Fact]
    public void TP003_TabbedPage_GetTabCount_ReturnsCount()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);
        tabbedPage.SetTabNames("Home", "Settings", "Profile", "About");

        // Act
        var count = tabbedPage.GetTabCount();

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public void TP004_TabbedPage_AssertTabCount_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);
        tabbedPage.SetTabNames("Tab1", "Tab2", "Tab3");

        // Act & Assert - should not throw
        tabbedPage.Invoking(t => t.AssertTabCount(3)).Should().NotThrow();
    }

    #endregion

    #region TabbedPage Selection Tests (TP-005 to TP-008)

    [Fact]
    public void TP005_TabbedPage_GetSelectedTabIndex_ReturnsIndex()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);
        tabbedPage.SetSelectedTabIndex(2);

        // Act
        var index = tabbedPage.GetSelectedTabIndex();

        // Assert
        index.Should().Be(2);
    }

    [Fact]
    public void TP006_TabbedPage_SelectTab_ByIndex_SelectsTab()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);
        tabbedPage.SetTabNames("Home", "Settings", "Profile");
        tabbedPage.SetSelectedTabIndex(0);

        // Act
        tabbedPage.SelectTab(1);

        // Assert
        tabbedPage.GetSelectedTabIndex().Should().Be(1);
    }

    [Fact]
    public void TP007_TabbedPage_SelectTab_ByName_SelectsTab()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);
        tabbedPage.SetTabNames("Home", "Settings", "Profile");
        tabbedPage.SetSelectedTabIndex(0);

        // Act
        tabbedPage.SelectTab("Profile");

        // Assert
        tabbedPage.GetSelectedTabName().Should().Be("Profile");
    }

    [Fact]
    public void TP008_TabbedPage_GetTabNames_ReturnsAllNames()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);
        tabbedPage.SetTabNames("Home", "Settings", "Profile");

        // Act
        var names = tabbedPage.GetTabNames();

        // Assert
        names.Should().BeEquivalentTo(new[] { "Home", "Settings", "Profile" });
    }

    #endregion

    #region TabbedPage Assertion Tests (TP-009 to TP-010)

    [Fact]
    public void TP009_TabbedPage_AssertSelectedTabIndex_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);
        tabbedPage.SetSelectedTabIndex(1);

        // Act & Assert - should not throw
        tabbedPage.Invoking(t => t.AssertSelectedTabIndex(1)).Should().NotThrow();
    }

    [Fact]
    public void TP010_TabbedPage_AssertSelectedTabName_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabbedPage = new TestableTabbedPageControl(context, "mainTabbedPage", null);
        tabbedPage.SetTabNames("Home", "Settings", "Profile");
        tabbedPage.SetSelectedTabIndex(1);

        // Act & Assert - should not throw
        tabbedPage.Invoking(t => t.AssertSelectedTabName("Settings")).Should().NotThrow();
    }

    #endregion

    #region TabBar Constructor Tests (TB-001 to TB-002)

    [Fact]
    public void TB001_TabBar_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        // Act
        var tabBar = new TestableTabBarControl(context, "mainTabBar", null);

        // Assert
        tabBar.Locator.Should().NotBeNull();
        tabBar.Locator.Value.Should().Be("mainTabBar");
        tabBar.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void TB002_TabBar_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);
        var locator = By.AutomationId("shellTabBar");

        // Act
        var tabBar = new TestableTabBarControl(context, locator, null);

        // Assert
        tabBar.Locator.Should().Be(locator);
    }

    #endregion

    #region TabBar Tab Count Tests (TB-003 to TB-004)

    [Fact]
    public void TB003_TabBar_GetTabCount_ReturnsCount()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabBar = new TestableTabBarControl(context, "mainTabBar", null);
        tabBar.SetTabNames("Home", "Search", "Profile");

        // Act
        var count = tabBar.GetTabCount();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public void TB004_TabBar_AssertTabCount_WhenMismatch_Throws()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabBar = new TestableTabBarControl(context, "mainTabBar", null);
        tabBar.SetTabNames("Home", "Search");

        // Act & Assert - should throw
        tabBar.Invoking(t => t.AssertTabCount(5)).Should().Throw<Exception>();
    }

    #endregion

    #region TabBar Selection Tests (TB-005 to TB-008)

    [Fact]
    public void TB005_TabBar_GetSelectedTabIndex_ReturnsIndex()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabBar = new TestableTabBarControl(context, "mainTabBar", null);
        tabBar.SetSelectedTabIndex(0);

        // Act
        var index = tabBar.GetSelectedTabIndex();

        // Assert
        index.Should().Be(0);
    }

    [Fact]
    public void TB006_TabBar_SelectTab_ByIndex_SelectsTab()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabBar = new TestableTabBarControl(context, "mainTabBar", null);
        tabBar.SetTabNames("Home", "Search", "Profile");

        // Act
        tabBar.SelectTab(2);

        // Assert
        tabBar.GetSelectedTabIndex().Should().Be(2);
    }

    [Fact]
    public void TB007_TabBar_WaitTabSelected_WhenSelected_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabBar = new TestableTabBarControl(context, "mainTabBar", null);
        tabBar.SetSelectedTabIndex(1);

        // Act
        var result = tabBar.WaitTabSelected(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TB008_TabBar_WaitTabSelected_WhenNotSelected_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var tabBar = new TestableTabBarControl(context, "mainTabBar", null);
        tabBar.SetSelectedTabIndex(0);

        // Act
        var result = tabBar.WaitTabSelected(2);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
