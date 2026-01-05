using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for TableControl functionality using the Dashboard page.
/// </summary>
[Collection("BlazorUITests")]
public class TableTests : BlazorSampleTestBase
{
    public TableTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Table_GetRowCount_ReturnsCorrectCount()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act
        var rowCount = dashboard.GetActivityRowCount();

        // Assert
        Assert.Equal(5, rowCount); // Dashboard has 5 activity rows
    }

    [Fact]
    public void Table_GetHeaders_ReturnsCorrectHeaders()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act
        var headers = dashboard.GetActivityHeaders();

        // Assert
        Assert.Equal(3, headers.Count);
        Assert.Equal("Date", headers[0]);
        Assert.Equal("Action", headers[1]);
        Assert.Equal("Status", headers[2]);
    }

    [Fact]
    public void Table_GetRowCells_ReturnsCorrectData()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act
        var row = dashboard.GetActivityRow(0);

        // Assert
        Assert.Equal(3, row.Count);
        Assert.Equal("2024-12-30", row[0]);
        Assert.Equal("User Login", row[1]);
        Assert.Contains("Success", row[2]);
    }

    [Fact]
    public void Table_GetCellText_ReturnsSpecificCell()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act
        var cellText = dashboard.ActivityTable.GetCellText(1, 1);

        // Assert
        Assert.Equal("Test Suite Run", cellText);
    }

    [Fact]
    public void Table_HasRowContaining_FindsRow()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act & Assert
        Assert.True(dashboard.ActivityTable.HasRowContaining("Configuration Update"));
        Assert.False(dashboard.ActivityTable.HasRowContaining("Nonexistent Action"));
    }

    [Fact]
    public void Table_FindRowContaining_ReturnsCorrectIndex()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act
        var index = dashboard.ActivityTable.FindRowContaining("Configuration Update");

        // Assert
        Assert.Equal(2, index); // Third row (0-indexed = 2)
    }

    [Fact]
    public void Table_GetColumnCells_ReturnsAllValuesInColumn()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act
        var actions = dashboard.ActivityTable.GetColumnCells(1); // Action column

        // Assert
        Assert.Equal(5, actions.Count);
        Assert.Contains("User Login", actions);
        Assert.Contains("Test Suite Run", actions);
        Assert.Contains("Configuration Update", actions);
        Assert.Contains("Data Export", actions);
        Assert.Contains("System Backup", actions);
    }

    [Fact]
    public void Table_AssertRowCount_PassesWithCorrectCount()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act & Assert - should not throw
        dashboard.ActivityTable.AssertRowCount(5);
    }

    [Fact]
    public void Table_AssertCellText_PassesWithCorrectText()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act & Assert - should not throw
        dashboard.ActivityTable.AssertCellText(0, 0, "2024-12-30");
        dashboard.ActivityTable.AssertCellText(0, 1, "User Login");
    }

    [Fact]
    public void Table_IsVisible_WhenDisplayed()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboard = new DashboardPage(Context!);
        dashboard.WaitForDisplayed();

        // Act & Assert
        Assert.True(dashboard.HasActivityTable());
        dashboard.ActivityTable.AssertVisible("Activity table should be visible");
    }
}
