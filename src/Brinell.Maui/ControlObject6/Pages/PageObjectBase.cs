using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using Brinell.Maui.ControlObject6.Controls;

namespace Brinell.Maui.ControlObject6.Pages;

/// <summary>
/// Base class for MAUI page objects.
/// Provides common page functionality and control access.
/// </summary>
/// <remarks>
/// Controls are created using the 'new' pattern:
/// <code>
/// public ButtonControl SubmitButton => new(Context, "SubmitBtn", this);
/// public EntryControl UsernameEntry => new(Context, "Username", this);
/// </code>
/// </remarks>
public abstract class PageObjectBase : IPageObject
{
    private readonly MauiTestContext _context;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <summary>
    /// Gets the test context.
    /// </summary>
    protected MauiTestContext Context => _context;

    /// <summary>
    /// Gets the locator used to identify when this page is loaded.
    /// Override in derived classes to specify the page identification locator.
    /// </summary>
    protected abstract ControlLocator PageLocator { get; }

    /// <summary>
    /// Creates a new page object.
    /// </summary>
    protected PageObjectBase(MauiTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Page State

    /// <inheritdoc />
    public virtual bool IsLoaded(int? timeoutMs = null)
    {
        var pageControl = new ControlObjectPlaceholder(_context, PageLocator, this);
        return pageControl.IsVisible();
    }

    /// <inheritdoc />
    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (IsLoaded(timeoutMs) == expected.Value)
                return true;

            Thread.Sleep(_context.DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc />
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (!WaitLoaded(expected, timeoutMs))
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
    public virtual string GetTitle(int? timeoutMs = null)
    {
        // For MAUI, try to get the page title from the navigation bar or Title property
        // This is a simplified implementation - may need platform-specific handling
        return Name;
    }

    /// <inheritdoc />
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetTitle(timeoutMs);
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
    protected ButtonControl Button(string automationId) => new(_context, automationId, this);

    /// <summary>
    /// Creates a button control on this page with explicit locator.
    /// </summary>
    protected ButtonControl Button(ControlLocator locator) => new(_context, locator, this);

    /// <summary>
    /// Creates an entry control on this page.
    /// </summary>
    protected EntryControl Entry(string automationId) => new(_context, automationId, this);

    /// <summary>
    /// Creates an entry control on this page with explicit locator.
    /// </summary>
    protected EntryControl Entry(ControlLocator locator) => new(_context, locator, this);

    #endregion

    #region Control Queries

    /// <summary>
    /// Checks if a control exists on this page.
    /// </summary>
    public bool ControlExists(ControlLocator locator, int? timeoutMs = null)
    {
        var control = new ControlObjectPlaceholder(_context, locator, this);
        return control.IsExists();
    }

    /// <summary>
    /// Waits for a control to exist on this page.
    /// </summary>
    public bool WaitControlExists(ControlLocator locator, bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var control = new ControlObjectPlaceholder(_context, locator, this);
        return control.WaitExists(expected, timeoutMs);
    }

    /// <summary>
    /// Asserts a control exists on this page.
    /// </summary>
    public void AssertControlExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var control = new ControlObjectPlaceholder(_context, locator, this);
        control.AssertExists(expected, message, timeoutMs);
    }

    #endregion

    #region Screenshot and Scrolling

    /// <inheritdoc />
    public void TakeScreenshot(string? filename, int? timeoutMs = null)
    {
        _context.TakeScreenshot(filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}");
    }

    /// <inheritdoc />
    public void ScrollToControl(ControlLocator? locator, int? timeoutMs = null)
    {
        if (locator is null) return;

        var control = new ControlObjectPlaceholder(_context, locator, this);
        if (!control.IsVisible())
        {
            _context.Log($"Would scroll to: {locator}");
        }
    }

    #endregion

    /// <summary>
    /// Private placeholder control for page state checks.
    /// Uses ClickableControlBase since we just need existence/visibility checks.
    /// </summary>
    private class ControlObjectPlaceholder : ClickableControlBase
    {
        public ControlObjectPlaceholder(MauiTestContext context, ControlLocator locator, IPageObject? page)
            : base(context, locator, page)
        {
        }
    }
}
