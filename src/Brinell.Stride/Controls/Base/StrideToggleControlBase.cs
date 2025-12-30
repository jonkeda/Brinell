using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Logging;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls.Base;

/// <summary>
/// Base class for toggle controls (CheckBox, ToggleButton).
/// </summary>
public abstract class StrideToggleControlBase : StrideContentControlBase, IToggleControl
{
    /// <summary>
    /// Create a new toggle control.
    /// </summary>
    protected StrideToggleControlBase(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <inheritdoc />
    public bool IsChecked() => GetState().IsChecked ?? false;

    /// <inheritdoc />
    public void Toggle()
    {
        var before = IsChecked();
        Click();
        LogAction("Toggle", $"{before} -> {!before}");
    }

    /// <inheritdoc />
    public void Check()
    {
        if (!IsChecked())
        {
            Click();
        }
        LogAction("Check");
    }

    /// <inheritdoc />
    public void Uncheck()
    {
        if (IsChecked())
        {
            Click();
        }
        LogAction("Uncheck");
    }

    /// <summary>
    /// Set specific checked state.
    /// </summary>
    public void SetChecked(bool value)
    {
        if (value)
            Check();
        else
            Uncheck();
    }

    /// <summary>
    /// Wait for checked state.
    /// </summary>
    public bool WaitChecked(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsChecked() == expected,
            timeoutMs,
            $"element '{AutomationId}' checked={expected}");
    }

    /// <summary>
    /// Assert is checked.
    /// </summary>
    public void AssertChecked(string? message = null)
    {
        var isChecked = IsChecked();
        LogAssertion("AssertChecked", true, isChecked);

        if (!isChecked)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' should be checked but is not.");
        }
    }

    /// <summary>
    /// Assert is unchecked.
    /// </summary>
    public void AssertUnchecked(string? message = null)
    {
        var isChecked = IsChecked();
        LogAssertion("AssertUnchecked", false, isChecked);

        if (isChecked)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' should be unchecked but is checked.");
        }
    }
}
