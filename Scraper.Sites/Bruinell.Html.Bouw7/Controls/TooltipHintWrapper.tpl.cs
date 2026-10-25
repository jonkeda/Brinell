// <scraped control="div||tooltip-hint-wrapper|button" from="2026-10-02-recording/008-department.nodes.jsonl#515" />
using Bruinell.Core.Locators;
using Bruinell.Html.Controls;
using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Bouw7.Controls;

public partial class TooltipHintWrapper<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    private static readonly Locator Button = Locator.ByCss("button.action_button");

    public TooltipHintWrapper(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public TooltipHintWrapper(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    protected override void EnsureReadyForActionCore(IHtmlElement element)
    {
        var button = FindButton(element);
        EnsureEnabledCore(button);
    }

    protected override void ClickCore(IHtmlElement element, int? timeoutMs = null)
    {
        FindButton(element).Click();
    }

    protected override string? GetTextCore(IHtmlElement element)
    {
        var button = FindButton(element);
        var text = button.Text?.Trim();

        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        return button.GetAttribute("aria-label")?.Trim()
            ?? button.GetAttribute("name")?.Trim();
    }

    private IHtmlElement FindButton(IHtmlElement element)
        => element.TryFindElement(Button)
           ?? throw new InvalidOperationException($"Tooltip button was not found. Locator: {Locator}");
}
