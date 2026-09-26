using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

/// <summary>
/// A &lt;video&gt; element.
/// </summary>
public partial class VideoControl<TScope> : MediaControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public VideoControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public VideoControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    /// <summary>The video's poster image.</summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual string? GetPosterCore(IHtmlElement element) => element.GetDomAttribute("poster");
}
