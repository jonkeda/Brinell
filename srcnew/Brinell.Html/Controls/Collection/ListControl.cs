using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Collection;

public class ListControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ListControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public ListControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public int ItemCount
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
