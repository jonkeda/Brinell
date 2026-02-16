using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

public abstract class ToggleControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected ToggleControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected ToggleControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public bool IsChecked()
    {
        return RunWithElement(element => element.IsChecked);
    }

    public TScope SetChecked(bool value)
    {
        return RunWithElement(element =>
        {
            if (value)
            {
                element.Check();
                return;
            }

            element.Uncheck();
        });
    }

    public bool WaitChecked(bool expected, int? timeoutMs = null)
    {
        return Poll(() => IsChecked() == expected, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertChecked(bool expected)
    {
        if (!WaitChecked(expected))
        {
            var actual = IsChecked();
            throw new AssertionException(
                $"Checked state mismatch. Expected: {expected}, Actual: {actual}");
        }

        return ContainingScope;
    }
}
