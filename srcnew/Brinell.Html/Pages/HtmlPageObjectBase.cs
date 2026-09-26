using Brinell.Core.Composition;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Pages;

// Core's [TestPage], because Html pages no longer implement Core's IPageObject: test composition
// and UAT find them by the attribute (the same step MAUI took, .my/stale-readiness/plan.md 2.1).
[TestPage]
public abstract class HtmlPageObjectBase<TSelf> : ObjectBase, IHtmlPage<TSelf>
    where TSelf : HtmlPageObjectBase<TSelf>
{
    private readonly IHtmlTestContext _context;

    protected HtmlPageObjectBase(IHtmlTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected HtmlPageObjectBase(IHtmlTestContext context, string url)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Url = url;
    }

    public override IHtmlTestContext Context => _context;

    public TSelf Self => (TSelf)this;

    public string? Url { get; }

    public virtual string Name => GetType().Name;

    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;

    protected virtual bool CompareUrl()
    {
        if (Url is null)
        {
            return true;
        }

        var uri = new Uri(_context.CurrentUrl);

        return uri.LocalPath == Url;
    }

    public virtual bool IsLoaded()
    {
        return CompareUrl()
            && _context.IsIdle();
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

    public virtual string? GetTitle()
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

    public void TakeScreenshot(string? filename = null)
    {
        var path = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        _context.SaveScreenshot(path);
    }

    public IHtmlPage? Page => this;

    public bool IsReady(int? timeoutMs = null)
    {
        return IsLoaded();
    }

    public bool WaitReady(int? timeoutMs = null)
    {
        return WaitLoaded(true, timeoutMs);
    }

    public void EnsureReady(int? timeoutMs = null)
    {
        if (!IsReady(timeoutMs))
        {
            throw new Exception($" Pager {Url} Not ready.");
        }
    }

    public IHtmlElement? TryFindElement(Locator locator)
    {
        return _context.TryFindElement(locator);
    }

    public IHtmlElement FindElement(Locator locator)
    {
        return _context.FindElement(locator);
    }

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator)
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
