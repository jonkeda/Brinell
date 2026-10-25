using Bruinell.Html.Context;
using Bruinell.Html.Playwright;
using Bruinell.Html.Testing;
using Bruinell.Samples.Todo.Api;
using Bruinell.Samples.Todo.Infrastructure;
using Bruinell.Samples.Todo.Web.UITests.Pages;

namespace Bruinell.Samples.Todo.Web.UITests.Live;

/// <summary>Runs only when <c>BRUINELL_UAT_LIVE_API=1</c>, as the MAUI live tier does.</summary>
public sealed class LiveApiFactAttribute : FactAttribute
{
    public LiveApiFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("BRUINELL_UAT_LIVE_API") != "1")
        {
            Skip = "The live tier runs with BRUINELL_UAT_LIVE_API=1.";
        }
    }
}

/// <summary>The real <c>Todo.Api</c> in this process, and the web head against it.</summary>
public sealed class LiveWebFixture : IAsyncLifetime
{
    /// <summary>The name this app sends in <c>X-Todo-Client</c>.</summary>
    public const string ClientId = "web-live";

    private readonly TempDatabase _serverFile = new("web-live-api");
    private readonly TempDatabase _appFile = new("web-live-app");
    private WebAppHost? _host;
    private PlaywrightTestContext? _context;

    public RunningTodoApi Api { get; private set; } = null!;

    public IHtmlTestContext Context => _context ?? throw new InvalidOperationException("The fixture is not started.");

    public string BaseUrl => _host?.BaseUrl ?? throw new InvalidOperationException("The fixture is not started.");

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("BRUINELL_UAT_LIVE_API") != "1")
        {
            return;
        }

        Api = await TodoApiHost.StartAsync(new TodoApiOptions { DatabasePath = _serverFile.Path });
        var settings = new LaunchSettings { DatabasePath = _appFile.Path, ApiBaseUrl = Api.BaseUrl, ClientId = ClientId };
        _host = WebAppHost.Start(new WebAppHostOptions
        {
            AppPath = WebApp.Assembly(),
            Environment = settings.ToVariables(),
            ReadyPath = "/health",
            Name = "Todo web (live)",
        });
        _context = await PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions { Name = "todo-web-live" });
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        _host?.Dispose();
        if (Api != null)
        {
            await Api.DisposeAsync();
        }

        _appFile.Dispose();
        _serverFile.Dispose();
    }
}

/// <summary>The real wire, once: a todo made in the web head reaches the server, sent by this client.</summary>
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiLive")]
[Trait("Head", "Web")]
public sealed class LiveWebTests(LiveWebFixture fixture) : IClassFixture<LiveWebFixture>
{
    [LiveApiFact]
    [Trait("Journey", "TOD.07.4")]
    [Trait("Journey", "TOD.07.10")]
    public void ATodoCreatedInTheWebApp_IsOnTheServerAfterSync_SentByThisClient()
    {
        var title = $"Created in the web app {Guid.NewGuid().ToString("N")[..6]}";
        fixture.Context.NavigateTo(fixture.BaseUrl + "/");
        var list = new TodoListPage(fixture.Context);
        list.AssertIdle();

        list.CreateTodo(title).Sync().Todos.AssertPending(title, expected: false);

        var stored = Assert.Single(fixture.Api.Store.GetAll(), todo => todo.Title == title);
        Assert.Contains(fixture.Api.Requests, request =>
            request.Method == "PUT" && request.Path == TodoApi.TodoPath(stored.Id) && request.ClientId == LiveWebFixture.ClientId);
    }
}
