using Brinell.Core.Interfaces;

namespace Brinell.Html.Interfaces;

/// <summary>
/// HTML element abstraction for web automation.
/// </summary>
public interface IHtmlElement : IElement<IHtmlElement>
{
    string? GetDomAttribute(string attributeName);
    string? GetDomProperty(string propertyName);
    string? GetCssValue(string propertyName);
    void Submit();
    string InnerHtml { get; }
    string OuterHtml { get; }
    bool IsChecked { get; }
    string InputValue { get; }
    void Fill(string value);
    void SelectOption(string value);
    void SelectOption(string[] values);
    void SelectOptionByLabel(string label);
    void Check();
    void Uncheck();
    void Focus();
    void Blur();
    T? Evaluate<T>(string expression);
    void Evaluate(string expression);
}