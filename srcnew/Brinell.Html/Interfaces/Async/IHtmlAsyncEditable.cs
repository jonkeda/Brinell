namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncEditable<TScope> : IHtmlAsyncFocusable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> SetText(string text);
    Task<string?> GetValue();
    Task<TScope> TypeText(string text);
    Task<TScope> AssertValue(string? expected);
    Task<TScope> WaitValue(string? expected, int? timeoutMs = null);

    Task<TScope> AppendText(string text);
}
