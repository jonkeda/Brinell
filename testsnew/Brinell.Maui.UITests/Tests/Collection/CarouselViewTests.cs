using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Collection;

/// <summary>UI tests for the CarouselView on the collection module page, holding five seeded cards.</summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "CarouselView")]
public class CarouselViewTests
{
    private readonly MauiFixture _fixture;

    public CarouselViewTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.Collection);
    }

    private CollectionModuleTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Item")]
    public Task CarouselView_Items_AreTheSeededCardsInOrder()
    {
        var carousel = GetPage().TestCarouselView
            .AssertItemCount(CollectionModuleTestPage.SeedCount);

        carousel.Item(0).Name.AssertText("Alpha");
        carousel.Item(4).Name.AssertText("Echo");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetPosition")]
    public Task CarouselView_Open_ShowsTheFirstCardOnly()
    {
        var carousel = GetPage().TestCarouselView.AssertPosition(0);

        carousel.Item(0).Name.AssertVisibleAfterScroll();
        carousel.TryItem(1)?.Name.AssertVisible(false);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SwipeNext")]
    public Task CarouselView_SwipeNext_ShowsTheNextCard()
    {
        var page = GetPage();

        var carousel = page.TestCarouselView.SwipeNext().AssertPosition(1);

        carousel.Item(1).Name.AssertVisibleAfterScroll();
        carousel.TryItem(0)?.Name.AssertVisible(false);
        page.CarouselPositionLabel.AssertText("1");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SwipePrevious")]
    public Task CarouselView_SwipePrevious_ShowsThePreviousCard()
    {
        var page = GetPage();

        var carousel = page.TestCarouselView
            .SwipeNext()
            .SwipeNext()
            .AssertPosition(2)
            .SwipePrevious()
            .AssertPosition(1);

        carousel.Item(1).Name.AssertVisibleAfterScroll();

        // Card 2 may no longer be realized once the carousel moved back: virtualization drops it.
        // Not realized is not shown, so only a realized card 2 has to prove it is off screen.
        carousel.TryItem(2)?.Name.AssertVisible(false);
        page.CarouselPositionLabel.AssertText("1");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetPosition")]
    public Task CarouselView_BoundPositionChanges_CarouselFollows()
    {
        var page = GetPage();

        page.CarouselNextButton.Click();

        var carousel = page.TestCarouselView.AssertPosition(1);
        carousel.Item(1).Name.AssertVisibleAfterScroll();
        carousel.TryItem(0)?.Name.AssertVisible(false);
        return Task.CompletedTask;
    }
}
