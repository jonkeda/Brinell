using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for text input controls.
/// Provides virtual text entry methods that can be overridden.
/// </summary>
public abstract class TextControlBase : ClickableControlBase, ITextControlObject
{
    /// <summary>
    /// Creates a new text control.
    /// </summary>
    protected TextControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new text control using AutomationId.
    /// </summary>
    protected TextControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    #region Focus

    /// <inheritdoc />
    public virtual bool IsFocused()
    {
        var element = FindElement();
        if (element is null) return false;

        try
        {
            var activeElement = Driver.SwitchTo().ActiveElement();
            return element.Equals(activeElement);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public virtual bool WaitFocused(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsFocused, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public virtual void CheckFocused(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (!WaitFocused(expected, timeoutMs))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Element {(expected.Value ? "does not have focus" : "still has focus")}",
                Locator.Value,
                timeout,
                "CheckFocused",
                $"Focused={IsFocused()}");
        }
    }

    /// <inheritdoc />
    public virtual void AssertFocused(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        CheckFocused(expected, timeoutMs);

        var actual = IsFocused();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected element to {(expected.Value ? "have focus" : "not have focus")}",
                Locator.Value,
                "AssertFocused");
        }
    }

    /// <inheritdoc />
    public virtual void Focus(int? timeoutMs = null)
    {
        Log("Focus()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);
        element.Click(); // Clicking focuses the element in MAUI
    }

    /// <inheritdoc />
    public virtual void Blur(int? timeoutMs = null)
    {
        Log("Blur()");
        var element = FindElement();
        if (element is not null && IsFocused())
        {
            element.SendKeys("\t");
        }
    }

    #endregion

    #region Text Input

    /// <inheritdoc />
    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text is null) return;

        Log($"Enter(\"{text}\")");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);
        element.Clear();
        element.SendKeys(text);
    }

    /// <inheritdoc />
    public virtual void Clear(int? timeoutMs = null)
    {
        Log("Clear()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);
        element.Clear();
    }

    /// <inheritdoc />
    public virtual void ClearAndEnter(string? text, int? timeoutMs = null)
    {
        Log($"ClearAndEnter(\"{text}\")");
        Clear(timeoutMs);

        if (text is not null)
        {
            var element = FindElementRequired(timeoutMs);
            element.SendKeys(text);
        }
    }

    /// <inheritdoc />
    public virtual void Append(string? text, int? timeoutMs = null)
    {
        if (text is null) return;

        Log($"Append(\"{text}\")");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);
        element.SendKeys(text);
    }

    #endregion

    #region Read-Only

    /// <inheritdoc />
    public virtual bool IsReadOnly()
    {
        var element = FindElement();
        if (element is null) return false;

        var readOnly = element.GetAttribute("IsReadOnly");
        return string.Equals(readOnly, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public virtual bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsReadOnly, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public virtual void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (!WaitReadOnly(expected, timeoutMs))
        {
            throw new AssertionException(
                message ?? $"Expected element to be {(expected.Value ? "read-only" : "editable")}",
                Locator.Value,
                "AssertReadOnly");
        }
    }

    #endregion

    #region Text Length

    /// <inheritdoc />
    public virtual int GetTextLength(int? timeoutMs = null)
    {
        return GetText(timeoutMs).Length;
    }

    /// <inheritdoc />
    public virtual void AssertTextLength(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetTextLength(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected text length {expected}, but was {actual}",
                Locator.Value,
                "AssertTextLength");
        }
    }

    #endregion

    /// <inheritdoc />
    public override string GetText(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);

        // For text controls, try to get the Text attribute first
        var text = element.GetAttribute("Text");
        if (text is not null)
            return text;

        // Fall back to element.Text
        return element.Text ?? string.Empty;
    }
}
