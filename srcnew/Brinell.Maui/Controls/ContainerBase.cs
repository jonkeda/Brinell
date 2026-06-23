using Brinell.Core.Locators;
using Brinell.Maui.Controls.DateTimes;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Controls.Range;
using Brinell.Maui.Controls.Text;
using Brinell.Maui.Controls.Toggle;
using Brinell.Maui.Enums;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for container controls that scope child element searches with fluent chaining.
/// Containers find their root element, then all child searches are scoped within it.
/// Implements IMauiContainer so containers can be used as scopes for child controls.
/// TParent can be a page or another container - both are scopes.
/// </summary>
/// <typeparam name="TParent">The parent scope type (page or container).</typeparam>
/// <typeparam name="TSelf">The container type itself (self-referencing for fluent returns).</typeparam>
public abstract class ContainerBase<TParent, TSelf> : ControlBase<TParent>, IMauiContainer<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : ContainerBase<TParent, TSelf>
{
    private readonly TParent _parent;
    private IMauiElement? _cachedRoot;
    private bool _rootCacheValid;
    
    /// <summary>
    /// Creates a new container within the specified scope.
    /// </summary>
    /// <param name="parentScope">The parent scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the container's root element.</param>
    protected ContainerBase(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
        _parent = parentScope.Self;
    }
    
    /// <summary>
    /// Gets this container as the typed container reference (for fluent chaining).
    /// </summary>
    public TSelf Self => (TSelf)this;
    
    /// <summary>
    /// Gets the parent scope (page or container).
    /// Navigate up the scope hierarchy by calling Parent.
    /// </summary>
    public TParent Parent => _parent;
    
    #region IContainerControl Implementation
    
    /// <inheritdoc />
    public IMauiElement ContainerRoot
    {
        get
        {
            // Try to return cached root if valid
            if (_rootCacheValid && _cachedRoot != null)
            {
                try
                {
                    // Verify element is not stale by accessing a property
                    _ = _cachedRoot.TagName;
                    return _cachedRoot;
                }
                catch (StaleElementReferenceException)
                {
                    // Cache is stale, invalidate and re-find
                    _rootCacheValid = false;
                    _cachedRoot = null;
                }
            }
            
            // Find and cache the root element
            _cachedRoot = FindContainerRootElement();
            _rootCacheValid = true;
            return _cachedRoot;
        }
    }
    
    /// <summary>
    /// Finds the container root element. Override to customize root-finding behavior,
    /// for example to search popup windows for dialogs that exist outside the normal
    /// scope chain (e.g., WinUI3 <c>ContentDialog</c>).
    /// </summary>
    /// <returns>The container root element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when the root element is not found.</exception>
    protected virtual IMauiElement FindContainerRootElement()
    {
        return FindElement();
    }
    
    /// <summary>
    /// Invalidates the cached container root element.
    /// Call this after UI refreshes that may have recreated the element.
    /// </summary>
    public void InvalidateCache()
    {
        _rootCacheValid = false;
        _cachedRoot = null;
    }
    
    #endregion
    
    #region IMauiElementScope Implementation
    
    /// <inheritdoc />
    IMauiTestContext IMauiElementScope.Context => Context;
    
    /// <inheritdoc />
    public new IPageObject? Page => _parent.Page;
    
    /// <inheritdoc />
    public bool IsReady(int? timeoutMs = null)
    {
        // Container is ready when parent is ready AND container root exists
        var parentReady = _parent.IsReady(timeoutMs);
        if (!parentReady) return false;
        
        return TryGetContainerRoot() != null;
    }
    
    /// <inheritdoc />
    public bool WaitReady(int? timeoutMs = null)
    {
        // First wait for parent to be ready
        var parentReady = _parent.WaitReady(timeoutMs);
        if (!parentReady) return false;
        
        // Then wait for container root to exist
        return WaitExists(true, timeoutMs);
    }
    
    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    /// <summary>
    /// Tries to find an element within the container's root.
    /// On Windows, layout containers (Grid, StackLayout, Frame, etc.) don't expose AutomationId,
    /// so we use marker elements (like Labels) as container roots. Since markers have no children,
    /// we fall back to parent scope search when direct search fails.
    /// </summary>
    /// <param name="locator">The locator for the child element.</param>
    /// <returns>The element if found, null otherwise.</returns>
    public IMauiElement? TryFindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        
        // First, ensure the container root exists
        var rootElement = TryGetContainerRoot();
        if (rootElement == null)
        {
            return null;
        }
        
