namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncClickable<TScope> : IHtmlAsyncControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> Click();
    Task<TScope> SendKeys(string text);
    Task<TScope> Clear();
    Task<TScope> ScrollIntoView(int timeoutMs = 5000);
    Task<TScope> DoubleClick();
    Task<TScope> RightClick();
    Task<TScope> Hover();
}
