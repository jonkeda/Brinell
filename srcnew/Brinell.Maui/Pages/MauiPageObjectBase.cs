using System.Diagnostics;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Controls;
using Brinell.Maui.Interfaces;

namespace Brinell.Maui.Pages;

/// <summary>
/// Base class for MAUI page objects with fluent method chaining support.
/// Uses CRTP (Curiously Recurring Template Pattern) for strongly-typed fluent returns.
/// Pages delegate element finding to the test context (driver root search).
/// Implements IMauiPagedScope so pages can be passed as scopes to controls.
/// </summary>
/// <typeparam name="TSelf">The concrete page type (CRTP pattern).</typeparam>
public abstract class MauiPageObjectBase<TSelf> : IPageObject<IMauiElement>, IMauiPagedScope<TSelf>
    where TSelf : MauiPageObjectBase<TSelf>
{
    private readonly IMauiTestContext _context;
    
    /// <summary>
    /// Creates a new page object with the specified context.
    /// </summary>
    /// <param name="context">The MAUI test context.</param>
    protected MauiPageObjectBase(IMauiTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
    /// <summary>
    /// Gets the test context.
    /// </summary>
    protected IMauiTestContext TestContext => _context;
    
    /// <summary>
    /// Gets this page as the typed page reference (for IMauiPagedScope).
    /// </summary>
    public TSelf Page => (TSelf)this;
    
    #region IPageObject Implementation
    
    /// <inheritdoc />
    public abstract string Name { get; }
    
    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;
    
    /// <inheritdoc />
    public abstract bool IsLoaded(int? timeoutMs = null);
    
    /// <inheritdoc />
    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        return Poll(
            () => IsLoaded() == expected.Value,
            timeout);
    }
    
    /// <inheritdoc />
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitLoaded(expected, timeoutMs))
        {
            var actual = IsLoaded();
            throw new PageLoadException(
                message ?? $"Expected page '{Name}' {(expected.Value ? "to be loaded" : "not to be loaded")} but loaded state is {actual}.");
        }
    }
    
    /// <inheritdoc />
    public virtual string? GetTitle(int? timeoutMs = null)
    {
        // Default implementation returns page name
        // Override for platforms that support page titles
        return Name;
    }
    
    /// <inheritdoc />
    public bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return Poll(
            () => GetTitle() == expected,
            timeout);
    }
    
    /// <inheritdoc />
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitTitle(expected, timeoutMs))
        {
            var actual = GetTitle();
            throw new PageLoadException(
                message ?? $"Expected page title '{expected}' but got '{actual ?? "(null)"}'.");
        }
    }
    
    /// <inheritdoc />
    public void TakeScreenshot(string? filename = null, int? timeoutMs = null)
    {
        var path = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        _context.SaveScreenshot(path);
    }
    
    #endregion
    
    #region IMauiElementScope Implementation
    
    /// <inheritdoc />
    public IMauiTestContext Context => _context;
    
    /// <inheritdoc />
    public IMauiElement? TryFindElement(Locator locator)
    {
        // Delegate to context (searches from driver root)
        return _context.TryFindElement(locator);
    }
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator)
    {
        // Delegate to context (searches from driver root)
        return _context.FindElement(locator);
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {
        // Delegate to context (searches from driver root)
        return _context.FindElements(locator);
    }
    
    #endregion
    
    #region Fluent Factory Methods
    
    /// <summary>
    /// Creates a button control scoped to this page with fluent chaining.
    /// </summary>
    /// <param name="locator">The button locator.</param>
    /// <returns>A new button control that returns this page when clicked.</returns>
    protected MauiButtonControl<TSelf> Button(Locator locator)
    {
        return new MauiButtonControl<TSelf>(this, locator);
    }
    
    /// <summary>
    /// Creates an entry control scoped to this page with fluent chaining.
    /// </summary>
    /// <param name="locator">The entry locator.</param>
    /// <returns>A new entry control that returns this page for fluent chaining.</returns>
    protected MauiEntryControl<TSelf> Entry(Locator locator)
    {
        return new MauiEntryControl<TSelf>(this, locator);
    }
    
    /// <summary>
    /// Creates a container control scoped to this page with fluent chaining.
    /// </summary>
    /// <param name="locator">The container locator.</param>
    /// <returns>A new container control that returns this page for fluent chaining.</returns>
    protected MauiContainerBase<TSelf> Container(Locator locator)
    {
        return new MauiContainerBase<TSelf>(this, locator);
    }
    
    /// <summary>
    /// Creates a generic control scoped to this page with fluent chaining.
    /// </summary>
    /// <param name="locator">The control locator.</param>
    /// <returns>A new control.</returns>
    protected MauiControlBase<TSelf> Control(Locator locator)
    {
        return new MauiControlBase<TSelf>(this, locator);
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Polls a condition until it returns true or timeout is reached.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool Poll(Func<bool> condition, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();
        var pollingInterval = _context.Timeouts.PollingInterval;
        
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions during polling, continue trying
            }
            
            Thread.Sleep(pollingInterval);
        }
        
        // Final check after timeout
        try
        {
            return condition();
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
}
