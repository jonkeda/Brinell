using Brinell.Maui.UITests;
using Brinell.Uat;

namespace Brinell.Maui.Uat.Tests.Runtime;

internal sealed class MauiUatRuntime
{
    private readonly UatReflectionRuntime _reflectionRuntime;

    public MauiUatRuntime(AppiumFixture fixture, string configFilePath)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(configFilePath);

        Config = UatConfigParser.ParseFile(configFilePath);
        ValidateConfig(Config, configFilePath);
        _reflectionRuntime = UatReflectionRuntime.FromRoot(fixture);
    }

    public UatConfig Config { get; }

    public string DiscoveryReport => string.Join(Environment.NewLine, _reflectionRuntime.DescribeDiscovery());

    public UatCommandCatalog CreateCommandCatalog()
    {
        return _reflectionRuntime.CreateCommandCatalog();
    }

    private static void ValidateConfig(UatConfig config, string configFilePath)
    {
        if (!config.Runtime.TryGetValue("Target", out var target) ||
            !target.Equals("MAUI", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"UAT config '{configFilePath}' must set Runtime Target to MAUI.");
        }

        if (!config.Runtime.TryGetValue("Fixture", out var fixture) ||
            !fixture.Equals("Appium", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"UAT config '{configFilePath}' must set Runtime Fixture to Appium.");
        }

        if (config.Assemblies.Count == 0)
        {
            throw new InvalidOperationException(
                $"UAT config '{configFilePath}' must register at least one assembly.");
        }
    }
}
