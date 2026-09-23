using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class VideoControl<TScope> : MediaControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public VideoControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public VideoControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    public string? GetPoster() => RunWithElement(e => e.GetDomAttribute("poster"));
}
