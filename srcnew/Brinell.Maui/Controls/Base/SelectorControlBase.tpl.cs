using Brinell.Core.Utilities;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for MAUI controls with selection capability.
/// Implements ISelectorControlObject with SelectByText, SelectByIndex, GetSelectedText.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class SelectorControlBase<TScope> : FocusableControlBase<TScope>,
    ISelectorControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new selector control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    public SelectorControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new selector control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public SelectorControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Selection - Core Methods

    /// <summary>
    /// Selects item by text on pre-found element.
    /// Uses FlaUI ExpandCollapse pattern when available for Windows ComboBox support.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="text">The text to select. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SelectByTextCore(IMauiElement element, string? text, int? timeoutMs = null)
    {
        if (text == null) return;

        // One question, then one route. The dropdown below opens the popup, waits up to two
        // seconds for its items to reach the accessibility tree, selects one and closes it
        // again - a visible journey standing in for a property assignment the app can make
        // directly. Asked, never tried: nothing here performs anything to find out.
        if (element.SupportsSelectByText)
        {
            element.SelectByText(text);
            return;
        }

        if (element.SupportsDropdown)
        {
            if (!SelectFromDropdown(element, items => items.FirstOrDefault(i => i.Name == text || i.Text == text)))
            {
                throw new InvalidOperationException($"Item with text '{text}' not found. Locator: {Locator}");
            }
            return;
        }

        // Default implementation: open picker and find item
        element.Click();

        // Find and click item with matching text
        var defaultItems = GetItemElementsCore(element);
        var defaultItem = defaultItems?.FirstOrDefault(i => i.Text == text);
        if (defaultItem != null)
        {
            defaultItem.Click();
        }
        else
        {
            throw new InvalidOperationException($"Item with text '{text}' not found. Locator: {Locator}");
        }
    }

    /// <summary>
    /// Selects item by index on pre-found element.
    /// Uses FlaUI ExpandCollapse pattern when available for Windows ComboBox support.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="index">The 0-based index to select. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SelectByIndexCore(IMauiElement element, int? index, int? timeoutMs = null)
    {
        if (index == null) return;

        // See SelectByTextCore. The app also range-checks against its own item list, which the
        // dropdown route could not: it counted the items the popup had rendered, and that is a
        // different number while a virtualized list is still filling.
        if (element.SupportsSelectIndex)
        {
            element.SelectIndex(index.Value);
            return;
        }

        if (element.SupportsDropdown)
        {
            if (!SelectFromDropdown(element, items => index.Value < items.Count ? items[index.Value] : null))
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Index {index} is out of range. Locator: {Locator}");
            }
            return;
        }

        // Default implementation
        element.Click();

        var defaultItems = GetItemElementsCore(element);
        if (defaultItems == null || index.Value >= defaultItems.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Index {index} is out of range. Available items: {defaultItems?.Count ?? 0}. Locator: {Locator}");
        }

        defaultItems[index.Value].Click();
    }

    /// <summary>
    /// Selects item by value on pre-found element.
    /// Override in derived classes for picker-specific implementation.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="value">The value to select. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SelectByValueCore(IMauiElement element, string? value, int? timeoutMs = null)
    {
        if (value == null) return;

        // Default: treat value same as text
        SelectByTextCore(element, value, timeoutMs);
    }

    #endregion

    #region Selection State - Core Methods

    /// <summary>
    /// Gets selected text from pre-found element.
    /// Uses SelectionPattern for ComboBox controls, which returns the actual selected item
    /// rather than the ComboBox header/title.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The selected text, or null if not available.</returns>
    protected virtual string? GetSelectedTextCore(IMauiElement? element)
    {
        if (element == null) return null;

        // A dropdown names its selected item separately from its header/title, which is what its
        // text would otherwise return.
        if (element.SupportsDropdown)
        {
            return element.SelectedItemText;
        }

        // Otherwise whatever the control renders as its current choice.
        return element.Text;
    }

    /// <summary>
    /// Gets selected index from pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The selected index, or null if not available.</returns>
    protected virtual int? GetSelectedIndexCore(IMauiElement? element)
    {
        if (element == null) return null;

        // No platform publishes a selected index. It is derived: find the selected text, then
        // its position among the item texts. Both halves are read from real elements.
        var selectedText = GetSelectedTextCore(element);
        if (!string.IsNullOrEmpty(selectedText))
        {
            var itemTexts = GetItemTextsCore(element);
            if (itemTexts != null)
            {
                for (int i = 0; i < itemTexts.Count; i++)
                {
                    if (itemTexts[i] == selectedText)
                        return i;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all item texts from pre-found element.
    /// Override in derived classes for picker-specific implementation.
    /// </summary>
    /// <remarks>
    /// Generates <c>GetItemTexts</c> plus sequence-aware comparisons. Plain equality is not
    /// generated for a collection: <c>==</c> would compare references, which no caller could
    /// satisfy.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>List of item texts, or null if not available.</returns>
    [GenerateComparisons(Comparison.SequenceEquals | Comparison.HasItem | Comparison.Count)]
    protected virtual IReadOnlyList<string>? GetItemTextsCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Opened for the read and closed again, so the item elements are still live while their
        // texts are read.
        if (element.SupportsDropdown)
        {
            return WithDropdownOpen(element,
                () => element.ReadDropdownItems().Select(i => i.Text ?? string.Empty).ToList());
        }

        var defaultItems = GetItemElementsCore(element);
        return defaultItems?.Select(i => i.Text ?? string.Empty).ToList();
    }

    /// <summary>
    /// Gets item count from pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The item count, or null if not available.</returns>
    protected virtual int? GetItemCountCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Counted, not asked: no platform publishes an item count, so the items are found.
        var items = GetItemElementsCore(element);
        return items?.Count;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Gets child item elements from the selector.
    /// Uses FlaUI ExpandCollapse pattern when available for Windows ComboBox support.
    /// </summary>
    /// <remarks>
    /// Not generated: a public <c>GetItemElements</c> would leak <see cref="IMauiElement"/>
    /// into the control's API, and a test should be reasoning about item text or count, not
    /// platform elements. It stays <c>virtual</c> so a derived control can change how items
    /// are discovered — which the previous non-virtual form prevented.
    /// </remarks>
    /// <param name="element">The parent selector element.</param>
    /// <returns>List of item elements, or null if not available.</returns>
    [SkipGeneration("A public wrapper would leak IMauiElement into the control's API.")]
    protected virtual IReadOnlyList<IMauiElement>? GetItemElementsCore(IMauiElement? element)
    {
        if (element == null) return null;

        if (element.SupportsDropdown)
        {
            return WithDropdownOpen(element, element.ReadDropdownItems);
        }

        // Default implementation - override for specific controls
        return null;
    }

    /// <summary>
    /// Opens the dropdown, chooses one of its items, and leaves it closed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The item is <see cref="IMauiElement.Select"/>ed, which throws where it cannot be. This used
    /// to live on the Windows element as <c>SelectItemByText</c>, and fell back to a pointer click
    /// on an item without the SelectionItem pattern - a physical click nobody asked for, inside a
    /// call that looked semantic (step 107).
    /// </para>
    /// </remarks>
    /// <param name="element">The selector, which answered <c>SupportsDropdown</c>.</param>
    /// <param name="choose">Picks the item from those the dropdown shows, or null for none.</param>
    /// <returns>False when <paramref name="choose"/> found nothing; the dropdown is closed again.</returns>
    protected bool SelectFromDropdown(
        IMauiElement element, Func<IReadOnlyList<IMauiElement>, IMauiElement?> choose)
    {
        element.OpenDropdown();

        var item = choose(element.ReadDropdownItems());
        if (item == null)
        {
            element.CloseDropdown();
            return false;
        }

        item.Select();

        // Choosing an item closes a combo box by itself; close it if this one did not.
        WaitHelper.WaitFor(() => !element.IsDropdownOpen, DefaultTimeoutMs, PollingIntervalMs);
        element.CloseDropdown();
        return true;
    }

    /// <summary>Runs a read with the dropdown open, restoring the state it was found in.</summary>
    protected static T WithDropdownOpen<T>(IMauiElement element, Func<T> read)
    {
        var wasOpen = element.IsDropdownOpen;
        if (!wasOpen)
        {
            element.OpenDropdown();
        }

        try
        {
            return read();
        }
        finally
        {
            if (!wasOpen)
            {
                element.CloseDropdown();
            }
        }
    }

    #endregion
}
