using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Brinell.Samples.Todo.Contracts;

namespace Brinell.Samples.Todo.UITests.Live;

/// <summary>
/// The real Todo API, as the live tests reach it: over HTTP, the way any client would.
/// </summary>
/// <remarks>
/// Deliberately not the app's own <c>TodoApiClient</c>: the live tier checks what the server holds,
/// and reading it with the code under test would let a shared mistake pass on both sides.
/// </remarks>
public sealed class LiveTodoApi : IDisposable
{
    /// <summary>How often <see cref="WaitUntilAsync"/> asks the server again.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly HttpClient _http;

    /// <summary>Talks to the API at <paramref name="baseUrl"/> with <paramref name="apiKey"/>.</summary>
    public LiveTodoApi(string baseUrl, string apiKey)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl + "/"), Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.Add(TodoApi.ApiKeyHeader, apiKey);
    }

    /// <summary>The API's address.</summary>
    public string BaseUrl { get; }

    /// <summary>Every todo on the server.</summary>
    public async Task<IReadOnlyList<TodoDto>> GetAllAsync()
        => await _http.GetFromJsonAsync<List<TodoDto>>(Relative(TodoApi.TodosPath), TodoApi.JsonOptions) ?? [];

    /// <summary>The todo with this title, or <c>null</c>.</summary>
    public async Task<TodoDto?> FindAsync(string title)
        => (await GetAllAsync()).SingleOrDefault(todo => todo.Title == title);

    /// <summary>Stores a todo on the server, as another device's sync would.</summary>
    public async Task PutAsync(TodoDto todo)
    {
        using var response = await _http.PutAsJsonAsync(Relative(TodoApi.TodoPath(todo.Id)), todo, TodoApi.JsonOptions);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>The status the server answers for one todo: 200 while it exists, 404 once deleted.</summary>
    public async Task<HttpStatusCode> StatusOfAsync(Guid id)
    {
        using var response = await _http.GetAsync(Relative(TodoApi.TodoPath(id)));
        return response.StatusCode;
    }

    /// <summary>
    /// Waits until <paramref name="condition"/> holds on the server.
    /// </summary>
    /// <remarks>
    /// For outcomes the app does not show: after a delete is synced, the row was already gone from
    /// the list before the sync, so nothing on screen says the server has been told.
    /// </remarks>
    /// <exception cref="TimeoutException">It did not hold in time; the message says what was waited for.</exception>
    public async Task WaitUntilAsync(Func<Task<bool>> condition, string what, TimeSpan? timeout = null)
    {
        var limit = timeout ?? TimeSpan.FromSeconds(10);
        var clock = Stopwatch.StartNew();

        while (!await condition())
        {
            if (clock.Elapsed >= limit)
            {
                throw new TimeoutException($"The API at {BaseUrl} did not reach this state within {limit.TotalSeconds:0} s: {what}.");
            }

            await Task.Delay(PollInterval);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();

    private static string Relative(string path) => path.TrimStart('/');
}
