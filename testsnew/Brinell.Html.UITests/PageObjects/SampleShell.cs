using Brinell.Core.Scopes;
using Brinell.Html.Scopes;

namespace Brinell.Html.UITests.PageObjects;

/// <summary>
/// Any page of the Blazor sample, as far as "is the app interactive" goes: the sample's layout
/// sets <c>data-busy</c> on its first interactive render, and this page requires it.
/// </summary>
public sealed class SampleShell : HtmlPageObjectBase<SampleShell>
{
    public SampleShell(IHtmlTestContext context)
        : base(context)
    {
    }

    protected override BusySignal BusySignal => BusySignal.Required;
}
