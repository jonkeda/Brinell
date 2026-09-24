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
/// Test sets are data (<c>TestData/*.json</c>), so the integration fixture and the UI fixture
/// mean the same thing by "three-todos". <see cref="SeedDatabaseAsync"/> writes the local half
/// with the app's own migrations and repository; <see cref="StubServer"/> serves the server half
/// from WireMock.
/// </para>
/// <para>
/// Files are read strictly: an unknown field, a missing required field or a <c>null</c> where the
/// type has none is an error naming the file, not a todo with a missing title.
/// </para>
/// <para>
/// <see cref="Now"/> is pinned in every tier that uses the test set (the UI tiers pass it as
/// <c>TODO_NOW</c>), so due dates written as plain dates are overdue or not on every run alike.
/// </para>
/// </remarks>
public sealed class TodoTestSet
{
    private const string ResourcePrefix = "TestData.";

    /// <summary>The serializer settings test-set files are read with.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(TodoApi.JsonOptions)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter<SyncState>() },
    };

    private TodoTestSet(string name, TodoTestSetFile file)
    {
        Name = name;
        Now = file.Now;
        Server = file.Server;
        Local = file.Local.Select(local => local.ToItem(file.Server, name)).ToList();
    }

    /// <summary>The test set's name, its file name without extension.</summary>
    public string Name { get; }

    /// <summary>The instant the test set is set at.</summary>
    public DateTimeOffset Now { get; }

    /// <summary>What the server has.</summary>
    public IReadOnlyList<TodoDto> Server { get; }

    /// <summary>What the device has, with the "same as the server" shortcut filled in.</summary>
    public IReadOnlyList<TodoItem> Local { get; }

    /// <summary>The test sets that exist.</summary>
    public static IReadOnlyList<string> Names { get; } = typeof(TodoTestSet).Assembly
        .GetManifestResourceNames()
        .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
        .Select(name => name[ResourcePrefix.Length..^".json".Length])
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>Loads a test set by name, e.g. <c>three-todos</c>.</summary>
    /// <exception cref="ArgumentException">There is no such test set; the message lists those there are.</exception>
    /// <exception cref="InvalidDataException">The file does not match the types; the message names the file, line and field.</exception>
    public static TodoTestSet Load(string name)
    {
        using var stream = typeof(TodoTestSet).Assembly.GetManifestResourceStream($"{ResourcePrefix}{name}.json")
            ?? throw new ArgumentException(
                $"There is no test set '{name}'. There are: {string.Join(", ", Names)}.", nameof(name));

        TodoTestSetFile? file;
        try
        {
            file = JsonSerializer.Deserialize<TodoTestSetFile>(stream, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"TestData/{name}.json:{exception.LineNumber + 1}: {exception.Message}", exception);
        }

        return new TodoTestSet(name, file ?? throw new InvalidDataException($"TestData/{name}.json is empty."));
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
}

/// <summary>The shape of a <c>TestData/*.json</c> file.</summary>
/// <param name="Now">The instant the test set is set at.</param>
/// <param name="Server">What the server has, as the API sends it.</param>
/// <param name="Local">What the device has.</param>
/// <param name="Schema">The <c>$schema</c> reference editors use; ignored when loading.</param>
public sealed record TodoTestSetFile(
    DateTimeOffset Now,
    IReadOnlyList<TodoDto> Server,
    IReadOnlyList<LocalTodoData> Local,
    [property: JsonPropertyName("$schema")] string? Schema = null);

/// <summary>
/// A todo on the device, as a test set writes it: either complete, or only <see cref="Id"/> and
/// <see cref="SyncState"/> to mean "the same as the server's todo with this id".
/// </summary>
/// <param name="Id">The todo's id.</param>
/// <param name="SyncState">Whether the server has this version.</param>
/// <param name="Title">The title; leave out, with every other content field, to copy the server's todo.</param>
/// <param name="Notes">Free text, or <c>null</c>.</param>
/// <param name="DueDate">The due date, or <c>null</c>.</param>
/// <param name="Status">The status; required when <paramref name="Title"/> is given.</param>
/// <param name="CreatedAt">When it was created; required when <paramref name="Title"/> is given.</param>
/// <param name="UpdatedAt">When it last changed; required when <paramref name="Title"/> is given.</param>
/// <param name="IsDeleted">A tombstone waiting for the server to be told.</param>
public sealed record LocalTodoData(
    Guid Id,
    SyncState SyncState,
    string? Title = null,
    string? Notes = null,
    DateOnly? DueDate = null,
    TodoStatusDto? Status = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null,
    bool IsDeleted = false)
{
    /// <summary>Whether this entry is the "same as the server" shortcut.</summary>
    public bool IsSameAsServer => Title is null;

    /// <summary>The device's todo, with the shortcut resolved against <paramref name="server"/>.</summary>
    /// <exception cref="InvalidDataException">The shortcut names no server todo, or a complete entry misses a field.</exception>
    public TodoItem ToItem(IReadOnlyList<TodoDto> server, string testSetName)
    {
        var dto = IsSameAsServer ? FromServer(server, testSetName) : Complete(testSetName);
        return TodoMapping.ToItem(dto) with { SyncState = SyncState, IsDeleted = IsDeleted };
    }

    private TodoDto FromServer(IReadOnlyList<TodoDto> server, string testSetName)
    {
        if (Notes is not null || DueDate is not null || Status is not null || CreatedAt is not null || UpdatedAt is not null)
        {
            throw Invalid(testSetName, "gives content fields but no title. Give all of them, or only id and syncState to copy the server's todo.");
        }

        return server.FirstOrDefault(todo => todo.Id == Id)
            ?? throw Invalid(testSetName, "copies the server's todo, but the server has no todo with this id.");
    }

    private TodoDto Complete(string testSetName) => new(
        Id,
        Title!,
        Notes,
        DueDate,
        Status ?? throw Invalid(testSetName, "has a title but no status."),
        CreatedAt ?? throw Invalid(testSetName, "has a title but no createdAt."),
        UpdatedAt ?? throw Invalid(testSetName, "has a title but no updatedAt."));

    private InvalidDataException Invalid(string testSetName, string problem)
        => new($"TestData/{testSetName}.json: local todo {Id} {problem}");
}
