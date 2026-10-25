using Brinell.Html.Context;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Html.Bunit;

/// <summary>What a <see cref="BunitTestContext"/> renders and with which services.</summary>
public sealed class BunitTestContextOptions : HtmlTestContextOptions
{
    /// <summary>
    /// The bUnit head's timeouts by default: the browser's, polling every <see cref="PollingIntervalMs"/>. An attempt here
    /// asks the rendered markup in the process, with no round trip, so a wait that ends a few milliseconds after its first
    /// miss need not sleep out the browser's 100 ms (next-plan item 12). A suite's own <c>Timeouts</c> replace them.
    /// </summary>
    public BunitTestContextOptions() => Timeouts = Brinell.Core.Configuration.TimeoutSettings.Default.With(pollingInterval: PollingIntervalMs);

    /// <summary>The bUnit head's polling interval, in milliseconds.</summary>
    public const int PollingIntervalMs = 10;

    /// <summary>The component rendered as the app: a suite's <c>Routes</c>, or one component on its own.</summary>
    public required Type RootComponent { get; init; }

    /// <summary>The app's services, registered by the suite before the root renders.</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }

    /// <summary>
    /// A provider asked for what <see cref="ConfigureServices"/> did not register: an app's own composition, such as its
    /// in-process host's services, so a suite need not repeat the app's registrations. The context takes a scope of it per
    /// render of the root, as each circuit has its own.
    /// </summary>
    public IServiceProvider? FallbackServices { get; init; }

    /// <summary>The signed-in user's name for bUnit's test authorization; null is signed out.</summary>
    public string? User { get; init; }

    /// <summary>The signed-in user's roles.</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>
    /// Loose (the default) answers every script call with a default; Strict fails on a call nobody planned,
    /// so a suite learns which scripts its pages need. <c>brinellBusy.set</c> is always planned.
    /// </summary>
    public JSRuntimeMode JsInterop { get; init; } = JSRuntimeMode.Loose;

    /// <summary>
    /// The script the app's pages call with <c>true</c> or <c>false</c> to say whether they are busy; Brinell's own
    /// <c>brinellBusy.set</c> unless the app has one of its own.
    /// </summary>
    public string BusyScript { get; init; } = BunitTestContext.BusyScript;

    /// <summary>Planned answers to the page's script calls, as bUnit's <c>Setup</c> and <c>SetupVoid</c> make them.</summary>
    public Action<BunitJSInterop>? ConfigureJs { get; init; }
}
