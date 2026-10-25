using Bruinell.Core.Scopes;
using Bruinell.Html.Interfaces;
using Bruinell.Html.Pages;
using Bruinell.Html.Scopes;

namespace Bruinell.Blazor.Pages;

public abstract class BlazorPageObjectBase<TSelf> : HtmlPageObjectBase<TSelf>
    where TSelf : BlazorPageObjectBase<TSelf>
{
    protected BlazorPageObjectBase(IHtmlTestContext context) : base(context) { }

    protected BlazorPageObjectBase(IHtmlTestContext context, string url) : base(context, url) { }

    /// <summary>
    /// A Blazor page is loaded once the app is interactive, which the app shows by setting
    /// <c>data-busy</c> on <c>&lt;body&gt;</c>. Before that, a prerendered page shows buttons whose
    /// clicks go nowhere (design 7.2, sample-app.md item 3).
    /// </summary>
    protected override BusySignal BusySignal => BusySignal.Required;
}
