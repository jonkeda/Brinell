namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncScrollable<TScope> : IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> ScrollTo(int x, int y);
    Task<TScope> ScrollToTop();
}
