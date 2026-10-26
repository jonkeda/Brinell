using System.Text.Json;
using Bruinet.Samples.Todo.Contracts;
using Bruinet.Samples.Todo.Core.Models;
using Bruinet.Samples.Todo.Core.Rules;
using Bruinet.TestData;

namespace Bruinet.Samples.Todo.TestSupport;

/// <summary>
/// The Todo test sets (<c>TestData/*.json</c>) and the rules they follow, for the xUnit gate and
/// the <c>bruinet data</c> tool.
/// </summary>
public sealed class TodoTestDataDefinition : TestDataDefinition<TodoTestSetFile>
{
    /// <inheritdoc />
    public override string Name => "Todo";

    /// <inheritdoc />
    public override JsonSerializerOptions JsonOptions => TodoTestSet.JsonOptions;

    /// <inheritdoc />
    public override IReadOnlyList<Type> TableTypes => [typeof(TodoDto), typeof(LocalTodoData), typeof(UserDto)];

    /// <inheritdoc />
    protected override string RelativeFolder => "TestData";

    /// <inheritdoc />
    protected override void DefineRules(TestDataRules<TodoTestSetFile> rules) => rules
        .UniqueIds("server", set => set.Server, todo => todo.Id)
        .UniqueIds("local", set => set.Local, todo => todo.Id)
        .Require("server", set => set.Server, todo => todo.CreatedAt <= todo.UpdatedAt, "updatedAt is before createdAt.")
        .ForEach("server", set => set.Server, todo => TodoValidation.ValidateTitle(todo.Title))
        .ForEach("local", set => set.Local, (set, todo) => todo.FindProblem(set.Server))
        .ForEach("local", set => set.Local, todo => todo.IsSameAsServer ? null : TodoValidation.ValidateTitle(todo.Title))
        .ForEach("local", set => set.Local, todo => todo.CreatedAt > todo.UpdatedAt ? "updatedAt is before createdAt." : null)
        .ForEach("local", set => set.Local, (set, todo) => SyncedButDifferent(set, todo))
        .UniqueIds("users", set => set.Users ?? [], user => user.Id)
        .UniqueIds("localUsers", set => set.LocalUsers ?? [], user => user.Id)
        .ForEach("users", set => set.Users ?? [], UserProblem)
        .ForEach("localUsers", set => set.LocalUsers ?? [], UserProblem)
        .ForEach("users", set => set.Users ?? [], (set, user) => ServerUserRules.IsDuplicateName(user, set.Users ?? [])
            ? "has the same name as another user that is not inactive."
            : null);

    // The server's own rules, so a test set cannot hold a user the API would refuse.
    private static string? UserProblem(UserDto user)
        => ServerUserRules.Validate(user) is { Count: > 0 } errors
            ? string.Join(" ", errors.Select(error => $"{error.Key}: {error.Value[0]}."))
            : null;

    private static string? SyncedButDifferent(TodoTestSetFile set, LocalTodoData todo)
    {
        if (todo.SyncState != SyncState.Synced || todo.IsSameAsServer || todo.FindProblem(set.Server) is not null)
        {
            return null;
        }

        var server = set.Server.FirstOrDefault(s => s.Id == todo.Id);
        if (server is null)
        {
            return "is Synced, but the server has no todo with this id.";
        }

        var local = new TodoDto(todo.Id, todo.Title!, todo.Notes, todo.DueDate, todo.Status!.Value, todo.CreatedAt!.Value, todo.UpdatedAt!.Value);
        return local == server
            ? null
            : "is Synced, but differs from the server's todo. Use only id and syncState to copy it, or mark it Pending.";
    }
}
