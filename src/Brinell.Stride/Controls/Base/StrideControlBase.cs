using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Exceptions;
using Brinell.Core.Logging;
using Brinell.Stride.Communication;
using Brinell.Stride.Infrastructure;
using System.Diagnostics;

namespace Brinell.Stride.Controls.Base;

/// <summary>
/// Base class for all Stride UI control objects.
/// Implements the Wait/Check/Is/Assert pattern.
/// </summary>
public abstract class StrideControlBase : IControlObject
{
    /// <summary>
    /// The test context.
    /// </summary>
    protected readonly StrideTestContext Context;

    /// <summary>
    /// The automation ID of this control.
    /// </summary>
    protected readonly string _automationId;

    /// <inheritdoc />
    public string AutomationId => _automationId;

    /// <inheritdoc />
    public IPageObject? Page { get; }

    /// <summary>
    /// Create a new control object.
    /// </summary>
    protected StrideControlBase(StrideTestContext context, IPageObject? page, string automationId)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Page = page;
        _automationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
    }

    #region Element State Access

    /// <summary>
    /// Get current element state from game.
    /// </summary>
    protected ElementState GetState() => Context.GetElementState(_automationId);

    /// <summary>
    /// Get element screen bounds.
    /// </summary>
    protected ElementBounds GetBounds() => GetState().Bounds;

    #endregion

    #region Is* Methods (Immediate State Check)

    /// <inheritdoc />
    public bool IsExists() => GetState().Exists;

    /// <inheritdoc />
    public bool IsVisible()
    {
        var state = GetState();
        return state.Exists && state.IsVisible;
    }

    /// <inheritdoc />
    public bool IsEnabled()
    {
        var state = GetState();
        return state.Exists && state.IsEnabled;
    }

    /// <summary>
    /// Check if element is clickable (visible, enabled, hit-test visible).
    /// </summary>
    public bool IsClickable()
    {
        var state = GetState();
        return state.Exists && state.IsVisible && state.IsEnabled && state.IsHitTestVisible;
    }

    /// <summary>
    /// Check if element is focused.
    /// </summary>
    public bool IsFocused() => GetState().IsFocused;

    /// <inheritdoc />
    public string GetText() => GetState().Text ?? string.Empty;

    #endregion

    #region Wait* Methods (Poll Until Condition)

    /// <inheritdoc />
    public bool WaitExists(bool expected = true, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = Context.WaitFor(
            () => IsExists() == expected,
            timeoutMs,
            $"element '{_automationId}' exists={expected}");
        sw.Stop();
        Context.Logger?.LogWait(Context.TestName, Page?.Name ?? "", _automationId, $"Exists={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <inheritdoc />
    public bool WaitVisible(bool expected = true, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = Context.WaitFor(
            () => IsVisible() == expected,
            timeoutMs,
            $"element '{_automationId}' visible={expected}");
        sw.Stop();
        Context.Logger?.LogWait(Context.TestName, Page?.Name ?? "", _automationId, $"Visible={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <inheritdoc />
    public bool WaitEnabled(bool expected = true, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = Context.WaitFor(
            () => IsEnabled() == expected,
            timeoutMs,
            $"element '{_automationId}' enabled={expected}");
        sw.Stop();
        Context.Logger?.LogWait(Context.TestName, Page?.Name ?? "", _automationId, $"Enabled={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Wait for element to be clickable.
    /// </summary>
    public bool WaitClickable(int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = Context.WaitFor(
            () => IsClickable(),
            timeoutMs,
            $"element '{_automationId}' clickable");
        sw.Stop();
        Context.Logger?.LogWait(Context.TestName, Page?.Name ?? "", _automationId, "Clickable", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Wait for specific text.
    /// </summary>
    public bool WaitText(string expected, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = Context.WaitFor(
            () => GetText() == expected,
            timeoutMs,
            $"element '{_automationId}' text='{expected}'");
        sw.Stop();
        Context.Logger?.LogWait(Context.TestName, Page?.Name ?? "", _automationId, $"Text='{expected}'", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Wait for text to contain substring.
    /// </summary>
    public bool WaitTextContains(string substring, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = Context.WaitFor(
            () => GetText().Contains(substring),
            timeoutMs,
            $"element '{_automationId}' text contains '{substring}'");
        sw.Stop();
        Context.Logger?.LogWait(Context.TestName, Page?.Name ?? "", _automationId, $"TextContains='{substring}'", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    #endregion

    #region Check* Methods (Wait + Throw on Failure)

    /// <inheritdoc />
    public void CheckExists(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitExists(expected, timeoutMs))
        {
            Context.Logger.ThrowCheckFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Exists",
                $"Control '{_automationId}' exists check failed. Expected: {expected}, Actual: {IsExists()}",
                Context);
        }
    }

    /// <inheritdoc />
    public void CheckVisible(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitVisible(expected, timeoutMs))
        {
            Context.Logger.ThrowCheckFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Visible",
                $"Control '{_automationId}' visibility check failed. Expected: {expected}, Actual: {IsVisible()}",
                Context);
        }
    }

    /// <inheritdoc />
    public void CheckEnabled(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitEnabled(expected, timeoutMs))
        {
            Context.Logger.ThrowCheckFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Enabled",
                $"Control '{_automationId}' enabled check failed. Expected: {expected}, Actual: {IsEnabled()}",
                Context);
        }
    }

    /// <summary>
    /// Check element is clickable - throws if not.
    /// </summary>
    public void CheckClickable(int? timeoutMs = null)
    {
        if (!WaitClickable(timeoutMs))
        {
            var state = GetState();
            Context.Logger.ThrowCheckFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Clickable",
                $"Control '{_automationId}' is not clickable. Visible: {state.IsVisible}, Enabled: {state.IsEnabled}, HitTestVisible: {state.IsHitTestVisible}",
                Context);
        }
    }

    /// <summary>
    /// Check text equals expected - throws if not.
    /// </summary>
    public void CheckText(string expected, int? timeoutMs = null)
    {
        if (!WaitText(expected, timeoutMs))
        {
            Context.Logger.ThrowCheckFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Text",
                $"Control '{_automationId}' text check failed. Expected: '{expected}', Actual: '{GetText()}'",
                Context);
        }
    }

    #endregion

    #region Assert* Methods (Semantic Assertion with Logging)

    /// <inheritdoc />
    public void AssertExists(string? message = null)
    {
        var exists = IsExists();
        if (exists)
        {
            LogAssertion("AssertExists", true, exists);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Exists",
                exists.ToString(),
                true.ToString(),
                message ?? $"Control '{_automationId}' should exist but does not.",
                Context);
        }
    }

    /// <inheritdoc />
    public void AssertNotExists(string? message = null)
    {
        var exists = IsExists();
        if (!exists)
        {
            LogAssertion("AssertNotExists", false, exists);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "NotExists",
                exists.ToString(),
                false.ToString(),
                message ?? $"Control '{_automationId}' should not exist but does.",
                Context);
        }
    }

    /// <inheritdoc />
    public void AssertVisible(string? message = null)
    {
        CheckExists();
        var visible = IsVisible();
        if (visible)
        {
            LogAssertion("AssertVisible", true, visible);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Visible",
                visible.ToString(),
                true.ToString(),
                message ?? $"Control '{_automationId}' should be visible but is not.",
                Context);
        }
    }

    /// <inheritdoc />
    public void AssertNotVisible(string? message = null)
    {
        var visible = IsExists() && IsVisible();
        if (!visible)
        {
            LogAssertion("AssertNotVisible", false, visible);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "NotVisible",
                visible.ToString(),
                false.ToString(),
                message ?? $"Control '{_automationId}' should not be visible but is.",
                Context);
        }
    }

    /// <inheritdoc />
    public void AssertEnabled(string? message = null)
    {
        CheckVisible();
        var enabled = IsEnabled();
        if (enabled)
        {
            LogAssertion("AssertEnabled", true, enabled);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Enabled",
                enabled.ToString(),
                true.ToString(),
                message ?? $"Control '{_automationId}' should be enabled but is not.",
                Context);
        }
    }

    /// <inheritdoc />
    public void AssertDisabled(string? message = null)
    {
        CheckVisible();
        var enabled = IsEnabled();
        if (!enabled)
        {
            LogAssertion("AssertDisabled", false, enabled);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "Disabled",
                enabled.ToString(),
                false.ToString(),
                message ?? $"Control '{_automationId}' should be disabled but is not.",
                Context);
        }
    }

    /// <inheritdoc />
    public void AssertTextEquals(string expected, string? message = null)
    {
        var actual = GetText();
        if (actual == expected)
        {
            LogAssertion("AssertTextEquals", expected, actual);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "TextEquals",
                actual,
                expected,
                message ?? $"Control '{_automationId}' text mismatch. Expected: '{expected}', Actual: '{actual}'",
                Context);
        }
    }

    /// <inheritdoc />
    public void AssertTextContains(string expected, string? message = null)
    {
        var actual = GetText();
        var contains = actual.Contains(expected);
        if (contains)
        {
            LogAssertion("AssertTextContains", expected, actual);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                "TextContains",
                actual,
                expected,
                message ?? $"Control '{_automationId}' text should contain '{expected}' but was '{actual}'",
                Context);
        }
    }

    #endregion

    #region Logging Helpers

    /// <summary>
    /// Log an action.
    /// </summary>
    protected void LogAction(string action, string? value = null)
    {
        Context.Logger?.LogAction(
            Context.TestName,
            Page?.Name ?? "",
            _automationId,
            action,
            value ?? "");
    }

    /// <summary>
    /// Log an assertion.
    /// </summary>
    protected void LogAssertion(string assertion, object expected, object actual)
    {
        var success = expected?.Equals(actual) ?? actual == null;
        if (success)
        {
            Context.Logger?.LogAssertPass(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                assertion,
                actual?.ToString(),
                expected?.ToString());
        }
        else
        {
            Context.Logger?.LogAssertFail(
                Context.TestName,
                Page?.Name ?? "",
                _automationId,
                assertion,
                actual?.ToString(),
                expected?.ToString());
        }
    }

    #endregion
}
