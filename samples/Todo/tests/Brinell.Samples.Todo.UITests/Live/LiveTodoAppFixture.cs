using Brinell.Samples.Todo.Api;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.Infrastructure;
using Brinell.Samples.Todo.TestSupport;

namespace Brinell.Samples.Todo.UITests.Live;

/// <summary>
/// The Todo app against the real <c>Todo.Api</c>: nothing faked between the app and the server.
/// </summary>
/// <remarks>
/// <para>
/// <b>Which API.</b> By default a fresh one, started in this test process on a free port with its
/// own server database: real Kestrel, real HTTP, the real store. Set
/// <see cref="BaseUrlVariable"/> (and <see cref="ApiKeyVariable"/>) to run the same tests against a
/// deployed instance instead - the stand-in for an acceptance environment.
/// </para>
/// <para>
/// <b>What is arranged.</b> The scenario's local half goes into the app's database as usual; its
/// server half is PUT to the API over HTTP, so a deployed API is arranged the same way as a local
/// one. Tests add todos of their own with unique titles and never rely on the server being empty.
/// </para>
/// <para>
/// Nothing is reset between tests: a real server keeps what it was sent. That is the point of
/// this tier, and why it holds only a few happy paths.
/// </para>
/// </remarks>
public sealed class LiveTodoAppFixture : TodoAppFixtureBase
{
    /// <summary>A deployed API to use instead of starting one.</summary>
    public const string BaseUrlVariable = "TODO_LIVE_API_BASEURL";

    /// <summary>The deployed API's key; the development key when unset.</summary>
    public const string ApiKeyVariable = "TODO_LIVE_API_KEY";

    private RunningTodoApi? _running;
    private LiveTodoApi? _api;
    private string _apiKey = string.Empty;

    /// <summary>The API the app talks to, for arranging and checking server state.</summary>
    public LiveTodoApi Api => _api
        ?? throw new InvalidOperationException("The API was used before it was started.");

    /// <inheritdoc />
    protected override string ScenarioName => "three-todos";

    /// <summary>A unique title, so a todo from this test cannot be confused with any other.</summary>
    public static string Unique(string title) => $"{title} {Guid.NewGuid().ToString("N")[..6]}";

    /// <summary>A new todo as another device would store it.</summary>
    public TodoDto NewTodo(string title)
    {
        var yesterday = Scenario.Now.AddDays(-1);
        return new TodoDto(Guid.NewGuid(), title, null, null, TodoStatusDto.Open, yesterday, yesterday);
    }

    /// <inheritdoc />
    protected override string StartBackend(TodoScenario scenario, string runFolder)
    {
        var deployed = Environment.GetEnvironmentVariable(BaseUrlVariable);
        var apiKey = Environment.GetEnvironmentVariable(ApiKeyVariable) is { Length: > 0 } key
            ? key
            : TodoApi.DevelopmentApiKey;

        string baseUrl;
        if (string.IsNullOrWhiteSpace(deployed))
        {
            _running = TodoApiHost.StartAsync(new TodoApiOptions
            {
                DatabasePath = Path.Combine(runFolder, "server.db"),
                ApiKey = apiKey,
            }).GetAwaiter().GetResult();
            baseUrl = _running.BaseUrl;
        }
        else
        {
            baseUrl = deployed;
        }

        _apiKey = apiKey;
        _api = new LiveTodoApi(baseUrl, apiKey);
        foreach (var todo in scenario.Server)
        {
            _api.PutAsync(todo).GetAwaiter().GetResult();
        }

        return baseUrl;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The app gets the same key the API was given. Without this the app kept the development key,
    /// which a deployed API with its own key refuses (401) - hidden while both sides used the
    /// development key.
    /// </remarks>
    protected override LaunchSettings AdjustSettings(LaunchSettings settings) => settings with { ApiKey = _apiKey };

    /// <inheritdoc />
    protected override void StopBackend()
    {
        _api?.Dispose();
        _running?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

/// <summary>
/// Holds the live app for the collection, and starts it only when a live test actually runs.
/// </summary>
/// <remarks>
/// xUnit builds a collection's fixtures even when every test in it is skipped, and a
/// <see cref="LiveTodoAppFixture"/> launches the app and the API in its constructor: measured, a
/// normal run with the live tier switched off paid about 2 s for an app nobody used. xUnit does not
/// construct a skipped test's class, so creating the app on first access from a test's constructor
/// means it starts only when the switch is on.
/// </remarks>
public sealed class LiveAppHolder : IDisposable
{
    private readonly Lazy<LiveTodoAppFixture> _app = new(() => new LiveTodoAppFixture());

    /// <summary>The live app, launched on first access.</summary>
    public LiveTodoAppFixture App => _app.Value;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_app.IsValueCreated)
        {
            _app.Value.Dispose();
        }
    }
}

/// <summary>Tests against the live app (<see cref="LiveAppHolder"/>).</summary>
[CollectionDefinition(Name)]
public sealed class LiveApiCollection : ICollectionFixture<LiveAppHolder>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Todo: live API";
}

/// <summary>
/// A live-tier test: skipped unless <c>BRINELL_UAT_LIVE_API=1</c>.
/// </summary>
/// <remarks>
/// The same switch the UAT projects use for their <c>live-api</c> tag, so one variable turns every
/// live tier on - in the nightly run, not on every PR.
/// </remarks>
public sealed class LiveApiFactAttribute : FactAttribute
{
    /// <summary>The switch.</summary>
    public const string Variable = "BRINELL_UAT_LIVE_API";

    /// <summary>Skips unless the switch is on; otherwise a fact with the usual timeout.</summary>
    public LiveApiFactAttribute()
    {
        Timeout = TestConstants.DefaultTestTimeoutMs;

        if (!IsOn)
        {
            Skip = $"Live tier: set {Variable}=1 to run it against the real Todo API.";
        }
    }

    /// <summary>Whether the live tier is switched on.</summary>
    public static bool IsOn => Environment.GetEnvironmentVariable(Variable) == "1";
}
