using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Collection;

/// <summary>
/// UI tests for PaginatedList verifying pagination controls and page navigation.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "PaginatedList")]
public class PaginatedListControlTests
{
    private readonly AppiumFixture _fixture;
    private CollectionDemoPage Page => _fixture.CollectionDemoPage;

    public PaginatedListControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToCollections();
    }

    #region State Tests

    /// <summary>
    /// Verifies that the paginated list container exists.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task PaginatedList_IsExists_ReturnsTrue()
    {
        Assert.True(Page.PagedListView.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the paginated list container is visible after scrolling into view.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task PaginatedList_IsVisible_ReturnsTrue()
    {
        Page.PagedListView.ScrollIntoView();
        Assert.True(Page.PagedListView.IsVisible());
        return Task.CompletedTask;
    }

    #endregion

    #region Navigation Button Tests

    /// <summary>
    /// Verifies that the next-page button exists.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task PaginatedList_NextPageButton_Exists()
    {
        Assert.True(Page.NextPageButton.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the previous-page button exists.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task PaginatedList_PreviousPageButton_Exists()
    {
        Assert.True(Page.PreviousPageButton.IsExists());
        return Task.CompletedTask;
    }

    #endregion

    #region Page Info Tests

    /// <summary>
    /// Verifies the page info label exists.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task PaginatedList_PageInfoLabel_Exists()
    {
        Assert.True(Page.PageInfoLabel.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies the initial page info shows "Page 1 of 4".
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task PaginatedList_PageInfoLabel_ShowsFirstPage()
    {
        var text = Page.PageInfoLabel.GetText();
        Assert.Equal("Page 1 of 4", text);
        return Task.CompletedTask;
    }

    #endregion
}
