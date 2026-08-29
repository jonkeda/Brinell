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

        // For ComboBox with ExpandCollapse pattern, use SelectItemByText for reliable selection
        if (element is IExpandCollapsePatternElement<IMauiElement> comboBox && comboBox.SupportsExpandCollapse)
        {
            if (!comboBox.SelectItemByText(text))
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

        // For ComboBox with ExpandCollapse pattern, use SelectItemByIndex for reliable selection
        if (element is IExpandCollapsePatternElement<IMauiElement> comboBox && comboBox.SupportsExpandCollapse)
        {
            if (!comboBox.SelectItemByIndex(index.Value))
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

        // For ComboBox (ExpandCollapse pattern), use SelectionPattern to get the selected item text
        // This avoids returning the ComboBox header/title instead of the selected value
        if (element is IExpandCollapsePatternElement<IMauiElement> comboBox && comboBox.SupportsExpandCollapse)
        {
            return comboBox.GetSelectedItemText();
        }

        // Try common selection attributes
        var selectedText = element.GetAttribute("Selection.Item.Name");
        if (!string.IsNullOrEmpty(selectedText)) return selectedText;

        // Try text content
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

        var indexAttr = element.GetAttribute("Selection.SelectedIndex");
        if (!string.IsNullOrEmpty(indexAttr) && int.TryParse(indexAttr, out var index))
        {
            return index;
        }

        var indexAttr2 = element.GetAttribute("SelectedIndex");
        if (!string.IsNullOrEmpty(indexAttr2) && int.TryParse(indexAttr2, out var index2))
        {
            return index2;
        }

        // Fallback: derive index by matching selected text against item texts
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
    /// Deliberately non-virtual: a virtual Get*Core would generate a Wait/Assert pair that
    /// compares IReadOnlyList&lt;string&gt; with ==, i.e. by reference, which no caller could
    /// satisfy. The public GetItemTexts is hand-written below instead. Derived controls
    /// needing different item discovery should override GetItemCountCore.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>List of item texts, or null if not available.</returns>
    protected IReadOnlyList<string>? GetItemTextsCore(IMauiElement? element)
    {
        if (element == null) return null;

        // For ExpandCollapse elements, expand first so item elements remain valid while reading texts
        if (element is IExpandCollapsePatternElement<IMauiElement> comboBox && comboBox.SupportsExpandCollapse)
        {
            comboBox.Expand();
            try
            {
                // GetExpandedItems sees already-expanded state, so won't collapse in its finally
                var items = comboBox.GetExpandedItems();
                return items?.Select(i => i.Text ?? string.Empty).ToList();
            }
            finally
            {
                comboBox.Collapse();
            }
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

        var countAttr = element.GetAttribute("ItemCount");
        if (!string.IsNullOrEmpty(countAttr) && int.TryParse(countAttr, out var count))
        {
            return count;
        }

        var items = GetItemElementsCore(element);
        return items?.Count;
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Gets all available item texts.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>List of item texts, or null if not available.</returns>
    public IReadOnlyList<string>? GetItemTexts(int? timeoutMs = null)
    {
        return RunGetWithElement(element => GetItemTextsCore(element), timeoutMs);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Gets child item elements from the selector.
    /// Uses FlaUI ExpandCollapse pattern when available for Windows ComboBox support.
    /// Deliberately non-virtual: a virtual Get*Core would generate a public GetItemElements
    /// wrapper, leaking platform elements into the control's API. Derived controls that need
    /// different item discovery should override GetItemTextsCore / GetItemCountCore instead.
    /// </summary>
    /// <param name="element">The parent selector element.</param>
    /// <returns>List of item elements, or null if not available.</returns>
    protected IReadOnlyList<IMauiElement>? GetItemElementsCore(IMauiElement? element)
    {
        if (element == null) return null;

        // For ComboBox with ExpandCollapse pattern, use GetExpandedItems which handles expand/collapse
        if (element is IExpandCollapsePatternElement<IMauiElement> comboBox && comboBox.SupportsExpandCollapse)
        {
            return comboBox.GetExpandedItems();
        }

        // Default implementation - override for specific controls
        return null;
    }

    #endregion
}
