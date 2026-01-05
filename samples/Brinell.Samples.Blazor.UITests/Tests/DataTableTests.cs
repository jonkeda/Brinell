using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for the DataTable page functionality.
/// </summary>
[Collection("BlazorUITests")]
public class DataTableTests : BlazorSampleTestBase
{
    public DataTableTests(ITestOutputHelper output) : base(output)
    {
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGE DISPLAY TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DataTable_InitialLoad_DisplaysTable()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/datatable");

        var dataTablePage = new DataTablePage(Context!);
        dataTablePage.WaitForDisplayed();

        // Assert
        dataTablePage.AssertDisplayed("DataTable page should be displayed");
        dataTablePage.DataTableTitle.AssertVisible("Title should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // SEARCH TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DataTable_Search_FiltersData()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/datatable");

        var dataTablePage = new DataTablePage(Context!);
        dataTablePage.WaitForDisplayed();

        // Act
        dataTablePage.SearchFor("test");

        // Assert
        dataTablePage.SearchInput.AssertTextEquals("test");
    }

    [Fact]
    public void DataTable_ClearFilters_Works()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/datatable");

        var dataTablePage = new DataTablePage(Context!);
        dataTablePage.WaitForDisplayed();
        dataTablePage.SearchFor("test");

        // Act
        dataTablePage.ClearFilters();

        // Assert
        dataTablePage.DataTable.AssertExists("Data table should exist after clearing filters");
    }

    // ═══════════════════════════════════════════════════════════════
    // FILTER TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DataTable_Filters_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/datatable");

        var dataTablePage = new DataTablePage(Context!);
        dataTablePage.WaitForDisplayed();

        // Assert
        dataTablePage.SearchInput.AssertVisible("Search input should be visible");
        dataTablePage.CategoryFilter.AssertVisible("Category filter should be visible");
        dataTablePage.StatusFilter.AssertVisible("Status filter should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGINATION TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DataTable_Pagination_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/datatable");

        var dataTablePage = new DataTablePage(Context!);
        dataTablePage.WaitForDisplayed();

        // Assert
        dataTablePage.PrevPageButton.AssertExists("Previous page button should exist");
        dataTablePage.NextPageButton.AssertExists("Next page button should exist");
        dataTablePage.PageSizeSelector.AssertExists("Page size selector should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // BULK ACTION TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DataTable_BulkActions_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/datatable");

        var dataTablePage = new DataTablePage(Context!);
        dataTablePage.WaitForDisplayed();

        // Assert
        dataTablePage.SelectAllButton.AssertExists("Select all button should exist");
        dataTablePage.ClearSelectionButton.AssertExists("Clear selection button should exist");
        dataTablePage.DeleteSelectedButton.AssertExists("Delete selected button should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // REFRESH TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DataTable_Refresh_Works()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/datatable");

        var dataTablePage = new DataTablePage(Context!);
        dataTablePage.WaitForDisplayed();

        // Act
        dataTablePage.Refresh();

        // Assert - Still on same page
        dataTablePage.DataTable.AssertExists("Data table should exist after refresh");
    }
}
