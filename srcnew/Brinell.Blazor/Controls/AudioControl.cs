using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class AudioControl<TScope> : MediaControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public AudioControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public AudioControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
