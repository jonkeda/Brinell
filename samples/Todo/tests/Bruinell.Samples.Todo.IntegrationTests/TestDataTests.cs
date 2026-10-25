using System.Text.Json;
using Bruinell.TestData;

namespace Bruinell.Samples.Todo.IntegrationTests;

/// <summary>
/// Keeps <c>TestSupport/TestData/*.json</c> loadable and consistent, so a UI test never fails
/// because its starting data was wrong.
/// </summary>
/// <remarks>
/// The rules live in <see cref="TodoTestDataDefinition"/> and run through
/// <see cref="TestDataValidator"/>, the same code the <c>bruinell data</c> tool runs.
/// </remarks>
[Trait("Module", "Todo")]
[Trait("Pyramid", "Integration")]
public sealed class TestDataTests
{
    private static readonly TodoTestDataDefinition Definition = new();

    public static TheoryData<string> TestSets => new(TodoTestSet.Names);

    [Fact]
    public void TheFilesAndTheEmbeddedTestSets_AreTheSame()
        => Assert.Equal(Definition.TestSetNames, TodoTestSet.Names);

    [Fact]
    public void EveryTestSet_FollowsTheRules()
    {
        var report = TestDataValidator.Validate(Definition);

        Assert.True(report.IsValid, report.Format());
    }

    [Theory]
    [MemberData(nameof(TestSets))]
    public void TestSet_Loads(string name)
        => Assert.Equal(name, TodoTestSet.Load(name).Name);

    [Fact]
    public void TheSchemaFile_IsUpToDate()
    {
        // BRUINELL_UPDATE_SCHEMA=1 rewrites the file instead of failing; bruinell data schema does the same.
        var path = TestDataSchema.PathOf(Definition);
        var expected = TestDataSchema.Export(Definition);
        if (Environment.GetEnvironmentVariable("BRUINELL_UPDATE_SCHEMA") == "1")
        {
            File.WriteAllText(path, expected);
        }

        Assert.True(File.Exists(path), $"{path} is missing. Run with BRUINELL_UPDATE_SCHEMA=1 to create it.");
        Assert.True(
            File.ReadAllText(path).ReplaceLineEndings("\n") == expected,
            $"{path} is out of date with the types. Run with BRUINELL_UPDATE_SCHEMA=1 to regenerate it.");
    }

    [Fact]
    public void ABrokenTestSet_IsReportedWithFileLineAndField()
    {
        var path = Path.Combine(Path.GetTempPath(), $"todo-broken-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
            {
              "now": "2026-03-10T09:00:00Z",
              "server": [
                { "id": "33333333-0000-0000-0000-000000000001", "titel": "Buy milk", "notes": null, "dueDate": null,
                  "status": "Open", "createdAt": "2026-03-01T08:00:00Z", "updatedAt": "2026-03-01T08:00:00Z" }
              ],
              "local": []
            }
            """);
        try
        {
            var diagnostic = Assert.Single(TestDataValidator.Validate(Definition, path).Diagnostics);

            Assert.Equal(TestDataCodes.UnknownField, diagnostic.Code);
            Assert.Equal(4, diagnostic.Line);
            Assert.Contains("Did you mean 'title'?", diagnostic.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ASyncedCopyThatDiffersFromTheServer_IsReported()
    {
        var path = Path.Combine(Path.GetTempPath(), $"todo-drift-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
            {
              "now": "2026-03-10T09:00:00Z",
              "server": [
                { "id": "33333333-0000-0000-0000-000000000001", "title": "Buy milk", "notes": null, "dueDate": null,
                  "status": "Open", "createdAt": "2026-03-01T08:00:00Z", "updatedAt": "2026-03-01T08:00:00Z" }
              ],
              "local": [
                { "id": "33333333-0000-0000-0000-000000000001", "title": "Buy oat milk", "status": "Open",
                  "createdAt": "2026-03-01T08:00:00Z", "updatedAt": "2026-03-01T08:00:00Z", "syncState": "Synced" }
              ]
            }
            """);
        try
        {
            var diagnostic = Assert.Single(TestDataValidator.Validate(Definition, path).Diagnostics);

            Assert.Equal("local[0]", diagnostic.Path);
            Assert.Equal(8, diagnostic.Line);
            Assert.Contains("differs from the server", diagnostic.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AnUnknownField_IsRefusedWhenLoading()
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
    public void AShortcutToAMissingServerTodo_IsRefused()
    {
        var local = new LocalTodoData(Guid.Parse("11111111-0000-0000-0000-000000000009"), SyncState.Synced);

        var exception = Assert.Throws<InvalidDataException>(() => local.ToItem([], "example"));
        Assert.Contains("no todo with this id", exception.Message);
    }
}
