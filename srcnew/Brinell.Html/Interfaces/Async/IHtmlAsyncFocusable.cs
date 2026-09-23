namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncFocusable<TScope> : IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> Focus();
    Task<TScope> Blur();
    Task<bool> HasFocus();
}
