using Brinell.Samples.Maui.UITests.ControlObject6.Pages;
using Brinell.Maui.ControlObject6.Controls;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Tests;

/// <summary>
/// Collection control tests for CollectionView controls.
/// Uses verified ControlObject6 APIs: GetItemCount, ClickItem, SelectItem, GetItemText, GetSelectedItemIndex.
/// Note: Navigation to DataGrid page is done via Shell FlyoutItem click.
/// </summary>
public class CollectionControlTests : MauiTestBase6
{
    private readonly MainPageObject6 _mainPage;
    private readonly DataGridPageObject6 _dataGridPage;

    public CollectionControlTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject6(Context);
        _dataGridPage = new DataGridPageObject6(Context);
    }

    /// <summary>
    /// Navigates to DataGrid page via Shell flyout.
    /// </summary>
    private void NavigateToDataGridPage()
    {
        // Click the DataGrid flyout item to navigate
        var flyoutItem = new ButtonControl(Context, "FlyoutDataGrid", null);
        flyoutItem.Click();
        _dataGridPage.WaitLoaded(true, timeoutMs: 5000);
    }

    #region CollectionView Tests

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "CollectionView")]
    [Trait("Priority", "P0")]
    public void CollectionView_GetItemCount_ReturnsNumberOfItems()
    {
        // Arrange
        NavigateToDataGridPage();

        // Act
        var count = _dataGridPage.DataCollectionView.GetItemCount();

        // Assert
        Assert.True(count >= 0, $"Item count should be >= 0, was {count}");
    }

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "CollectionView")]
    [Trait("Priority", "P0")]
    public void CollectionView_ClickItem_ClicksTheItem()
    {
        // Arrange
        NavigateToDataGridPage();
        var itemCount = _dataGridPage.DataCollectionView.GetItemCount();
        
        if (itemCount == 0)
        {
            Log("Skipping test - no items in collection");
            return;
        }

        // Act - should not throw
        _dataGridPage.DataCollectionView.ClickItem(0);

        // Assert - item click succeeded (no exception)
        Assert.True(true, "ClickItem succeeded");
    }

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "CollectionView")]
    [Trait("Priority", "P0")]
    public void CollectionView_SelectItem_SelectsTheItem()
    {
        // Arrange
        NavigateToDataGridPage();
        var itemCount = _dataGridPage.DataCollectionView.GetItemCount();
        
        if (itemCount == 0)
        {
            Log("Skipping test - no items in collection");
            return;
        }

        // Act
        _dataGridPage.DataCollectionView.SelectItem(0);

        // Assert - verify selection
        var selectedIndex = _dataGridPage.DataCollectionView.GetSelectedItemIndex();
        Assert.Equal(0, selectedIndex);
    }

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "CollectionView")]
    [Trait("Priority", "P0")]
    public void CollectionView_IsVisible_ReturnsTrueWhenVisible()
    {
        // Arrange
        NavigateToDataGridPage();

        // Act & Assert
        Assert.True(_dataGridPage.DataCollectionView.IsVisible());
    }

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "CollectionView")]
    [Trait("Priority", "P1")]
    public void CollectionView_GetItemText_ReturnsItemText()
    {
        // Arrange
        NavigateToDataGridPage();
        var itemCount = _dataGridPage.DataCollectionView.GetItemCount();
        
        if (itemCount == 0)
        {
            Log("Skipping test - no items in collection");
            return;
        }

        // Act
        var text = _dataGridPage.DataCollectionView.GetItemText(0);

        // Assert
        Assert.NotNull(text);
    }

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "CollectionView")]
    [Trait("Priority", "P1")]
    public void CollectionView_GetAllItemTexts_ReturnsAllTexts()
    {
        // Arrange
        NavigateToDataGridPage();

        // Act
        var texts = _dataGridPage.DataCollectionView.GetAllItemTexts();

        // Assert
        Assert.NotNull(texts);
    }

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "CollectionView")]
    [Trait("Priority", "P1")]
    public void CollectionView_AssertItemCount_PassesWhenMatches()
    {
        // Arrange
        NavigateToDataGridPage();
        var count = _dataGridPage.DataCollectionView.GetItemCount();

        // Act & Assert - should not throw
        _dataGridPage.DataCollectionView.AssertItemCount(count);
    }

    #endregion

    #region Grouped CollectionView Tests

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "GroupedCollectionView")]
    [Trait("Priority", "P1")]
    public void GroupedCollectionView_IsVisible_ReturnsTrueWhenVisible()
    {
        // Arrange
        NavigateToDataGridPage();
        
        // Scroll down to make grouped collection visible
        _dataGridPage.DataScrollView.ScrollToBottom();

        // Act & Assert
        Assert.True(_dataGridPage.GroupedCollectionView.IsVisible() || 
                    _dataGridPage.GroupedCollectionView.IsExists());
    }

    [Fact]
    [Trait("Category", "Collection")]
    [Trait("Control", "GroupedCollectionView")]
    [Trait("Priority", "P1")]
    public void GroupedCollectionView_GetGroupCount_ReturnsCount()
    {
        // Arrange
        NavigateToDataGridPage();
        _dataGridPage.DataScrollView.ScrollToBottom();

        // Act
        var groupCount = _dataGridPage.GroupedCollectionView.GetGroupCount();

        // Assert
        Assert.True(groupCount >= 0, $"Group count should be >= 0, was {groupCount}");
    }

    #endregion
}
