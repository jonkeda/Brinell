using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Core.Composition;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TestModuleScanAttribute : Attribute
{
    public TestModuleScanAttribute(Type assemblyMarkerType)
    {
        AssemblyMarkerType = assemblyMarkerType;
    }

    public Type AssemblyMarkerType { get; }

    public string? NamespacePrefix { get; init; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TestPageAttribute : Attribute
{
    public TestPageAttribute(string? name = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    public string? Name { get; }

    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Scoped;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TestScenarioServiceAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Scoped;
}

public abstract class TestScenarioServiceBase
{
}
