using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brinell.Samples.Todo.Contracts;

/// <summary>
/// A todo as it travels between the app and the API.
/// </summary>
/// <remarks>
/// The id is chosen by the client, so a todo created offline keeps its id when it reaches the
/// server and a retried <c>PUT</c> cannot create a duplicate.
/// </remarks>
/// <param name="Id">The client-chosen id.</param>
/// <param name="Title">The title; required, at most 100 characters.</param>
/// <param name="Notes">Free text, or <c>null</c>.</param>
/// <param name="DueDate">The due date, or <c>null</c> for none.</param>
/// <param name="Status">Where the todo is in its cycle.</param>
/// <param name="CreatedAt">When it was created, UTC.</param>
/// <param name="UpdatedAt">When it last changed, UTC. The newer one wins a conflict.</param>
public sealed record TodoDto(
    Guid Id,
    string Title,
    string? Notes,
    DateOnly? DueDate,
    TodoStatusDto Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>A todo's status on the wire, written as its name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TodoStatusDto>))]
public enum TodoStatusDto
{
    /// <summary>Not started.</summary>
    Open,

    /// <summary>Being worked on.</summary>
    InProgress,

    /// <summary>Finished.</summary>
    Done,
}

/// <summary>
/// The routes, header and serializer settings both ends agree on.
/// </summary>
public static class TodoApi
{
    /// <summary>The collection route.</summary>
    public const string TodosPath = "/api/todos";

    /// <summary>The header carrying the API key.</summary>
    public const string ApiKeyHeader = "X-Api-Key";

    /// <summary>Where a development API listens, and where the app looks when nothing says otherwise.</summary>
    public const string DevelopmentBaseUrl = "http://localhost:5080";

    /// <summary>The key a development API and the tests use when none is configured.</summary>
    public const string DevelopmentApiKey = "dev-key";

    /// <summary>Serializer settings: the web defaults (camelCase).</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);

    /// <summary>The route of one todo.</summary>
    public static string TodoPath(Guid id) => $"{TodosPath}/{id:D}";
}
