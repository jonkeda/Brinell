using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Bruinell.TestData;

/// <summary>Serializer settings for test-set files.</summary>
public static class TestDataJson
{
    /// <summary>
    /// The web defaults (camelCase), comments allowed, and strict: an unknown field, a missing
    /// required constructor parameter or a <c>null</c> where the type has none is an error.
    /// </summary>
    public static JsonSerializerOptions StrictOptions { get; } = Strict(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    /// <summary>A strict copy of <paramref name="baseOptions"/>, e.g. an app's own wire settings.</summary>
    public static JsonSerializerOptions Strict(JsonSerializerOptions baseOptions) => new(baseOptions)
    {
        TypeInfoResolver = baseOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver(),
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };
}
