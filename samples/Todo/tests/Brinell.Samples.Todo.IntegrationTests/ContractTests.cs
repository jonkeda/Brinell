using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Brinell.Samples.Todo.Api;
using Brinell.Samples.Todo.Infrastructure.Api;

namespace Brinell.Samples.Todo.IntegrationTests;

/// <summary>
/// Keeps the mocks honest: the real <c>Todo.Api</c>, run in this process, against the app's
/// client, the recorded samples, and the WireMock fake.
/// </summary>
/// <remarks>
/// Every hermetic test trusts the fake backend to behave like the real one. These tests are what
/// that trust rests on: when the API changes, they fail here, before a UI test passes against a
/// server that no longer exists.
/// </remarks>
[Trait("Module", "Todo")]
[Trait("Pyramid", "Integration")]
[Trait("Journey", "TOD.07.9")]
public sealed class ContractTests : IAsyncLifetime
{
    private readonly TempDatabase _serverFile = new("api");
    private RunningTodoApi _api = null!;

    public async Task InitializeAsync()
        => _api = await TodoApiHost.StartAsync(new TodoApiOptions { DatabasePath = _serverFile.Path });

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        _serverFile.Dispose();
    }

    [Fact]
    public async Task TheAppsClient_WorksAgainstTheRealApi()
    {
        using var http = TodoApiClient.CreateHttpClient(_api.BaseUrl, TodoApi.DevelopmentApiKey, TimeSpan.FromSeconds(5));
        var client = new TodoApiClient(http);
        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = "Contract",
            Notes = "Round trip",
            DueDate = new DateOnly(2026, 3, 12),
            Status = TodoStatus.InProgress,
            CreatedAt = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 3, 2, 8, 0, 0, TimeSpan.Zero),
        };

        var stored = await client.PutAsync(todo);
        var listed = Assert.Single(await client.GetAllAsync());
        await client.DeleteAsync(todo.Id);
        await client.DeleteAsync(todo.Id);

        Assert.Equal(todo, stored);
        Assert.Equal(todo, listed);
        Assert.Empty(await client.GetAllAsync());
    }

    [Fact]
    public async Task TheRealApi_KeepsTheNewerVersion_AsThePlannerAssumes()
    {
        using var http = TodoApiClient.CreateHttpClient(_api.BaseUrl, TodoApi.DevelopmentApiKey, TimeSpan.FromSeconds(5));
        var client = new TodoApiClient(http);
        var newer = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = "Newer",
            CreatedAt = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 3, 5, 8, 0, 0, TimeSpan.Zero),
        };
        await client.PutAsync(newer);

        var answer = await client.PutAsync(newer with { Title = "Older", UpdatedAt = newer.UpdatedAt.AddDays(-1) });

        Assert.Equal("Newer", answer.Title);
    }

    [Fact]
    public async Task TheRecordedSample_HasExactlyTheShapeTheRealApiSends()
    {
        var sample = SampleFiles.Load("ContractSamples", "todos/list.json");
        var strict = new JsonSerializerOptions(TodoApi.JsonOptions)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };
        var recorded = JsonSerializer.Deserialize<List<TodoDto>>(sample, strict)!;

        using var http = Http(_api.BaseUrl);
        foreach (var todo in recorded)
        {
            (await http.PutAsJsonAsync(TodoApi.TodoPath(todo.Id), todo, TodoApi.JsonOptions)).EnsureSuccessStatusCode();
        }

        var live = await http.GetStringAsync(TodoApi.TodosPath);

        Assert.Equal(PropertyNames(sample), PropertyNames(live));
        Assert.Equal(recorded, JsonSerializer.Deserialize<List<TodoDto>>(live, strict));
    }

    [Fact]
    public async Task TheWireMockFake_AnswersLikeTheRealApi()
    {
        using var mock = MockApiServer.Start();
        TodoServerStub.Install(mock, []);

        var real = await RunScriptAsync(_api.BaseUrl);
        var fake = await RunScriptAsync(mock.BaseUrl);

        Assert.Equal(real, fake);
    }

    /// <summary>
    /// The same requests, in the same order, to one backend; what came back, normalised to status
    /// codes and parsed bodies so formatting differences do not count.
    /// </summary>
    private static async Task<IReadOnlyList<string>> RunScriptAsync(string baseUrl)
    {
        using var keyed = Http(baseUrl);
        using var anonymous = new HttpClient { BaseAddress = new Uri(baseUrl + "/") };

        var id = Guid.Parse("66666666-0000-0000-0000-000000000001");
        var todo = new TodoDto(id, "Script", null, null, TodoStatusDto.Open,
            new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 2, 8, 0, 0, TimeSpan.Zero));

        var steps = new List<(string Name, Func<Task<HttpResponseMessage>> Send)>
        {
            ("list without key", () => anonymous.GetAsync(Relative(TodoApi.TodosPath))),
            ("list empty", () => keyed.GetAsync(Relative(TodoApi.TodosPath))),
            ("put", () => keyed.PutAsJsonAsync(Relative(TodoApi.TodoPath(id)), todo, TodoApi.JsonOptions)),
            ("put older", () => keyed.PutAsJsonAsync(Relative(TodoApi.TodoPath(id)), todo with { Title = "Older", UpdatedAt = todo.UpdatedAt.AddDays(-1) }, TodoApi.JsonOptions)),
            ("put wrong id", () => keyed.PutAsJsonAsync(Relative(TodoApi.TodoPath(Guid.NewGuid())), todo, TodoApi.JsonOptions)),
            ("put empty title", () => keyed.PutAsJsonAsync(Relative(TodoApi.TodoPath(id)), todo with { Title = " " }, TodoApi.JsonOptions)),
            ("get one", () => keyed.GetAsync(Relative(TodoApi.TodoPath(id)))),
            ("list one", () => keyed.GetAsync(Relative(TodoApi.TodosPath))),
            ("delete", () => keyed.DeleteAsync(Relative(TodoApi.TodoPath(id)))),
            ("delete again", () => keyed.DeleteAsync(Relative(TodoApi.TodoPath(id)))),
            ("get deleted", () => keyed.GetAsync(Relative(TodoApi.TodoPath(id)))),
        };

        var outcomes = new List<string>();
        foreach (var (name, send) in steps)
        {
            using var response = await send();
            outcomes.Add($"{name}: {(int)response.StatusCode} {await NormaliseAsync(response)}");
        }

        return outcomes;
    }

    private static async Task<string> NormaliseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            // Error bodies are prose, and prose is not part of the contract.
            return string.Empty;
        }

        var body = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(body);
        return node is JsonArray
            ? JsonSerializer.Serialize(JsonSerializer.Deserialize<List<TodoDto>>(body, TodoApi.JsonOptions))
            : JsonSerializer.Serialize(JsonSerializer.Deserialize<TodoDto>(body, TodoApi.JsonOptions));
    }

    private static HttpClient Http(string baseUrl)
        => TodoApiClient.CreateHttpClient(baseUrl, TodoApi.DevelopmentApiKey, TimeSpan.FromSeconds(5));

    private static string Relative(string path) => path.TrimStart('/');

    private static IReadOnlyList<string> PropertyNames(string json)
        => JsonNode.Parse(json)!.AsArray()
            .SelectMany(item => item!.AsObject().Select(property => property.Key))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();
}
