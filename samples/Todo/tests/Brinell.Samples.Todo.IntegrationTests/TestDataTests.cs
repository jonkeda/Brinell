using System.Text.Json;
using Brinell.Samples.Todo.Core.Rules;
using Brinell.Samples.Todo.Infrastructure.Api;

namespace Brinell.Samples.Todo.IntegrationTests;

/// <summary>
/// Keeps <c>TestSupport/TestData/*.json</c> loadable and consistent, so a UI test never fails
/// because its starting data was wrong.
/// </summary>
[Trait("Module", "Todo")]
[Trait("Pyramid", "Integration")]
public sealed class TestDataTests
{
    public static TheoryData<string> TestSets => new(TodoTestSet.Names);

    [Fact]
    public void TheTestSets_AreFound()
        => Assert.Contains("three-todos", TodoTestSet.Names);

    [Theory]
    [MemberData(nameof(TestSets))]
    public void TestSet_Loads(string name)
        => Assert.Equal(name, TodoTestSet.Load(name).Name);

    [Theory]
    [MemberData(nameof(TestSets))]
    public void TestSet_HasUniqueIds(string name)
    {
        var testSet = TodoTestSet.Load(name);

        AssertUnique(testSet.Server.Select(todo => todo.Id), $"{name}: server");
        AssertUnique(testSet.Local.Select(todo => todo.Id), $"{name}: local");
    }

    [Theory]
    [MemberData(nameof(TestSets))]
    public void TestSet_NeverUpdatesBeforeItCreates(string name)
    {
        var testSet = TodoTestSet.Load(name);

        var wrong = testSet.Server.Where(todo => todo.UpdatedAt < todo.CreatedAt).Select(todo => $"server {todo.Id}")
            .Concat(testSet.Local.Where(todo => todo.UpdatedAt < todo.CreatedAt).Select(todo => $"local {todo.Id}"))
            .ToList();

        Assert.True(wrong.Count == 0, $"{name}: updatedAt is before createdAt for {string.Join(", ", wrong)}.");
    }

    [Theory]
    [MemberData(nameof(TestSets))]
    public void TestSet_HasOnlyTitlesTheAppAccepts(string name)
    {
        var testSet = TodoTestSet.Load(name);

        var refused = testSet.Server.Select(todo => (todo.Id, todo.Title))
            .Concat(testSet.Local.Select(todo => (todo.Id, todo.Title)))
            .Select(todo => (todo.Id, Error: TodoValidation.ValidateTitle(todo.Title)))
            .Where(todo => todo.Error is not null)
            .Select(todo => $"{todo.Id}: {todo.Error}")
            .ToList();

        Assert.True(refused.Count == 0, $"{name}: {string.Join("; ", refused)}");
    }

    [Theory]
    [MemberData(nameof(TestSets))]
    public void TestSet_SyncedLocalTodos_MatchTheServer(string name)
    {
        var testSet = TodoTestSet.Load(name);
        var server = testSet.Server.ToDictionary(todo => todo.Id);

        foreach (var local in testSet.Local.Where(todo => todo.SyncState == SyncState.Synced))
        {
            Assert.True(server.TryGetValue(local.Id, out var dto), $"{name}: local {local.Id} is Synced, but the server does not have it.");
            Assert.Equal(TodoMapping.ToItem(dto) with { SyncState = SyncState.Synced, IsDeleted = local.IsDeleted }, local);
        }
    }

    [Fact]
    public void AnUnknownField_IsRefused()
    {
        const string json = """
            { "now": "2026-03-10T09:00:00Z",
              "server": [ { "id": "11111111-0000-0000-0000-000000000001", "titel": "Buy milk", "notes": null, "dueDate": null,
                            "status": "Open", "createdAt": "2026-03-01T08:00:00Z", "updatedAt": "2026-03-01T08:00:00Z" } ],
              "local": [] }
            """;

        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TodoTestSetFile>(json, TodoTestSet.JsonOptions));
        Assert.Contains("titel", exception.Message);
    }

    [Fact]
    public void AMissingRequiredField_IsRefused()
    {
        const string json = """
            { "now": "2026-03-10T09:00:00Z",
              "server": [ { "id": "11111111-0000-0000-0000-000000000001", "notes": null, "dueDate": null,
                            "status": "Open", "createdAt": "2026-03-01T08:00:00Z", "updatedAt": "2026-03-01T08:00:00Z" } ],
              "local": [] }
            """;

        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TodoTestSetFile>(json, TodoTestSet.JsonOptions));
        Assert.Contains("title", exception.Message);
    }

    [Fact]
    public void AShortcutToAMissingServerTodo_IsRefused()
    {
        var local = new LocalTodoData(Guid.Parse("11111111-0000-0000-0000-000000000009"), SyncState.Synced);

        var exception = Assert.Throws<InvalidDataException>(() => local.ToItem([], "example"));
        Assert.Contains("no todo with this id", exception.Message);
    }

    private static void AssertUnique(IEnumerable<Guid> ids, string where)
    {
        var duplicates = ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        Assert.True(duplicates.Count == 0, $"{where} has duplicate ids: {string.Join(", ", duplicates)}.");
    }
}
