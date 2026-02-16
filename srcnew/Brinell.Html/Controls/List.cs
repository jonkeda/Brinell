using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

public class List<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public List(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public List(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public int Count
    {
        get
        {
            var root = FindElement();
            return root.FindElements(Locator.ByCss("li")).Count;
        }
    }

    public string? GetItemText(int index)
    {
        var root = FindElement();
        var items = root.FindElements(Locator.ByCss("li"));
        return index >= 0 && index < items.Count ? items[index].Text : null;
    }

    public IReadOnlyList<string?> GetItemTexts()
    {
        var root = FindElement();
        var items = root.FindElements(Locator.ByCss("li"));
        return items.Select(item => item.Text).ToList();
    }
}
