using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Collection;

/// <summary>
/// UI tests for the IndicatorView on the collection module page, bound to a carousel of five
/// seeded cards.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "IndicatorView")]
public class IndicatorViewTests
{
    private readonly MauiFixture _fixture;

    public IndicatorViewTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.Collection);
    }

    private CollectionModuleTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetCount")]
    public Task IndicatorView_Open_ShowsOneDotPerCardWithTheFirstSelected()
    {
        GetPage().TestIndicatorView
            .AssertVisible(true)
            .TestIndicatorView.AssertCount(CollectionModuleTestPage.SeedCount)
            .TestIndicatorView.AssertPosition(0);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetPosition")]
    public Task IndicatorView_CarouselSwiped_SelectsTheNextDot()
    {
        var page = GetPage();

        page.TestCarouselView.SwipeNext();

        page.TestIndicatorView.AssertPosition(1);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetCount")]
    public Task IndicatorView_CardAdded_AddsADot()
    {
        GetPage().AddButton.Click()
            .CountLabel.AssertText("6")
            .TestIndicatorView.AssertCount(CollectionModuleTestPage.SeedCount + 1)
            .RemoveButton.Click()
            .TestIndicatorView.AssertCount(CollectionModuleTestPage.SeedCount);
        return Task.CompletedTask;
    }
}
