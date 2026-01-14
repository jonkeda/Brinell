using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Extensions;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for container controls that scope child element searches with fluent chaining.
/// Containers find their root element, then all child searches are scoped within it.
/// Implements IMauiPagedScope so containers can be used as scopes for child controls.
/// </summary>
/// <typeparam name="TPage">The parent page type for fluent chaining.</typeparam>
public class MauiContainerBase<TPage> : MauiControlBase<TPage>, IContainerControl<IMauiElement>, IMauiPagedScope<TPage>
    where TPage : IPageObject
{
    private IMauiElement? _cachedRoot;
    private bool _rootCacheValid;
    
    /// <summary>
    /// Creates a new container within the specified scope.
    /// </summary>
    /// <param name="scope">The paged scope (page or container) providing element finding and page reference.</param>
    /// <param name="locator">The locator for the container's root element.</param>
    public MauiContainerBase(IMauiPagedScope<TPage> scope, Locator locator)
        : base(scope, locator)
    {
    }
    
    #region IContainerControl Implementation
    
    /// <inheritdoc />
    object IContainerControl.ContainerRoot => ContainerRoot;
    
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
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    /// <summary>
    /// Tries to find an element within the container's root.
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
            var by = locator.ToBy();
            return rootElement.FindElements(by).ToList();
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();
            
            rootElement = TryGetContainerRoot();
            if (rootElement == null) return Array.Empty<IMauiElement>();
            
            try
            {
                var by = locator.ToBy();
                return rootElement.FindElements(by).ToList();
            }
            catch
            {
                return Array.Empty<IMauiElement>();
            }
        }
        catch (WebDriverException)
        {
            return Array.Empty<IMauiElement>();
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
        return new MauiButtonControl<TPage>(this, locator);
    }
    
    /// <summary>
    /// Creates an entry control scoped to this container.
    /// </summary>
    /// <param name="locator">The entry locator.</param>
    /// <returns>A new entry control that returns the page for fluent chaining.</returns>
    public MauiEntryControl<TPage> Entry(Locator locator)
    {
        return new MauiEntryControl<TPage>(this, locator);
    }
    
    /// <summary>
    /// Creates a nested container control scoped to this container.
    /// </summary>
    /// <param name="locator">The container locator.</param>
    /// <returns>A new container control that returns the page for fluent chaining.</returns>
    public MauiContainerBase<TPage> Container(Locator locator)
    {
        return new MauiContainerBase<TPage>(this, locator);
    }
    
    /// <summary>
    /// Creates a generic control scoped to this container.
    /// </summary>
    /// <param name="locator">The control locator.</param>
    /// <returns>A new control.</returns>
    public MauiControlBase<TPage> Control(Locator locator)
    {
        return new MauiControlBase<TPage>(this, locator);
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
        catch
        {
            return null;
        }
    }
    
    #endregion
}
