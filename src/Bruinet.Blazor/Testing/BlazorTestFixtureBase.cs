using Bruinet.Core.Configuration;
using Bruinet.Blazor.Context;
using Bruinet.Html.Context;
using Bruinet.Html.Interfaces;
using Bruinet.Html.Testing;

namespace Bruinet.Blazor.Testing;

public abstract class BlazorTestFixtureBase : HtmlTestFixtureBase
{
    protected override async Task<IHtmlTestContext> CreateContextAsync(HtmlTestContextOptions options)
        => await BlazorTestContext.CreateAsync(options);

    protected BlazorTestContext BlazorContext => (BlazorTestContext)Context;
}
