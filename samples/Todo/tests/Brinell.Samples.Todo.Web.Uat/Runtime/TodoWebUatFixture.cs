using Brinell.Core.Composition;
using Brinell.Core.Interfaces;
using Brinell.Core.Services;
using Brinell.Html.Interfaces;
using Brinell.Samples.Todo.TestSupport.Uat;
using Brinell.Samples.Todo.Web.Uat.Phrases;
using Brinell.Samples.Todo.Web.UITests;
using Brinell.Samples.Todo.Web.UITests.Pages;
using Brinell.Uat;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Samples.Todo.Web.Uat.Runtime;

/// <summary>
/// The Todo web head, hermetic, presented to the UAT engine as its root: the web counterpart of
/// the MAUI suite's <c>TodoUatFixture</c>, running the same scenario files. It starts on the
/// <c>DefaultTestData</c> of <c>uat.config.md</c>; a scenario file that names another test set
/// switches to it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Several apps.</b> The apps of the <c>Apps</c> table are more devices on the same fake backend,
/// each with its own database, host, browser context and <c>X-Todo-Client</c>. <c>Alice uses the
/// app</c> starts Alice's device the first time and makes it current; a scenario starts on the main
/// device. See <c>.my/html/todo-blazor.md</c> section 4.
/// </para>
/// <para>
/// Each device has its own composition, whose <see cref="IHtmlTestContext"/> is that device's. A
/// restart keeps a device's browser context, so a composition is never rebuilt.
/// </para>
/// </remarks>
[TestModuleScan(typeof(TodoListPage), NamespacePrefix = "Brinell.Samples.Todo.Web.UITests.Pages")]
[TestModuleScan(typeof(TodoScreenPhrases), NamespacePrefix = "Brinell.Samples.Todo.Web.Uat.Phrases")]
[TestModuleScan(typeof(TodoDataPhrases), NamespacePrefix = "Brinell.Samples.Todo.TestSupport.Uat")]
public sealed class TodoWebUatFixture : TodoWebFixture, IUatSessionHost
{
    private readonly Dictionary<string, App> _apps = new(StringComparer.OrdinalIgnoreCase);
    private TestComposition? _composition;

    /// <summary>The test set a scenario file runs on when its metadata names none: <c>DefaultTestData</c> in <c>uat.config.md</c>.</summary>
    public static string DefaultTestData { get; } = ReadConfig().Runtime.TryGetValue(UatConfigFields.DefaultTestData, out var name) && name.Length > 0
        ? name
        : throw new InvalidOperationException($"uat.config.md has no '{UatConfigFields.DefaultTestData}' in its Runtime table.");

    /// <inheritdoc />
    protected override string TestDataName => DefaultTestData;

    /// <summary>The page graph and DI scope the UAT runtime resolves controls through: the main device's.</summary>
    public TestComposition Composition => _composition
        ?? throw new InvalidOperationException("The fixture is not started.");

    /// <summary>Captures the current app's page when a scenario fails (UAT evidence).</summary>
    public IScreenshotService ScreenshotService => new ScreenshotService(Context, Context.Logger);

    /// <summary>The name <paramref name="app"/> sends in <c>X-Todo-Client</c>.</summary>
    public string ClientIdOf(string app) => _apps.TryGetValue(app, out var started)
        ? started.Device.ClientId!
        : ClientIdFor(app);

    /// <inheritdoc />
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _composition = ComposeFor(MainDevice);
    }

    /// <inheritdoc />
    public IServiceProvider UseSession(string name)
    {
        if (!_apps.TryGetValue(name, out var app))
        {
            var device = StartDeviceAsync(name.ToLowerInvariant(), ClientIdFor(name)).GetAwaiter().GetResult();
            app = new App(device, ComposeFor(device).CreateScope());
            _apps[name] = app;
        }

        Device = app.Device;
        OpenList();
        return app.Scope.ServiceProvider;
    }

    /// <inheritdoc />
    /// <remarks>Every scenario starts on the main device; the other apps are put online again.</remarks>
    public override TodoListPage StartTest()
    {
        Device = MainDevice;
        foreach (var app in _apps.Values)
        {
            app.Device.Network.SetOnline();
        }

        return base.StartTest();
    }

    /// <inheritdoc />
    /// <remarks>The other apps hold the old test set's data, so they are stopped; a scenario starts them again.</remarks>
    public override TodoListPage UseTestSet(string name)
    {
        if (name != TestSet.Name)
        {
            StopApps();
        }

        Device = MainDevice;
        return base.UseTestSet(name);
    }

    /// <inheritdoc />
    public override async Task DisposeAsync()
    {
        StopApps();
        await base.DisposeAsync();
    }

    // The client id an app sends: its ClientId column, or web-<name>.
    private static string ClientIdFor(string app)
        => ReadConfig().Apps.FirstOrDefault(row => string.Equals(row.Name, app, StringComparison.OrdinalIgnoreCase)) is { } row
           && row.Values.TryGetValue("ClientId", out var clientId) && clientId.Length > 0
            ? clientId
            : $"web-{app.ToLowerInvariant()}";

    private void StopApps()
    {
        foreach (var app in _apps.Values)
        {
            app.Scope.Dispose();
            app.Device.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _apps.Clear();
    }

    private TestComposition ComposeFor(TodoWebDevice device)
        => TestComposition.ForFixture(this, services => services.AddSingleton<IHtmlTestContext>(device.Context));

    private static UatConfig ReadConfig() => UatConfigParser.ParseFile(UatScenarioSource.GetConfigFilePath());

    private sealed record App(TodoWebDevice Device, IServiceScope Scope);
}

/// <summary>Binds every UAT scenario class to the one <see cref="TodoWebUatFixture"/> (one app per run).</summary>
[CollectionDefinition(CollectionName)]
public sealed class TodoWebUatCollection : ICollectionFixture<TodoWebUatFixture>
{
    /// <summary>The collection name shared by the UAT scenario classes.</summary>
    public const string CollectionName = "Todo web UAT";
}
