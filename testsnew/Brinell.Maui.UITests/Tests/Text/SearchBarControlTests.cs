using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Text;

/// <summary>
/// UI tests for MauiSearchBarControl verifying search text operations.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "SearchBar")]
public class SearchBarControlTests
{
    private readonly AppiumFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public SearchBarControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that search bar exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task SearchBar_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.UserSearchBar.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that search bar is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task SearchBar_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.UserSearchBar.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region Text Entry Tests

    /// <summary>
    /// Verifies Enter() sets search text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Enter")]
    public Task SearchBar_Enter_SetsText()
    {
        // Arrange
        Page.UserSearchBar.Clear();

        // Act
        Page.UserSearchBar.Enter("search query");

        // Assert
        Assert.Equal("search query", Page.UserSearchBar.GetText());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies Clear() removes search text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Clear")]
    public Task SearchBar_Clear_RemovesText()
    {
        // Arrange
        Page.UserSearchBar.Enter("text to clear");

        // Act
        Page.UserSearchBar.Clear();

        // Assert
        Assert.Equal("", Page.UserSearchBar.GetText());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies GetText() returns entered text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task SearchBar_GetText_ReturnsEnteredText()
    {
        // Arrange
        Page.UserSearchBar.Clear();
        const string searchTerm = "John Doe";
        Page.UserSearchBar.Enter(searchTerm);

        // Act
        var text = Page.UserSearchBar.GetText();

        // Assert
        Assert.Equal(searchTerm, text);
        return Task.CompletedTask;
    }

    #endregion

    #region Search Operation Tests

    /// <summary>
    /// Verifies Search() enters text and triggers search action.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Search")]
    public Task SearchBar_Search_EntersTextAndTriggersSearch()
    {
        // Arrange
        Page.UserSearchBar.Clear();

        // Act - Search() enters text and submits
        Page.UserSearchBar.Search("test search");

        // Assert - verify text was entered
        Assert.Equal("test search", Page.UserSearchBar.GetText());
        return Task.CompletedTask;
    }

    #endregion

    #region Assertion Tests

    /// <summary>
    /// Verifies AssertText passes with correct text.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertText")]
    public Task SearchBar_AssertText_PassesWithCorrectText()
    {
        // Arrange
        Page.UserSearchBar.Clear();
        Page.UserSearchBar.Enter("expected search");

        // Assert - no exception means success
        Page.UserSearchBar.AssertText("expected search");
        return Task.CompletedTask;
    }

    #endregion
}
