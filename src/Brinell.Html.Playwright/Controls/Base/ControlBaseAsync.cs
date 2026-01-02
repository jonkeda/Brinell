using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Exceptions;
using Brinell.Core.Logging;
using Brinell.Html.Playwright.Infrastructure;
using Microsoft.Playwright;
using System.Diagnostics;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Base class for async control operations in Playwright.
/// Implements IControlObjectAsync using native Playwright async/await patterns.
/// </summary>
public abstract class ControlBaseAsync : IControlObjectAsync
{
    protected readonly PlaywrightTestContext _context;
    protected readonly IPageObject? _page;
    protected readonly string _selector;

    /// <inheritdoc />
    public string AutomationId { get; }

    /// <inheritdoc />
    public IPageObject? Page => _page;

    /// <summary>
    /// Page name for logging.
    /// </summary>
    protected string PageName => _page?.Name ?? "Global";

    /// <summary>
    /// Test name for logging.
    /// </summary>
    protected string TestName => _context.TestName;

    /// <summary>
    /// Logger instance.
    /// </summary>
    protected ITestLogger? Logger => _context.Logger;

    /// <summary>
    /// Create an async control with page context.
    /// </summary>
    protected ControlBaseAsync(PlaywrightTestContext context, IPageObject? page, string automationId, string selector)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _page = page;
        AutomationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    #region Logging Helpers

    /// <summary>
    /// Log a control action.
    /// </summary>
    protected void LogAction(string action, string? value = null)
    {
        Logger?.LogAction(TestName, PageName, AutomationId, action, value);
    }

    /// <summary>
    /// Log assertion pass.
    /// </summary>
    protected void LogAssertPass(string assertType, string? actual, string? expected)
    {
        Logger?.LogAssertPass(TestName, PageName, AutomationId, assertType, actual, expected);
    }

    /// <summary>
    /// Log assertion failure and throw.
    /// </summary>
    protected void ThrowAssertionFailed(string assertType, string? actual, string? expected, string message)
    {
        Logger?.ThrowAssertionFailed(TestName, PageName, AutomationId, assertType, actual, expected, message, _context);
    }

    /// <summary>
    /// Log wait result.
    /// </summary>
    protected void LogWait(string waitType, bool success, int elapsedMs)
    {
        Logger?.LogWait(TestName, PageName, AutomationId, waitType, success, elapsedMs);
    }

    #endregion

    #region Existence Checks - Is/Wait/Check/Assert

