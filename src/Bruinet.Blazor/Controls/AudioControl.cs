using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Blazor.Controls;

public class AudioControl<TScope> : MediaControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public AudioControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public AudioControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
