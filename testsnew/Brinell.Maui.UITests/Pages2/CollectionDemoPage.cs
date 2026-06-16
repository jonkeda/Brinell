namespace Brinell.Maui.UITests.Pages2;

/// <summary>
/// Page object for the Collections tab demonstrating CarouselView, TableView, and PaginatedList.
/// Exposes controls from CollectionDemoView.xaml with their AutomationIds.
/// </summary>
public class CollectionDemoPage : PageObjectBase<CollectionDemoPage>
{
    public CollectionDemoPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "CollectionDemoPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return CollectionDemoTitle.IsExists();
    }

    #region Labels

    /// <summary>
    /// The page-level ScrollView wrapping all content.
    /// </summary>
    public ScrollView<CollectionDemoPage> CollectionDemoScrollView => new(this, "CollectionDemoScrollView");

    /// <summary>
    /// The main title label "Collections Demo".
    /// </summary>
    public Label<CollectionDemoPage> CollectionDemoTitle => new(this, "CollectionDemoTitle");

    /// <summary>
    /// The carousel position label.
    /// </summary>
    public Label<CollectionDemoPage> CarouselPositionLabel => new(this, "CarouselPositionLabel");

    /// <summary>
    /// The page info label showing "Page X of Y".
    /// </summary>
    public Label<CollectionDemoPage> PageInfoLabel => new(this, "PageInfoLabel");

    #endregion

    #region CarouselView

    /// <summary>
    /// The demo CarouselView control.
    /// </summary>
    public CarouselView<CollectionDemoPage> DemoCarouselView => new(this, "DemoCarouselView");

    #endregion

    #region TableView

    /// <summary>
    /// The demo TableView control with settings intent.
    /// </summary>
    public TableView<CollectionDemoPage> DemoTableView => new(this, "DemoTableView");

    #endregion

    #region PaginatedList

    /// <summary>
    /// The paginated list container (CollectionView displaying current page items).
    /// </summary>
    public CollectionView<CollectionDemoPage> PagedListView => new(this, "PagedListView");

    /// <summary>
    /// The previous-page navigation button.
    /// </summary>
    public Button<CollectionDemoPage> PreviousPageButton => new(this, "PreviousPageButton");

    /// <summary>
    /// The next-page navigation button.
    /// </summary>
    public Button<CollectionDemoPage> NextPageButton => new(this, "NextPageButton");

    #endregion
}
