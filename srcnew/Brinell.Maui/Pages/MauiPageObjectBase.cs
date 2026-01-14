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
/// Implements IMauiPage so pages can be used as scopes for child controls.
/// </summary>
/// <typeparam name="TSelf">The concrete page type (CRTP pattern).</typeparam>
public abstract class MauiPageObjectBase<TSelf> : MauiObjectBase, IMauiPage<TSelf>
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
    
    /// <inheritdoc />
    public override IMauiTestContext Context => _context;
    
    /// <summary>
    /// Gets this page as the typed page reference (for fluent chaining).
    /// </summary>
    public TSelf Self => (TSelf)this;
    
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
    
    #region Factory Methods
    
    /// <summary>
    /// Creates a button control within this page scope.
    /// </summary>
    protected MauiButtonControl<TSelf> Button(Locator locator)
        => new MauiButtonControl<TSelf>(this, locator);
    
    /// <summary>
    /// Creates a button control within this page scope using automation ID.
    /// </summary>
    protected MauiButtonControl<TSelf> Button(string automationId)
        => Button(Locator.ById(automationId));
    
    /// <summary>
    /// Creates an entry control within this page scope.
    /// </summary>
    protected MauiEntryControl<TSelf> Entry(Locator locator)
        => new MauiEntryControl<TSelf>(this, locator);
    
    /// <summary>
    /// Creates an entry control within this page scope using automation ID.
    /// </summary>
    protected MauiEntryControl<TSelf> Entry(string automationId)
        => Entry(Locator.ById(automationId));
    
    #endregion
}
