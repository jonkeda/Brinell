using Brinell.Core.Exceptions;
using Brinell.Stride.Infrastructure;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Base class for text-related Stride controls.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract class TextControlBase<TScope> : ControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public abstract bool IsEditable { get; }

    protected TextControlBase(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    public TScope Enter(string text)
    {
        if (!IsEditable)
            throw new InvalidOperationException($"Control '{AutomationId}' is read-only.");

        // Server-side: append text to current value
        var current = GetText() ?? "";
        Context.SetElementText(AutomationId, current + text);
        LogAction("Enter", text);
        return ContainingScope;
    }

    public virtual TScope Clear()
    {
        if (!IsEditable)
            throw new InvalidOperationException($"Control '{AutomationId}' is read-only.");

        // Server-side: set text to empty directly via automation pipe
        Context.SetElementText(AutomationId, "");
        LogAction("Clear");
        return ContainingScope;
    }

    public TScope ClearAndEnter(string text)
    {
        Clear();
        Enter(text);
        return ContainingScope;
    }

    public virtual TScope SetText(string text) => ClearAndEnter(text);

    public TScope Append(string text)
    {
        if (!IsEditable)
            throw new InvalidOperationException($"Control '{AutomationId}' is read-only.");

        // Server-side: append text to current value via automation pipe
        var current = GetText() ?? "";
        Context.SetElementText(AutomationId, current + text);
        LogAction("Append", text);
        return ContainingScope;
    }

    public bool IsReadOnly() => !IsEditable;
    public int GetTextLength() => GetText()?.Length ?? 0;

    public TScope AssertTextEmpty(string? message = null)
    {
        var actual = GetText();
        if (!string.IsNullOrEmpty(actual))
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' text should be empty but was '{actual}'");
        }
        return ContainingScope;
    }

    public TScope AssertTextNotEmpty(string? message = null)
    {
        var actual = GetText();
        if (string.IsNullOrEmpty(actual))
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' text should not be empty");
        }
        return ContainingScope;
    }
}
