using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Display;

namespace Brinell.Maui.Pages;

/// <summary>
/// Base class for MAUI page objects with fluent method chaining support.
/// Uses CRTP (Curiously Recurring Template Pattern) for strongly-typed fluent returns.
/// Pages delegate element finding to the test context (driver root search).
/// Implements IMauiPage so pages can be used as scopes for child controls.
/// </summary>
/// <typeparam name="TSelf">The concrete page type (CRTP pattern).</typeparam>
public abstract class PageObjectBase<TSelf> : ObjectBase, IMauiPage<TSelf>
    where TSelf : PageObjectBase<TSelf>
{
    private readonly IMauiTestContext _context;
    
    /// <summary>
    /// Creates a new page object with the specified context.
    /// </summary>
    /// <param name="context">The MAUI test context.</param>
    protected PageObjectBase(IMauiTestContext context)
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
    public virtual string Name => GetType().Name;
    
    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;
    
    /// <inheritdoc />
    public Label<TSelf> BusySentinel => new (this, "UITest_IsBusy");

    private bool _ensuringLoad = false;
    private bool EnsureLoaded()
    {
        if (_ensuringLoad) return true;
        try
        {
            _ensuringLoad = true;
            return IsLoaded();
        }
        finally
        {
            _ensuringLoad = false;
        }
    }
    
    /// <inheritdoc />
    public virtual bool IsLoaded(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? 0;
        return timeout > 0
            ? Poll(IsVisiblePageRootLoaded, timeout)
            : IsVisiblePageRootLoaded();
    }

    private bool IsVisiblePageRootLoaded()
        => _context
            .FindElements(Locator.ByAutomationId(Name))
            .Any(ElementSearch.HasUsableBounds);

    /// <summary>
    /// Waits for the page to finish loading.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True when page becomes idle; otherwise false.</returns>
    public bool WaitIdle(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        return Poll(() => BusySentinel.GetText() == "False", timeout);
    }

    /// <summary>
    /// Asserts that the page is idle.
    /// </summary>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <exception cref="PageLoadException">Thrown when page does not become idle within timeout.</exception>
    public void AssertIdle(string? message = null, int? timeoutMs = null)
    {
        if (!WaitIdle(timeoutMs))
        {
            var actual = BusySentinel.GetText();
            throw new PageLoadException(
                message ?? $"Page '{Name}' did not become idle within timeout. UITest_IsBusy text: '{actual ?? "(not found)"}'.");
        }
    }
    
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
    public IPageObject? Page => this;
    
    /// <inheritdoc />
    public bool IsReady(int? timeoutMs = null)
    {
        // For pages, ready means loaded
        return IsLoaded(timeoutMs);
    }
    
    /// <inheritdoc />
    public bool WaitReady(int? timeoutMs = null)
    {
        // For pages, wait ready means wait loaded
        return WaitLoaded(true, timeoutMs);
    }
    
    /// <inheritdoc />
    IMauiElement? IElementScope<IMauiElement>.TryFindElement(Locator locator)
    {
        EnsureLoaded();
        return _context.TryFindElement(locator);
    }
    
    /// <inheritdoc />
    IMauiElement IElementScope<IMauiElement>.FindElement(Locator locator)
    {
        EnsureLoaded();
        return _context.FindElement(locator);
    }
    
    /// <inheritdoc />
    IReadOnlyList<IMauiElement> IElementScope<IMauiElement>.FindElements(Locator locator)
    {
        EnsureLoaded();
        return _context.FindElements(locator);
    }

    #endregion
}
