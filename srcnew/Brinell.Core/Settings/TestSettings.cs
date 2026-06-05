using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Brinell.Core.Settings;

public sealed class TestSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly JsonObject _values;

    public TestSettings(JsonObject? values = null, IReadOnlyList<string>? sources = null)
    {
        _values = values ?? [];
        Sources = sources ?? [];
    }

    public static TestSettings Empty { get; } = new();

    public IReadOnlyList<string> Sources { get; }

    public bool TryGetValue<T>(string path, out T? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!TryGetNode(path, out var node) || node is null)
        {
            value = default;
            return false;
        }

        value = node.Deserialize<T>(SerializerOptions);
        return true;
    }

    public T GetRequired<T>(string path)
    {
        if (!TryGetValue<T>(path, out var value) || value is null)
        {
            throw new InvalidOperationException($"Required test setting '{path}' was not found.");
        }

        return value;
    }

    public bool Contains(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return TryGetNode(path, out _);
    }

    public TestSettings GetSection(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!TryGetNode(path, out var node) || node is not JsonObject section)
        {
            return new TestSettings(sources: Sources);
        }

        return new TestSettings((JsonObject)section.DeepClone(), Sources);
    }

    public T Bind<T>()
        where T : class
    {
        return (T)Bind(typeof(T));
    }

    public object Bind(Type settingsType)
    {
        ArgumentNullException.ThrowIfNull(settingsType);

        if (settingsType == typeof(TestSettings))
        {
            return this;
        }

        var section = settingsType.GetCustomAttribute<TestSettingsSectionAttribute>();
        var settings = section is null
            ? this
            : GetSection(section.Path);

        var result = JsonSerializer.Deserialize(
            settings._values.ToJsonString(),
            settingsType,
            SerializerOptions);

        return result ?? throw new InvalidOperationException(
            $"Test settings could not be bound to '{settingsType.FullName}'.");
    }

    public string ToJsonString(bool indented = false)
    {
        return _values.ToJsonString(new JsonSerializerOptions { WriteIndented = indented });
    }

    private bool TryGetNode(string path, out JsonNode? node)
    {
        node = _values;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (node is not JsonObject current ||
                !TryGetProperty(current, segment, out node))
            {
                node = null;
                return false;
            }
        }

        return true;
    }

    private static bool TryGetProperty(JsonObject obj, string propertyName, out JsonNode? value)
    {
        if (obj.TryGetPropertyValue(propertyName, out value))
        {
            return true;
        }

        foreach (var candidate in obj)
        {
            if (candidate.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = candidate.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
