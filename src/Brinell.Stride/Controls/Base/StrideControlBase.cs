using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Exceptions;
using Brinell.Stride.Communication;
using Brinell.Stride.Infrastructure;

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
        return Context.WaitFor(
            () => IsExists() == expected,
            timeoutMs,
            $"element '{_automationId}' exists={expected}");
    }

    /// <inheritdoc />
    public bool WaitVisible(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsVisible() == expected,
            timeoutMs,
            $"element '{_automationId}' visible={expected}");
    }

    /// <inheritdoc />
    public bool WaitEnabled(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsEnabled() == expected,
            timeoutMs,
            $"element '{_automationId}' enabled={expected}");
    }

    /// <summary>
    /// Wait for element to be clickable.
    /// </summary>
    public bool WaitClickable(int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsClickable(),
            timeoutMs,
            $"element '{_automationId}' clickable");
    }

    /// <summary>
    /// Wait for specific text.
    /// </summary>
    public bool WaitText(string expected, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetText() == expected,
            timeoutMs,
            $"element '{_automationId}' text='{expected}'");
    }

    /// <summary>
    /// Wait for text to contain substring.
    /// </summary>
    public bool WaitTextContains(string substring, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetText().Contains(substring),
            timeoutMs,
            $"element '{_automationId}' text contains '{substring}'");
    }

    #endregion

    #region Check* Methods (Wait + Throw on Failure)

    /// <inheritdoc />
    public void CheckExists(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitExists(expected, timeoutMs))
        {
            throw new CheckFailedException(
                $"Control '{_automationId}' exists check failed. Expected: {expected}, Actual: {IsExists()}");
        }
    }

    /// <inheritdoc />
    public void CheckVisible(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitVisible(expected, timeoutMs))
        {
            throw new CheckFailedException(
                $"Control '{_automationId}' visibility check failed. Expected: {expected}, Actual: {IsVisible()}");
        }
    }

    /// <inheritdoc />
    public void CheckEnabled(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitEnabled(expected, timeoutMs))
        {
            throw new CheckFailedException(
                $"Control '{_automationId}' enabled check failed. Expected: {expected}, Actual: {IsEnabled()}");
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
            throw new CheckFailedException(
                $"Control '{_automationId}' is not clickable. " +
                $"Visible: {state.IsVisible}, Enabled: {state.IsEnabled}, HitTestVisible: {state.IsHitTestVisible}");
        }
    }

    /// <summary>
    /// Check text equals expected - throws if not.
    /// </summary>
    public void CheckText(string expected, int? timeoutMs = null)
    {
        if (!WaitText(expected, timeoutMs))
        {
            throw new CheckFailedException(
                $"Control '{_automationId}' text check failed. Expected: '{expected}', Actual: '{GetText()}'");
        }
    }

    #endregion

    #region Assert* Methods (Semantic Assertion with Logging)

    /// <inheritdoc />
    public void AssertExists(string? message = null)
    {
        var exists = IsExists();
        LogAssertion("AssertExists", true, exists);

        if (!exists)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should exist but does not.");
        }
    }

    /// <inheritdoc />
    public void AssertNotExists(string? message = null)
    {
        var exists = IsExists();
        LogAssertion("AssertNotExists", false, exists);

        if (exists)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should not exist but does.");
        }
    }

    /// <inheritdoc />
    public void AssertVisible(string? message = null)
    {
        CheckExists();
        var visible = IsVisible();
        LogAssertion("AssertVisible", true, visible);

        if (!visible)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should be visible but is not.");
        }
    }

    /// <inheritdoc />
    public void AssertNotVisible(string? message = null)
    {
        var visible = IsExists() && IsVisible();
        LogAssertion("AssertNotVisible", false, visible);

        if (visible)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should not be visible but is.");
        }
    }

    /// <inheritdoc />
    public void AssertEnabled(string? message = null)
    {
        CheckVisible();
        var enabled = IsEnabled();
        LogAssertion("AssertEnabled", true, enabled);

        if (!enabled)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should be enabled but is not.");
        }
    }

    /// <inheritdoc />
    public void AssertDisabled(string? message = null)
    {
        CheckVisible();
        var enabled = IsEnabled();
        LogAssertion("AssertDisabled", false, enabled);

        if (enabled)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should be disabled but is not.");
        }
    }

    /// <inheritdoc />
    public void AssertTextEquals(string expected, string? message = null)
    {
        var actual = GetText();
        LogAssertion("AssertTextEquals", expected, actual);

        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' text mismatch. Expected: '{expected}', Actual: '{actual}'");
        }
    }

    /// <inheritdoc />
    public void AssertTextContains(string expected, string? message = null)
    {
        var actual = GetText();
        var contains = actual.Contains(expected);
        LogAssertion("AssertTextContains", expected, actual);

        if (!contains)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' text should contain '{expected}' but was '{actual}'");
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
