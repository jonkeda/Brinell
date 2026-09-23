using Brinell.Core;

namespace Brinell.Html.Interfaces.Async;

public interface IAsyncHtmlElement
{
    Task<bool> IsVisible();
    Task<bool> IsEnabled();
    Task<bool> IsSelected();
    Task<string?> GetText();
    Task<string?> GetTagName();

    Task Click();
    Task SendKeys(string text, TextInputMethod method = TextInputMethod.Keys);
    Task Clear();
    Task DoubleClick();
    Task RightClick();
    Task Hover();
    Task LongPress(int durationMs = 1000);
    Task ScrollIntoView(int timeoutMs = 5000);

    Task<string?> GetAttribute(string name);

    Task<string> GetInnerHtml();
    Task<string> GetOuterHtml();
    Task<bool> GetIsChecked();
    Task<string> GetInputValue();
    Task<string?> GetDomAttribute(string attributeName);
    Task<string?> GetDomProperty(string propertyName);
    Task<string?> GetCssValue(string propertyName);
    Task Submit();
    Task Fill(string value);
    Task SelectOption(string value);
    Task SelectOption(string[] values);
    Task SelectOptionByLabel(string label);
    Task Check();
    Task Uncheck();
    Task Focus();
    Task Blur();
    Task<T?> Evaluate<T>(string expression);
    Task Evaluate(string expression);
}
