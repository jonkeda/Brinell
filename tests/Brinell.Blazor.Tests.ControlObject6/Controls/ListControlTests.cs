using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for ListControl (BLI-001 to BLI-025).
/// </summary>
[Trait("Category", "Collection")]
[Trait("Platform", "Blazor")]
public class ListControlTests
{
    #region Constructor Tests (BLI-001 to BLI-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BLI001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var list = new ListControl(context, "itemsList", null);

        // Assert
        list.Locator.Should().NotBeNull();
        list.Locator.Value.Should().Be("itemsList");
        list.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BLI002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myList");

        // Act
        var list = new ListControl(context, locator, null);

        // Assert
        list.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BLI-003 to BLI-006)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI003_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var list = new ListControl(context, "list", null);

        // Act
        var exists = await list.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI004_IsExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 0);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var list = new ListControl(context, "list", null);

        // Act
        var exists = await list.IsExistsAsync();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI005_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var list = new ListControl(context, "list", null);

        // Act
        var visible = await list.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI006_IsVisibleAsync_WhenNotVisible_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var list = new ListControl(context, "list", null);

        // Act
        var visible = await list.IsVisibleAsync();

        // Assert
        visible.Should().BeFalse();
    }

    #endregion

    #region Item Count Tests (BLI-007 to BLI-009)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI007_GetItemCountAsync_ReturnsLiCount()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        var count = await list.GetItemCountAsync();

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI008_GetItemCountAsync_WhenEmpty_ReturnsZero()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(0);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        var count = await list.GetItemCountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI009_GetItemsAsync_ReturnsAllItemTexts()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = new Mock<ILocator>();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        
        var mockItem1 = new Mock<ILocator>();
        mockItem1.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Item 1");
        var mockItem2 = new Mock<ILocator>();
        mockItem2.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Item 2");
        var mockItem3 = new Mock<ILocator>();
        mockItem3.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Item 3");
        
        mockItemsLocator.Setup(l => l.Nth(0)).Returns(mockItem1.Object);
        mockItemsLocator.Setup(l => l.Nth(1)).Returns(mockItem2.Object);
        mockItemsLocator.Setup(l => l.Nth(2)).Returns(mockItem3.Object);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        var items = await list.GetItemsAsync();

        // Assert
        items.Should().HaveCount(3);
        items.Should().ContainInOrder("Item 1", "Item 2", "Item 3");
    }

    #endregion

    #region Item Text Tests (BLI-010 to BLI-012)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI010_GetItemTextAsync_ReturnsItemAtIndex()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Second Item");
        
        mockLocator.Setup(l => l.Locator("li:nth-child(2)", null)).Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        var text = await list.GetItemTextAsync(1); // 0-based index

        // Assert
        text.Should().Be("Second Item");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI011_GetItemTextAsync_FirstItem_ReturnsFirstItemText()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("First Item");
        
        mockLocator.Setup(l => l.Locator("li:nth-child(1)", null)).Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        var text = await list.GetItemTextAsync(0);

        // Assert
        text.Should().Be("First Item");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI012_HasItemAsync_WhenContains_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = new Mock<ILocator>();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(2);
        
        var mockItem1 = new Mock<ILocator>();
        mockItem1.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Apple");
        var mockItem2 = new Mock<ILocator>();
        mockItem2.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Banana");
        
        mockItemsLocator.Setup(l => l.Nth(0)).Returns(mockItem1.Object);
        mockItemsLocator.Setup(l => l.Nth(1)).Returns(mockItem2.Object);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        var hasItem = await list.HasItemAsync("Apple");

        // Assert
        hasItem.Should().BeTrue();
    }

    #endregion

    #region Click Action Tests (BLI-013 to BLI-016)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI013_ClickItemAsync_ClicksItemAtIndex()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockLocator.Setup(l => l.Locator("li:nth-child(2)", null)).Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        await list.ClickItemAsync(1); // 0-based index

        // Assert
        mockItemLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI014_ClickItemAsync_FirstItem_ClicksFirstItem()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockLocator.Setup(l => l.Locator("li:nth-child(1)", null)).Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        await list.ClickItemAsync(0);

        // Assert
        mockItemLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI015_ClickItemByTextAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var list = new ListControl(context, "list", null);

        // Act & Assert - should not throw
        await list.Invoking(l => l.ClickItemByTextAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BLI016_ClickItemByTextAsync_WithText_ClicksMatchingItem()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockItemsLocator = new Mock<ILocator>();
        var mockFilteredLocator = new Mock<ILocator>();
        mockFilteredLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockItemsLocator.Setup(l => l.Filter(It.IsAny<LocatorFilterOptions>()))
            .Returns(mockFilteredLocator.Object);
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        await list.ClickItemByTextAsync("Click Me");

        // Assert
        mockFilteredLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    #endregion

    #region HasItem Tests (BLI-017 to BLI-019)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI017_HasItemAsync_WhenNotContains_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = new Mock<ILocator>();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(2);
        
        var mockItem1 = new Mock<ILocator>();
        mockItem1.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Apple");
        var mockItem2 = new Mock<ILocator>();
        mockItem2.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Banana");
        
        mockItemsLocator.Setup(l => l.Nth(0)).Returns(mockItem1.Object);
        mockItemsLocator.Setup(l => l.Nth(1)).Returns(mockItem2.Object);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        var hasItem = await list.HasItemAsync("Orange");

        // Assert
        hasItem.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI018_HasItemAsync_WithNull_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var list = new ListControl(context, "list", null);

        // Act
        var hasItem = await list.HasItemAsync(null);

        // Assert
        hasItem.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI019_HasItemAsync_EmptyList_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = new Mock<ILocator>();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(0);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act
        var hasItem = await list.HasItemAsync("Apple");

        // Assert
        hasItem.Should().BeFalse();
    }

    #endregion

    #region Assertion Tests (BLI-020 to BLI-025)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI020_AssertItemCountAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act & Assert - should not throw
        await list.Invoking(l => l.AssertItemCountAsync(5)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI021_AssertItemCountAsync_WhenMismatch_Throws()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act & Assert - should throw
        await list.Invoking(l => l.AssertItemCountAsync(10)).Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI022_AssertItemCountAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var list = new ListControl(context, "list", null);

        // Act & Assert - should not throw
        await list.Invoking(l => l.AssertItemCountAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI023_AssertHasItemAsync_WhenContains_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = new Mock<ILocator>();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        
        var mockItem = new Mock<ILocator>();
        mockItem.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Test Item");
        mockItemsLocator.Setup(l => l.Nth(0)).Returns(mockItem.Object);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act & Assert - should not throw
        await list.Invoking(l => l.AssertHasItemAsync("Test Item")).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI024_AssertHasItemAsync_WhenNotContains_Throws()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemsLocator = new Mock<ILocator>();
        mockItemsLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        
        var mockItem = new Mock<ILocator>();
        mockItem.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Other Item");
        mockItemsLocator.Setup(l => l.Nth(0)).Returns(mockItem.Object);
        
        mockLocator.Setup(l => l.Locator("li", null)).Returns(mockItemsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act & Assert - should throw
        await list.Invoking(l => l.AssertHasItemAsync("Missing Item")).Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BLI025_AssertItemTextAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockItemLocator = new Mock<ILocator>();
        mockItemLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Expected Text");
        
        mockLocator.Setup(l => l.Locator("li:nth-child(1)", null)).Returns(mockItemLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var list = new ListControl(context, "list", null);

        // Act & Assert - should not throw
        await list.Invoking(l => l.AssertItemTextAsync(0, "Expected Text")).Should().NotThrowAsync();
    }

    #endregion
}
