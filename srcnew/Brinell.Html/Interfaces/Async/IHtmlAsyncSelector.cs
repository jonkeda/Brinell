namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncSelector<TScope> : IHtmlAsyncFocusable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> SelectByValue(string value);
    Task<TScope> SelectByText(string text);
    Task<string?> GetSelectedValue();

    Task<TScope> SelectMultiple(params string[] values);
}
