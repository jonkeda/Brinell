using Brinell.Core.Interfaces;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Base class for clickable Stride controls (buttons, content controls).
/// Implements IClickableControlObject with fluent TScope chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract class ClickableControlBase<TScope> : ControlBase<TScope>, IClickableControlObject<TScope>
    where TScope : IStrideScope<TScope>
{
    protected ClickableControlBase(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    public new bool? IsClickable() => base.IsClickable();

    public TScope Click(int? timeoutMs = null)
    {
        AssertClickable(true, timeoutMs: timeoutMs);
        // Use server-side click via automation pipe (runs on game thread, no focus/coordinate issues)
        var response = Context.SendCommand(
            Brinell.Stride.Communication.AutomationCommand.Action("Click", AutomationId));
        if (!response.Success)
            throw new InvalidOperationException($"Server-side click failed for '{AutomationId}': {response.Error}");
        LogAction("Click");
        return ContainingScope;
    }

    public TScope DoubleClick(int? timeoutMs = null)
    {
        AssertClickable(true, timeoutMs: timeoutMs);
        // Server-side: raise Click event twice
        var cmd = Brinell.Stride.Communication.AutomationCommand.Action("Click", AutomationId);
        Context.SendCommand(cmd);
        Context.SendCommand(cmd);
        LogAction("DoubleClick");
        return ContainingScope;
    }

    public TScope RightClick(int? timeoutMs = null)
    {
        throw new NotSupportedException("RightClick is not supported in Stride UI automation.");
    }

    public new bool WaitClickable(bool? expected, int? timeoutMs = null)
        => base.WaitClickable(expected, timeoutMs);

    public new TScope AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
        => base.AssertClickable(expected, message, timeoutMs);

    public TScope Hover(int? timeoutMs = null)
    {
        throw new NotSupportedException("Hover is not supported in Stride UI automation.");
    }

    public TScope LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        throw new NotSupportedException("LongPress is not supported in Stride UI automation.");
    }
}
