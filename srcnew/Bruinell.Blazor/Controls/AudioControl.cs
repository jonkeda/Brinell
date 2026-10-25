using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class AudioControl<TScope> : MediaControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public AudioControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public AudioControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
