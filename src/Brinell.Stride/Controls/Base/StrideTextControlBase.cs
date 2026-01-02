using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Logging;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls.Base;

/// <summary>
/// Base class for text-related controls.
/// </summary>
public abstract class StrideTextControlBase : StrideControlBase, ITextControl
{
    /// <summary>
    /// Whether this control supports text input.
    /// </summary>
    public abstract bool IsEditable { get; }

    /// <summary>
    /// Create a new text control.
    /// </summary>
    protected StrideTextControlBase(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <inheritdoc />
    public virtual void Enter(string text)
    {
        if (!IsEditable)
        {
            Context.Logger.LogAndThrow(
                Context.TestName,
                Page?.Name ?? "",
                AutomationId,
                "Enter",
                new NotSupportedException($"Control '{AutomationId}' is read-only."),
                Context);
        }
        Context.TypeText(text);
        LogAction("Enter", text);
    }

    /// <inheritdoc />
    public virtual void Clear()
    {
        if (!IsEditable)
        {
            Context.Logger.LogAndThrow(
                Context.TestName,
                Page?.Name ?? "",
                AutomationId,
                "Clear",
                new NotSupportedException($"Control '{AutomationId}' is read-only."),
                Context);
        }
        Context.Input.HotKey(VirtualKey.A, VirtualKey.Control);
        Context.PressKey(VirtualKey.Delete);
        LogAction("Clear");
    }

    /// <inheritdoc />
    public virtual void ClearAndEnter(string text)
    {
        Clear();
        Enter(text);
    }

    /// <inheritdoc />
    public virtual void SetText(string text) => ClearAndEnter(text);

    /// <inheritdoc />
    public virtual void Append(string text)
    {
        if (!IsEditable)
        {
            Context.Logger.LogAndThrow(
                Context.TestName,
                Page?.Name ?? "",
                AutomationId,
                "Append",
                new NotSupportedException($"Control '{AutomationId}' is read-only."),
                Context);
        }
        Context.PressKey(VirtualKey.End);
        Context.TypeText(text);
        LogAction("Append", text);
    }

    /// <inheritdoc />
    public virtual bool IsReadOnly() => !IsEditable;

    /// <inheritdoc />
    public virtual int GetTextLength() => GetText().Length;
    
    /// <summary>
    /// Assert control text is empty.
    /// </summary>
    public virtual void AssertTextEmpty(string? message = null)
    {
        var actual = GetText();
        var isEmpty = string.IsNullOrEmpty(actual);
        
        if (isEmpty)
        {
            LogAssertion("AssertTextEmpty", "(empty)", actual);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                AutomationId,
                "TextEmpty",
                actual,
                "(empty)",
                message ?? $"Control '{AutomationId}' text should be empty but was '{actual}'",
                Context);
        }
    }
    
    /// <summary>
    /// Assert control text is not empty.
    /// </summary>
    public virtual void AssertTextNotEmpty(string? message = null)
    {
        var actual = GetText();
        var isEmpty = string.IsNullOrEmpty(actual);
        
        if (!isEmpty)
        {
            LogAssertion("AssertTextNotEmpty", "(non-empty)", actual);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                AutomationId,
                "TextNotEmpty",
                actual,
                "(non-empty)",
                message ?? $"Control '{AutomationId}' text should not be empty",
                Context);
        }
    }
    
    /// <summary>
    /// Assert control text starts with expected prefix.
    /// </summary>
    public virtual void AssertTextStartsWith(string prefix, string? message = null)
    {
        var actual = GetText();
        var startsWith = actual.StartsWith(prefix, StringComparison.Ordinal);
        
        if (startsWith)
        {
            LogAssertion("AssertTextStartsWith", prefix, actual);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                AutomationId,
                "TextStartsWith",
                actual,
                prefix,
                message ?? $"Control '{AutomationId}' text should start with '{prefix}' but was '{actual}'",
                Context);
        }
    }
    
    /// <summary>
    /// Assert control text ends with expected suffix.
    /// </summary>
    public virtual void AssertTextEndsWith(string suffix, string? message = null)
    {
        var actual = GetText();
        var endsWith = actual.EndsWith(suffix, StringComparison.Ordinal);
        
        if (endsWith)
        {
            LogAssertion("AssertTextEndsWith", suffix, actual);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                AutomationId,
                "TextEndsWith",
                actual,
                suffix,
                message ?? $"Control '{AutomationId}' text should end with '{suffix}' but was '{actual}'",
                Context);
        }
    }
    
    /// <summary>
    /// Assert control text matches regex pattern.
    /// </summary>
    public virtual void AssertTextMatches(string pattern, string? message = null)
    {
        var actual = GetText();
        var matches = System.Text.RegularExpressions.Regex.IsMatch(actual, pattern);
        
        if (matches)
        {
            LogAssertion("AssertTextMatches", pattern, actual);
        }
        else
        {
            Context.Logger.ThrowAssertionFailed(
                Context.TestName,
                Page?.Name ?? "",
                AutomationId,
                "TextMatches",
                actual,
                pattern,
                message ?? $"Control '{AutomationId}' text should match pattern '{pattern}' but was '{actual}'",
                Context);
        }
    }
}
