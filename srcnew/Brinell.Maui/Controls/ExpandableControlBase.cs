namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with expand/collapse capability.
/// Inherits from ClickableControlBase to provide click capability.
/// Implements IExpandableControlObject with Expand, Collapse, ToggleExpanded.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract class ExpandableControlBase<TScope> : ClickableControlBase<TScope>, IExpandableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new expandable control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    public ExpandableControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new expandable control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public ExpandableControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
    
    #region IExpandableControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope Expand(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            ExpandCore(element);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope Collapse(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            CollapseCore(element);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope ToggleExpanded(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            ToggleExpandedCore(element);
        }, timeoutMs);
    }
    
    #endregion
    
    #region Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Expands the control. No-op if already expanded.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void ExpandCore(IMauiElement element)
    {
        var isExpanded = IsExpandedCore(element);
        if (isExpanded != true)
        {
            ToggleExpandedCore(element);
        }
    }
    
    /// <summary>
    /// Collapses the control. No-op if already collapsed.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void CollapseCore(IMauiElement element)
    {
        var isExpanded = IsExpandedCore(element);
        if (isExpanded == true)
        {
            ToggleExpandedCore(element);
        }
    }
    
    /// <summary>
    /// Toggles the expanded state by clicking.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void ToggleExpandedCore(IMauiElement element)
    {
        element.Click();
    }
    
    /// <summary>
    /// Gets expanded state from pre-found element.
    /// Reads from ExpandCollapse.ExpandCollapseState or IsExpanded attribute.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if expanded, false if collapsed, null if element is null.</returns>
    protected virtual bool? IsExpandedCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        // Try ExpandCollapseState attribute first (Windows/MAUI)
        var expandState = element.GetAttribute("ExpandCollapse.ExpandCollapseState");
        if (!string.IsNullOrEmpty(expandState))
        {
            return expandState.Equals("Expanded", StringComparison.OrdinalIgnoreCase) ||
                   expandState.Equals("1", StringComparison.OrdinalIgnoreCase);
        }
        
        // Try IsExpanded attribute
        var isExpandedAttr = element.GetAttribute("IsExpanded");
        if (!string.IsNullOrEmpty(isExpandedAttr))
        {
            return isExpandedAttr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        // Try aria-expanded for accessibility
        var ariaExpanded = element.GetAttribute("aria-expanded");
        if (!string.IsNullOrEmpty(ariaExpanded))
        {
            return ariaExpanded.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default to false if no attribute found
        return false;
    }
    
    #endregion
    
    #region IsExpanded
    
    /// <inheritdoc />
    public bool? IsExpanded()
    {
        return IsExpandedCore(TryFindElement());
    }
    
    #endregion
    
    #region WaitExpanded
    
    /// <summary>
    /// Waits for expanded state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected expanded state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitExpandedCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsExpandedCore(e) == expected,
            timeoutMs);
    }

    /// <inheritdoc />
    public bool WaitExpanded(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        var element = TryFindElement();
        if (element == null)
        {
            return false;
        }
        
        return WaitExpandedCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
    
    #region AssertExpanded
    
    /// <summary>
    /// Asserts the element is expanded. Throws if it isn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertExpanded(string? message = null, int? timeoutMs = null)
        => AssertExpanded(true, message, timeoutMs);
    
    /// <inheritdoc />
    public TScope AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertExpanded), expected, () =>
        {
            WaitExpanded(expected, timeoutMs);
            return IsExpanded();
        }, message ?? $"Expected element {(expected.Value ? "to be expanded" : "to be collapsed")}. Locator: {Locator}");
    }
    
    #endregion
}
