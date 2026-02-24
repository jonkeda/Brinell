using Brinell.Blazor.Context;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;
using Brinell.Html.Testing;

namespace Brinell.Blazor.Testing;

public abstract class BlazorTestFixtureBase : HtmlTestFixtureBase
{
    protected override async Task<IHtmlTestContext> CreateContextAsync(HtmlTestContextOptions options)
        => await BlazorTestContext.CreateAsync(options);

    protected BlazorTestContext BlazorContext => (BlazorTestContext)Context;
}
