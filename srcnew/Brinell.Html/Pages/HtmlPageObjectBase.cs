using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Pages;

public abstract class HtmlPageObjectBase<TSelf> : ObjectBase, IHtmlPage<TSelf>
    where TSelf : HtmlPageObjectBase<TSelf>
{
    private readonly IHtmlTestContext _context;

    protected HtmlPageObjectBase(IHtmlTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override IHtmlTestContext Context => _context;

    public TSelf Self => (TSelf)this;

    public virtual string Name => GetType().Name;

    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;

    public virtual bool IsLoaded(int? timeoutMs = null)
    {
        return true;
    }

    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        return Poll(() => IsLoaded() == expected.Value, timeout);
    }

    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return;
        }

        if (!WaitLoaded(expected, timeoutMs))
        {
            var actual = IsLoaded();
            throw new PageLoadException(
                message ?? $"Expected page '{Name}' {(expected.Value ? "to be loaded" : "not to be loaded")} but loaded state is {actual}.");
        }
    }

    public virtual string? GetTitle(int? timeoutMs = null)
    {
        return _context.PageTitle;
    }

    public bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return Poll(() => GetTitle() == expected, timeout);
    }

    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return;
        }

        if (!WaitTitle(expected, timeoutMs))
        {
            var actual = GetTitle();
            throw new PageLoadException(message ?? $"Expected page title '{expected}' but got '{actual ?? "(null)"}'.");
        }
    }

    public void TakeScreenshot(string? filename = null, int? timeoutMs = null)
    {
        var path = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        _context.SaveScreenshot(path);
    }

    public IPageObject? Page => this;

    public bool IsReady(int? timeoutMs = null)
    {
        return IsLoaded(timeoutMs);
    }

    public bool WaitReady(int? timeoutMs = null)
    {
        return WaitLoaded(true, timeoutMs);
    }

    IHtmlElement? IElementScope<IHtmlElement>.TryFindElement(Locator locator)
    {
        return _context.TryFindElement(locator);
    }

    IHtmlElement IElementScope<IHtmlElement>.FindElement(Locator locator)
    {
        return _context.FindElement(locator);
    }

    IReadOnlyList<IHtmlElement> IElementScope<IHtmlElement>.FindElements(Locator locator)
    {
        return _context.FindElements(locator);
    }

    protected HtmlTestContextOptions DefaultOptions() => new();

    #region Async Methods

    public async Task<bool> WaitLoadedAsync(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        return await PollAsync(async () => IsLoaded() == expected.Value, timeout).ConfigureAwait(false);
    }

    public async Task AssertLoadedAsync(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        if (!await WaitLoadedAsync(expected, timeoutMs).ConfigureAwait(false))
        {
            var actual = IsLoaded();
            throw new PageLoadException(
                message ?? $"Expected page '{Name}' {(expected.Value ? "to be loaded" : "not to be loaded")} but loaded state is {actual}.");
        }
    }

    public async Task<bool> WaitTitleAsync(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return await PollAsync(async () => GetTitle() == expected, timeout).ConfigureAwait(false);
    }

    public async Task AssertTitleAsync(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        if (!await WaitTitleAsync(expected, timeoutMs).ConfigureAwait(false))
        {
            var actual = GetTitle();
            throw new PageLoadException(
                message ?? $"Expected page title '{expected}' but got '{actual ?? "(null)"}'.");
        }
    }

    #endregion
}