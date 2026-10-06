using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Brinell.TestData;

/// <summary>The JSON Schema of a definition's test-set files, for editors.</summary>
public static class TestDataSchema
{
    /// <summary>The conventional schema file name next to the test sets.</summary>
    public const string FileName = "testdata.schema.json";

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>The schema of <paramref name="definition"/>'s set type, as indented JSON.</summary>
    public static string Export(ITestDataDefinition definition)
    {
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
            definition.JsonOptions,
            definition.SetType,
            new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true });

        if (schema is JsonObject root)
        {
            root.Insert(0, "$schema", "https://json-schema.org/draft/2020-12/schema");
            root.Insert(1, "title", $"{definition.Name} test set");
        }

        return schema.ToJsonString(WriteOptions).ReplaceLineEndings("\n") + "\n";
    }

    /// <summary>Where the schema file of <paramref name="definition"/> belongs.</summary>
    public static string PathOf(ITestDataDefinition definition) => Path.Combine(definition.Folder, FileName);
}
