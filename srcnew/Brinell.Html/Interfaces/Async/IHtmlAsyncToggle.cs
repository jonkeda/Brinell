namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncToggle<TScope> : IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<bool> IsChecked();
    Task<TScope> SetChecked(bool value);
    Task<bool> WaitChecked(bool expected, int? timeoutMs = null);
    Task<TScope> AssertChecked(bool expected);

    Task<TScope> Check();
    Task<TScope> Uncheck();
    Task<TScope> Toggle();
}
