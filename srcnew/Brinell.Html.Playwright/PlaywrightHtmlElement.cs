using System.Drawing;
using Brinell.Core;
using Brinell.Core.Calls;
using Brinell.Core.Exceptions;
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
/// <remarks>
/// Every Playwright call that can wait is given what is left of the running call's budget as its
/// timeout (the call-model contract), so no Playwright wait outlasts the caller's
/// <c>timeoutMs</c>. Outside a call (a plain <c>Is*</c>) the cap is <see cref="OutsideACallMs"/>,
/// not Playwright's 30 s default. A read waits for its element no longer than
/// <see cref="ReadAttachMs"/>: see <see cref="Read{T}"/>.
/// </remarks>
public sealed class PlaywrightHtmlElement : IHtmlElement
{
    /// <summary>The longest a Playwright call made outside a Brinell call may wait.</summary>
    public const int OutsideACallMs = 5000;

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

    /// <summary>The longest a read waits for its element to be attached.</summary>
    public const int ReadAttachMs = 250;

    private static float Budget => CallBudget.RemainingOr(OutsideACallMs);

    /// <summary>
    /// A read of the element's state, which is about now (R2): an attached element answers at once,
    /// so the read waits for attachment only <see cref="ReadAttachMs"/>, and a node that is gone by
    /// then is stale (R5), for the poll to find again. Without the cap, a locator whose node was
    /// re-rendered away waits for the rest of the call, and one read ends the poll.
    /// </summary>
    private T Read<T>(Func<float, Task<T>> read)
    {
        var budget = Budget;
        if (budget <= ReadAttachMs)
        {
            return Live(() => read(budget).GetAwaiter().GetResult());
        }

        return Live(() =>
        {
            try
            {
                return read(ReadAttachMs).GetAwaiter().GetResult();
            }
            catch (TimeoutException error)
            {
                throw new StaleElementException(platformError: error);
            }
        });
    }

    /// <inheritdoc />
    public string InstanceKey => Evaluate<string>(InstanceKeyScript);

    public bool Visible => Live(() => _locator.IsVisibleAsync().GetAwaiter().GetResult());

    public bool Enabled => Read(timeout => _locator.IsEnabledAsync(new() { Timeout = timeout }));

    public bool Selected => IsChecked;

    public string? Text => Read<string?>(async timeout => await _locator.InnerTextAsync(new() { Timeout = timeout }));

    public string? TagName => Evaluate<string>("el => el.tagName.toLowerCase()");

    public Point Location => EvaluateRect(box => new Point((int)box.X, (int)box.Y));

    public Size Size => EvaluateRect(box => new Size((int)box.Width, (int)box.Height));

    public Rectangle Rect => EvaluateRect(box => new Rectangle((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height));

    public void Click() => Live(() => _locator.ClickAsync(new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys) => Live(() =>
    {
        switch (method)
        {
            case TextInputMethod.SetValue:
            case TextInputMethod.Paste:
                _locator.FillAsync(text, new() { Timeout = Budget }).GetAwaiter().GetResult();
                break;
            default:
                _locator.PressSequentiallyAsync(text, new() { Timeout = Budget }).GetAwaiter().GetResult();
                break;
        }
    });

    public void Clear() => Live(() => _locator.ClearAsync(new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void DoubleClick() => Live(() => _locator.DblClickAsync(new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void RightClick()
        => Live(() => _locator.ClickAsync(new() { Button = MouseButton.Right, Timeout = Budget }).GetAwaiter().GetResult());

    public void Hover() => Live(() => _locator.HoverAsync(new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void LongPress(int durationMs = 1000)
        => Live(() => _locator.ClickAsync(new() { Delay = durationMs, Timeout = Budget + durationMs }).GetAwaiter().GetResult());

    public void ScrollIntoView(int timeoutMs)
        => Live(() => _locator.ScrollIntoViewIfNeededAsync(new() { Timeout = timeoutMs }).GetAwaiter().GetResult());

    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500) => Live(() =>
    {
        var page = _locator.Page;
        page.Mouse.MoveAsync(startX, startY).GetAwaiter().GetResult();
        page.Mouse.DownAsync().GetAwaiter().GetResult();
        page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = Math.Max(1, durationMs / 16) }).GetAwaiter().GetResult();
        page.Mouse.UpAsync().GetAwaiter().GetResult();
    });

    public string? GetAttribute(string name)
        => Read(timeout => _locator.GetAttributeAsync(name, new() { Timeout = timeout }));

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

    public IHtmlElement? TryGetFrameDocument() => Live<IHtmlElement?>(() =>
    {
        var body = _locator.ContentFrame.Locator("body");
        return body.CountAsync().GetAwaiter().GetResult() > 0 ? new PlaywrightHtmlElement(body.First) : null;
    });

    public string? GetDomAttribute(string attributeName) => GetAttribute(attributeName);

    public string? GetDomProperty(string propertyName)
        => Read(timeout => _locator.EvaluateAsync<string?>("(el, propertyName) => el[propertyName]", propertyName, new() { Timeout = timeout }));

    public string? GetCssValue(string propertyName)
        => Read(timeout => _locator.EvaluateAsync<string>("(el, propertyName) => getComputedStyle(el).getPropertyValue(propertyName)", propertyName, new() { Timeout = timeout }));

    public void Submit()
        => Evaluate("el => { const form = el.form || el.closest('form'); if (form) form.submit(); }");

    public string InnerHtml => Read(timeout => _locator.InnerHTMLAsync(new() { Timeout = timeout }));

    public string OuterHtml => Evaluate<string>("el => el.outerHTML");

    public bool IsChecked => Read(timeout => _locator.IsCheckedAsync(new() { Timeout = timeout }));

    public string InputValue => Read(timeout => _locator.InputValueAsync(new() { Timeout = timeout }));

    public void Evaluate(string expression)
        => Live(() => _locator.EvaluateAsync(expression, null, new() { Timeout = Budget }).GetAwaiter().GetResult());

    public T Evaluate<T>(string expression)
        => Read(timeout => _locator.EvaluateAsync<T>(expression, null, new() { Timeout = timeout }));

    public void Fill(string value) => Live(() => _locator.FillAsync(value, new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void SelectOption(string value)
        => Live(() => _locator.SelectOptionAsync(new SelectOptionValue { Value = value }, new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void SelectOption(string[] values)
    {
        var options = values.Select(value => new SelectOptionValue { Value = value }).ToArray();
        Live(() => _locator.SelectOptionAsync(options, new() { Timeout = Budget }).GetAwaiter().GetResult());
    }

    public void Check() => Live(() => _locator.CheckAsync(new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void Uncheck() => Live(() => _locator.UncheckAsync(new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void Focus() => Live(() => _locator.FocusAsync(new() { Timeout = Budget }).GetAwaiter().GetResult());

    public void Blur() => Live(() => _locator.BlurAsync(new() { Timeout = Budget }).GetAwaiter().GetResult());

    private TResult EvaluateRect<TResult>(Func<LocatorBoundingBoxResult, TResult> selector)
    {
        var box = Read(timeout => _locator.BoundingBoxAsync(new() { Timeout = timeout }));
        return box == null ? default! : selector(box);
    }
}
