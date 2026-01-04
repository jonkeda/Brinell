using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for TableControl (BTB-001 to BTB-030).
/// </summary>
[Trait("Category", "Collection")]
[Trait("Platform", "Blazor")]
public class TableControlTests
{
    #region Constructor Tests (BTB-001 to BTB-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BTB001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var table = new TableControl(context, "dataTable", null);

        // Assert
        table.Locator.Should().NotBeNull();
        table.Locator.Value.Should().Be("dataTable");
        table.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BTB002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myTable");

        // Act
        var table = new TableControl(context, locator, null);

        // Assert
        table.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BTB-003 to BTB-005)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB003_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var table = new TableControl(context, "table", null);

        // Act
        var exists = await table.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB004_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var table = new TableControl(context, "table", null);

        // Act
        var visible = await table.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB005_IsVisibleAsync_WhenNotVisible_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var table = new TableControl(context, "table", null);

        // Act
        var visible = await table.IsVisibleAsync();

        // Assert
        visible.Should().BeFalse();
    }

    #endregion

    #region Row/Column Count Tests (BTB-006 to BTB-010)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB006_GetRowCountAsync_ReturnsTbodyTrCount()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockRowsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockRowsLocator.Setup(l => l.CountAsync()).ReturnsAsync(10);
        
        mockLocator.Setup(l => l.Locator("tbody tr", null)).Returns(mockRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var rowCount = await table.GetRowCountAsync();

        // Assert
        rowCount.Should().Be(10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB007_GetRowCountAsync_WhenEmpty_ReturnsZero()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockRowsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockRowsLocator.Setup(l => l.CountAsync()).ReturnsAsync(0);
        
        mockLocator.Setup(l => l.Locator("tbody tr", null)).Returns(mockRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var rowCount = await table.GetRowCountAsync();

        // Assert
        rowCount.Should().Be(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB008_GetColumnCountAsync_ReturnsColumnCount()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockFirstRowLocator = new Mock<ILocator>();
        var mockCellsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockCellsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        var mockRowsLocator = new Mock<ILocator>();
        mockRowsLocator.Setup(l => l.First).Returns(mockFirstRowLocator.Object);
        mockFirstRowLocator.Setup(l => l.Locator("th, td", null)).Returns(mockCellsLocator.Object);
        
        mockLocator.Setup(l => l.Locator("thead tr, tbody tr", null)).Returns(mockRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var columnCount = await table.GetColumnCountAsync();

        // Assert
        columnCount.Should().Be(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB009_GetHeaderRowCountAsync_ReturnsTHeadTrCount()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockHeaderRowsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockHeaderRowsLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        
        mockLocator.Setup(l => l.Locator("thead tr", null)).Returns(mockHeaderRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var headerRowCount = await table.GetHeaderRowCountAsync();

        // Assert
        headerRowCount.Should().Be(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB010_GetHeaderRowCountAsync_MultipleHeaders_ReturnsCount()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockHeaderRowsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockHeaderRowsLocator.Setup(l => l.CountAsync()).ReturnsAsync(2);
        
        mockLocator.Setup(l => l.Locator("thead tr", null)).Returns(mockHeaderRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var headerRowCount = await table.GetHeaderRowCountAsync();

        // Assert
        headerRowCount.Should().Be(2);
    }

    #endregion

    #region Cell Access Tests (BTB-011 to BTB-016)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB011_GetCellTextAsync_ReturnsCellContent()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockCellLocator = new Mock<ILocator>();
        mockCellLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Cell Value");
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(2) td:nth-child(3)", null))
            .Returns(mockCellLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var cellText = await table.GetCellTextAsync(1, 2); // 0-based indices

        // Assert
        cellText.Should().Be("Cell Value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB012_GetCellTextAsync_FirstCell_ReturnsContent()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockCellLocator = new Mock<ILocator>();
        mockCellLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("First Cell");
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(1) td:nth-child(1)", null))
            .Returns(mockCellLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var cellText = await table.GetCellTextAsync(0, 0);

        // Assert
        cellText.Should().Be("First Cell");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB013_GetRowTextAsync_ReturnsAllCellsInRow()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockCellsLocator = new Mock<ILocator>();
        mockCellsLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        
        var mockCell1 = new Mock<ILocator>();
        mockCell1.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("A");
        var mockCell2 = new Mock<ILocator>();
        mockCell2.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("B");
        var mockCell3 = new Mock<ILocator>();
        mockCell3.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("C");
        
        mockCellsLocator.Setup(l => l.Nth(0)).Returns(mockCell1.Object);
        mockCellsLocator.Setup(l => l.Nth(1)).Returns(mockCell2.Object);
        mockCellsLocator.Setup(l => l.Nth(2)).Returns(mockCell3.Object);
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(1) td", null))
            .Returns(mockCellsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var rowText = await table.GetRowTextAsync(0);

        // Assert
        rowText.Should().HaveCount(3);
        rowText.Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB014_GetColumnTextAsync_ReturnsAllCellsInColumn()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockCellsLocator = new Mock<ILocator>();
        mockCellsLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        
        var mockCell1 = new Mock<ILocator>();
        mockCell1.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Row1Col1");
        var mockCell2 = new Mock<ILocator>();
        mockCell2.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Row2Col1");
        var mockCell3 = new Mock<ILocator>();
        mockCell3.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Row3Col1");
        
        mockCellsLocator.Setup(l => l.Nth(0)).Returns(mockCell1.Object);
        mockCellsLocator.Setup(l => l.Nth(1)).Returns(mockCell2.Object);
        mockCellsLocator.Setup(l => l.Nth(2)).Returns(mockCell3.Object);
        
        mockLocator.Setup(l => l.Locator("tbody tr td:nth-child(1)", null))
            .Returns(mockCellsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var columnText = await table.GetColumnTextAsync(0);

        // Assert
        columnText.Should().HaveCount(3);
        columnText.Should().ContainInOrder("Row1Col1", "Row2Col1", "Row3Col1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB015_GetHeaderTextAsync_ReturnsHeaderContent()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockHeaderLocator = new Mock<ILocator>();
        mockHeaderLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Name");
        
        mockLocator.Setup(l => l.Locator("thead tr th:nth-child(1)", null))
            .Returns(mockHeaderLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var headerText = await table.GetHeaderTextAsync(0);

        // Assert
        headerText.Should().Be("Name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB016_GetHeadersAsync_ReturnsAllHeaders()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockHeadersLocator = new Mock<ILocator>();
        mockHeadersLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        
        var mockHeader1 = new Mock<ILocator>();
        mockHeader1.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Name");
        var mockHeader2 = new Mock<ILocator>();
        mockHeader2.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Age");
        var mockHeader3 = new Mock<ILocator>();
        mockHeader3.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Email");
        
        mockHeadersLocator.Setup(l => l.Nth(0)).Returns(mockHeader1.Object);
        mockHeadersLocator.Setup(l => l.Nth(1)).Returns(mockHeader2.Object);
        mockHeadersLocator.Setup(l => l.Nth(2)).Returns(mockHeader3.Object);
        
        mockLocator.Setup(l => l.Locator("thead tr th", null)).Returns(mockHeadersLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        var headers = await table.GetHeadersAsync();

        // Assert
        headers.Should().HaveCount(3);
        headers.Should().ContainInOrder("Name", "Age", "Email");
    }

    #endregion

    #region Click Tests (BTB-017 to BTB-022)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB017_ClickRowAsync_ClicksSpecifiedRow()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockRowLocator = new Mock<ILocator>();
        mockRowLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(2)", null))
            .Returns(mockRowLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        await table.ClickRowAsync(1); // 0-based index

        // Assert
        mockRowLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB018_ClickRowAsync_FirstRow_ClicksFirstRow()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockRowLocator = new Mock<ILocator>();
        mockRowLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(1)", null))
            .Returns(mockRowLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        await table.ClickRowAsync(0);

        // Assert
        mockRowLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB019_ClickCellAsync_ClicksSpecifiedCell()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockCellLocator = new Mock<ILocator>();
        mockCellLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(2) td:nth-child(3)", null))
            .Returns(mockCellLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        await table.ClickCellAsync(1, 2); // 0-based indices

        // Assert
        mockCellLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB020_ClickCellAsync_FirstCell_ClicksFirstCell()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockCellLocator = new Mock<ILocator>();
        mockCellLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(1) td:nth-child(1)", null))
            .Returns(mockCellLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        await table.ClickCellAsync(0, 0);

        // Assert
        mockCellLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB021_ClickHeaderAsync_ClicksSpecifiedHeader()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockHeaderLocator = new Mock<ILocator>();
        mockHeaderLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockLocator.Setup(l => l.Locator("thead tr th:nth-child(2)", null))
            .Returns(mockHeaderLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        await table.ClickHeaderAsync(1); // 0-based index

        // Assert
        mockHeaderLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTB022_ClickHeaderAsync_FirstHeader_ClicksFirstHeader()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        var mockHeaderLocator = new Mock<ILocator>();
        mockHeaderLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        mockLocator.Setup(l => l.Locator("thead tr th:nth-child(1)", null))
            .Returns(mockHeaderLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act
        await table.ClickHeaderAsync(0);

        // Assert
        mockHeaderLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    #endregion

    #region Assertion Tests (BTB-023 to BTB-030)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB023_AssertRowCountAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockRowsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockRowsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        mockLocator.Setup(l => l.Locator("tbody tr", null)).Returns(mockRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act & Assert - should not throw
        await table.Invoking(t => t.AssertRowCountAsync(5)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB024_AssertRowCountAsync_WhenMismatch_Throws()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockRowsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockRowsLocator.Setup(l => l.CountAsync()).ReturnsAsync(5);
        
        mockLocator.Setup(l => l.Locator("tbody tr", null)).Returns(mockRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act & Assert - should throw
        await table.Invoking(t => t.AssertRowCountAsync(10)).Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB025_AssertRowCountAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var table = new TableControl(context, "table", null);

        // Act & Assert - should not throw
        await table.Invoking(t => t.AssertRowCountAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB026_AssertColumnCountAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockFirstRowLocator = new Mock<ILocator>();
        var mockCellsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockCellsLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        
        var mockRowsLocator = new Mock<ILocator>();
        mockRowsLocator.Setup(l => l.First).Returns(mockFirstRowLocator.Object);
        mockFirstRowLocator.Setup(l => l.Locator("th, td", null)).Returns(mockCellsLocator.Object);
        
        mockLocator.Setup(l => l.Locator("thead tr, tbody tr", null)).Returns(mockRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act & Assert - should not throw
        await table.Invoking(t => t.AssertColumnCountAsync(3)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB027_AssertColumnCountAsync_WhenMismatch_Throws()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockFirstRowLocator = new Mock<ILocator>();
        var mockCellsLocator = MockPlaywrightFactory.CreateMockLocator();
        mockCellsLocator.Setup(l => l.CountAsync()).ReturnsAsync(3);
        
        var mockRowsLocator = new Mock<ILocator>();
        mockRowsLocator.Setup(l => l.First).Returns(mockFirstRowLocator.Object);
        mockFirstRowLocator.Setup(l => l.Locator("th, td", null)).Returns(mockCellsLocator.Object);
        
        mockLocator.Setup(l => l.Locator("thead tr, tbody tr", null)).Returns(mockRowsLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act & Assert - should throw
        await table.Invoking(t => t.AssertColumnCountAsync(5)).Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB028_AssertCellTextAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockCellLocator = new Mock<ILocator>();
        mockCellLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Expected Value");
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(1) td:nth-child(1)", null))
            .Returns(mockCellLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act & Assert - should not throw
        await table.Invoking(t => t.AssertCellTextAsync(0, 0, "Expected Value")).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB029_AssertCellTextAsync_WhenMismatch_Throws()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        var mockCellLocator = new Mock<ILocator>();
        mockCellLocator.Setup(l => l.InnerTextAsync(null)).ReturnsAsync("Actual Value");
        
        mockLocator.Setup(l => l.Locator("tbody tr:nth-child(1) td:nth-child(1)", null))
            .Returns(mockCellLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var table = new TableControl(context, "table", null);

        // Act & Assert - should throw
        await table.Invoking(t => t.AssertCellTextAsync(0, 0, "Expected Value")).Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTB030_AssertCellTextAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var table = new TableControl(context, "table", null);

        // Act & Assert - should not throw
        await table.Invoking(t => t.AssertCellTextAsync(0, 0, null)).Should().NotThrowAsync();
    }

    #endregion
}
