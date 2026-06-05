using System.Reflection;
using Brinell.Core.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Uat;

public sealed class UatRuntime
{
    private readonly UatReflectionRuntime _reflectionRuntime;

    public UatRuntime(
        object root,
        string configFilePath,
        UatRuntimeValidationOptions? validationOptions = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(configFilePath);

        ConfigFilePath = Path.GetFullPath(configFilePath);
        ConfigDirectory = Path.GetDirectoryName(ConfigFilePath) ?? Directory.GetCurrentDirectory();
        Config = UatConfigParser.ParseFile(ConfigFilePath, root.GetType().Assembly.GetName().Name);
        ValidateConfig(Config, ConfigFilePath, validationOptions ?? UatRuntimeValidationOptions.Default);
        Composition = ResolveComposition(root);
        _reflectionRuntime = Composition is null
            ? UatReflectionRuntime.FromRoot(root)
            : UatReflectionRuntime.FromComposition(root, Composition);
    }

    public string ConfigFilePath { get; }

    public string ConfigDirectory { get; }

    public UatConfig Config { get; }

    public TestComposition? Composition { get; }

    public string DiscoveryReport => string.Join(Environment.NewLine, _reflectionRuntime.DescribeDiscovery());

    public UatCommandCatalog CreateCommandCatalog()
    {
        return _reflectionRuntime.CreateCommandCatalog();
    }

    public IServiceScope? CreateScope()
    {
        return Composition?.CreateScope();
    }

    public static void ConfigureScope(UatExecutionContext context, IServiceProvider serviceProvider)
    {
        UatReflectionRuntime.SetServiceProvider(context, serviceProvider);
    }

    private static TestComposition? ResolveComposition(object root)
    {
        if (root is null)
        {
            return null;
        }

        var properties = root.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var property = FindCompositionProperty(properties, "Composition") ??
                       FindCompositionProperty(properties, "TestComposition");

        return property?.GetValue(root) as TestComposition;
    }

    private static PropertyInfo? FindCompositionProperty(
        IEnumerable<PropertyInfo> properties,
        string propertyName)
    {
        return properties.FirstOrDefault(property =>
            property.Name.Equals(propertyName, StringComparison.Ordinal) &&
            property.GetIndexParameters().Length == 0 &&
            typeof(TestComposition).IsAssignableFrom(property.PropertyType));
    }

    private static void ValidateConfig(
        UatConfig config,
        string configFilePath,
        UatRuntimeValidationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Target) &&
            (!config.Runtime.TryGetValue("Target", out var target) ||
             !target.Equals(options.Target, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"UAT config '{configFilePath}' must set Runtime Target to {options.Target}.");
        }

        if (!string.IsNullOrWhiteSpace(options.Fixture) &&
            (!config.Runtime.TryGetValue("Fixture", out var fixture) ||
             !fixture.Equals(options.Fixture, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"UAT config '{configFilePath}' must set Runtime Fixture to {options.Fixture}.");
        }

        if (options.RequireAssemblies && config.Assemblies.Count == 0)
        {
            throw new InvalidOperationException(
                $"UAT config '{configFilePath}' must register at least one assembly.");
        }
    }
}

public sealed record UatRuntimeValidationOptions(
    string? Target = null,
    string? Fixture = null,
    bool RequireAssemblies = true)
{
    public static UatRuntimeValidationOptions Default { get; } = new();
}
