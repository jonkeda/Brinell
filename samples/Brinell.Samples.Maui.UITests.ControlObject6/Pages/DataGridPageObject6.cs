using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;
using Brinell.Maui.ControlObject6.Controls;
using Brinell.Maui.ControlObject6.Pages;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Pages;

/// <summary>
/// Page object for the DataGridPage using ControlObject6 API.
/// Based on actual DataGridPage.xaml - tests CollectionView, RefreshView, SearchBar, SwipeView.
/// </summary>
public class DataGridPageObject6 : PageObjectBase
{
    public override string Name => "DataGridPage";

    protected override ControlLocator PageLocator => By.AutomationId("DataGridTitle");

    public DataGridPageObject6(MauiTestContext context) : base(context)
    {
    }

    #region Headers

    /// <summary>Page title label.</summary>
    public LabelControl DataGridTitle => new(Context, "DataGridTitle", this);

    /// <summary>Featured items label.</summary>
    public LabelControl FeaturedLabel => new(Context, "FeaturedLabel", this);

    /// <summary>Data items label.</summary>
    public LabelControl DataItemsLabel => new(Context, "DataItemsLabel", this);

    /// <summary>Grouped items label.</summary>
    public LabelControl GroupedItemsLabel => new(Context, "GroupedItemsLabel", this);

    #endregion

    #region Search and Filter

    /// <summary>Data search bar.</summary>
    public EntryControl DataSearchBar => new(Context, "DataSearchBar", this);

    /// <summary>Clear filter button.</summary>
    public ButtonControl ClearFilterButton => new(Context, "ClearFilterButton", this);

    /// <summary>Selected count label.</summary>
    public LabelControl SelectedCountLabel => new(Context, "SelectedCountLabel", this);

    #endregion

    #region Collection Views

    /// <summary>Main data collection view with items.</summary>
    public CollectionViewControl DataCollectionView => new(Context, "DataCollectionView", this);

    /// <summary>Grouped collection view.</summary>
    public CollectionViewControl GroupedCollectionView => new(Context, "GroupedCollectionView", this);

    /// <summary>Featured carousel.</summary>
    public CollectionViewControl FeaturedCarousel => new(Context, "FeaturedCarousel", this);

    #endregion

    #region Scroll and Refresh

    /// <summary>Data scroll view.</summary>
    public ScrollViewControl DataScrollView => new(Context, "DataScrollView", this);

    /// <summary>Data refresh view.</summary>
    public RefreshViewControl DataRefreshView => new(Context, "DataRefreshView", this);

    #endregion

    #region Action Buttons

    /// <summary>Refresh data button.</summary>
    public ButtonControl RefreshDataButton => new(Context, "RefreshDataButton", this);

    /// <summary>Select all button.</summary>
    public ButtonControl SelectAllButton => new(Context, "SelectAllButton", this);

    /// <summary>Unselect all button.</summary>
    public ButtonControl UnselectAllButton => new(Context, "UnselectAllButton", this);

    #endregion

    #region Page Actions

    /// <summary>Gets the number of data items.</summary>
    public int GetDataItemCount()
    {
        return DataCollectionView.GetItemCount();
    }

    /// <summary>Selects a data item by index.</summary>
    public DataGridPageObject6 SelectDataItem(int index)
    {
        DataCollectionView.SelectItem(index);
        return this;
    }

    /// <summary>Clicks a data item by index.</summary>
    public DataGridPageObject6 ClickDataItem(int index)
    {
        DataCollectionView.ClickItem(index);
        return this;
    }

    /// <summary>Gets the text of a data item by index.</summary>
    public string GetDataItemText(int index)
    {
        return DataCollectionView.GetItemText(index);
    }

    /// <summary>Refreshes the data list.</summary>
    public DataGridPageObject6 RefreshData()
    {
        RefreshDataButton.Click();
        return this;
    }

    /// <summary>Clears the search filter.</summary>
    public DataGridPageObject6 ClearFilter()
    {
        ClearFilterButton.Click();
        return this;
    }

    /// <summary>Selects all items.</summary>
    public DataGridPageObject6 SelectAll()
    {
        SelectAllButton.Click();
        return this;
    }

    /// <summary>Unselects all items.</summary>
    public DataGridPageObject6 UnselectAll()
    {
        UnselectAllButton.Click();
        return this;
    }

    #endregion
}
