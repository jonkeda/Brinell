using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class SelectorControlBase<TScope> : FocusableControlBase<TScope>, IHtmlAsyncSelector<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected SelectorControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected SelectorControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public abstract TScope SelectByValue(string value);

    public abstract TScope SelectByText(string text);

    public abstract string? GetSelectedValue();

    #region IHtmlAsyncSelector<TScope> explicit implementation

    Task<TScope> IHtmlAsyncSelector<TScope>.SelectByValue(string value)
        => Task.FromResult(SelectByValue(value));

    Task<TScope> IHtmlAsyncSelector<TScope>.SelectByText(string text)
        => Task.FromResult(SelectByText(text));

    Task<string?> IHtmlAsyncSelector<TScope>.GetSelectedValue()
        => Task.FromResult(GetSelectedValue());

    Task<TScope> IHtmlAsyncSelector<TScope>.SelectMultiple(params string[] values)
    {
        foreach (var value in values)
        {
            SelectByValue(value);
        }

        return Task.FromResult(ContainingScope);
    }

    #endregion
}
