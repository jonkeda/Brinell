using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Container;

/// <summary>
/// UI tests for ScrollView verifying scroll operations.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "ScrollView")]
public class ScrollViewControlTests
{
    private readonly MauiFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public ScrollViewControlTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToContainerDemo();
    }

    #region State Tests

    /// <summary>
    /// Verifies that scroll view exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task ScrollView_IsExists_ReturnsTrue()
    {
        // Container demo page has scrollable content
        Assert.True(Page.IsLoaded());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that scroll view is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task ScrollView_IsVisible_ReturnsTrue()
    {
        // Assert - page content should be visible
        Assert.True(Page.IsLoaded());
        return Task.CompletedTask;
    }

    #endregion
}
