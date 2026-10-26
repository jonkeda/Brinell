using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Bruinet.TestData;

/// <summary>What the types of a test set look like on the wire: their fields, for suggestions and <c>describe</c>.</summary>
public static class TestDataTypes
{
    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(string)] = "string",
        [typeof(bool)] = "bool",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
    };

    /// <summary>The fields of <paramref name="type"/> as a file writes them.</summary>
    public static IReadOnlyList<TestDataField> FieldsOf(JsonSerializerOptions options, Type type)
    {
        var info = options.GetTypeInfo(type);
        if (info.Kind != JsonTypeInfoKind.Object)
        {
            return [];
        }

        return info.Properties
            .Select(property =>
            {
                var parameter = property.AssociatedParameter;
                var valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var nullable = parameter?.IsNullable ?? (Nullable.GetUnderlyingType(property.PropertyType) is not null || !property.PropertyType.IsValueType);
                var required = property.IsRequired || parameter is { HasDefaultValue: false };
                return new TestDataField(
                    property.Name,
                    DisplayName(property.PropertyType),
                    required,
                    nullable,
                    valueType.IsEnum ? Enum.GetNames(valueType) : []);
            })
            .ToList();
    }

    /// <summary>The root type, the table types and every type reachable from them through fields and collections.</summary>
    public static IReadOnlyList<Type> Reachable(JsonSerializerOptions options, Type root, IEnumerable<Type> extra)
    {
        var seen = new List<Type>();
        var queue = new Queue<Type>([root, .. extra]);
        while (queue.TryDequeue(out var type))
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (seen.Contains(type) || type.IsPrimitive || type == typeof(string))
            {
                continue;
            }

            seen.Add(type);
            var info = options.GetTypeInfo(type);
            if (info.ElementType is { } element)
            {
                queue.Enqueue(element);
            }

            if (info.Kind == JsonTypeInfoKind.Object)
            {
                foreach (var property in info.Properties)
                {
                    queue.Enqueue(property.PropertyType);
                }
            }
        }

        return seen;
    }

    /// <summary>The JSON field names of the known type whose full name is <paramref name="typeFullName"/>.</summary>
    public static IReadOnlyList<string> JsonNamesOf(JsonSerializerOptions options, IEnumerable<Type> types, string typeFullName)
        => types.FirstOrDefault(t => t.FullName == typeFullName) is { } type
            ? FieldsOf(options, type).Select(field => field.JsonName).ToList()
            : [];

    /// <summary>The candidate closest to <paramref name="wrong"/>, when it is close enough to be a typo.</summary>
    public static string? Suggest(string wrong, IEnumerable<string> candidates)
        => candidates
            .Select(candidate => (candidate, distance: Distance(wrong.ToLowerInvariant(), candidate.ToLowerInvariant())))
            .Where(pair => pair.distance <= Math.Max(2, wrong.Length / 3))
            .OrderBy(pair => pair.distance)
            .Select(pair => pair.candidate)
            .FirstOrDefault();

    private static string DisplayName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return DisplayName(underlying) + "?";
        }

        if (!type.IsGenericType)
        {
            return Keywords.TryGetValue(type, out var keyword) ? keyword : type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(DisplayName))}>";
    }

    private static int Distance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            previous = current;
        }

        return previous[b.Length];
    }
}

/// <summary>One field of a test-data type.</summary>
/// <param name="JsonName">The name a file uses, e.g. <c>dueDate</c>.</param>
/// <param name="TypeName">The .NET type, e.g. <c>DateOnly?</c>.</param>
/// <param name="Required">Whether a file must give it.</param>
/// <param name="Nullable">Whether it may be <c>null</c>.</param>
/// <param name="Values">The allowed names, for an enum.</param>
public sealed record TestDataField(string JsonName, string TypeName, bool Required, bool Nullable, IReadOnlyList<string> Values);
