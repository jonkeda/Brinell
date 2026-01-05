using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for TabControl (TC-001 to TC-012).
/// </summary>
[Trait("Category", "Navigation")]
[Trait("Platform", "Blazor")]
[Trait("Priority", "P2")]
public class TabControlTests
{
    #region Constructor Tests (TC-001 to TC-002)

    [Fact]
    public void TC001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Assert
        tabControl.Locator.Should().NotBeNull();
        tabControl.Locator.Value.Should().Be("settingsTabs");
        tabControl.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    public void TC002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("tabContainer");

        // Act
        var tabControl = new TabControl(context, locator, null);

        // Assert
        tabControl.Locator.Should().Be(locator);
    }

    #endregion

    #region GetTabCount Tests (TC-003 to TC-004)

    [Fact]
    public async Task TC003_GetTabCountAsync_ReturnsTabCount()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockTabLocator = new Mock<ILocator>();
        mockTabLocator.Setup(l => l.CountAsync()).ReturnsAsync(4);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act
        var count = await tabControl.GetTabCountAsync();

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public async Task TC004_GetTabsAsync_ReturnsAllTabTitles()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockTabLocator = new Mock<ILocator>();
        mockTabLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        mockTabLocator.Setup(l => l.Nth(0)).Returns(CreateNthMockWithText("General"));
        mockTabLocator.Setup(l => l.Nth(1)).Returns(CreateNthMockWithText("Privacy"));
        mockTabLocator.Setup(l => l.Nth(2)).Returns(CreateNthMockWithText("Advanced"));
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act
        var tabs = await tabControl.GetTabsAsync();

        // Assert
        tabs.Should().BeEquivalentTo(new[] { "General", "Privacy", "Advanced" });
    }

    #endregion

    #region SelectTab Tests (TC-005 to TC-007)

    [Fact]
    public async Task TC005_SelectTabAsync_ClicksTabByIndex()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockTabLocator = new Mock<ILocator>();
        var mockNthLocator = new Mock<ILocator>();
        mockNthLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        mockTabLocator.Setup(l => l.Nth(It.IsAny<int>()))
            .Returns(mockNthLocator.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act
        await tabControl.SelectTabAsync(2);

        // Assert
        mockNthLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    public async Task TC006_SelectTabByTextAsync_ClicksTabByTitle()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockTabLocator = new Mock<ILocator>();
        var mockFilteredLocator = new Mock<ILocator>();
        mockFilteredLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        mockTabLocator.Setup(l => l.Filter(It.IsAny<LocatorFilterOptions>()))
            .Returns(mockFilteredLocator.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act
        await tabControl.SelectTabByTextAsync("Privacy");

        // Assert
        mockFilteredLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    public async Task TC007_SelectTabByTextAsync_WithNullTitle_DoesNotThrow()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act & Assert - should not throw
        await tabControl.Invoking(t => t.SelectTabByTextAsync(null)).Should().NotThrowAsync();
    }

    #endregion

    #region Selected Tab Tests (TC-008 to TC-010)

    [Fact]
    public async Task TC008_GetSelectedIndexAsync_ReturnsSelectedIndex()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockTabLocator = new Mock<ILocator>();
        mockTabLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        
        // Tab 0 - not selected
        var tab0 = new Mock<ILocator>();
        tab0.Setup(l => l.GetAttributeAsync("aria-selected", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("false");
        tab0.Setup(l => l.EvaluateAsync<bool>(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(false);
        
        // Tab 1 - selected
        var tab1 = new Mock<ILocator>();
        tab1.Setup(l => l.GetAttributeAsync("aria-selected", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("true");
        tab1.Setup(l => l.EvaluateAsync<bool>(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(false);
        
        mockTabLocator.Setup(l => l.Nth(0)).Returns(tab0.Object);
        mockTabLocator.Setup(l => l.Nth(1)).Returns(tab1.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act
        var selectedIndex = await tabControl.GetSelectedIndexAsync();

        // Assert
        selectedIndex.Should().Be(1);
    }

    [Fact]
    public async Task TC009_GetSelectedTabAsync_ReturnsSelectedTitle()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockTabLocator = new Mock<ILocator>();
        mockTabLocator.Setup(l => l.CountAsync()).ReturnsAsync(2);
        
        // Tab 0 - selected
        var tab0 = new Mock<ILocator>();
        tab0.Setup(l => l.GetAttributeAsync("aria-selected", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("true");
        tab0.Setup(l => l.EvaluateAsync<bool>(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(false);
        tab0.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ReturnsAsync("General");
        
        // Tab 1 - not selected (needed for GetTabsAsync to iterate)
        var tab1 = new Mock<ILocator>();
        tab1.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ReturnsAsync("Privacy");
        
        mockTabLocator.Setup(l => l.Nth(0)).Returns(tab0.Object);
        mockTabLocator.Setup(l => l.Nth(1)).Returns(tab1.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act
        var selectedTab = await tabControl.GetSelectedTabAsync();

        // Assert
        selectedTab.Should().Be("General");
    }

    [Fact]
    public async Task TC010_GetSelectedIndexAsync_WhenNoneSelected_ReturnsNegative()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockTabLocator = new Mock<ILocator>();
        mockTabLocator.Setup(l => l.CountAsync()).ReturnsAsync(2);
        
        // Neither tab is selected
        var tab0 = new Mock<ILocator>();
        tab0.Setup(l => l.GetAttributeAsync("aria-selected", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("false");
        tab0.Setup(l => l.EvaluateAsync<bool>(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(false);
        
        var tab1 = new Mock<ILocator>();
        tab1.Setup(l => l.GetAttributeAsync("aria-selected", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("false");
        tab1.Setup(l => l.EvaluateAsync<bool>(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(false);
        
        mockTabLocator.Setup(l => l.Nth(0)).Returns(tab0.Object);
        mockTabLocator.Setup(l => l.Nth(1)).Returns(tab1.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act
        var selectedIndex = await tabControl.GetSelectedIndexAsync();

        // Assert
        selectedIndex.Should().Be(-1);
    }

    #endregion

    #region Assertion Tests (TC-011 to TC-012)

    [Fact]
    public async Task TC011_AssertSelectedIndexAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockTabLocator = new Mock<ILocator>();
        mockTabLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        
        var tab0 = new Mock<ILocator>();
        tab0.Setup(l => l.GetAttributeAsync("aria-selected", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("true");
        tab0.Setup(l => l.EvaluateAsync<bool>(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(false);
        
        mockTabLocator.Setup(l => l.Nth(0)).Returns(tab0.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act & Assert - should not throw
        await tabControl.Invoking(t => t.AssertSelectedIndexAsync(0)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task TC012_AssertTabCountAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockTabLocator = new Mock<ILocator>();
        mockTabLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockTabLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var tabControl = new TabControl(context, "settingsTabs", null);

        // Act & Assert - should not throw
        await tabControl.Invoking(t => t.AssertTabCountAsync(5)).Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private static ILocator CreateNthMockWithText(string text)
    {
        var mockNth = new Mock<ILocator>();
        mockNth.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ReturnsAsync(text);
        return mockNth.Object;
    }

    #endregion
}
