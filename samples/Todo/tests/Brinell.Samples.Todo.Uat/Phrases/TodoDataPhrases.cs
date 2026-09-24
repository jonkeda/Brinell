using Brinell.Core.Testing;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.TestSupport;
using Brinell.Samples.Todo.Uat.Runtime;
using Brinell.Uat;

namespace Brinell.Samples.Todo.Uat.Phrases;

/// <summary>
/// Steps that reach past the screen: add todos to the fake server, and check what the server and
/// the app's database hold.
/// </summary>
/// <remarks>
/// Tables bind to the real types (<see cref="TodoDto"/>, the app's <c>TodoItem</c>), listing only
/// the columns that matter; relative dates are resolved against the test set's now. Read the
/// database only after a UI sentinel (the row is on screen), so the app's write has landed.
/// </remarks>
[UatPhraseClass]
public sealed class TodoDataPhrases(TodoUatFixture fixture) : UatPhraseClassBase
{
    /// <summary>Adds the table's todos to the fake server, as another device would.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Given, "the server also has todos")]
    public void ServerAlsoHas(UatTable table)
    {
        var data = new TodoData(fixture.TestSet.Now);
        foreach (var row in table.Bind<ServerTodoRow>(fixture.TestSet.Now))
        {
            fixture.Server.Store(data.Todo(row.Title, row.Status, row.DueDate, row.Notes));
        }
    }

    /// <summary>The fake server holds a todo like each row.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the server should have todos")]
    public void ServerHas(UatTable table) => table.AssertContains(fixture.Server.Todos, fixture.TestSet.Now);

    /// <summary>The app's database holds a todo like each row.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the database should have todos")]
    public void DatabaseHas(UatTable table) => table.AssertContains(TodoDbReader.Todos(fixture.DatabasePath), fixture.TestSet.Now);

    /// <summary>The app's database holds exactly the rows' todos. Order-dependent unless the database is reset per scenario.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the database should have only todos")]
    public void DatabaseHasOnly(UatTable table) => table.AssertExactly(TodoDbReader.Todos(fixture.DatabasePath), fixture.TestSet.Now);

    /// <summary>No todo in the app's database has an empty or whitespace title.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the database should not have a todo with a blank title")]
    public void DatabaseHasNoBlankTitle()
    {
        var blank = TodoDbReader.Todos(fixture.DatabasePath).Where(todo => string.IsNullOrWhiteSpace(todo.Title)).ToList();
        if (blank.Count > 0)
        {
            throw new InvalidOperationException($"The database holds {blank.Count} todo(s) with a blank title: {string.Join(", ", blank.Select(t => t.Id))}.");
        }
    }

    /// <summary>A server todo as a step table writes it.</summary>
    /// <param name="Title">The title.</param>
    /// <param name="Status">The status; Open when left out.</param>
    /// <param name="DueDate">The due date, e.g. <c>+2d</c>; none when left out.</param>
    /// <param name="Notes">Notes; none when left out.</param>
    public sealed record ServerTodoRow(string Title, TodoStatusDto Status = TodoStatusDto.Open, DateOnly? DueDate = null, string? Notes = null);
}
