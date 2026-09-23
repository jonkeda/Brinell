using System.Text.Json;
using System.Text.Json.Serialization;
using Brinell.Mocking;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Infrastructure.Api;
using Brinell.Samples.Todo.Infrastructure.Data;

namespace Brinell.Samples.Todo.TestSupport;

/// <summary>
/// A named starting state: what the server has, what the device has, and what time it is.
/// </summary>
/// <remarks>
/// <para>
/// Scenarios are data (<c>Scenarios/*.json</c>), so the integration fixture and the UI fixture
/// mean the same thing by "three-todos". <see cref="SeedDatabaseAsync"/> writes the local half
/// with the app's own migrations and repository; <see cref="StubServer"/> serves the server half
/// from WireMock.
/// </para>
/// <para>
/// <see cref="Now"/> is pinned in every tier that uses the scenario (the UI tiers pass it as
/// <c>TODO_NOW</c>), so due dates written as plain dates are overdue or not on every run alike.
/// </para>
/// </remarks>
public sealed class TodoScenario
{
    private static readonly JsonSerializerOptions JsonOptions = new(TodoApi.JsonOptions)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter<SyncState>() },
    };

    private TodoScenario(string name, ScenarioFile file)
    {
        Name = name;
        Now = file.Now;
        Server = file.Server;
        Local = file.Local.Select(local => local.ToItem()).ToList();
    }

    /// <summary>The scenario's name, its file name without extension.</summary>
    public string Name { get; }

    /// <summary>The instant the scenario is set at.</summary>
    public DateTimeOffset Now { get; }

    /// <summary>What the server has.</summary>
    public IReadOnlyList<TodoDto> Server { get; }

    /// <summary>What the device has.</summary>
    public IReadOnlyList<TodoItem> Local { get; }

    /// <summary>The scenarios that exist.</summary>
    public static IReadOnlyList<string> Names { get; } = typeof(TodoScenario).Assembly
        .GetManifestResourceNames()
        .Where(name => name.StartsWith("Scenarios.", StringComparison.Ordinal))
        .Select(name => name["Scenarios.".Length..^".json".Length])
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>Loads a scenario by name, e.g. <c>three-todos</c>.</summary>
    /// <exception cref="ArgumentException">There is no such scenario; the message lists those there are.</exception>
    public static TodoScenario Load(string name)
    {
        using var stream = typeof(TodoScenario).Assembly.GetManifestResourceStream($"Scenarios.{name}.json")
            ?? throw new ArgumentException(
                $"There is no scenario '{name}'. There are: {string.Join(", ", Names)}.", nameof(name));

        var file = JsonSerializer.Deserialize<ScenarioFile>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Scenario '{name}' is empty.");

        return new TodoScenario(name, file);
    }

    /// <summary>Creates the database at <paramref name="path"/> and stores the local half.</summary>
    public async Task SeedDatabaseAsync(string path, CancellationToken cancellationToken = default)
    {
        var database = new TodoDatabase(path);
        await database.MigrateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var repository = new SqliteTodoRepository(database);
        foreach (var item in Local)
        {
            await repository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Serves the server half from <paramref name="mock"/>, as a fake that remembers writes.</summary>
    public TodoServerStub StubServer(MockApiServer mock, string apiKey = TodoApi.DevelopmentApiKey)
        => TodoServerStub.Install(mock, Server, apiKey);

    private sealed record ScenarioFile(
        DateTimeOffset Now,
        IReadOnlyList<TodoDto> Server,
        IReadOnlyList<LocalTodo> Local);

    private sealed record LocalTodo(
        Guid Id,
        string Title,
        string? Notes,
        DateOnly? DueDate,
        TodoStatusDto Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        SyncState SyncState,
        bool IsDeleted)
    {
        public TodoItem ToItem()
            => TodoMapping.ToItem(new TodoDto(Id, Title, Notes, DueDate, Status, CreatedAt, UpdatedAt))
                with { SyncState = SyncState, IsDeleted = IsDeleted };
    }
}