    /// <inheritdoc />
    public virtual async ValueTask<bool> IsExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var element = await _context.Page.QuerySelectorAsync(_selector);
            return element != null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<bool> WaitExistsAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        
        try
        {
            await _context.Page.WaitForSelectorAsync(
                _selector,
                new PageWaitForSelectorOptions
                {
                    Timeout = expected ? timeout : 0,
                    State = expected ? WaitForSelectorState.Attached : WaitForSelectorState.Hidden
                });
            sw.Stop();
            LogWait($"Exists={expected}", true, (int)sw.ElapsedMilliseconds);
            return true;
        }
        catch (TimeoutException)
        {
            sw.Stop();
            LogWait($"Exists={expected}", false, (int)sw.ElapsedMilliseconds);
            return false;
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask CheckExistsAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var success = await WaitExistsAsync(expected, timeoutMs, cancellationToken);
        
        if (!success)
        {
            var state = await IsExistsAsync(cancellationToken) ? "exists" : "does not exist";
            ThrowAssertionFailed(
                "Exists",
                state,
                expected ? "exists" : "does not exist",
                $"Check failed: Element '{AutomationId}' {state}, expected {(expected ? "exists" : "not exists")}.");
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask AssertExistsAsync(string? message = null, CancellationToken cancellationToken = default)
    {
        await CheckExistsAsync(expected: true, cancellationToken: cancellationToken);
        LogAssertPass("Exists", "true", "true");
    }

    /// <inheritdoc />
    public virtual async ValueTask AssertNotExistsAsync(string? message = null, CancellationToken cancellationToken = default)
    {
        await CheckExistsAsync(expected: false, cancellationToken: cancellationToken);
        LogAssertPass("NotExists", "false", "false");
    }

    #endregion

    #region Visibility Checks - Is/Wait/Check/Assert

    /// <inheritdoc />
    public virtual async ValueTask<bool> IsVisibleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var element = await _context.Page.QuerySelectorAsync(_selector);
            if (element == null) return false;
            
            return await element.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<bool> WaitVisibleAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var startTime = sw.Elapsed;
        
        while (sw.Elapsed - startTime < TimeSpan.FromMilliseconds(timeout))
        {
            var isVisible = await IsVisibleAsync(cancellationToken);
            if (isVisible == expected)
            {
                sw.Stop();
                LogWait($"Visible={expected}", true, (int)sw.ElapsedMilliseconds);
                return true;
            }
            await Task.Delay(100, cancellationToken);
        }
        
        sw.Stop();
        LogWait($"Visible={expected}", false, (int)sw.ElapsedMilliseconds);
        return false;
    }

    /// <inheritdoc />
    public virtual async ValueTask CheckVisibleAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var success = await WaitVisibleAsync(expected, timeoutMs, cancellationToken);
        
        if (!success)
        {
            var state = await IsVisibleAsync(cancellationToken) ? "visible" : "not visible";
            ThrowAssertionFailed(
                "Visible",
                state,
                expected ? "visible" : "not visible",
                $"Check failed: Element '{AutomationId}' {state}, expected {(expected ? "visible" : "not visible")}.");
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask AssertVisibleAsync(string? message = null, CancellationToken cancellationToken = default)
    {
        await CheckVisibleAsync(expected: true, cancellationToken: cancellationToken);
        LogAssertPass("Visible", "true", "true");
    }

    /// <inheritdoc />
    public virtual async ValueTask AssertNotVisibleAsync(string? message = null, CancellationToken cancellationToken = default)
    {
        await CheckVisibleAsync(expected: false, cancellationToken: cancellationToken);
        LogAssertPass("NotVisible", "false", "false");
    }

    #endregion

    #region Enabled State Checks - Is/Wait/Check/Assert

    /// <inheritdoc />
    public virtual async ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var element = await _context.Page.QuerySelectorAsync(_selector);
            if (element == null) return false;
            
            var disabled = await element.GetAttributeAsync("disabled");
            return disabled == null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<bool> WaitEnabledAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var startTime = sw.Elapsed;
        
        while (sw.Elapsed - startTime < TimeSpan.FromMilliseconds(timeout))
        {
            var isEnabled = await IsEnabledAsync(cancellationToken);
            if (isEnabled == expected)
            {
                sw.Stop();
                LogWait($"Enabled={expected}", true, (int)sw.ElapsedMilliseconds);
                return true;
            }
            await Task.Delay(100, cancellationToken);
        }
        
        sw.Stop();
        LogWait($"Enabled={expected}", false, (int)sw.ElapsedMilliseconds);
        return false;
    }

    /// <inheritdoc />
    public virtual async ValueTask CheckEnabledAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var success = await WaitEnabledAsync(expected, timeoutMs, cancellationToken);
        
        if (!success)
        {
            var state = await IsEnabledAsync(cancellationToken) ? "enabled" : "disabled";
            ThrowAssertionFailed(
                "Enabled",
                state,
                expected ? "enabled" : "disabled",
                $"Check failed: Element '{AutomationId}' {state}, expected {(expected ? "enabled" : "disabled")}.");
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask AssertEnabledAsync(string? message = null, CancellationToken cancellationToken = default)
    {
        await CheckEnabledAsync(expected: true, cancellationToken: cancellationToken);
        LogAssertPass("Enabled", "true", "true");
    }

    /// <inheritdoc />
    public virtual async ValueTask AssertDisabledAsync(string? message = null, CancellationToken cancellationToken = default)
    {
        await CheckEnabledAsync(expected: false, cancellationToken: cancellationToken);
        LogAssertPass("Disabled", "false", "false");
    }

    #endregion

    #region Text Access - Get/Wait/Check/Assert

    /// <inheritdoc />
    public virtual async ValueTask<string> GetTextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var element = await _context.Page.QuerySelectorAsync(_selector);
            if (element == null) return string.Empty;
            
            return await element.TextContentAsync() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<bool> WaitTextAsync(string expected, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var startTime = sw.Elapsed;
        
        while (sw.Elapsed - startTime < TimeSpan.FromMilliseconds(timeout))
        {
            var text = await GetTextAsync(cancellationToken);
            if (text == expected)
            {
                sw.Stop();
                LogWait($"Text='{expected}'", true, (int)sw.ElapsedMilliseconds);
                return true;
            }
            await Task.Delay(100, cancellationToken);
        }
        
        sw.Stop();
        LogWait($"Text='{expected}'", false, (int)sw.ElapsedMilliseconds);
        return false;
    }

    /// <inheritdoc />
    public virtual async ValueTask CheckTextAsync(string expected, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var success = await WaitTextAsync(expected, timeoutMs, cancellationToken);
        
        if (!success)
        {
            var actual = await GetTextAsync(cancellationToken);
            ThrowAssertionFailed(
                "Text",
                actual,
                expected,
                $"Check failed: Element '{AutomationId}' text mismatch. Expected: '{expected}', Actual: '{actual}'.");
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask AssertTextEqualsAsync(string expected, string? message = null, CancellationToken cancellationToken = default)
    {
        var actual = await GetTextAsync(cancellationToken);
        
        if (actual == expected)
        {
            LogAssertPass("TextEquals", actual, expected);
        }
        else
        {
            ThrowAssertionFailed(
                "TextEquals",
                actual,
                expected,
                message ?? $"Control '{AutomationId}' text mismatch. Expected: '{expected}', Actual: '{actual}'");
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask AssertTextContainsAsync(string substring, string? message = null, CancellationToken cancellationToken = default)
    {
        var actual = await GetTextAsync(cancellationToken);
        
        if (actual.Contains(substring))
        {
            LogAssertPass("TextContains", substring, actual);
        }
        else
        {
            ThrowAssertionFailed(
                "TextContains",
                actual,
                substring,
                message ?? $"Control '{AutomationId}' text should contain '{substring}' but was '{actual}'");
        }
    }

    #endregion
}
