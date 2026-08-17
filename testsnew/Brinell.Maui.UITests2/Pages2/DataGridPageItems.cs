namespace Brinell.Maui.UITests.Pages2;

/// <summary>
/// Container for a data item in the DataCollectionView.
/// Owns the structure and controls that make up a data item row.
/// </summary>
public class DataItem : ControlBase<DataGridPage>
{
    private readonly int _itemId;
    private readonly IMauiScope<DataGridPage> _scope;

    public DataItem(IMauiScope<DataGridPage> scope, int itemId)
        : base(scope, $"DataItem_{itemId}")
    {
        _itemId = itemId;
        _scope = scope;
    }

    /// <summary>
    /// Gets the title label for this data item.
    /// </summary>
    public Label<DataGridPage> Title => new(_scope, $"ItemTitle_{_itemId}");

    /// <summary>
    /// Gets the description label for this data item.
    /// </summary>
    public Label<DataGridPage> Description => new(_scope, $"ItemDesc_{_itemId}");

    /// <summary>
    /// Gets the status label for this data item.
    /// </summary>
    public Label<DataGridPage> Status => new(_scope, $"ItemStatus_{_itemId}");

    /// <summary>
    /// Gets the star indicator label for this data item (visible when starred).
    /// </summary>
    public Label<DataGridPage> Star => new(_scope, $"ItemStar_{_itemId}");

    /// <summary>
    /// Gets the swipe view container for this data item.
    /// </summary>
    public SwipeView<DataGridPage> ItemSwipeView => new(_scope, $"SwipeItem_{_itemId}");

    /// <summary>
    /// Gets the star swipe action for this data item.
    /// </summary>
    public Label<DataGridPage> StarSwipeAction => new(_scope, $"StarSwipe_{_itemId}");

    /// <summary>
    /// Gets the delete swipe action for this data item.
    /// </summary>
    public Label<DataGridPage> DeleteSwipeAction => new(_scope, $"DeleteSwipe_{_itemId}");
}

/// <summary>
/// Container for a grouped item in the GroupedCollectionView.
/// Owns the structure and controls that make up a grouped item row.
/// </summary>
public class GroupedItem : ControlBase<DataGridPage>
{
    private readonly int _itemId;
    private readonly IMauiScope<DataGridPage> _scope;

    public GroupedItem(IMauiScope<DataGridPage> scope, int itemId)
        : base(scope, $"GroupItem_{itemId}")
    {
        _itemId = itemId;
        _scope = scope;
    }

    /// <summary>
    /// Gets the title label for this grouped item.
    /// </summary>
    public Label<DataGridPage> Title => new(_scope, $"GroupItem_{_itemId}");
}
