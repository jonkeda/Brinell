namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<bool> IsExists();
    Task<bool?> IsVisible();
    Task<bool?> IsEnabled();

    Task<bool> WaitExists(bool? expected, int? timeoutMs = null);
    Task<bool> WaitVisible(bool? expected, int? timeoutMs = null);
    Task<bool> WaitEnabled(bool? expected, int? timeoutMs = null);

    Task<TScope> AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    Task<TScope> AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    Task<TScope> AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);

    Task<string?> GetText(int? timeoutMs = null);
    Task<bool> WaitText(string? expected, int? timeoutMs = null);
    Task<TScope> AssertText(string? expected, string? message = null, int? timeoutMs = null);
    Task<TScope> AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);

    Task<string?> GetAttribute(string name);
}
