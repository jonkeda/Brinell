using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for NavMenuControl (NM-001 to NM-012).
/// </summary>
[Trait("Category", "Navigation")]
[Trait("Platform", "Blazor")]
[Trait("Priority", "P2")]
public class NavMenuControlTests
{
    #region Constructor Tests (NM-001 to NM-002)

    [Fact]
    public void NM001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Assert
        navMenu.Locator.Should().NotBeNull();
        navMenu.Locator.Value.Should().Be("mainNavMenu");
        navMenu.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    public void NM002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("navMenu");

        // Act
        var navMenu = new NavMenuControl(context, locator, null);

        // Assert
        navMenu.Locator.Should().Be(locator);
    }

    #endregion

    #region GetItemCount Tests (NM-003 to NM-004)

    [Fact]
    public async Task NM003_GetItemCountAsync_ReturnsCount()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act
        var count = await navMenu.GetItemCountAsync();

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    public async Task NM004_GetItemCountAsync_WhenEmpty_ReturnsZero()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.CountAsync()).ReturnsAsync(0);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act
        var count = await navMenu.GetItemCountAsync();

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region Navigation Tests (NM-005 to NM-007)

    [Fact]
    public async Task NM005_NavigateToAsync_ClicksMenuItem()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockItemLocator = new Mock<ILocator>();
        var mockFilteredLocator = new Mock<ILocator>();
        mockFilteredLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        mockItemLocator.Setup(l => l.Filter(It.IsAny<LocatorFilterOptions>()))
            .Returns(mockFilteredLocator.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act
        await navMenu.NavigateToAsync("Home");

        // Assert
        mockFilteredLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    public async Task NM006_NavigateToIndexAsync_ClicksMenuItemByIndex()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockItemLocator = new Mock<ILocator>();
        var mockNthLocator = new Mock<ILocator>();
        mockNthLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        mockItemLocator.Setup(l => l.Nth(It.IsAny<int>()))
            .Returns(mockNthLocator.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act
        await navMenu.NavigateToIndexAsync(2);

        // Assert
        mockNthLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    public async Task NM007_NavigateToAsync_WithNullText_DoesNotThrow()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act & Assert - should not throw
        await navMenu.Invoking(n => n.NavigateToAsync(null)).Should().NotThrowAsync();
    }

    #endregion

    #region Active Item Tests (NM-008 to NM-010)

    [Fact]
    public async Task NM008_GetActiveItemAsync_ReturnsActiveText()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockActiveLocator = new Mock<ILocator>();
        var mockFirstLocator = new Mock<ILocator>();
        mockFirstLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        mockFirstLocator.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ReturnsAsync("Dashboard");
        mockActiveLocator.SetupGet(l => l.First).Returns(mockFirstLocator.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockActiveLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act
        var activeItem = await navMenu.GetActiveItemAsync();

        // Assert
        activeItem.Should().Be("Dashboard");
    }

    [Fact]
    public async Task NM009_GetActiveItemAsync_WhenNoActive_ReturnsNull()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockActiveLocator = new Mock<ILocator>();
        var mockFirstLocator = new Mock<ILocator>();
        mockFirstLocator.Setup(l => l.CountAsync()).ReturnsAsync(0);
        mockActiveLocator.SetupGet(l => l.First).Returns(mockFirstLocator.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockActiveLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act
        var activeItem = await navMenu.GetActiveItemAsync();

        // Assert
        activeItem.Should().BeNull();
    }

    [Fact]
    public async Task NM010_AssertActiveItemAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockActiveLocator = new Mock<ILocator>();
        var mockFirstLocator = new Mock<ILocator>();
        mockFirstLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        mockFirstLocator.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ReturnsAsync("Home");
        mockActiveLocator.SetupGet(l => l.First).Returns(mockFirstLocator.Object);
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockActiveLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act & Assert - should not throw
        await navMenu.Invoking(n => n.AssertActiveItemAsync("Home")).Should().NotThrowAsync();
    }

    #endregion

    #region HasItem Tests (NM-011 to NM-012)

    [Fact]
    public async Task NM011_HasItemAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        mockItemLocator.Setup(l => l.Nth(0)).Returns(CreateNthMockWithText("Home"));
        mockItemLocator.Setup(l => l.Nth(1)).Returns(CreateNthMockWithText("About"));
        mockItemLocator.Setup(l => l.Nth(2)).Returns(CreateNthMockWithText("Contact"));
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act
        var hasItem = await navMenu.HasItemAsync("About");

        // Assert
        hasItem.Should().BeTrue();
    }

    [Fact]
    public async Task NM012_HasItemAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.CountAsync()).ReturnsAsync(2);
        mockItemLocator.Setup(l => l.Nth(0)).Returns(CreateNthMockWithText("Home"));
        mockItemLocator.Setup(l => l.Nth(1)).Returns(CreateNthMockWithText("About"));
        mockLocator.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>()))
            .Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var navMenu = new NavMenuControl(context, "mainNavMenu", null);

        // Act
        var hasItem = await navMenu.HasItemAsync("Settings");

        // Assert
        hasItem.Should().BeFalse();
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
