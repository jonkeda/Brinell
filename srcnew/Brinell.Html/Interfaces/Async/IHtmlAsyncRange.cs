namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncRange<TScope> : IHtmlAsyncFocusable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<string?> GetMin();
    Task<string?> GetMax();
    Task<string?> GetStep();
    Task<string> GetValue();
    Task<TScope> SetValue(string value);
}
