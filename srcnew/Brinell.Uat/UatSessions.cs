using System.Runtime.CompilerServices;
using Brinell.Core.Composition;
using Brinell.Core.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Uat;

/// <summary>A fixture with a pinned clock: relative dates in steps (<c>today</c>, <c>+2d</c>) resolve against it.</summary>
public interface IUatClock
{
    /// <summary>The time the scenario's test data calls now.</summary>
    DateTimeOffset Now { get; }
}

/// <summary>
/// A fixture that runs several apps in one scenario: two devices on one backend, two users. Each
/// app is a session with its own test context and page objects; <c>{app} uses the app</c> makes
/// one current. See <c>.docs/contracts/uat-apps-host.md</c>.
/// </summary>
/// <remarks>
/// Apps of one head share the suite's page and phrase types: a session changes which app a page
/// object drives, not which page objects there are. An app of another head brings its own
/// (<see cref="CompositionFor"/>). The app names are the <c>Apps</c> table's, which the engine
/// checks before it asks the fixture.
/// </remarks>
public interface IUatSessionHost
{
    /// <summary>
    /// Makes <paramref name="name"/> the current app, starting it when it is not running yet.
    /// </summary>
    /// <param name="name">The app's name, as in the <c>Apps</c> table.</param>
    /// <returns>The services the app's page objects and phrases resolve through, for the rest of the scenario.</returns>
    IServiceProvider UseSession(string name);

    /// <summary>
    /// The composition whose pages and phrases <paramref name="name"/>'s head uses, when it is
    /// not the fixture's own: an app of another head in a mixed suite.
    /// </summary>
    /// <param name="name">The app's name, as in the <c>Apps</c> table.</param>
    /// <returns>The composition, or null for the fixture's own.</returns>
    TestComposition? CompositionFor(string name) => null;
}

/// <summary>
/// A fixture whose apps are a <see cref="TestAppSet"/>, shared with its UI tests: sessions, subject
/// resolution and a composition per head come from the set (<c>.docs/contracts/uat-apps-host.md</c>).
/// </summary>
/// <remarks>
/// Each app gets one composition and one scope, built the first time a scenario uses it, with the
/// app's own services (<see cref="AddAppServices"/>) on top of its head's modules.
/// </remarks>
public interface IUatAppsHost : IUatSessionHost
{
    /// <summary>The apps; their names are the <c>Apps</c> table's.</summary>
    TestAppSet Apps { get; }

    /// <summary>The modules whose pages and phrases <paramref name="app"/>'s head uses; null for the fixture's own.</summary>
    /// <param name="app">The app.</param>
    TestCompositionDiscoveryOptions? ModulesFor(TestAppDefinition app) => null;

    /// <summary>Adds what <paramref name="app"/>'s page objects need, such as its test context.</summary>
    /// <param name="app">The started app.</param>
    /// <param name="services">Its composition's services.</param>
    void AddAppServices(ITestApp app, IServiceCollection services);

    /// <summary>Called each time <paramref name="app"/> becomes current, for a fixture that tracks it.</summary>
    /// <param name="app">The app.</param>
    void OnAppUsed(ITestApp app)
    {
    }

    IServiceProvider IUatSessionHost.UseSession(string name) => UatAppSessions.Use(this, name);

    TestComposition? IUatSessionHost.CompositionFor(string name) => UatAppSessions.CompositionFor(this, name);
}

internal static class UatAppSessions
{
    // Keyed by the app object: a stopped and restarted app is a new object with a new scope.
    private static readonly ConditionalWeakTable<ITestApp, IServiceScope> Scopes = new();
    private static readonly ConditionalWeakTable<IUatAppsHost, Dictionary<string, TestComposition>> Heads = new();

    public static IServiceProvider Use(IUatAppsHost host, string name)
    {
        var app = host.Apps.Get(name);
        var scope = Scopes.GetValue(app, started => TestComposition
            .ForFixture(host, Modules(host, started.Definition), services => host.AddAppServices(started, services))
            .CreateScope());
        host.OnAppUsed(app);
        return scope.ServiceProvider;
    }

    // For discovery only: the head's page and phrase types, with no app started.
    public static TestComposition? CompositionFor(IUatAppsHost host, string name)
    {
        var definition = host.Apps.Resolve(name);
        if (host.ModulesFor(definition) is not { } modules)
        {
            return null;
        }

        var heads = Heads.GetOrCreateValue(host);
        lock (heads)
        {
            var head = definition.Head ?? string.Empty;
            if (!heads.TryGetValue(head, out var composition))
            {
                composition = TestComposition.ForFixture(host, modules);
                heads[head] = composition;
            }

            return composition;
        }
    }

    private static TestCompositionDiscoveryOptions Modules(IUatAppsHost host, TestAppDefinition app)
        => host.ModulesFor(app) ?? TestCompositionDiscoveryOptions.FromFixture(host.GetType());
}

internal static class UatSessionSteps
{
    public const string CommandId = "Builtin.Session.Use";

    public const string Phrase = "{app} uses the app";

    /// <summary>Registers <c>{app} uses the app</c>, as a Given and a When, for a fixture that hosts sessions.</summary>
    public static void Register(UatCommandCatalog catalog, IUatSessionHost? host, UatConfig config)
    {
        UatCommandHandler handler = (context, invocation, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var requested = invocation.Arguments["app"];
            var app = config.Apps.FirstOrDefault(candidate => string.Equals(candidate.Name, requested, StringComparison.OrdinalIgnoreCase));
            if (config.Apps.Count > 0 && app is null)
            {
                return Task.FromResult(UatStepResult.Failed(
                    invocation,
                    $"No app '{requested}' in the Apps table of uat.config.md. It has: {string.Join(", ", config.Apps.Select(candidate => $"'{candidate.Name}'"))}."));
            }

            context.SessionHost ??= host;
            context.SwitchApp(app?.Name ?? requested);
            return Task.FromResult(UatStepResult.Passed(invocation));
        };

        catalog.Register(UatEffectiveStepKeyword.Given, Phrase, CommandId, allowsTable: false, handler: handler);
        catalog.Register(UatEffectiveStepKeyword.When, Phrase, CommandId, allowsTable: false, handler: handler);
    }
}
