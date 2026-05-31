using System.Drawing;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Microsoft.Playwright;

namespace Brinell.Html.Playwright;

public sealed class PlaywrightHtmlElement : IHtmlElement
{
    private readonly ILocator _locator;

    public PlaywrightHtmlElement(ILocator locator)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    public bool Visible => _locator.IsVisibleAsync().GetAwaiter().GetResult();

    public bool Enabled => _locator.IsEnabledAsync().GetAwaiter().GetResult();

    public bool Selected => IsChecked;

    public string? Text => _locator.InnerTextAsync().GetAwaiter().GetResult();

    public string? TagName => _locator.EvaluateAsync<string>("el => el.tagName.toLowerCase()").GetAwaiter().GetResult();

    public Point Location => EvaluateRect(box => new Point((int)box.X, (int)box.Y));

    public Size Size => EvaluateRect(box => new Size((int)box.Width, (int)box.Height));

    public Rectangle Rect => EvaluateRect(box => new Rectangle((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height));

    public void Click() => _locator.ClickAsync().GetAwaiter().GetResult();

    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys)
    {
        switch (method)
        {
            case TextInputMethod.SetValue:
                _locator.FillAsync(text).GetAwaiter().GetResult();
                break;
            case TextInputMethod.Paste:
                _locator.FillAsync(text).GetAwaiter().GetResult();
                break;
            default:
                _locator.PressSequentiallyAsync(text).GetAwaiter().GetResult();
                break;
        }
    }

    public void Clear() => _locator.ClearAsync().GetAwaiter().GetResult();

    public void DoubleClick() => _locator.DblClickAsync().GetAwaiter().GetResult();

    public void RightClick() => _locator.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right }).GetAwaiter().GetResult();

    public void Hover() => _locator.HoverAsync().GetAwaiter().GetResult();

    public void LongPress(int durationMs = 1000) => _locator.ClickAsync(new LocatorClickOptions { Delay = durationMs }).GetAwaiter().GetResult();

    public void ScrollIntoView(int timeoutMs = 5000)
        => _locator.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions { Timeout = timeoutMs }).GetAwaiter().GetResult();

    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        var page = _locator.Page;
        page.Mouse.MoveAsync(startX, startY).GetAwaiter().GetResult();
        page.Mouse.DownAsync().GetAwaiter().GetResult();
        page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = Math.Max(1, durationMs / 16) }).GetAwaiter().GetResult();
        page.Mouse.UpAsync().GetAwaiter().GetResult();
    }

    public string? GetAttribute(string name) => _locator.GetAttributeAsync(name).GetAwaiter().GetResult();

    public IHtmlElement FindElement(Locator locator, int timeoutMs = 5000)
    {
        var childLocator = LocatorExtensions.ToPlaywrightLocator(_locator, locator);
        if (timeoutMs > 0)
        {
            try
            {
                childLocator.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = timeoutMs
                }).GetAwaiter().GetResult();
            }
            catch (TimeoutException)
            {
                throw new ElementNotFoundException(locator, timeoutMs);
            }
            catch (PlaywrightException)
            {
                throw new ElementNotFoundException(locator, timeoutMs);
            }
        }

        var count = childLocator.CountAsync().GetAwaiter().GetResult();
        if (count <= 0)
        {
            throw timeoutMs > 0
                ? new ElementNotFoundException(locator, timeoutMs)
                : new ElementNotFoundException(locator);
        }

        return new PlaywrightHtmlElement(childLocator.First);
    }

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        var childLocator = LocatorExtensions.ToPlaywrightLocator(_locator, locator);

        if (timeoutMs > 0)
        {
            try
            {
                childLocator.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = timeoutMs
                }).GetAwaiter().GetResult();
            }
            catch (TimeoutException)
            {
                return [];
            }
            catch (PlaywrightException)
            {
                return [];
            }
        }

        var count = childLocator.CountAsync().GetAwaiter().GetResult();
        var elements = new List<IHtmlElement>(count);
        for (var i = 0; i < count; i++)
        {
            elements.Add(new PlaywrightHtmlElement(childLocator.Nth(i)));
        }

        return elements;
    }

    public bool TryFindElement(Locator locator, out IHtmlElement? element, int timeoutMs = 0)
    {
        try
        {
            element = FindElement(locator, timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            element = null;
            return false;
        }
    }

    public string? GetDomAttribute(string attributeName) => GetAttribute(attributeName);

    public string? GetDomProperty(string propertyName)
        => _locator.EvaluateAsync<string?>("(el, propertyName) => el[propertyName]", propertyName).GetAwaiter().GetResult();

    public string? GetCssValue(string propertyName)
        => _locator.EvaluateAsync<string>("(el, propertyName) => getComputedStyle(el).getPropertyValue(propertyName)", propertyName).GetAwaiter().GetResult();

    public void Submit()
        => _locator.EvaluateAsync("el => { const form = el.form || el.closest('form'); if (form) form.submit(); }").GetAwaiter().GetResult();

    public string InnerHtml => _locator.InnerHTMLAsync().GetAwaiter().GetResult();

    public string OuterHtml => _locator.EvaluateAsync<string>("el => el.outerHTML").GetAwaiter().GetResult();

    public bool IsChecked => _locator.IsCheckedAsync().GetAwaiter().GetResult();

    public string InputValue => _locator.InputValueAsync().GetAwaiter().GetResult();

    public void Evaluate(string expression)
        => _locator.EvaluateAsync(expression).GetAwaiter().GetResult();

    public T Evaluate<T>(string expression)
        => _locator.EvaluateAsync<T>(expression).GetAwaiter().GetResult();

    public void Fill(string value) => _locator.FillAsync(value).GetAwaiter().GetResult();

    public void SelectOption(string value) => _locator.SelectOptionAsync(new SelectOptionValue { Value = value }).GetAwaiter().GetResult();

    public void SelectOption(string[] values)
    {
        var options = values.Select(value => new SelectOptionValue { Value = value }).ToArray();
        _locator.SelectOptionAsync(options).GetAwaiter().GetResult();
    }

    public void Check() => _locator.CheckAsync().GetAwaiter().GetResult();

    public void Uncheck() => _locator.UncheckAsync().GetAwaiter().GetResult();

    public void Focus() => _locator.FocusAsync().GetAwaiter().GetResult();

    public void Blur() => _locator.BlurAsync().GetAwaiter().GetResult();

    private TResult EvaluateRect<TResult>(Func<LocatorBoundingBoxResult, TResult> selector)
    {
        var box = _locator.BoundingBoxAsync().GetAwaiter().GetResult();
        if (box == null)
        {
            return default!;
        }

        return selector(box);
    }
}
