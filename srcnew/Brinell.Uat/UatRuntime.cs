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
        _reflectionRuntime = UatReflectionRuntime.FromRoot(root);
    }

    public string ConfigFilePath { get; }

    public string ConfigDirectory { get; }

    public UatConfig Config { get; }

    public string DiscoveryReport => string.Join(Environment.NewLine, _reflectionRuntime.DescribeDiscovery());

    public UatCommandCatalog CreateCommandCatalog()
    {
        return _reflectionRuntime.CreateCommandCatalog();
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
