using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class ToggleControlBase<TScope> : ClickableControlBase<TScope>, IHtmlAsyncToggle<TScope>
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

    #region IHtmlAsyncToggle<TScope> explicit implementation

    Task<bool> IHtmlAsyncToggle<TScope>.IsChecked()
        => Task.FromResult(IsChecked());

    Task<TScope> IHtmlAsyncToggle<TScope>.SetChecked(bool value)
        => Task.FromResult(SetChecked(value));

    Task<bool> IHtmlAsyncToggle<TScope>.WaitChecked(bool expected, int? timeoutMs)
        => Task.FromResult(WaitChecked(expected, timeoutMs));

    Task<TScope> IHtmlAsyncToggle<TScope>.AssertChecked(bool expected)
        => Task.FromResult(AssertChecked(expected));

    Task<TScope> IHtmlAsyncToggle<TScope>.Check()
        => Task.FromResult(SetChecked(true));

    Task<TScope> IHtmlAsyncToggle<TScope>.Uncheck()
        => Task.FromResult(SetChecked(false));

    async Task<TScope> IHtmlAsyncToggle<TScope>.Toggle()
    {
        var current = IsChecked();
        return SetChecked(!current);
    }

    #endregion
}