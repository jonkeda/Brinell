using System.Drawing;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using IHtmlElement = Brinell.Html.Interfaces.IHtmlElement;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Brinell.Html.Bunit;

/// <summary>
/// One element of the rendered DOM, found again on every read as Playwright's locators are: a render
/// replaces bUnit's nodes, so a held node would go stale. Gone is <see cref="StaleElementException"/>,
/// which the call around it treats as a signal to look again.
/// </summary>
public sealed class BunitHtmlElement : IHtmlElement
{
    private readonly BunitTestContext _context;
    private readonly Func<IElement?> _resolve;
    private readonly Locator? _locator;

    internal BunitHtmlElement(BunitTestContext context, Func<IElement?> resolve, Locator? locator)
    {
        _context = context;
        _resolve = resolve;
        _locator = locator;
    }

    private IElement Node => _resolve() ?? throw new StaleElementException(_locator);

    #region Identity

    // The path from the root by element index: the same element after a render keeps its key.
    public string InstanceKey
    {
        get
        {
            var parts = new List<int>();
            for (var node = Node; node.ParentElement is { } parent; node = parent)
            {
                parts.Add(parent.Children.Index(node));
            }

            parts.Reverse();
            return string.Join('/', parts);
        }
    }

    #endregion

    #region State

    public bool Visible
    {
        get
        {
            for (var node = Node; node is not null; node = node.ParentElement)
            {
                if (node.HasAttribute("hidden") || HidesItself(node.GetAttribute("style")))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool Enabled => !Node.HasAttribute("disabled");

    public bool Selected => Node is IHtmlOptionElement option ? option.IsSelected : Node.HasAttribute("selected");

    public string? Text => BunitLocators.Normalize(Node.TextContent);

    public string? TagName => Node.TagName.ToLowerInvariant();

    public Point Location => throw new BrowserOnlyException(nameof(Location));

    public Size Size => throw new BrowserOnlyException(nameof(Size));

    public Rectangle Rect => throw new BrowserOnlyException(nameof(Rect));

    public string? GetAttribute(string name) => Node.GetAttribute(name);

    public string? GetDomAttribute(string attributeName) => Node.GetAttribute(attributeName);

    public string? GetDomProperty(string propertyName) => propertyName switch
    {
        "value" => InputValue,
        "checked" => IsChecked ? "true" : "false",
        "disabled" => Enabled ? "false" : "true",
        "textContent" or "innerText" => Text,
        _ => Node.GetAttribute(propertyName),
    };

    public string? GetCssValue(string propertyName) => throw new BrowserOnlyException(nameof(GetCssValue));

    public string InnerHtml => Node.InnerHtml;

    public string OuterHtml => Node.OuterHtml;

    public bool IsChecked => Node is IHtmlInputElement input ? input.IsChecked || Node.HasAttribute("checked") : Node.HasAttribute("checked");

    public string InputValue => Node switch
    {
        IHtmlInputElement input => Node.GetAttribute("value") ?? input.Value,
        IHtmlTextAreaElement area => Node.GetAttribute("value") ?? area.Value,
        IHtmlSelectElement select => select.Value ?? string.Empty,
        _ => Node.GetAttribute("value") ?? string.Empty,
    };

    #endregion

    #region Actions

    // A browser's click: the handler if there is one; else a submit button submits its form and a link
    // follows its href, which Blazor's script does in a browser and nothing does here.
    public void Click()
    {
        var node = Node;
        if (Handles(node, "onclick"))
        {
            node.Click();
        }
        else if (node is IHtmlButtonElement { Type: "submit" or "" } or IHtmlInputElement { Type: "submit" } && node.Closest("form") is { } form && Handles(form, "onsubmit"))
        {
            form.Submit();
        }
        else if (node.Closest("a") is { } link && link.GetAttribute("href") is { Length: > 0 } href && !href.StartsWith('#'))
        {
            _context.NavigateTo(href);
        }
    }

    public void ClickAt(double x, double y) => throw new BrowserOnlyException(nameof(ClickAt));

    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys) => Fill(InputValue + text);

    public void PressKey(string key)
    {
        var node = Node;
        var args = new KeyboardEventArgs { Key = key, Code = key, Type = "keydown" };
        if (Handles(node, "onkeydown"))
        {
            node.KeyDown(args);
        }

        if (Handles(node, "onkeyup"))
        {
            node.KeyUp(new KeyboardEventArgs { Key = key, Code = key, Type = "keyup" });
        }

        if (key == "Enter" && node.Closest("form") is { } form && Handles(form, "onsubmit"))
        {
            form.Submit();
        }
    }

    public void Clear() => Fill(string.Empty);

    public void DoubleClick()
    {
        var node = Node;
        if (Handles(node, "ondblclick"))
        {
            node.DoubleClick();
        }
    }

    public void RightClick()
    {
        var node = Node;
        if (Handles(node, "oncontextmenu"))
        {
            node.ContextMenu();
        }
    }

    public void Hover() => throw new BrowserOnlyException(nameof(Hover));

    public void LongPress(int durationMs = 1000) => throw new BrowserOnlyException(nameof(LongPress));

    // Nothing scrolls without layout; an element in the DOM is as reachable as it will be.
    public void ScrollIntoView(int timeoutMs)
    {
    }

    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500) => throw new BrowserOnlyException(nameof(Swipe));

    public void Submit()
    {
        var form = Node.Closest("form") ?? Node;
        form.Submit();
    }

    // What @bind sees: the input event where it listens to one, then the change event.
    public void Fill(string value)
    {
        var node = Node;
        var input = Handles(node, "oninput");
        var change = Handles(node, "onchange");
        if (input)
        {
            node.Input(new ChangeEventArgs { Value = value });
        }

        if (change)
        {
            Node.Change(new ChangeEventArgs { Value = value });
        }

        if (!input && !change)
        {
            node.SetAttribute("value", value);
        }
    }

    public void SelectOption(string value) => Node.Change(new ChangeEventArgs { Value = value });

    public void SelectOption(string[] values) => Node.Change(new ChangeEventArgs { Value = values });

    public void Check() => SetChecked(true);

    public void Uncheck() => SetChecked(false);

    public void Focus()
    {
        var node = Node;
        if (Handles(node, "onfocus"))
        {
            node.Focus();
        }
    }

    public void Blur()
    {
        var node = Node;
        if (Handles(node, "onblur"))
        {
            node.Blur();
        }
    }

    public void Evaluate(string expression) => throw new BrowserOnlyException(nameof(Evaluate));

    public T Evaluate<T>(string expression) => throw new BrowserOnlyException(nameof(Evaluate));

    #endregion

    #region Child lookup

    public IHtmlElement? TryFindElement(Locator locator) => FindElements(locator).FirstOrDefault();

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var node = Node;
        var count = BunitLocators.FindAll(css => node.QuerySelectorAll(css), locator).Count;
        var elements = new List<IHtmlElement>(count);
        for (var i = 0; i < count; i++)
        {
            var index = i;
            elements.Add(new BunitHtmlElement(_context, () => _resolve() is { } parent
                ? BunitLocators.FindAll(css => parent.QuerySelectorAll(css), locator).ElementAtOrDefault(index)
                : null, locator));
        }

        return elements;
    }

    #endregion

    private void SetChecked(bool value)
    {
        var node = Node;
        if (IsChecked == value)
        {
            return;
        }

        if (Handles(node, "onchange"))
        {
            node.Change(new ChangeEventArgs { Value = value });
        }
        else if (Handles(node, "onclick"))
        {
            node.Click();
        }
    }

    // bUnit writes a handled event as a blazor:on… attribute on the element.
    private static bool Handles(IElement element, string eventName) => element.HasAttribute($"blazor:{eventName}");

    private static bool HidesItself(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return false;
        }

        var compact = style.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return compact.Contains("display:none", StringComparison.Ordinal) || compact.Contains("visibility:hidden", StringComparison.Ordinal);
    }
}
