using System.Text.Json;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Rules;
using Brinell.TestData;

namespace Brinell.Samples.Todo.TestSupport;

/// <summary>
/// The Todo test sets (<c>TestData/*.json</c>) and the rules they follow, for the xUnit gate and
/// the <c>brinell-data</c> tool.
/// </summary>
public sealed class TodoTestDataDefinition : TestDataDefinition<TodoTestSetFile>
{
    /// <inheritdoc />
    public override string Name => "Todo";

    /// <inheritdoc />
    public override JsonSerializerOptions JsonOptions => TodoTestSet.JsonOptions;

    /// <inheritdoc />
    public override IReadOnlyList<Type> TableTypes => [typeof(TodoDto), typeof(LocalTodoData)];

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
        .ForEach("local", set => set.Local, (set, todo) => SyncedButDifferent(set, todo));

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
