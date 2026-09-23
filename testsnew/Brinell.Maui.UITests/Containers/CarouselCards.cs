using Brinell.Maui.Containers;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// The collection page's carousel, handing out one <see cref="CarouselCard"/> per card.
/// </summary>
/// <remarks>
/// Cards are discovered by the repeating "CarouselCard" automation id on each card's root. The
/// carousel does not loop: a looping carousel on Windows publishes its cards thousands of times
/// over, and no search through it finishes.
/// </remarks>
public class CarouselCards : CarouselView<CollectionModuleTestPage, CarouselCards, CarouselCard>
{
    public CarouselCards(IMauiScope<CollectionModuleTestPage> parentScope, string automationId)
        : base(parentScope,
               automationId,
               ItemStrategy.ByAutomationId("CarouselCard"),
               (collection, itemRoot, index) => new CarouselCard(collection, itemRoot, index))
    {
    }
}
