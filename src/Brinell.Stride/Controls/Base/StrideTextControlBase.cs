using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
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
            throw new NotSupportedException($"Control '{AutomationId}' is read-only.");
        Context.TypeText(text);
        LogAction("Enter", text);
    }

    /// <inheritdoc />
    public virtual void Clear()
    {
        if (!IsEditable)
            throw new NotSupportedException($"Control '{AutomationId}' is read-only.");
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
            throw new NotSupportedException($"Control '{AutomationId}' is read-only.");
        Context.PressKey(VirtualKey.End);
        Context.TypeText(text);
        LogAction("Append", text);
    }

    /// <inheritdoc />
    public virtual bool IsReadOnly() => !IsEditable;

    /// <inheritdoc />
    public virtual int GetTextLength() => GetText().Length;
}
