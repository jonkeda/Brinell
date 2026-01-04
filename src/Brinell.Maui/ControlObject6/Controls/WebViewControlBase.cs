using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for WebView controls in MAUI.
/// </summary>
public abstract class WebViewControlBase : ControlObjectBase, IWebViewControlObject
{
    /// <summary>
    /// Creates a new WebView control.
    /// </summary>
    protected WebViewControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new WebView control using AutomationId.
    /// </summary>
    protected WebViewControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region URL

    /// <inheritdoc/>
    public virtual string? GetCurrentUrl(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var url = element.GetAttribute("Source") ?? element.GetAttribute("Url");
        Log($"GetCurrentUrl: {url}");
        return url;
    }

    /// <inheritdoc/>
    public virtual void AssertCurrentUrl(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetCurrentUrl(timeoutMs);
        if (actual != expected)
        {
            var msg = message ?? $"Expected URL '{expected}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertCurrentUrl");
        }
    }

    /// <inheritdoc/>
    public virtual void AssertUrlContains(string? substring, string? message = null, int? timeoutMs = null)
    {
        if (substring is null) return;

        var actual = GetCurrentUrl(timeoutMs);
        if (actual is null || !actual.Contains(substring))
        {
            var msg = message ?? $"Expected URL to contain '{substring}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertUrlContains");
        }
    }

    #endregion

    #region Navigation

    /// <inheritdoc/>
    public virtual void NavigateTo(string? url, int? timeoutMs = null)
    {
        if (url is null) return;
        Log($"NavigateTo({url})");
    }

    /// <inheritdoc/>
    public virtual void GoBack(int? timeoutMs = null)
    {
        Log("GoBack()");
        var element = FindElementRequired(timeoutMs);
        element.SendKeys("\uE012");
    }

    /// <inheritdoc/>
    public virtual void GoForward(int? timeoutMs = null)
    {
        Log("GoForward()");
        var element = FindElementRequired(timeoutMs);
        element.SendKeys("\uE013");
    }

    /// <inheritdoc/>
    public virtual void Refresh(int? timeoutMs = null)
    {
        Log("Refresh()");
        var element = FindElementRequired(timeoutMs);
        element.SendKeys("\uE035");
    }

    /// <inheritdoc/>
    public virtual bool CanGoBack(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var canGoBack = element.GetAttribute("CanGoBack");
        return canGoBack == "True" || canGoBack == "true";
    }

    /// <inheritdoc/>
    public virtual bool CanGoForward(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var canGoForward = element.GetAttribute("CanGoForward");
        return canGoForward == "True" || canGoForward == "true";
    }

    #endregion

    #region Loading State

    /// <inheritdoc/>
    public virtual bool IsLoading(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var isLoading = element.GetAttribute("IsLoading");
        var result = isLoading == "True" || isLoading == "true";
        Log($"IsLoading: {result}");
        return result;
    }

    /// <inheritdoc/>
    public virtual bool WaitLoaded(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (!IsLoading(timeoutMs))
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertLoaded(string? message = null, int? timeoutMs = null)
    {
        if (IsLoading(timeoutMs))
        {
            var msg = message ?? "Expected WebView to be loaded but it is still loading";
            throw new AssertionException(msg, Locator.Value, "AssertLoaded");
        }
    }

    #endregion

    #region Title

    /// <inheritdoc/>
    public virtual string? GetTitle(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var title = element.GetAttribute("Title");
        Log($"GetTitle: {title}");
        return title;
    }

    /// <inheritdoc/>
    public virtual void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetTitle(timeoutMs);
        if (actual != expected)
        {
            var msg = message ?? $"Expected title '{expected}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertTitle");
        }
    }

    #endregion

    #region JavaScript

    /// <inheritdoc/>
    public virtual string? ExecuteJavaScript(string? script, int? timeoutMs = null)
    {
        if (script is null) return null;
        Log($"ExecuteJavaScript({script.Substring(0, Math.Min(50, script.Length))}...)");
        return null;
    }

    #endregion
}
