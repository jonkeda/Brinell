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

    /// <summary>
    /// Whether element lookups on this page require the page to be loaded first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// True by default: an element cannot be inside a page that is not on screen, so looking
    /// for one is a guaranteed timeout and the wait tells the reader nothing.
    /// </para>
    /// <para>
    /// Override to <c>false</c> for a page that deliberately resolves elements outside its own
    /// root — a dialog host, for instance, where a WinUI3 <c>ContentDialog</c> renders in a
    /// separate popup window that is not a descendant of the page.
    /// </para>
    /// </remarks>
    protected virtual bool RequiresLoadedPage => true;

    private bool _ensuringLoad = false;

    /// <summary>
    /// Whether the page is loaded, guarding against re-entry.
    /// </summary>
    /// <remarks>
    /// <see cref="IsLoaded()"/> resolves elements through this same scope, so without the
    /// guard the load check would recurse into itself. During that inner call the answer is
    /// reported as true, which lets the check complete rather than deadlock.
    /// </remarks>
    private bool EnsureLoaded(bool wait = false)
    {
        if (_ensuringLoad) return true;
        try
        {
            _ensuringLoad = true;

            if (!wait) return IsLoaded();

            // Polls the no-argument form rather than passing a timeout to IsLoaded. Page
            // objects routinely override IsLoaded to check for a signature control - "loaded
            // means the status label exists" - and those overrides ignore the timeout
            // parameter, so delegating the wait to them would silently do nothing.
            return IsLoaded() || Poll(() => IsLoaded(), _context.Timeouts.PageLoad);
        }
        finally
        {
            _ensuringLoad = false;
        }
    }

    /// <summary>
    /// Whether an element lookup may proceed.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="EnsureLoaded"/> so the re-entrancy guard stays the only
    /// thing that method does, and so an opted-out page skips the load check entirely rather
    /// than paying for it and ignoring the result.
    /// </remarks>
    private bool CanResolveElements(bool wait = false)
        => !RequiresLoadedPage || EnsureLoaded(wait);

    /// <summary>
    /// The exception thrown when a lookup is attempted on a page that is not loaded.
    /// </summary>
    /// <remarks>
    /// Names the page as well as the element. The element name alone is what made this
    /// condition so hard to read: it describes a symptom of being on the wrong page, not the
    /// cause. See <c>.my/maui/rca/rca-002-page-precondition-discarded-slow-failures.md</c>.
    /// </remarks>
    private ElementNotFoundException PageNotLoaded(Locator locator)
        => new($"Page '{Name}' is not loaded, so '{locator}' cannot be found in it. " +
               $"The page root is located by AutomationId:{Name}. " +
               "Navigate to the page first, or override RequiresLoadedPage if this page " +
               "resolves elements outside its own root.");
    
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
            .Any(element => element.HasUsableBounds());

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
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Waits for the page, unlike <see cref="FindElement"/>. The difference is who does the
    /// polling: an action resolves through <c>FindElement</c>, which throws and lets the
    /// caller's <c>RunPoll</c> retry, so a wait here would nest one poll inside another. A
    /// query has no loop above it — <c>IsExists()</c> asks once and returns — so if it does
    /// not wait, nothing does.
    /// </para>
    /// <para>
    /// It waits for the <em>page</em> only, never for the element. "Is this element present?"
    /// must stay cheap to answer with "no", or <c>AssertExists(false)</c> costs a full timeout
    /// on every call. Being on the page is a precondition for the question being meaningful;
    /// the element's absence is the answer.
    /// </para>
    /// </remarks>
    IMauiElement? IElementScope<IMauiElement>.TryFindElement(Locator locator)
    {
        if (!CanResolveElements(wait: true)) return null;

        return _context.TryFindElement(locator);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Throws immediately when the page is not loaded rather than searching for an element
    /// that cannot be there. The search would spend the full <c>ElementFind</c> timeout and
    /// then report a missing element, which describes the symptom rather than the cause.
    /// </remarks>
    IMauiElement IElementScope<IMauiElement>.FindElement(Locator locator)
    {
        if (!CanResolveElements()) throw PageNotLoaded(locator);

        return _context.FindElement(locator);
    }

    /// <inheritdoc />
    IReadOnlyList<IMauiElement> IElementScope<IMauiElement>.FindElements(Locator locator)
    {
        if (!CanResolveElements()) return [];

        return _context.FindElements(locator);
    }

    #endregion
}
