namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with selection capability.
/// Implements ISelectorControlObject with SelectByText, SelectByIndex, GetSelectedText.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract class SelectorControlBase<TScope> : ControlBase<TScope>, ISelectorControlObject<TScope>
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
    
    #region ISelectorControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope SelectByText(string? text, int? timeoutMs = null)
    {
        return RunSetWithElement(text, element =>
        {
            SelectByTextCore(element, text!);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope SelectByIndex(int? index, int? timeoutMs = null)
    {
        return RunSetWithElement(index, element =>
        {
            SelectByIndexCore(element, index!.Value);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope SelectByValue(string? value, int? timeoutMs = null)
    {
        return RunSetWithElement(value, element =>
        {
            SelectByValueCore(element, value!);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public string? GetSelectedText(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return GetSelectedTextCore(TryFindElement());
    }
    
    /// <inheritdoc />
    public int? GetSelectedIndex(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return GetSelectedIndexCore(TryFindElement());
    }
    
    /// <inheritdoc />
    public IReadOnlyList<string>? GetItemTexts(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return GetItemTextsCore(TryFindElement());
    }
    
    /// <inheritdoc />
    public int? GetItemCount(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return GetItemCountCore(TryFindElement());
    }
    
    #endregion
    
    #region Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Selects item by text on pre-found element.
    /// Uses FlaUI ExpandCollapse pattern when available for Windows ComboBox support.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="text">The text to select.</param>
    protected virtual void SelectByTextCore(IMauiElement element, string text)
    {
        // For ComboBox with ExpandCollapse pattern, use SelectItemByText for reliable selection
        if (element is Interfaces.IExpandCollapsePatternElement comboBox && comboBox.SupportsExpandCollapse)
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
    /// <param name="index">The 0-based index to select.</param>
    protected virtual void SelectByIndexCore(IMauiElement element, int index)
    {
        // For ComboBox with ExpandCollapse pattern, use SelectItemByIndex for reliable selection
        if (element is Interfaces.IExpandCollapsePatternElement comboBox && comboBox.SupportsExpandCollapse)
        {
            if (!comboBox.SelectItemByIndex(index))
            {
                throw new ArgumentOutOfRangeException(nameof(index), 
                    $"Index {index} is out of range. Locator: {Locator}");
            }
            return;
        }
        
        // Default implementation
        element.Click();
        
        var defaultItems = GetItemElementsCore(element);
        if (defaultItems == null || index >= defaultItems.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), 
                $"Index {index} is out of range. Available items: {defaultItems?.Count ?? 0}. Locator: {Locator}");
        }
        
        defaultItems[index].Click();
    }
    
    /// <summary>
    /// Selects item by value on pre-found element.
    /// Override in derived classes for picker-specific implementation.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="value">The value to select.</param>
    protected virtual void SelectByValueCore(IMauiElement element, string value)
    {
        // Default: treat value same as text
        SelectByTextCore(element, value);
    }
    
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
        if (element is Interfaces.IExpandCollapsePatternElement comboBox && comboBox.SupportsExpandCollapse)
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
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>List of item texts, or null if not available.</returns>
    protected virtual IReadOnlyList<string>? GetItemTextsCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        // For ExpandCollapse elements, expand first so item elements remain valid while reading texts
        if (element is Interfaces.IExpandCollapsePatternElement comboBox && comboBox.SupportsExpandCollapse)
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
    
    /// <summary>
    /// Gets child item elements from the selector.
    /// Uses FlaUI ExpandCollapse pattern when available for Windows ComboBox support.
    /// </summary>
    /// <param name="element">The parent selector element.</param>
    /// <returns>List of item elements, or null if not available.</returns>
    protected virtual IReadOnlyList<IMauiElement>? GetItemElementsCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        // For ComboBox with ExpandCollapse pattern, use GetExpandedItems which handles expand/collapse
        if (element is Interfaces.IExpandCollapsePatternElement comboBox && comboBox.SupportsExpandCollapse)
        {
            return comboBox.GetExpandedItems();
        }
        
        // Default implementation - override for specific controls
        return null;
    }
    
    #endregion
    
    #region WaitSelectedText
    
    /// <inheritdoc />
    public bool WaitSelectedText(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        return RunCheck(
            () => GetSelectedText() == expected,
            timeoutMs);
    }
    
    #endregion
    
    #region AssertSelectedText
    
    /// <inheritdoc />
    public TScope AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertSelectedText), expected, () =>
        {
            WaitSelectedText(expected, timeoutMs);
            return GetSelectedText();
        }, message ?? $"Expected selected text '{expected}'. Locator: {Locator}");
    }
    
    #endregion
    
    #region WaitSelectedIndex
    
    /// <inheritdoc />
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        return RunCheck(
            () => GetSelectedIndex() == expected.Value,
            timeoutMs);
    }
    
    #endregion
    
    #region AssertSelectedIndex
    
    /// <inheritdoc />
    public TScope AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertSelectedIndex), expected, () =>
        {
            WaitSelectedIndex(expected, timeoutMs);
            return GetSelectedIndex();
        }, message ?? $"Expected selected index '{expected}'. Locator: {Locator}");
    }
    
    #endregion
    
    #region WaitItemCount
    
    /// <inheritdoc />
    public bool WaitItemCount(int? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        return RunCheck(
            () => GetItemCount() == expected.Value,
            timeoutMs);
    }
    
    #endregion
    
    #region AssertItemCount
    
    /// <inheritdoc />
    public TScope AssertItemCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertItemCount), expected, () =>
        {
            WaitItemCount(expected, timeoutMs);
            return GetItemCount();
        }, message ?? $"Expected item count '{expected}'. Locator: {Locator}");
    }
    
    #endregion
}
