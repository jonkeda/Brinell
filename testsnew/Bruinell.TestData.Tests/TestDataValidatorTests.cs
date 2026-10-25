using System.Text.Json.Nodes;
using Xunit;

namespace Bruinell.TestData.Tests;

public sealed class TestDataValidatorTests : IDisposable
{
    private readonly SampleTestDataDefinition _definition = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bruinell", "TestDataTests", Guid.NewGuid().ToString("N"));

    public TestDataValidatorTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void TheFolder_IsFoundNextToTheDefinitionsSourceFile()
    {
        Assert.Equal("Samples", Path.GetFileName(_definition.Folder));
        Assert.True(Directory.Exists(_definition.Folder), _definition.Folder);
        Assert.DoesNotContain($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", _definition.Folder);
        Assert.Equal(_definition.ProjectDirectory, Path.GetDirectoryName(_definition.Folder));
        Assert.Equal(["two-items"], _definition.TestSetNames);
    }

    [Fact]
    public void AValidFolder_HasNoDiagnostics()
    {
        var report = TestDataValidator.Validate(_definition);

        Assert.True(report.IsValid, report.Format());
    }

    [Fact]
    public void AnUnknownField_IsReportedWithItsLineAndASuggestion()
    {
        var diagnostic = Single("""
            {
              "now": "2026-03-10T09:00:00Z",
              "items": [
                { "id": 1, "titel": "First", "status": "Open" }
              ]
            }
            """);

        Assert.Equal(TestDataCodes.UnknownField, diagnostic.Code);
        Assert.Equal(4, diagnostic.Line);
        Assert.Contains("'titel'", diagnostic.Message);
        Assert.Contains("Did you mean 'title'?", diagnostic.Message);
    }

    [Fact]
    public void AMissingField_IsReported()
    {
        var diagnostic = Single("""
            { "now": "2026-03-10T09:00:00Z", "items": [ { "id": 1, "status": "Open" } ] }
            """);

        Assert.Equal(TestDataCodes.MissingField, diagnostic.Code);
        Assert.Contains("title", diagnostic.Message);
    }

    [Fact]
    public void AWrongEnumName_ListsTheAllowedNames()
    {
        var diagnostic = Single("""
            { "now": "2026-03-10T09:00:00Z", "items": [ { "id": 1, "title": "A", "status": "Opne" } ] }
            """);

        Assert.Equal(TestDataCodes.WrongValue, diagnostic.Code);
        Assert.Contains("Open, Done", diagnostic.Message);
    }

    [Fact]
    public void ANullWhereTheTypeHasNone_IsReported()
    {
        var diagnostic = Single("""
            { "now": "2026-03-10T09:00:00Z", "items": [ { "id": 1, "title": null, "status": "Open" } ] }
            """);

        Assert.Equal(TestDataCodes.WrongValue, diagnostic.Code);
    }

    [Fact]
    public void ADuplicateId_PointsAtTheSecondItemsLine()
    {
        var diagnostic = Single("""
            {
              "now": "2026-03-10T09:00:00Z",
              "items": [
                { "id": 1, "title": "A", "status": "Open" },
                { "id": 1, "title": "B", "status": "Open" }
              ]
            }
            """);

        Assert.Equal(TestDataCodes.DuplicateId, diagnostic.Code);
        Assert.Equal("items[1]", diagnostic.Path);
        Assert.Equal(5, diagnostic.Line);
        Assert.Contains("items[0]", diagnostic.Message);
    }

    [Fact]
    public void ARuleFailure_IsReportedAtItsItem()
    {
        var diagnostic = Single("""
            {
              "now": "2026-03-10T09:00:00Z",
              "items": [
                { "id": 1, "title": "A", "status": "Open" },

                { "id": 2, "title": "Much too long a title", "status": "Open" }
              ]
            }
            """);

        Assert.Equal(TestDataCodes.Rule, diagnostic.Code);
        Assert.Equal(6, diagnostic.Line);
        Assert.Equal("path:6: DATA010 items[1]: title is longer than 10 characters.", diagnostic.ToString().Replace(diagnostic.File, "path"));
    }

    [Fact]
    public void ARuleThatThrows_IsReportedInsteadOfCrashing()
    {
        var diagnostic = Single("""
            { "now": "2026-03-10T09:00:00Z", "items": [ { "id": 1, "title": "boom", "status": "Open" } ] }
            """);

        Assert.Equal(TestDataCodes.RuleCrashed, diagnostic.Code);
        Assert.Contains("kaboom", diagnostic.Message);
    }

    [Fact]
    public void BrokenJson_IsASyntaxError()
    {
        var diagnostic = Single("""{ "now": "2026-03-10T09:00:00Z", "items": [ """);

        Assert.Equal(TestDataCodes.Syntax, diagnostic.Code);
    }

    [Fact]
    public void TheSchema_ForbidsUnknownFieldsAndNamesTheSet()
    {
        var schema = JsonNode.Parse(TestDataSchema.Export(_definition))!;

        Assert.Equal("Sample test set", (string?)schema["title"]);
        Assert.False((bool?)schema["properties"]!["items"]!["items"]!["additionalProperties"]);
        Assert.Contains("Done", schema["properties"]!["items"]!["items"]!["properties"]!["status"]!.ToJsonString());
    }

    [Fact]
    public void TheFields_SayWhatIsRequiredAndWhichValuesAreAllowed()
    {
        var fields = TestDataTypes.FieldsOf(SampleTestDataDefinition.Options, typeof(SampleItem)).ToDictionary(f => f.JsonName);

        Assert.True(fields["title"].Required);
        Assert.False(fields["title"].Nullable);
        Assert.False(fields["notes"].Required);
        Assert.True(fields["notes"].Nullable);
        Assert.Equal(["Open", "Done"], fields["status"].Values);
    }

    [Theory]
    [InlineData("titel", "title")]
    [InlineData("stauts", "status")]
    [InlineData("nothingLikeIt", null)]
    public void Suggest_FindsTypos(string wrong, string? expected)
        => Assert.Equal(expected, TestDataTypes.Suggest(wrong, ["id", "title", "status", "notes"]));

    private TestDataDiagnostic Single(string json)
    {
        var path = Path.Combine(_folder, "case.json");
        File.WriteAllText(path, json);
        return Assert.Single(TestDataValidator.Validate(_definition, path).Diagnostics);
    }
}
