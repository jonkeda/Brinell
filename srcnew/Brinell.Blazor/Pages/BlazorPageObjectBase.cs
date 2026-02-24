using Brinell.Html.Interfaces;
using Brinell.Html.Pages;

namespace Brinell.Blazor.Pages;

public abstract class BlazorPageObjectBase<TSelf> : HtmlPageObjectBase<TSelf>
    where TSelf : BlazorPageObjectBase<TSelf>
{
    protected BlazorPageObjectBase(IHtmlTestContext context) : base(context) { }
}
