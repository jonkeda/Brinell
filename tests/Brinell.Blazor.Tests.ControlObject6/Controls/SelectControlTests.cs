using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for SelectControl (BSC-001 to BSC-025).
/// </summary>
[Trait("Category", "Selection")]
[Trait("Platform", "Blazor")]
public class SelectControlTests
{
    #region Constructor Tests (BSC-001 to BSC-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BSC001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var select = new SelectControl(context, "countrySelect", null);

        // Assert
        select.Locator.Should().NotBeNull();
        select.Locator.Value.Should().Be("countrySelect");
        select.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BSC002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("mySelect");

        // Act
        var select = new SelectControl(context, locator, null);

        // Assert
        select.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BSC-003 to BSC-007)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC003_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var select = new SelectControl(context, "select", null);

        // Act
        var exists = await select.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC004_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var select = new SelectControl(context, "select", null);

        // Act
        var visible = await select.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC005_IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var select = new SelectControl(context, "select", null);

        // Act
        var enabled = await select.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC006_IsEnabledAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var select = new SelectControl(context, "select", null);

        // Act
        var enabled = await select.IsEnabledAsync();

        // Assert
        enabled.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC007_ClickAsync_CallsLocatorClick()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        await select.ClickAsync();

        // Assert
        mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    #endregion

    #region Item Retrieval Tests (BSC-008 to BSC-012)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC008_GetItemCountAsync_ReturnsOptionCount()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockOptionsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockOptionsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        mockLocator.Setup(l => l.Locator("option", null)).Returns(mockOptionsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        var count = await select.GetItemCountAsync();

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC009_GetItemsAsync_ReturnsAllOptions()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockOptionsLocator = new Mock<ILocator>();
        mockOptionsLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        
        var mockOption1 = new Mock<ILocator>();
        mockOption1.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Option 1");
        var mockOption2 = new Mock<ILocator>();
        mockOption2.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Option 2");
        var mockOption3 = new Mock<ILocator>();
        mockOption3.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Option 3");
        
        mockOptionsLocator.Setup(l => l.Nth(0)).Returns(mockOption1.Object);
        mockOptionsLocator.Setup(l => l.Nth(1)).Returns(mockOption2.Object);
        mockOptionsLocator.Setup(l => l.Nth(2)).Returns(mockOption3.Object);
        
        mockLocator.Setup(l => l.Locator("option", null)).Returns(mockOptionsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        var items = await select.GetItemsAsync();

        // Assert
        items.Should().HaveCount(3);
        items.Should().ContainInOrder("Option 1", "Option 2", "Option 3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC010_GetSelectedItemAsync_ReturnsSelectedText()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockSelectedLocator = new Mock<ILocator>();
        mockSelectedLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        mockSelectedLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("United States");
        
        mockLocator.Setup(l => l.Locator("option:checked", null)).Returns(mockSelectedLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        var selectedItem = await select.GetSelectedItemAsync();

        // Assert
        selectedItem.Should().Be("United States");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC011_GetSelectedItemAsync_WhenNoSelection_ReturnsNull()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockSelectedLocator = new Mock<ILocator>();
        mockSelectedLocator.Setup(l => l.CountAsync()).ReturnsAsync(0);
        
        mockLocator.Setup(l => l.Locator("option:checked", null)).Returns(mockSelectedLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        var selectedItem = await select.GetSelectedItemAsync();

        // Assert
        selectedItem.Should().BeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC012_GetSelectedIndexAsync_ReturnsSelectedIndex()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        mockLocator.Setup(l => l.EvaluateAsync<int>("el => el.selectedIndex", null))
            .ReturnsAsync(2);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        var selectedIndex = await select.GetSelectedIndexAsync();

        // Assert
        selectedIndex.Should().Be(2);
    }

    #endregion

    #region Selection Action Tests (BSC-013 to BSC-018)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC013_SelectItemAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should not throw
        await select.Invoking(s => s.SelectItemAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC014_SelectItemAsync_WithText_SelectsOption()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.SelectOptionAsync(It.IsAny<SelectOptionValue>(), It.IsAny<LocatorSelectOptionOptions?>()))
            .ReturnsAsync(new[] { "us" });
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        await select.SelectItemAsync("United States");

        // Assert
        mockLocator.Verify(l => l.SelectOptionAsync(
            It.Is<SelectOptionValue>(v => v.Label == "United States"), 
            It.IsAny<LocatorSelectOptionOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC015_SelectItemByIndexAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should not throw
        await select.Invoking(s => s.SelectItemByIndexAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC016_SelectItemByIndexAsync_WithIndex_SelectsOption()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.SelectOptionAsync(It.IsAny<SelectOptionValue>(), It.IsAny<LocatorSelectOptionOptions?>()))
            .ReturnsAsync(new[] { "us" });
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        await select.SelectItemByIndexAsync(2);

        // Assert
        mockLocator.Verify(l => l.SelectOptionAsync(
            It.Is<SelectOptionValue>(v => v.Index == 2), 
            It.IsAny<LocatorSelectOptionOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC017_SelectItemByValueAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should not throw
        await select.Invoking(s => s.SelectItemByValueAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BSC018_SelectItemByValueAsync_WithValue_SelectsOption()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.SelectOptionAsync(It.IsAny<string>(), It.IsAny<LocatorSelectOptionOptions?>()))
            .ReturnsAsync(new[] { "us" });
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act
        await select.SelectItemByValueAsync("us");

        // Assert
        mockLocator.Verify(l => l.SelectOptionAsync("us", It.IsAny<LocatorSelectOptionOptions?>()), Times.Once);
    }

    #endregion

    #region Assertion Tests (BSC-019 to BSC-025)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BSC019_AssertSelectedItemAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should not throw
        await select.Invoking(s => s.AssertSelectedItemAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BSC020_AssertSelectedItemAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockSelectedLocator = new Mock<ILocator>();
        mockSelectedLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        mockSelectedLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("United States");
        
        mockLocator.Setup(l => l.Locator("option:checked", null)).Returns(mockSelectedLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should not throw
        await select.Invoking(s => s.AssertSelectedItemAsync("United States")).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BSC021_AssertSelectedItemAsync_WhenMismatch_Throws()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockSelectedLocator = new Mock<ILocator>();
        mockSelectedLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        mockSelectedLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Canada");
        
        mockLocator.Setup(l => l.Locator("option:checked", null)).Returns(mockSelectedLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should throw
        await select.Invoking(s => s.AssertSelectedItemAsync("United States")).Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BSC022_AssertSelectedIndexAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        mockLocator.Setup(l => l.EvaluateAsync<int>("el => el.selectedIndex", null))
            .ReturnsAsync(2);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should not throw
        await select.Invoking(s => s.AssertSelectedIndexAsync(2)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BSC023_AssertItemCountAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockOptionsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockOptionsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        mockLocator.Setup(l => l.Locator("option", null)).Returns(mockOptionsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should not throw
        await select.Invoking(s => s.AssertItemCountAsync(5)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BSC024_AssertItemCountAsync_WhenMismatch_Throws()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockOptionsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockOptionsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        mockLocator.Setup(l => l.Locator("option", null)).Returns(mockOptionsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should throw
        await select.Invoking(s => s.AssertItemCountAsync(10)).Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BSC025_AssertHasItemAsync_WhenContains_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockOptionsLocator = new Mock<ILocator>();
        mockOptionsLocator.Setup(l => l.CountAsync()).ReturnsAsync(2);
        
        var mockOption1 = new Mock<ILocator>();
        mockOption1.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Option 1");
        var mockOption2 = new Mock<ILocator>();
        mockOption2.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Option 2");
        
        mockOptionsLocator.Setup(l => l.Nth(0)).Returns(mockOption1.Object);
        mockOptionsLocator.Setup(l => l.Nth(1)).Returns(mockOption2.Object);
        
        mockLocator.Setup(l => l.Locator("option", null)).Returns(mockOptionsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var select = new SelectControl(context, "select", null);

        // Act & Assert - should not throw
        await select.Invoking(s => s.AssertHasItemAsync("Option 1")).Should().NotThrowAsync();
    }

    #endregion
}
