using Brinell.Core.Testing;
using Brinell.Bdd;

namespace Brinell.Samples.Todo.TestSupport.Uat;

/// <summary>
/// Steps that check what the app stored: its SQLite database.
/// </summary>
/// <remarks>
/// Tables bind to the app's <c>TodoItem</c>, listing only the columns that matter; relative dates
/// are resolved against the test set's now. Read the database only after a UI sentinel (the row is
/// on screen), so the app's write has landed. The same steps for every head: each head's fixture is
/// an <see cref="ITodoArrangement"/>.
/// </remarks>
[BddStepDefinitions(Namespace = "StoredData")]
public sealed class StoredDataPhrases(ITodoArrangement fixture) : BddStepDefinitionsBase
{
    /// <summary>The app's database holds a todo like each row.</summary>
    [BddThen("the database should have todos")]
    public void DatabaseHas(UatTable table) => table.AssertContains(TodoDbReader.Todos(fixture.DatabasePath), fixture.TestSet.Now);

    /// <summary>The app's database holds exactly the rows' todos. Order-dependent unless the database is reset per scenario.</summary>
    [BddThen("the database should have only todos")]
    public void DatabaseHasOnly(UatTable table) => table.AssertExactly(TodoDbReader.Todos(fixture.DatabasePath), fixture.TestSet.Now);

    /// <summary>No todo in the app's database has an empty or whitespace title.</summary>
    [BddThen("the database should not have a todo with a blank title")]
    public void DatabaseHasNoBlankTitle()
    {
        var blank = TodoDbReader.Todos(fixture.DatabasePath).Where(todo => string.IsNullOrWhiteSpace(todo.Title)).ToList();
        if (blank.Count > 0)
        {
            throw new InvalidOperationException($"The database holds {blank.Count} todo(s) with a blank title: {string.Join(", ", blank.Select(t => t.Id))}.");
        }
    }
}
