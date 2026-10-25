using Brinell.Html.Context;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Html.Bunit;

/// <summary>What a <see cref="BunitTestContext"/> renders and with which services.</summary>
public sealed class BunitTestContextOptions : HtmlTestContextOptions
{
    /// <summary>The component rendered as the app: a suite's <c>Routes</c>, or one component on its own.</summary>
    public required Type RootComponent { get; init; }

    /// <summary>The app's services, registered by the suite before the root renders.</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }

    /// <summary>The signed-in user's name for bUnit's test authorization; null is signed out.</summary>
    public string? User { get; init; }

    /// <summary>The signed-in user's roles.</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>
    /// Loose (the default) answers every script call with a default; Strict fails on a call nobody planned,
    /// so a suite learns which scripts its pages need. <c>brinellBusy.set</c> is always planned.
    /// </summary>
    public JSRuntimeMode JsInterop { get; init; } = JSRuntimeMode.Loose;

    /// <summary>Planned answers to the page's script calls, as bUnit's <c>Setup</c> and <c>SetupVoid</c> make them.</summary>
    public Action<BunitJSInterop>? ConfigureJs { get; init; }
}
