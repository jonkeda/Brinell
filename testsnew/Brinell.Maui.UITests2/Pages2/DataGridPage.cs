namespace Brinell.Maui.UITests.Pages2;

/// <summary>
/// Page object for the DataGridPage of the Brinell sample MAUI app.
/// Exposes controls from DataGridPage.xaml for testing data management, filtering, and collection operations.
/// </summary>
public class DataGridPage : PageObjectBase<DataGridPage>
{
    public DataGridPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "DataGridPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the title label exists
        return DataGridTitle.IsExists();
    }

    #region Page Title and Headers

    /// <summary>
    /// The main title label "Data Management".
    /// </summary>
    public Label<DataGridPage> DataGridTitle => new(this,"DataGridTitle");

    /// <summary>
    /// The featured items section label.
    /// </summary>
    public Label<DataGridPage> FeaturedLabel => new(this,"FeaturedLabel");

    /// <summary>
    /// The data items section label.
    /// </summary>
    public Label<DataGridPage> DataItemsLabel => new(this,"DataItemsLabel");

    /// <summary>
    /// The grouped items section label.
    /// </summary>
    public Label<DataGridPage> GroupedItemsLabel => new(this,"GroupedItemsLabel");

    #endregion

    #region Search and Filter Controls

    /// <summary>
    /// The search bar for filtering data items.
    /// </summary>
    public SearchBar<DataGridPage> DataSearchBar => new(this,"DataSearchBar");

    /// <summary>
    /// The clear filter button.
    /// </summary>
    public Button<DataGridPage> ClearFilterButton => new(this,"ClearFilterButton");

    /// <summary>
    /// The label showing the selected count.
    /// </summary>
    public Label<DataGridPage> SelectedCountLabel => new(this,"SelectedCountLabel");

    /// <summary>
    /// The search border container.
    /// </summary>
    public Border<DataGridPage> SearchBorder => new(this,"SearchBorder");

    #endregion

    #region Carousel Controls

    /// <summary>
    /// The featured items carousel view.
    /// </summary>
    public CarouselView<DataGridPage> FeaturedCarousel => new(this,"FeaturedCarousel");

    #endregion

    #region Main Collection Views

    /// <summary>
    /// The main collection view displaying filtered data items with typed access to DataItem containers.
    /// </summary>
    public CollectionView<DataGridPage, DataItem> DataCollectionView 
        => new(this, "DataCollectionView", "DataItem_", (scope, index) => new DataItem(scope, index));

    /// <summary>
    /// The grouped collection view displaying grouped data items with typed access to GroupedItem containers.
    /// </summary>
    public CollectionView<DataGridPage, GroupedItem> GroupedCollectionView 
        => new(this, "GroupedCollectionView", "GroupItem_", (scope, index) => new GroupedItem(scope, index));

    #endregion

    #region RefreshView and ScrollView

    /// <summary>
    /// The refresh view for pulling to refresh data.
    /// </summary>
    public RefreshView<DataGridPage> DataRefreshView => new(this,"DataRefreshView");

    /// <summary>
    /// The scroll view containing the main content.
    /// </summary>
    public ScrollView<DataGridPage> DataScrollView => new(this, "DataScrollView");

    #endregion

    #region Action Buttons

    /// <summary>
    /// The refresh data button.
    /// </summary>
    public Button<DataGridPage> RefreshDataButton => new(this,"RefreshDataButton");

    /// <summary>
    /// The select all button.
    /// </summary>
    public Button<DataGridPage> SelectAllButton => new(this,"SelectAllButton");

    /// <summary>
    /// The unselect all button.
    /// </summary>
    public Button<DataGridPage> UnselectAllButton => new(this,"UnselectAllButton");

    #endregion
}
