using Bruinell.Core.Configuration;
using Bruinell.Blazor.Context;
using Bruinell.Html.Context;
using Bruinell.Html.Interfaces;
using Bruinell.Html.Testing;

namespace Bruinell.Blazor.Testing;

public abstract class BlazorTestFixtureBase : HtmlTestFixtureBase
{
    protected override async Task<IHtmlTestContext> CreateContextAsync(HtmlTestContextOptions options)
        => await BlazorTestContext.CreateAsync(options);

    protected BlazorTestContext BlazorContext => (BlazorTestContext)Context;
}
