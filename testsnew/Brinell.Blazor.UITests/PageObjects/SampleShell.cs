namespace Brinell.Blazor.UITests.PageObjects;

/// <summary>
/// Any page of the Blazor sample, as far as "is the app interactive" goes: a Blazor page requires
/// the <c>data-busy</c> signal the sample's layout sets on its first interactive render.
/// </summary>
public sealed class SampleShell : BlazorPageObjectBase<SampleShell>
{
    public SampleShell(IHtmlTestContext context)
        : base(context)
    {
    }
}
