using Brinell.Core.Exceptions;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Base class for toggle controls (CheckBox, ToggleButton).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract class ToggleControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    protected ToggleControlBase(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    public bool IsChecked() => GetState().IsChecked ?? false;

    public TScope Toggle()
    {
        Click();
        LogAction("Toggle");
        return ContainingScope;
    }

    public TScope Check()
    {
        if (!IsChecked())
            Click();
        LogAction("Check");
        return ContainingScope;
    }

    public TScope Uncheck()
    {
        if (IsChecked())
            Click();
        LogAction("Uncheck");
        return ContainingScope;
    }

    public TScope SetChecked(bool value) => value ? Check() : Uncheck();

    public bool WaitChecked(bool expected = true, int? timeoutMs = null)
    {
        return Poll(() => IsChecked() == expected, timeoutMs ?? Context.Timeouts.DefaultWait);
    }

    public TScope AssertChecked(string? message = null)
    {
        if (!IsChecked())
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' should be checked but is not.");
        }
        return ContainingScope;
    }

    public TScope AssertUnchecked(string? message = null)
    {
        if (IsChecked())
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' should be unchecked but is checked.");
        }
        return ContainingScope;
    }
}
