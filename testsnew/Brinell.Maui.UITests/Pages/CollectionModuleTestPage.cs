using Brinell.Maui.UITests.Containers;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the collection module page: the CarouselView with its IndicatorView, and the
/// buttons that change the items they share.
/// </summary>
/// <remarks>
/// The page holds five seeded cards. Each open from the hub pushes a fresh page with a fresh view
/// model, so a test starts from the seed.
/// </remarks>
public class CollectionModuleTestPage : PageObjectBase<CollectionModuleTestPage>
{
    /// <summary>The number of items the page seeds on open and on reset.</summary>
    public const int SeedCount = 5;

    public CollectionModuleTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "CollectionModulePage";

    #region Carousel

    /// <summary>The carousel of item cards.</summary>
    public CarouselCards TestCarouselView => new(this, "TestCarouselView");

    /// <summary>The indicator dots bound to the carousel.</summary>
    public IndicatorView<CollectionModuleTestPage> TestIndicatorView => new(this, "TestIndicatorView");

    /// <summary>The label showing the carousel's position, as the page's view model holds it.</summary>
    public Label<CollectionModuleTestPage> CarouselPositionLabel => new(this, "CarouselPositionLabel");

    /// <summary>Moves the carousel to the previous card through its bound position.</summary>
    public Button<CollectionModuleTestPage> CarouselPreviousButton => new(this, "CarouselPreviousButton");

    /// <summary>Moves the carousel to the next card through its bound position.</summary>
    public Button<CollectionModuleTestPage> CarouselNextButton => new(this, "CarouselNextButton");

    #endregion

    #region Items

    /// <summary>The number of items, as the page reports it.</summary>
    public Label<CollectionModuleTestPage> CountLabel => new(this, "CollectionCountLabel");

    /// <summary>Appends an item to the list and the carousel.</summary>
    public Button<CollectionModuleTestPage> AddButton => new(this, "CollectionAddButton");

    /// <summary>Removes the last item.</summary>
    public Button<CollectionModuleTestPage> RemoveButton => new(this, "CollectionRemoveButton");

    /// <summary>Restores the seeded items and moves the carousel back to the first card.</summary>
    public Button<CollectionModuleTestPage> ResetButton => new(this, "CollectionResetButton");

    #endregion
}
