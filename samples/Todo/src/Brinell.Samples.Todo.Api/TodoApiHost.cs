using Brinell.Samples.Todo.Contracts;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Brinell.Samples.Todo.Api;

/// <summary>How the API runs.</summary>
public sealed record TodoApiOptions
{
    /// <summary>The server's SQLite file.</summary>
    public string DatabasePath { get; init; } = Path.Combine(AppContext.BaseDirectory, "todo-server.db");

    /// <summary>The key clients must send in <see cref="TodoApi.ApiKeyHeader"/>.</summary>
    public string ApiKey { get; init; } = TodoApi.DevelopmentApiKey;
}

/// <summary>
/// Builds and starts the API: as a process (<c>Program</c>), or inside a test on a free port.
/// </summary>
public static class TodoApiHost
{
    /// <summary>The configuration section read by <see cref="Build"/>: <c>Todo:DatabasePath</c>, <c>Todo:ApiKey</c>.</summary>
    public const string ConfigurationSection = "Todo";

    /// <summary>
    /// Builds the API. Options passed here win over configuration.
    /// </summary>
    public static WebApplication Build(string[] args, TodoApiOptions? options = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        var configured = builder.Configuration.GetSection(ConfigurationSection).Get<TodoApiOptions>() ?? new TodoApiOptions();
        var effective = options ?? configured;

        builder.Services.AddSingleton(effective);
        builder.Services.AddSingleton(new ServerTodoStore(effective.DatabasePath));
        builder.Services.ConfigureHttpJsonOptions(json =>
        {
            json.SerializerOptions.PropertyNamingPolicy = TodoApi.JsonOptions.PropertyNamingPolicy;
        });

        var app = builder.Build();
        app.MapTodoApi();
        return app;
    }

    /// <summary>
    /// Starts the API in this process on a free port, for a test.
    /// </summary>
    public static async Task<RunningTodoApi> StartAsync(TodoApiOptions options, CancellationToken cancellationToken = default)
    {
        var app = Build([], options);
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");

        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        return new RunningTodoApi(app, address.TrimEnd('/'));
    }

    /// <summary>The endpoints, behind the API-key check.</summary>
    public static void MapTodoApi(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok("healthy"));

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var expected = context.RequestServices.GetRequiredService<TodoApiOptions>().ApiKey;
                if (!string.Equals(context.Request.Headers[TodoApi.ApiKeyHeader], expected, StringComparison.Ordinal))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await next(context);
        });

        var todos = app.MapGroup(TodoApi.TodosPath);

        todos.MapGet("/", (ServerTodoStore store) => Results.Ok(store.GetAll()));

        todos.MapGet("/{id:guid}", (Guid id, ServerTodoStore store)
            => store.Get(id) is { } todo ? Results.Ok(todo) : Results.NotFound());

        todos.MapPut("/{id:guid}", (Guid id, TodoDto todo, ServerTodoStore store) =>
        {
            if (todo.Id != id)
            {
                return Results.BadRequest($"The body's id {todo.Id} is not the route's id {id}.");
            }

            if (string.IsNullOrWhiteSpace(todo.Title) || todo.Title.Length > 100)
            {
                return Results.BadRequest("A title is required, and at most 100 characters.");
            }

            return Results.Ok(store.Upsert(todo));
        });

        todos.MapDelete("/{id:guid}", (Guid id, ServerTodoStore store)
            => store.Delete(id) ? Results.NoContent() : Results.NotFound());
    }
}

/// <summary>An API running in this process. Dispose to stop it.</summary>
public sealed class RunningTodoApi(WebApplication app, string baseUrl) : IAsyncDisposable
{
    /// <summary>Where it listens, without a trailing slash.</summary>
    public string BaseUrl { get; } = baseUrl;

    /// <summary>The server's store, to arrange or check server state directly.</summary>
    public ServerTodoStore Store => app.Services.GetRequiredService<ServerTodoStore>();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await app.StopAsync().ConfigureAwait(false);
        await app.DisposeAsync().ConfigureAwait(false);
    }
}
