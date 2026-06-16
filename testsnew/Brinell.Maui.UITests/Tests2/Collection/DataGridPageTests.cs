using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Collection;

/// <summary>
/// UI tests for DataGridPage verifying data management, search/filter, selection, and collection operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "DataGrid")]
public class DataGridPageTests
{
    private readonly AppiumFixture _fixture;
    private DataGridPage Page => new DataGridPage(_fixture.Context);

    public DataGridPageTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        // Navigate to DataGridPage via the app fixture
        _fixture.NavigateToDataGrid();
    }

    #region State Tests

    /// <summary>
    /// Verifies that the DataGridPage loads successfully.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsLoaded")]
    public Task DataGridPage_IsLoaded_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.IsLoaded());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the main title is visible on page load.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_Title_IsVisible()
    {
        // Assert
        Assert.True(Page.DataGridTitle.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the featured carousel is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_Carousel_IsVisible()
    {
        // Assert
        Assert.True(Page.FeaturedCarousel.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the main collection view is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_CollectionView_IsVisible()
    {
        // Assert
        Assert.True(Page.DataCollectionView.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the grouped collection view is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_GroupedCollectionView_IsVisible()
    {
        // Assert
        Assert.True(Page.GroupedCollectionView.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that all section labels are visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_SectionLabels_AreVisible()
    {
        // Assert
        Assert.True(Page.FeaturedLabel.IsVisible());
        Assert.True(Page.DataItemsLabel.IsVisible());
        Assert.True(Page.GroupedItemsLabel.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Search and Filter Tests

    /// <summary>
    /// Verifies that the search bar is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_SearchBar_IsVisible()
    {
        // Assert
        Assert.True(Page.DataSearchBar.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the clear filter button is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_ClearFilterButton_IsVisible()
    {
        // Assert
        Assert.True(Page.ClearFilterButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the selected count label is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_SelectedCountLabel_IsVisible()
    {
        // Assert
        Assert.True(Page.SelectedCountLabel.IsVisible());
        return Task.CompletedTask;
    }

    /*/// <summary>
    /// Verifies that the search bar can accept text input.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SetText")]
    public Task DataGridPage_SearchBar_CanSetText()
    {
        // Act
        Page.DataSearchBar.SetText("test");

        // Assert - Verify text was set (would need to read the value back)
        return Task.CompletedTask;
    }*/

    #endregion

    #region Action Buttons Tests

    /// <summary>
    /// Verifies that the refresh button is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_RefreshButton_IsVisible()
    {
        // Assert
        Assert.True(Page.RefreshDataButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the select all button is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_SelectAllButton_IsVisible()
    {
        // Assert
        Assert.True(Page.SelectAllButton.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the unselect all button is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_UnselectAllButton_IsVisible()
    {
        // Assert
        Assert.True(Page.UnselectAllButton.IsVisible());
        return Task.CompletedTask;
    }

    /*
    /// <summary>
    /// Verifies that the select all button can be clicked.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task DataGridPage_SelectAllButton_CanClick()
    {
        // Act & Assert
        Page.SelectAllButton.Click();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the refresh button can be clicked.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task DataGridPage_RefreshButton_CanClick()
    {
        // Act & Assert
        Page.RefreshDataButton.Click();
        return Task.CompletedTask;
    }*/

    #endregion

    #region Data Item Tests

    /// <summary>
    /// Verifies that the first data item exists and has expected content.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task DataGridPage_FirstDataItem_IsExists()
    {
        // Arrange
        var itemIndex = 0;

        // Act & Assert
        var item = Page.DataCollectionView.Item(itemIndex);
        Assert.NotNull(item);
        Assert.True(item.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that item title is visible for the first item.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_ItemTitle_IsVisible()
    {
        // Arrange
        var itemIndex = 0;

        // Act & Assert
        var item = Page.DataCollectionView.Item(itemIndex);
        Assert.NotNull(item);
        Assert.True(item.Title.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that item status is visible for the first item.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_ItemStatus_IsVisible()
    {
        // Arrange
        var itemIndex = 0;

        // Act & Assert
        var item = Page.DataCollectionView.Item(itemIndex);
        Assert.NotNull(item);
        Assert.True(item.Status.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Group Data Tests

    /// <summary>
    /// Verifies that a group item is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_GroupItem_IsVisible()
    {
        // Arrange
        var itemIndex = 0;

        // Act & Assert
        var item = Page.GroupedCollectionView.Item(itemIndex);
        Assert.NotNull(item);
        Assert.True(item.Title.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region RefreshView Tests

    /// <summary>
    /// Verifies that the refresh view is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_RefreshView_IsVisible()
    {
        // Assert
        Assert.True(Page.DataRefreshView.IsVisible());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the scroll view is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DataGridPage_ScrollView_IsVisible()
    {
        // Assert
        Assert.True(Page.DataScrollView.IsVisible());
        return Task.CompletedTask;
    }

    #endregion
}
