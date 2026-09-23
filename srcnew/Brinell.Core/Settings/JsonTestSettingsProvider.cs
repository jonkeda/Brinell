using System.Text.Json.Nodes;

namespace Brinell.Core.Settings;

public sealed class JsonTestSettingsProvider : ITestSettingsProvider
{
    public TestSettings Resolve(TestSettingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SettingsRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DefaultFile);

        var rootDirectory = ResolveRootDirectory(request.ProjectDirectory, request.SettingsRoot);
        JsonObject values = [];
        List<string> sources = [];
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);

        LoadIfExists(rootDirectory, request.DefaultFile, values, sources, visited);
        if (!string.IsNullOrWhiteSpace(request.LocalFile))
        {
            LoadIfExists(rootDirectory, request.LocalFile, values, sources, visited);
        }

        if (!string.IsNullOrWhiteSpace(request.ScenarioId) &&
            !string.IsNullOrWhiteSpace(request.ScenarioConvention))
        {
            LoadIfExists(
                rootDirectory,
                request.ScenarioConvention.Replace("{ScenarioId}", request.ScenarioId, StringComparison.OrdinalIgnoreCase),
                values,
                sources,
                visited);
        }

        foreach (var explicitFile in request.ExplicitFiles ?? [])
        {
            LoadRequired(rootDirectory, explicitFile, values, sources, visited);
        }

        return new TestSettings(values, sources);
    }

    private static string ResolveRootDirectory(string projectDirectory, string settingsRoot)
    {
        return Path.GetFullPath(
            Path.IsPathRooted(settingsRoot)
                ? settingsRoot
                : Path.Combine(projectDirectory, settingsRoot));
    }

    private static void LoadIfExists(
        string rootDirectory,
        string relativeOrAbsolutePath,
        JsonObject target,
        ICollection<string> sources,
        ISet<string> visited)
    {
        var filePath = ResolveFilePath(rootDirectory, relativeOrAbsolutePath);
        if (!File.Exists(filePath))
        {
            return;
        }

        LoadFile(rootDirectory, filePath, target, sources, visited);
    }

    private static void LoadRequired(
        string rootDirectory,
        string relativeOrAbsolutePath,
        JsonObject target,
        ICollection<string> sources,
        ISet<string> visited)
    {
        var filePath = ResolveFilePath(rootDirectory, relativeOrAbsolutePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Required test settings file '{filePath}' was not found.", filePath);
        }

        LoadFile(rootDirectory, filePath, target, sources, visited);
    }

    private static string ResolveFilePath(string rootDirectory, string relativeOrAbsolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeOrAbsolutePath);
        return Path.GetFullPath(
            Path.IsPathRooted(relativeOrAbsolutePath)
                ? relativeOrAbsolutePath
                : Path.Combine(rootDirectory, relativeOrAbsolutePath));
    }

    private static void LoadFile(
        string rootDirectory,
        string filePath,
        JsonObject target,
        ICollection<string> sources,
        ISet<string> visited)
    {
        if (!visited.Add(filePath))
        {
            return;
        }

        var extension = Path.GetExtension(filePath);
        if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Test settings file '{filePath}' uses '{extension}'. JSON is supported in this slice; YAML can be added later.");
        }

        var node = JsonNode.Parse(File.ReadAllText(filePath)) as JsonObject
            ?? throw new InvalidOperationException($"Test settings file '{filePath}' must contain a JSON object.");

        sources.Add(filePath);

        if (node.TryGetPropertyValue("settings", out var settingsNode) &&
            settingsNode is JsonObject settingsObject)
        {
            MergeObject(target, settingsObject);
        }

        if (node.TryGetPropertyValue("include", out var includeNode) &&
            includeNode is JsonArray includes)
        {
            foreach (var include in includes)
            {
                var includePath = include?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(includePath))
                {
                    LoadRequired(rootDirectory, includePath, target, sources, visited);
                }
            }
        }
    }

    private static void MergeObject(JsonObject target, JsonObject incoming)
    {
        foreach (var property in incoming)
        {
            var targetKey = FindExistingKey(target, property.Key) ?? property.Key;
            if (property.Value is JsonObject incomingObject &&
                target[targetKey] is JsonObject targetObject)
            {
                MergeObject(targetObject, incomingObject);
                continue;
            }

            target[targetKey] = property.Value?.DeepClone();
        }
    }

    private static string? FindExistingKey(JsonObject obj, string key)
    {
        foreach (var property in obj)
        {
            if (property.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return property.Key;
            }
        }

        return null;
    }
}
