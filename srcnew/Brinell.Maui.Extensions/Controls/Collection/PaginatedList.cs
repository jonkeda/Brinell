namespace Brinell.Maui.Extensions.Controls.Collection;

/// <summary>
/// MAUI control for displaying a paginated list of items with next/previous page navigation.
/// Extends List with page-level operations: tracking current page, total pages,
/// and interacting with load-more / next-page buttons.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public class PaginatedList<TScope, TItem> : List<TScope, TItem>
    where TScope : IMauiScope<TScope>
    where TItem : class
{
    private readonly Locator? _nextPageLocator;
    private readonly Locator? _previousPageLocator;
    private readonly Locator? _pageInfoLocator;

    /// <summary>
    /// Creates a PaginatedList control.
    /// </summary>
    /// <param name="scope">The containing scope.</param>
    /// <param name="listLocator">Locator for the list container element.</param>
    /// <param name="itemAutomationIdPrefix">Prefix for item AutomationIds (e.g., "PagedItem_").</param>
    /// <param name="itemFactory">Factory to create item containers.</param>
    /// <param name="nextPageLocator">Locator for the next-page button.</param>
    /// <param name="previousPageLocator">Locator for the previous-page button.</param>
    /// <param name="pageInfoLocator">Locator for the page-info label (e.g., "Page 1 of 5").</param>
    public PaginatedList(
        IMauiScope<TScope> scope,
        Locator listLocator,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory,
        Locator? nextPageLocator = null,
        Locator? previousPageLocator = null,
        Locator? pageInfoLocator = null)
        : base(scope, listLocator, itemAutomationIdPrefix, itemFactory)
    {
        _nextPageLocator = nextPageLocator;
        _previousPageLocator = previousPageLocator;
        _pageInfoLocator = pageInfoLocator;
    }

    /// <summary>
    /// Creates a PaginatedList control using automation IDs.
    /// </summary>
    /// <param name="scope">The containing scope.</param>
    /// <param name="listAutomationId">AutomationId for the list container.</param>
    /// <param name="itemAutomationIdPrefix">Prefix for item AutomationIds.</param>
    /// <param name="itemFactory">Factory to create item containers.</param>
    /// <param name="nextPageAutomationId">AutomationId for the next-page button.</param>
    /// <param name="previousPageAutomationId">AutomationId for the previous-page button.</param>
    /// <param name="pageInfoAutomationId">AutomationId for the page-info label.</param>
    public PaginatedList(
        IMauiScope<TScope> scope,
        string listAutomationId,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory,
        string? nextPageAutomationId = null,
        string? previousPageAutomationId = null,
        string? pageInfoAutomationId = null)
        : base(scope, listAutomationId, itemAutomationIdPrefix, itemFactory)
    {
        _nextPageLocator = nextPageAutomationId != null
            ? new Locator(LocatorStrategy.AutomationId, nextPageAutomationId)
            : null;
        _previousPageLocator = previousPageAutomationId != null
            ? new Locator(LocatorStrategy.AutomationId, previousPageAutomationId)
            : null;
        _pageInfoLocator = pageInfoAutomationId != null
            ? new Locator(LocatorStrategy.AutomationId, pageInfoAutomationId)
            : null;
    }

    #region Page Navigation

    /// <summary>
    /// Clicks the next-page button to load the next page of items.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope NextPage()
    {
        if (_nextPageLocator == null)
            throw new InvalidOperationException("No next-page locator configured for this PaginatedList.");

        var element = ContainingScope.FindElement(_nextPageLocator);
        element.Click();
        return ContainingScope;
    }

    /// <summary>
    /// Clicks the previous-page button to load the previous page of items.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope PreviousPage()
    {
        if (_previousPageLocator == null)
            throw new InvalidOperationException("No previous-page locator configured for this PaginatedList.");

        var element = ContainingScope.FindElement(_previousPageLocator);
        element.Click();
        return ContainingScope;
    }

    /// <summary>
    /// Checks whether the next-page button exists and is enabled.
    /// </summary>
    /// <returns>True if navigating forward is possible, false otherwise, null if locator not configured.</returns>
    public bool? HasNextPage()
    {
        if (_nextPageLocator == null) return null;

        var element = ContainingScope.TryFindElement(_nextPageLocator);
        if (element == null) return false;

        return element.Enabled;
    }

    /// <summary>
    /// Checks whether the previous-page button exists and is enabled.
    /// </summary>
    /// <returns>True if navigating backward is possible, false otherwise, null if locator not configured.</returns>
    public bool? HasPreviousPage()
    {
        if (_previousPageLocator == null) return null;

        var element = ContainingScope.TryFindElement(_previousPageLocator);
        if (element == null) return false;

        return element.Enabled;
    }

    #endregion

    #region Page Info

    /// <summary>
    /// Gets the text content of the page-info label (e.g., "Page 1 of 5").
    /// </summary>
    /// <returns>The page info text, or null if element not found or locator not configured.</returns>
    public string? GetPageInfoText()
    {
        if (_pageInfoLocator == null) return null;

        var element = ContainingScope.TryFindElement(_pageInfoLocator);
        return element?.Text;
    }

    #endregion

    #region Wait / Assert

    /// <summary>
    /// Waits for the page info text to match the expected value.
    /// </summary>
    /// <param name="expected">The expected page info text.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if the text matched within the timeout, false otherwise.</returns>
    public bool WaitPageInfo(string expected, int? timeoutMs = null)
    {
        return RunCheck(()  => GetPageInfoText() == expected, timeoutMs);
    }

    /// <summary>
    /// Asserts the page info text matches the expected value.
    /// </summary>
    /// <param name="expected">The expected page info text.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertPageInfo(string expected, string? message = null, int? timeoutMs = null)
    {
        if (!WaitPageInfo(expected, timeoutMs))
        {
            var actual = GetPageInfoText();
            throw new AssertionException(
                message ?? $"Expected page info '{expected}' but got '{actual}'. Locator: {Locator}");
        }
        return ContainingScope;
    }

    /// <summary>
    /// Waits for the next-page button to become available or unavailable.
    /// </summary>
    /// <param name="expected">True to wait until next page is available, false to wait until it is not.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if the condition was met within the timeout.</returns>
    public bool WaitHasNextPage(bool expected, int? timeoutMs = null)
    {
        return RunCheck(() => HasNextPage() == expected, timeoutMs);
    }

    /// <summary>
    /// Waits for the previous-page button to become available or unavailable.
    /// </summary>
    /// <param name="expected">True to wait until previous page is available, false to wait until it is not.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if the condition was met within the timeout.</returns>
    public bool WaitHasPreviousPage(bool expected, int? timeoutMs = null)
    {
        return RunCheck(() => HasPreviousPage() == expected, timeoutMs);
    }

    #endregion
}
