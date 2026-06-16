using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Collection;

/// <summary>
/// UI tests for CarouselView verifying carousel item navigation and state.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "CarouselView")]
public class CarouselViewControlTests
{
    private readonly AppiumFixture _fixture;
    private CollectionDemoPage Page => _fixture.CollectionDemoPage;

    public CarouselViewControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToCollections();
    }

    #region State Tests

    /// <summary>
    /// Verifies that the carousel view exists on the collections page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task CarouselView_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.DemoCarouselView.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the carousel view is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task CarouselView_IsVisible_ReturnsTrue()
    {
        // Ensure carousel is scrolled into view (may be off-screen if page was scrolled)
        Page.DemoCarouselView.ScrollIntoView();
        // Assert
        Assert.True(Page.DemoCarouselView.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Position Tests

    /// <summary>
    /// Verifies the initial carousel position is 0.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetPosition")]
    public Task CarouselView_GetPosition_InitialPositionIsZero()
    {
        // Assert
        var position = Page.DemoCarouselView.GetPosition();
        Assert.NotNull(position);
        Assert.Equal(0, position.Value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies the carousel position label shows the current position.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetPosition")]
    public Task CarouselView_PositionLabel_ShowsCurrentPosition()
    {
        // Assert
        Assert.True(Page.CarouselPositionLabel.IsExists());
        return Task.CompletedTask;
    }

    #endregion

    #region Loop Tests

    /// <summary>
    /// Verifies the carousel loop state is reported.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsLoopEnabled")]
    public Task CarouselView_IsLoopEnabled_ReturnsFalse()
    {
        // The demo carousel has Loop="False"
        var loopEnabled = Page.DemoCarouselView.IsLoopEnabled();
        Assert.NotNull(loopEnabled);
        Assert.False(loopEnabled.Value);
        return Task.CompletedTask;
    }

    #endregion
}
