using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Editable text input control for Stride UI.
/// </summary>
public class EditText<TScope> : TextControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public override bool IsEditable => true;

    public EditText(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    public override TScope SetText(string text)
    {
        // Server-side: set text directly via automation pipe
        var success = Context.SetElementText(AutomationId, text);
        if (!success)
            throw new InvalidOperationException($"Failed to set text on '{AutomationId}'");
        return ContainingScope;
    }

    public override TScope Clear()
    {
        // Server-side: set text to empty via automation pipe
        Context.SetElementText(AutomationId, "");
        return ContainingScope;
    }

    /// <summary>
    /// Focus this control via server-side click.
    /// </summary>
    public TScope Focus()
    {
        // Server-side click to focus — no physical mouse needed
        var response = Context.SendCommand(
            Brinell.Stride.Communication.AutomationCommand.Action("Click", AutomationId));
        if (!response.Success)
            throw new InvalidOperationException($"Failed to focus '{AutomationId}': {response.Error}");
        return ContainingScope;
    }

    /// <summary>
    /// Select all text via server-side automation.
    /// </summary>
    public TScope SelectAll()
    {
        var response = Context.SendCommand(
            Brinell.Stride.Communication.AutomationCommand.Action("SelectAll", AutomationId));
        if (!response.Success)
            throw new InvalidOperationException($"Server-side SelectAll failed for '{AutomationId}': {response.Error}");
        return ContainingScope;
    }

    /// <summary>
    /// Copy selected text (Ctrl+C) via server-side key combination.
    /// </summary>
    public TScope Copy()
    {
        var response = Context.SendCommand(
            Brinell.Stride.Communication.AutomationCommand.Action("SimulateKeyCombination", null, "LeftCtrl", "C"));
        if (!response.Success)
            throw new InvalidOperationException($"Server-side Copy failed: {response.Error}");
        return ContainingScope;
    }

    /// <summary>
    /// Cut selected text (Ctrl+X) via server-side key combination.
    /// </summary>
    public TScope Cut()
    {
        var response = Context.SendCommand(
            Brinell.Stride.Communication.AutomationCommand.Action("SimulateKeyCombination", null, "LeftCtrl", "X"));
        if (!response.Success)
            throw new InvalidOperationException($"Server-side Cut failed: {response.Error}");
        return ContainingScope;
    }

    /// <summary>
    /// Paste from clipboard (Ctrl+V) via server-side key combination.
    /// </summary>
    public TScope Paste()
    {
        var response = Context.SendCommand(
            Brinell.Stride.Communication.AutomationCommand.Action("SimulateKeyCombination", null, "LeftCtrl", "V"));
        if (!response.Success)
            throw new InvalidOperationException($"Server-side Paste failed: {response.Error}");
        return ContainingScope;
    }

    /// <summary>
    /// Undo last change (Ctrl+Z) via server-side key combination.
    /// </summary>
    public TScope Undo()
    {
        var response = Context.SendCommand(
            Brinell.Stride.Communication.AutomationCommand.Action("SimulateKeyCombination", null, "LeftCtrl", "Z"));
        if (!response.Success)
            throw new InvalidOperationException($"Server-side Undo failed: {response.Error}");
        return ContainingScope;
    }

    /// <summary>
    /// Redo last undo (Ctrl+Y) via server-side key combination.
    /// </summary>
    public TScope Redo()
    {
        var response = Context.SendCommand(
            Brinell.Stride.Communication.AutomationCommand.Action("SimulateKeyCombination", null, "LeftCtrl", "Y"));
        if (!response.Success)
            throw new InvalidOperationException($"Server-side Redo failed: {response.Error}");
        return ContainingScope;
    }
}
