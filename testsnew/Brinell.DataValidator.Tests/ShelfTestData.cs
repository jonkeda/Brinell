using System.Text.Json.Serialization;
using Brinell.TestData;

namespace Brinell.DataValidator.Tests;

public sealed record Shelf(
    IReadOnlyList<Book> Books,
    [property: JsonPropertyName("$schema")] string? Schema = null);

public sealed record Book(int Id, string Title, Genre Genre);

[JsonConverter(typeof(JsonStringEnumConverter<Genre>))]
public enum Genre
{
    Novel,
    Poetry,
}

/// <summary>Loaded by the tool from this test assembly, in its own load context.</summary>
public sealed class ShelfTestDataDefinition : TestDataDefinition<Shelf>
{
    public override IReadOnlyList<Type> TableTypes => [typeof(Book)];

    protected override string RelativeFolder => "Samples";

    protected override void DefineRules(TestDataRules<Shelf> rules)
        => rules.UniqueIds("books", shelf => shelf.Books, book => book.Id);
}