        try
        {
            return rootElement.FindElement(locator, timeoutMs: 0);
        }
        catch (ElementNotFoundException)
        {
            // Element not found in container - do NOT fall back to parent
            // Container scoping means elements must be within the container
            return null;
        }
        catch (StaleElementReferenceException)
        {
            // Root became stale, invalidate and retry once
            InvalidateCache();
            
            rootElement = TryGetContainerRoot();
            if (rootElement == null) return null;
            
            try
            {
                return rootElement.FindElement(locator, timeoutMs: 0);
            }
            catch (ElementNotFoundException)
            {
                // Element not found in container - do NOT fall back to parent
                return null;
            }
        }
    }
    
    /// <summary>
    /// Finds an element within the container's root.
    /// </summary>
    /// <param name="locator">The locator for the child element.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    public IMauiElement FindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        
        var element = TryFindElement(locator);
        if (element == null)
        {
            throw new ElementNotFoundException(
                $"Element not found within container. Container locator: {Locator}, Child locator: {locator}");
        }
        
        return element;
    }
    
    /// <summary>
    /// Finds all matching elements within the container's root.
    /// </summary>
    /// <param name="locator">The locator for the child elements.</param>
    /// <returns>A list of matching elements (empty if none found).</returns>
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        
        var rootElement = TryGetContainerRoot();
        if (rootElement == null)
        {
            return Array.Empty<IMauiElement>();
        }
        
        try
        {
            return rootElement.FindElements(locator, timeoutMs: 0);
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();
            
            rootElement = TryGetContainerRoot();
            if (rootElement == null) return Array.Empty<IMauiElement>();
            
            return rootElement.FindElements(locator, timeoutMs: 0);
        }
    }
    
    #endregion
    
    #region Private Helpers
    
    /// <summary>
    /// Tries to get the container root without throwing.
    /// </summary>
    private IMauiElement? TryGetContainerRoot()
    {
        try
        {
            return ContainerRoot;
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a label control within this container scope.
    /// </summary>
    protected Label<TSelf> Label(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a label control within this container scope using the scope default locator.
    /// </summary>
    protected Label<TSelf> Label(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a checkbox control within this container scope.
    /// </summary>
    protected CheckBox<TSelf> CheckBox(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a checkbox control within this container scope using the scope default locator.
    /// </summary>
    protected CheckBox<TSelf> CheckBox(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a button control within this container scope.
    /// </summary>
    protected Button<TSelf> Button(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a button control within this page scope using the scope default locator.
    /// </summary>
    protected Button<TSelf> Button(string locator)
        => new(this, locator);

    // Extension control icon command button, round button, and editable field factories
    // have been moved to Brinell.Maui.Extensions.
    // Import those namespaces to use them directly within container scopes.

    #region Display Controls

    /// <summary>
    /// Creates an image control within this container scope.
    /// </summary>
    protected Image<TSelf> Image(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an image control within this container scope using the scope default locator.
    /// </summary>
    protected Image<TSelf> Image(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a progress bar control within this container scope.
    /// </summary>
    protected ProgressBar<TSelf> ProgressBar(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a progress bar control within this container scope using the scope default locator.
    /// </summary>
    protected ProgressBar<TSelf> ProgressBar(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an activity indicator control within this container scope.
    /// </summary>
    protected ActivityIndicator<TSelf> ActivityIndicator(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an activity indicator control within this container scope using the scope default locator.
    /// </summary>
    protected ActivityIndicator<TSelf> ActivityIndicator(string locator)
        => new(this, locator);

    #endregion

    #region Toggle Controls

    /// <summary>
    /// Creates a switch control within this container scope.
    /// </summary>
    protected Switch<TSelf> Switch(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a switch control within this container scope using the scope default locator.
    /// </summary>
    protected Switch<TSelf> Switch(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a radio button control within this container scope.
    /// </summary>
    protected RadioButton<TSelf> RadioButton(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a radio button control within this container scope using the scope default locator.
    /// </summary>
    protected RadioButton<TSelf> RadioButton(string locator)
        => new(this, locator);

    #endregion

    #region Text Controls

    /// <summary>
    /// Creates an entry control within this container scope.
    /// </summary>
    protected Entry<TSelf> Entry(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an entry control within this container scope using the scope default locator.
    /// </summary>
    protected Entry<TSelf> Entry(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an editor control within this container scope.
    /// </summary>
    protected Editor<TSelf> Editor(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an editor control within this container scope using the scope default locator.
    /// </summary>
    protected Editor<TSelf> Editor(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a search bar control within this container scope.
    /// </summary>
    protected SearchBar<TSelf> SearchBar(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a search bar control within this container scope using the scope default locator.
    /// </summary>
    protected SearchBar<TSelf> SearchBar(string locator)
        => new(this, locator);

    #endregion

    #region Selection Controls

    // Note: Picker control is not yet implemented in Brinell.Maui
    // Uncomment and implement when Picker<TSelf> control is created

    // protected Picker<TSelf> Picker(Locator locator)
    //     => new(this, locator);

    // protected Picker<TSelf> Picker(string locator)
    //     => new(this, locator);

    // Note: GenericBrowser, SelectionList, and TabMenu extension control factories
    // have been moved to Brinell.Maui.Extensions.
    // Import those namespaces to use them directly within container scopes.

    #endregion

    #region Range Controls

    /// <summary>
    /// Creates a slider control within this container scope.
    /// </summary>
    protected Slider<TSelf> Slider(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a slider control within this container scope using the scope default locator.
    /// </summary>
    protected Slider<TSelf> Slider(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a stepper control within this container scope.
    /// </summary>
    protected Stepper<TSelf> Stepper(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a stepper control within this container scope using the scope default locator.
    /// </summary>
    protected Stepper<TSelf> Stepper(string locator)
        => new(this, locator);

    #endregion

    #region DateTime Controls

    /// <summary>
    /// Creates a date picker control within this container scope.
    /// </summary>
    protected DatePicker<TSelf> DatePicker(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a date picker control within this container scope using the scope default locator.
    /// </summary>
    protected DatePicker<TSelf> DatePicker(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a time picker control within this container scope.
    /// </summary>
    protected TimePicker<TSelf> TimePicker(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a time picker control within this container scope using the scope default locator.
    /// </summary>
    protected TimePicker<TSelf> TimePicker(string locator)
        => new(this, locator);

    #endregion

    #endregion
}
