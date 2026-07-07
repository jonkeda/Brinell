using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Controls;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class NavMenuControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    private readonly string _itemSelector;

    public NavMenuControl(IHtmlScope<TScope> scope, Locator locator, string itemSelector = "a, .nav-link, [role='menuitem']")
        : base(scope, locator) => _itemSelector = itemSelector;
    public NavMenuControl(IHtmlScope<TScope> scope, string selectorOrId, string itemSelector = "a, .nav-link, [role='menuitem']")
        : base(scope, selectorOrId) => _itemSelector = itemSelector;

    public int GetItemCount() => RunWithElement(e =>
        e.FindElements(Locator.ByCss(_itemSelector)).Count);

#pragma warning disable CS8603 // Possible null reference return.
    public IReadOnlyList<string> GetItems() => RunWithElement(e =>
        e.FindElements(Locator.ByCss(_itemSelector))
            .Select(item => item.Text?.Trim() ?? "")
            .ToList());
#pragma warning restore CS8603 // Possible null reference return.

    public string? GetActiveItem() => RunWithElement(e =>
    {
        var items = e.FindElements(Locator.ByCss(
            $"{_itemSelector}.active, {_itemSelector}[aria-current='page'], {_itemSelector}[aria-current='true']"));
        return items.Count > 0 ? items[0].Text?.Trim() : null;
    });

    public bool IsActive(string itemText) =>
        string.Equals(GetActiveItem(), itemText, StringComparison.OrdinalIgnoreCase);

    public TScope NavigateTo(string itemText)
    {
        return RunWithElement(e =>
        {
            var items = e.FindElements(Locator.ByCss(_itemSelector));
            var target = items.FirstOrDefault(i =>
                string.Equals(i.Text?.Trim(), itemText, StringComparison.OrdinalIgnoreCase));
            if (target == null)
                throw new InvalidOperationException($"Nav menu item '{itemText}' not found");
            target.Click();
        });
    }

    public TScope NavigateToIndex(int index)
    {
        return RunWithElement(e =>
        {
            var items = e.FindElements(Locator.ByCss(_itemSelector));
            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Index {index} out of range (0-{items.Count - 1})");
            items[index].Click();
        });
    }

    public string? GetItemHref(string itemText) => RunWithElement(e =>
    {
        var items = e.FindElements(Locator.ByCss(_itemSelector));
        var target = items.FirstOrDefault(i =>
            string.Equals(i.Text?.Trim(), itemText, StringComparison.OrdinalIgnoreCase));
        return target?.GetAttribute("href");
    });

    public bool HasItem(string itemText)
    {
        var items = GetItems();
        return items.Any(i => string.Equals(i, itemText, StringComparison.OrdinalIgnoreCase));
    }

    // Assertions
    public TScope AssertActiveItem(string? expected, string? message = null)
    {
        var actual = GetActiveItem();
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new AssertionException(message ?? $"Expected active item '{expected}' but was '{actual}'");
        return ContainingScope;
    }

    public TScope AssertHasItem(string itemText, string? message = null)
    {
        if (!HasItem(itemText))
            throw new AssertionException(message ?? $"Expected nav menu to contain item '{itemText}'");
        return ContainingScope;
    }

    public TScope AssertItemCount(int expected, string? message = null)
    {
        var actual = GetItemCount();
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected {expected} nav items but found {actual}");
        return ContainingScope;
    }
}
