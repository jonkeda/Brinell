using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Html.Controls;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

/// <summary>
/// A navigation menu: its items are links, one of which is active.
/// </summary>
public partial class NavMenuControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    private readonly Locator _items;

    private readonly string _itemSelector;

    public NavMenuControl(IHtmlScope<TScope> scope, Locator locator, string itemSelector = "a, .nav-link, [role='menuitem']")
        : base(scope, locator)
    {
        _itemSelector = itemSelector;
        _items = Locator.ByCss(itemSelector);
    }

    public NavMenuControl(IHtmlScope<TScope> scope, string selectorOrId, string itemSelector = "a, .nav-link, [role='menuitem']")
        : base(scope, selectorOrId)
    {
        _itemSelector = itemSelector;
        _items = Locator.ByCss(itemSelector);
    }

    /// <summary>How many items the menu has.</summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual int? GetItemCountCore(IHtmlElement element) => element.FindElements(_items).Count;

    /// <summary>The items' texts, in order.</summary>
    /// <param name="element">The pre-found element.</param>
    [GenerateComparisons(Comparison.SequenceEquals | Comparison.HasItem | Comparison.Count)]
    protected virtual IReadOnlyList<string>? GetItemsCore(IHtmlElement element)
        => element.FindElements(_items).Select(item => item.Text?.Trim() ?? string.Empty).ToList();

    /// <summary>The active item's text; null when no item is active.</summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual string? GetActiveItemCore(IHtmlElement element)
    {
        var active = element.FindElements(Locator.ByCss(
            $"{_itemSelector}.active, {_itemSelector}[aria-current='page'], {_itemSelector}[aria-current='true']"));
        return active.Count > 0 ? active[0].Text?.Trim() : null;
    }

    /// <summary>The <c>href</c> of the item whose text is <paramref name="itemText"/>; null when there is none.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="itemText">The item's text.</param>
    protected virtual string? GetItemHrefCore(IHtmlElement element, string itemText)
        => ItemWithText(element, itemText)?.GetAttribute("href");

    /// <summary>Clicks the item whose text is <paramref name="itemText"/>.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="itemText">The item's text.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void NavigateToCore(IHtmlElement element, string itemText, int? timeoutMs = null)
    {
        var target = ItemWithText(element, itemText)
            ?? throw new InvalidOperationException($"Nav menu item '{itemText}' not found. Locator: {Locator}");
        target.Click();
    }

    /// <summary>Clicks the item at <paramref name="index"/> (0-based).</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="index">The item's position.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void NavigateToIndexCore(IHtmlElement element, int index, int? timeoutMs = null)
    {
        var items = element.FindElements(_items);
        if (index < 0 || index >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range (0-{items.Count - 1})");
        }

        items[index].Click();
    }

    private IHtmlElement? ItemWithText(IHtmlElement element, string itemText)
        => element.FindElements(_items)
            .FirstOrDefault(item => string.Equals(item.Text?.Trim(), itemText, StringComparison.OrdinalIgnoreCase));
}
