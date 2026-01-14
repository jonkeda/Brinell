using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Context;
using Brinell.Maui.Extensions;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for container controls that scope child element searches with fluent chaining.
/// Containers find their root element, then all child searches are scoped within it.
/// </summary>
/// <typeparam name="TPage">The parent page type for fluent chaining.</typeparam>
public class MauiContainerBase<TPage> : MauiControlBase<TPage>, IContainerControl<AppiumElement>, IMauiElementScope
    where TPage : IPageObject
{
    private AppiumElement? _cachedRoot;
    private bool _rootCacheValid;
    
    /// <summary>
    /// Creates a new container within the specified scope.
    /// </summary>
    /// <param name="page">The parent page for fluent chaining.</param>
    /// <param name="scope">The parent element scope (page or container).</param>
    /// <param name="locator">The locator for the container's root element.</param>
    public MauiContainerBase(TPage page, IMauiElementScope scope, Locator locator)
        : base(page, scope, locator)
    {
    }
    
    #region IContainerControl Implementation
    
    /// <inheritdoc />
    object IContainerControl.ContainerRoot => ContainerRoot;
    
    /// <inheritdoc />
    public AppiumElement ContainerRoot
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
                catch (WebDriverException)
                {
                    _rootCacheValid = false;
                    _cachedRoot = null;
                }
            }
            
            // Find and cache the root element
            _cachedRoot = FindElement();
            _rootCacheValid = true;
            return _cachedRoot;
        }
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
    public LocatorStrategy DefaultLocatorStrategy => Context.Timeouts != null 
        ? LocatorStrategy.AutomationId 
        : LocatorStrategy.AutomationId;
    
    /// <summary>
    /// Tries to find an element within the container's root.
    /// </summary>
    /// <param name="locator">The locator for the child element.</param>
    /// <returns>The element if found, null otherwise.</returns>
    public AppiumElement? TryFindElement(Locator locator)
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
            var by = locator.ToBy();
            return rootElement.FindElement(by);
        }
        catch (NoSuchElementException)
        {
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
                var by = locator.ToBy();
                return rootElement.FindElement(by);
            }
            catch
            {
                return null;
            }
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Finds an element within the container's root.
    /// </summary>
    /// <param name="locator">The locator for the child element.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    public AppiumElement FindElement(Locator locator)
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
    public IReadOnlyList<AppiumElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        
        var rootElement = TryGetContainerRoot();
        if (rootElement == null)
        {
            return Array.Empty<AppiumElement>();
        }
        
        try
        {
            var by = locator.ToBy();
            return rootElement.FindElements(by).ToList();
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();
            
            rootElement = TryGetContainerRoot();
            if (rootElement == null) return Array.Empty<AppiumElement>();
            
            try
            {
                var by = locator.ToBy();
                return rootElement.FindElements(by).ToList();
            }
            catch
            {
                return Array.Empty<AppiumElement>();
            }
        }
        catch (WebDriverException)
        {
            return Array.Empty<AppiumElement>();
        }
    }
    
    #endregion
    
    #region Factory Methods for Child Controls
    
    /// <summary>
    /// Creates a button control scoped to this container.
    /// </summary>
    /// <param name="locator">The button locator.</param>
    /// <returns>A new button control that returns the page when clicked.</returns>
    public MauiButtonControl<TPage> Button(Locator locator)
    {
        return new MauiButtonControl<TPage>(Page, this, locator);
    }
    
    /// <summary>
    /// Creates an entry control scoped to this container.
    /// </summary>
    /// <param name="locator">The entry locator.</param>
    /// <returns>A new entry control that returns the page for fluent chaining.</returns>
    public MauiEntryControl<TPage> Entry(Locator locator)
    {
        return new MauiEntryControl<TPage>(Page, this, locator);
    }
    
    /// <summary>
    /// Creates a nested container control scoped to this container.
    /// </summary>
    /// <param name="locator">The container locator.</param>
    /// <returns>A new container control that returns the page for fluent chaining.</returns>
    public MauiContainerBase<TPage> Container(Locator locator)
    {
        return new MauiContainerBase<TPage>(Page, this, locator);
    }
    
    /// <summary>
    /// Creates a generic control scoped to this container.
    /// </summary>
    /// <param name="locator">The control locator.</param>
    /// <returns>A new control.</returns>
    public MauiControlBase<TPage> Control(Locator locator)
    {
        return new MauiControlBase<TPage>(Page, this, locator);
    }
    
    #endregion
    
    #region Private Helpers
    
    /// <summary>
    /// Tries to get the container root without throwing.
    /// </summary>
    private AppiumElement? TryGetContainerRoot()
    {
        try
        {
            return ContainerRoot;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
}

/// <summary>
/// Base class for container controls that scope child element searches (non-generic for backward compatibility).
/// Containers find their root element, then all child searches are scoped within it.
/// </summary>
public class MauiContainerBase : MauiControlBase, IContainerControl<AppiumElement>, IMauiElementScope
{
    private AppiumElement? _cachedRoot;
    private bool _rootCacheValid;
    
    /// <summary>
    /// Creates a new container within the specified scope.
    /// </summary>
    /// <param name="scope">The parent element scope (page or container).</param>
    /// <param name="locator">The locator for the container's root element.</param>
    public MauiContainerBase(IMauiElementScope scope, Locator locator)
        : base(scope, locator)
    {
    }
    
    #region IContainerControl Implementation
    
    /// <inheritdoc />
    object IContainerControl.ContainerRoot => ContainerRoot;
    
    /// <inheritdoc />
    public AppiumElement ContainerRoot
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
                catch (WebDriverException)
                {
                    _rootCacheValid = false;
                    _cachedRoot = null;
                }
            }
            
            // Find and cache the root element
            _cachedRoot = FindElement();
            _rootCacheValid = true;
            return _cachedRoot;
        }
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
    public LocatorStrategy DefaultLocatorStrategy => Context.Timeouts != null 
        ? LocatorStrategy.AutomationId 
        : LocatorStrategy.AutomationId;
    
    /// <summary>
    /// Tries to find an element within the container's root.
    /// </summary>
    /// <param name="locator">The locator for the child element.</param>
    /// <returns>The element if found, null otherwise.</returns>
    public AppiumElement? TryFindElement(Locator locator)
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
            var by = locator.ToBy();
            return rootElement.FindElement(by);
        }
        catch (NoSuchElementException)
        {
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
                var by = locator.ToBy();
                return rootElement.FindElement(by);
            }
            catch
            {
                return null;
            }
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Finds an element within the container's root.
    /// </summary>
    /// <param name="locator">The locator for the child element.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    public AppiumElement FindElement(Locator locator)
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
    public IReadOnlyList<AppiumElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        
        var rootElement = TryGetContainerRoot();
        if (rootElement == null)
        {
            return Array.Empty<AppiumElement>();
        }
        
        try
        {
            var by = locator.ToBy();
            return rootElement.FindElements(by).ToList();
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();
            
            rootElement = TryGetContainerRoot();
            if (rootElement == null) return Array.Empty<AppiumElement>();
            
            try
            {
                var by = locator.ToBy();
                return rootElement.FindElements(by).ToList();
            }
            catch
            {
                return Array.Empty<AppiumElement>();
            }
        }
        catch (WebDriverException)
        {
            return Array.Empty<AppiumElement>();
        }
    }
    
    #endregion
    
    #region Private Helpers
    
    /// <summary>
    /// Tries to get the container root without throwing.
    /// </summary>
    private AppiumElement? TryGetContainerRoot()
    {
        try
        {
            return ContainerRoot;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
}
