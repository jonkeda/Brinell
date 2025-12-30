using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI text input controls.
/// </summary>
public class StrideEditTextControl : StrideTextControlBase, IEditableTextControl
{
    /// <inheritdoc />
    public override bool IsEditable => true;

    /// <summary>
    /// Create a new edit text control.
    /// </summary>
    public StrideEditTextControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <inheritdoc />
    public override void SetText(string text)
    {
        Focus();
        Clear();
        Context.TypeText(text);
        LogAction("SetText", text);
    }

    /// <inheritdoc />
    public void AppendText(string text)
    {
        Focus();
        MoveToEnd();
        Context.TypeText(text);
        LogAction("AppendText", text);
    }

    /// <inheritdoc />
    public override void Clear()
    {
        Focus();
        SelectAll();
        Context.PressKey(VirtualKey.Delete);
        LogAction("Clear");
    }

    /// <summary>
    /// Focus this control.
    /// </summary>
    public void Focus()
    {
        CheckVisible();
        Context.ClickElement(_automationId);
        LogAction("Focus");
    }

    /// <summary>
    /// Select all text (Ctrl+A).
    /// </summary>
    public void SelectAll()
    {
        Context.Input.HotKey(VirtualKey.A, VirtualKey.Control);
        LogAction("SelectAll");
    }

    /// <summary>
    /// Copy selected text (Ctrl+C).
    /// </summary>
    public void Copy()
    {
        Context.Input.HotKey(VirtualKey.C, VirtualKey.Control);
        LogAction("Copy");
    }

    /// <summary>
    /// Cut selected text (Ctrl+X).
    /// </summary>
    public void Cut()
    {
        Context.Input.HotKey(VirtualKey.X, VirtualKey.Control);
        LogAction("Cut");
    }

    /// <summary>
    /// Paste from clipboard (Ctrl+V).
    /// </summary>
    public void Paste()
    {
        Context.Input.HotKey(VirtualKey.V, VirtualKey.Control);
        LogAction("Paste");
    }

    /// <summary>
    /// Undo last change (Ctrl+Z).
    /// </summary>
    public void Undo()
    {
        Context.Input.HotKey(VirtualKey.Z, VirtualKey.Control);
        LogAction("Undo");
    }

    /// <summary>
    /// Redo last undo (Ctrl+Y).
    /// </summary>
    public void Redo()
    {
        Context.Input.HotKey(VirtualKey.Y, VirtualKey.Control);
        LogAction("Redo");
    }

    /// <summary>
    /// Move cursor to end (End key).
    /// </summary>
    private void MoveToEnd()
    {
        Context.PressKey(VirtualKey.End);
    }
}
