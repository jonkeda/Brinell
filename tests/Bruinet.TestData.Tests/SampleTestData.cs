using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bruinet.TestData.Tests;

public sealed record SampleSet(
    DateTimeOffset Now,
    IReadOnlyList<SampleItem> Items,
    [property: JsonPropertyName("$schema")] string? Schema = null);

public sealed record SampleItem(int Id, string Title, SampleStatus Status, string? Notes = null);

[JsonConverter(typeof(JsonStringEnumConverter<SampleStatus>))]
public enum SampleStatus
{
    Open,
    Done,
}

public sealed class SampleTestDataDefinition : TestDataDefinition<SampleSet>
{
    public static JsonSerializerOptions Options { get; } = TestDataJson.Strict(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public override JsonSerializerOptions JsonOptions => Options;

    public override IReadOnlyList<Type> TableTypes => [typeof(SampleItem)];

    protected override string RelativeFolder => "Samples";

    protected override void DefineRules(TestDataRules<SampleSet> rules) => rules
        .UniqueIds("items", set => set.Items, item => item.Id)
        .Require("items", set => set.Items, item => item.Title.Length <= 10, "title is longer than 10 characters.")
        .ForEach("items", set => set.Items, item => item.Title == "boom" ? throw new InvalidOperationException("kaboom") : null);
}
