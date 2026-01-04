using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Pages;

/// <summary>
/// Base class for Blazor page objects.
/// Provides async page functionality using Playwright.
/// </summary>
/// <remarks>
/// Controls are created using the 'new' pattern:
/// <code>
/// public ButtonControl SubmitButton => new(Context, "submitBtn", this);
/// public InputControl UsernameInput => new(Context, "username", this);
/// </code>
/// </remarks>
public abstract class AsyncPageObjectBase : IAsyncPageObject
{
    private readonly BlazorTestContext _context;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <summary>
    /// Gets the test context.
    /// </summary>
    protected BlazorTestContext Context => _context;

    /// <summary>
    /// Gets the locator used to identify when this page is loaded.
    /// Override in derived classes to specify the page identification locator.
    /// </summary>
    protected abstract ControlLocator PageLocator { get; }

    /// <summary>
    /// Creates a new page object.
    /// </summary>
    protected AsyncPageObjectBase(BlazorTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Page State

    /// <inheritdoc />
    public virtual async Task<bool> IsLoadedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var pageControl = new ControlObjectPlaceholder(_context, PageLocator, this);
        return await pageControl.IsVisibleAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> WaitLoadedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsLoadedAsync(timeoutMs, ct) == expected.Value)
                return true;

            await Task.Delay(_context.DefaultPollingIntervalMs, ct);
        }

        return false;
    }

    /// <inheritdoc />
    public async Task AssertLoadedAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await WaitLoadedAsync(expected, timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? $"Expected page '{Name}' to be {(expected.Value ? "loaded" : "unloaded")}",
                Name,
                "AssertLoaded");
        }
    }

    #endregion

    #region Title

    /// <inheritdoc />
    public virtual async Task<string> GetTitleAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await _context.Page.TitleAsync();
    }

    /// <inheritdoc />
    public async Task AssertTitleAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTitleAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected page title '{expected}', but was '{actual}'",
                Name,
                "AssertTitle");
        }
    }

    #endregion

    #region Control Access - Using 'new' Pattern

    /// <summary>
    /// Creates a button control on this page.
    /// </summary>
    protected ButtonControl Button(string testId) => new(_context, testId, this);

    /// <summary>
    /// Creates a button control on this page with explicit locator.
    /// </summary>
    protected ButtonControl Button(ControlLocator locator) => new(_context, locator, this);

    /// <summary>
    /// Creates an input control on this page.
    /// </summary>
    protected InputControl Input(string testId) => new(_context, testId, this);

    /// <summary>
    /// Creates an input control on this page with explicit locator.
    /// </summary>
    protected InputControl Input(ControlLocator locator) => new(_context, locator, this);

    #endregion

    #region Control Queries

    /// <summary>
    /// Checks if a control exists on this page.
    /// </summary>
    public async Task<bool> ControlExistsAsync(ControlLocator locator, int? timeoutMs = null, CancellationToken ct = default)
    {
        var control = new ControlObjectPlaceholder(_context, locator, this);
        return await control.IsExistsAsync(ct);
    }

    /// <summary>
    /// Waits for a control to exist on this page.
    /// </summary>
    public async Task<bool> WaitControlExistsAsync(ControlLocator locator, bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;
        var control = new ControlObjectPlaceholder(_context, locator, this);
        return await control.WaitExistsAsync(expected, timeoutMs, ct);
    }

    /// <summary>
    /// Asserts a control exists on this page.
    /// </summary>
    public async Task AssertControlExistsAsync(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        var control = new ControlObjectPlaceholder(_context, locator, this);
        await control.AssertExistsAsync(expected, message, timeoutMs, ct);
    }

    #endregion

    #region Screenshot and Scrolling

    /// <inheritdoc />
    public async Task TakeScreenshotAsync(string? filename, int? timeoutMs = null, CancellationToken ct = default)
    {
        await _context.TakeScreenshotAsync(filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}", ct);
    }

    /// <inheritdoc />
    public async Task ScrollToControlAsync(ControlLocator? locator, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (locator is null) return;

        // Use Playwright's scrollIntoViewIfNeeded via TestId locator
        var playwrightLocator = _context.Page.GetByTestId(locator.Value);
        await playwrightLocator.ScrollIntoViewIfNeededAsync();
    }

    #endregion

    /// <summary>
    /// Private placeholder control for page state checks.
    /// Uses AsyncClickableControlBase since we just need existence/visibility checks.
    /// </summary>
    private class ControlObjectPlaceholder : AsyncClickableControlBase
    {
        public ControlObjectPlaceholder(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
            : base(context, locator, page)
        {
        }
    }
}
