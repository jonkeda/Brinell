using Brinell.Maui.Containers;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// One carousel card, scoped to its own root.
/// </summary>
/// <remarks>
/// The card's label id repeats unchanged on every card; the card is handed its root and never
/// locates itself by a unique id.
/// </remarks>
public class CarouselCard : ItemObjectBase<CarouselCards, CarouselCard>
{
    public CarouselCard(CarouselCards collection, IMauiElement itemRoot, int index)
        : base(collection, itemRoot, index)
    {
    }

    /// <summary>The name shown on the card.</summary>
    public Label<CarouselCard> Name => new(this, "CarouselCardLabel");
}
