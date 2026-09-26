using System.Drawing;
using Brinell.Core;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Microsoft.Playwright;
using static Brinell.Html.Playwright.PlaywrightErrors;

namespace Brinell.Html.Playwright;

/// <summary>
/// <see cref="IHtmlElement"/> over a Playwright <see cref="ILocator"/>. Every call goes through
/// <see cref="PlaywrightErrors.Live{T}"/>, so a closed browser and a detached element arrive as
/// Brinell's signals, not as <see cref="PlaywrightException"/>.
/// </summary>
public sealed class PlaywrightHtmlElement : IHtmlElement
{
    // The key the element carries for InstanceKey; written once, on first read. A replaced node
    // has none, so it gets a new key, and "the same element" is a string comparison.
    private const string InstanceKeyScript =
        "el => el.dataset.brinellKey || (el.dataset.brinellKey = crypto.randomUUID())";

    private readonly ILocator _locator;

    /// <summary>Wraps <paramref name="locator"/>.</summary>
    public PlaywrightHtmlElement(ILocator locator)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    /// <inheritdoc />
    public string InstanceKey => Live(() => _locator.EvaluateAsync<string>(InstanceKeyScript).GetAwaiter().GetResult());

    public bool Visible => Live(() => _locator.IsVisibleAsync().GetAwaiter().GetResult());

    public bool Enabled => Live(() => _locator.IsEnabledAsync().GetAwaiter().GetResult());

    public bool Selected => IsChecked;

    public string? Text => Live(() => _locator.InnerTextAsync().GetAwaiter().GetResult());

    public string? TagName => Live(() => _locator.EvaluateAsync<string>("el => el.tagName.toLowerCase()").GetAwaiter().GetResult());

    public Point Location => EvaluateRect(box => new Point((int)box.X, (int)box.Y));

    public Size Size => EvaluateRect(box => new Size((int)box.Width, (int)box.Height));

    public Rectangle Rect => EvaluateRect(box => new Rectangle((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height));

    public void Click() => Live(() => _locator.ClickAsync().GetAwaiter().GetResult());

    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys) => Live(() =>
    {
        switch (method)
        {
            case TextInputMethod.SetValue:
            case TextInputMethod.Paste:
                _locator.FillAsync(text).GetAwaiter().GetResult();
                break;
            default:
                _locator.PressSequentiallyAsync(text).GetAwaiter().GetResult();
                break;
        }
    });

    public void Clear() => Live(() => _locator.ClearAsync().GetAwaiter().GetResult());

    public void DoubleClick() => Live(() => _locator.DblClickAsync().GetAwaiter().GetResult());

    public void RightClick() => Live(() => _locator.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right }).GetAwaiter().GetResult());

    public void Hover() => Live(() => _locator.HoverAsync().GetAwaiter().GetResult());

    public void LongPress(int durationMs = 1000) => Live(() => _locator.ClickAsync(new LocatorClickOptions { Delay = durationMs }).GetAwaiter().GetResult());

    public void ScrollIntoView(int timeoutMs = 5000)
        => Live(() => _locator.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions { Timeout = timeoutMs }).GetAwaiter().GetResult());

    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500) => Live(() =>
    {
        var page = _locator.Page;
        page.Mouse.MoveAsync(startX, startY).GetAwaiter().GetResult();
        page.Mouse.DownAsync().GetAwaiter().GetResult();
        page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = Math.Max(1, durationMs / 16) }).GetAwaiter().GetResult();
        page.Mouse.UpAsync().GetAwaiter().GetResult();
    });

    public string? GetAttribute(string name) => Live(() => _locator.GetAttributeAsync(name).GetAwaiter().GetResult());

    public IHtmlElement? TryFindElement(Locator locator) => Live<IHtmlElement?>(() =>
    {
        var childLocator = LocatorExtensions.ToPlaywrightLocator(_locator, locator);
        return childLocator.CountAsync().GetAwaiter().GetResult() > 0
            ? new PlaywrightHtmlElement(childLocator.First)
            : null;
    });

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator) => Live<IReadOnlyList<IHtmlElement>>(() =>
    {
        var childLocator = LocatorExtensions.ToPlaywrightLocator(_locator, locator);
        var count = childLocator.CountAsync().GetAwaiter().GetResult();
        var elements = new List<IHtmlElement>(count);
        for (var i = 0; i < count; i++)
        {
            elements.Add(new PlaywrightHtmlElement(childLocator.Nth(i)));
        }

        return elements;
    });

    public string? GetDomAttribute(string attributeName) => GetAttribute(attributeName);

    public string? GetDomProperty(string propertyName)
        => Live(() => _locator.EvaluateAsync<string?>("(el, propertyName) => el[propertyName]", propertyName).GetAwaiter().GetResult());

    public string? GetCssValue(string propertyName)
        => Live(() => _locator.EvaluateAsync<string>("(el, propertyName) => getComputedStyle(el).getPropertyValue(propertyName)", propertyName).GetAwaiter().GetResult());

    public void Submit()
        => Live(() => _locator.EvaluateAsync("el => { const form = el.form || el.closest('form'); if (form) form.submit(); }").GetAwaiter().GetResult());

    public string InnerHtml => Live(() => _locator.InnerHTMLAsync().GetAwaiter().GetResult());

    public string OuterHtml => Live(() => _locator.EvaluateAsync<string>("el => el.outerHTML").GetAwaiter().GetResult());

    public bool IsChecked => Live(() => _locator.IsCheckedAsync().GetAwaiter().GetResult());

    public string InputValue => Live(() => _locator.InputValueAsync().GetAwaiter().GetResult());

    public void Evaluate(string expression)
        => Live(() => _locator.EvaluateAsync(expression).GetAwaiter().GetResult());

    public T Evaluate<T>(string expression)
        => Live(() => _locator.EvaluateAsync<T>(expression).GetAwaiter().GetResult());

    public void Fill(string value) => Live(() => _locator.FillAsync(value).GetAwaiter().GetResult());

    public void SelectOption(string value)
        => Live(() => _locator.SelectOptionAsync(new SelectOptionValue { Value = value }).GetAwaiter().GetResult());

    public void SelectOption(string[] values)
    {
        var options = values.Select(value => new SelectOptionValue { Value = value }).ToArray();
        Live(() => _locator.SelectOptionAsync(options).GetAwaiter().GetResult());
    }

    public void Check() => Live(() => _locator.CheckAsync().GetAwaiter().GetResult());

    public void Uncheck() => Live(() => _locator.UncheckAsync().GetAwaiter().GetResult());

    public void Focus() => Live(() => _locator.FocusAsync().GetAwaiter().GetResult());

    public void Blur() => Live(() => _locator.BlurAsync().GetAwaiter().GetResult());

    private TResult EvaluateRect<TResult>(Func<LocatorBoundingBoxResult, TResult> selector)
    {
        var box = Live(() => _locator.BoundingBoxAsync().GetAwaiter().GetResult());
        return box == null ? default! : selector(box);
    }
}
