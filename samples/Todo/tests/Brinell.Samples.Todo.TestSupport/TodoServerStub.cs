using System.Text.Json;
using Brinell.Mocking;
using Brinell.Samples.Todo.Contracts;

namespace Brinell.Samples.Todo.TestSupport;

/// <summary>
/// The Todo API, faked on a <see cref="MockApiServer"/>: it remembers what it is sent.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why stateful.</b> The app pushes, then pulls. A fake that always returns the scenario's
/// list would answer the pull without the todo just pushed, and the app would rightly conclude it
/// had been deleted on the server. So the fake keeps a list, as the real API does, and a test can
/// read it (<see cref="Todos"/>) to see what the app sent.
/// </para>
/// <para>
/// <b>Kept honest by the contract tests.</b> The same requests go to this fake and to the real
/// <c>Todo.Api</c>, and the answers are compared; a difference fails there before it can give a
/// UI test false confidence.
/// </para>
/// <para>
/// One test's variation is layered on top at a higher priority, e.g.
/// <c>mock.Get(TodoApi.TodosPath).AtPriority(1).Fails(500)</c>, and <see cref="MockApiServer.Reset"/>
/// removes it again.
/// </para>
/// </remarks>
public sealed class TodoServerStub
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, TodoDto> _todos;
    private readonly string _apiKey;

    private TodoServerStub(IEnumerable<TodoDto> initial, string apiKey)
    {
        _todos = initial.ToDictionary(todo => todo.Id);
        _apiKey = apiKey;
    }

    /// <summary>What the fake server holds now, oldest first.</summary>
    public IReadOnlyList<TodoDto> Todos
    {
        get
        {
            lock (_gate)
            {
                return _todos.Values.OrderBy(todo => todo.CreatedAt).ToList();
            }
        }
    }

    /// <summary>
    /// Registers the fake's endpoints on <paramref name="mock"/>, starting from <paramref name="initial"/>.
    /// </summary>
    public static TodoServerStub Install(MockApiServer mock, IEnumerable<TodoDto> initial, string apiKey = TodoApi.DevelopmentApiKey)
    {
        var stub = new TodoServerStub(initial, apiKey);

        mock.Get(TodoApi.TodosPath).RespondsWith(stub.List);
        mock.Get(TodoApi.TodosPath + "/*").RespondsWith(stub.GetOne);
        mock.Put(TodoApi.TodosPath + "/*").RespondsWith(stub.Put);
        mock.Delete(TodoApi.TodosPath + "/*").RespondsWith(stub.Delete);

        return stub;
    }

    /// <summary>Adds or replaces a todo on the server, as if another device had synced it.</summary>
    public void Store(TodoDto todo)
    {
        lock (_gate)
        {
            _todos[todo.Id] = todo;
        }
    }

    /// <summary>Removes a todo from the server, as if another device had deleted it.</summary>
    public void Remove(Guid id)
    {
        lock (_gate)
        {
            _todos.Remove(id);
        }
    }

    private MockResponse List(RecordedRequest request)
        => Authorized(request) ?? Json(Todos);

    private MockResponse GetOne(RecordedRequest request)
    {
        if (Authorized(request) is { } refused)
        {
            return refused;
        }

        if (!TryReadId(request, out var id))
        {
            return MockResponse.Status(404);
        }

        lock (_gate)
        {
            return _todos.TryGetValue(id, out var todo) ? Json(todo) : MockResponse.Status(404);
        }
    }

    private MockResponse Put(RecordedRequest request)
    {
        if (Authorized(request) is { } refused)
        {
            return refused;
        }

        if (!TryReadId(request, out var id))
        {
            return MockResponse.Status(404);
        }

        TodoDto? sent;
        try
        {
            sent = request.BodyAs<TodoDto>(TodoApi.JsonOptions);
        }
        catch (JsonException)
        {
            return MockResponse.Status(400);
        }

        if (sent is null || sent.Id != id || string.IsNullOrWhiteSpace(sent.Title) || sent.Title.Length > 100)
        {
            return MockResponse.Status(400);
        }

        lock (_gate)
        {
            // Last writer wins on UpdatedAt, as the real API does.
            if (!_todos.TryGetValue(id, out var stored) || sent.UpdatedAt >= stored.UpdatedAt)
            {
                _todos[id] = sent with { CreatedAt = stored?.CreatedAt ?? sent.CreatedAt };
            }

            return Json(_todos[id]);
        }
    }

    private MockResponse Delete(RecordedRequest request)
    {
        if (Authorized(request) is { } refused)
        {
            return refused;
        }

        if (!TryReadId(request, out var id))
        {
            return MockResponse.Status(404);
        }

        lock (_gate)
        {
            return MockResponse.Status(_todos.Remove(id) ? 204 : 404);
        }
    }

    private MockResponse? Authorized(RecordedRequest request)
        => request.Headers.TryGetValue(TodoApi.ApiKeyHeader, out var key) && key == _apiKey
            ? null
            : MockResponse.Status(401);

    private static bool TryReadId(RecordedRequest request, out Guid id)
        => Guid.TryParse(request.Path[(request.Path.LastIndexOf('/') + 1)..], out id);

    private static MockResponse Json(object body) => MockResponse.Json(body, options: TodoApi.JsonOptions);
}
