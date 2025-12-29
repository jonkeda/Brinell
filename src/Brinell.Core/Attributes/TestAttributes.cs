using Xunit.Abstractions;
using Xunit.Sdk;

namespace Brinell.Core.Attributes;

/// <summary>
/// Marks a test as a UI test.
/// </summary>
[TraitDiscoverer("Brinell.Core.Attributes.UITestTraitDiscoverer", "Brinell.Core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class UITestAttribute : Attribute, ITraitAttribute
{
}

/// <summary>
/// Trait discoverer for UITestAttribute.
/// </summary>
public class UITestTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>(TestTraits.Category, TestTraits.UITest);
    }
}

/// <summary>
/// Marks a test as a smoke test.
/// </summary>
[TraitDiscoverer("Brinell.Core.Attributes.SmokeTestTraitDiscoverer", "Brinell.Core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SmokeTestAttribute : Attribute, ITraitAttribute
{
}

/// <summary>
/// Trait discoverer for SmokeTestAttribute.
/// </summary>
public class SmokeTestTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>(TestTraits.Category, TestTraits.Smoke);
        yield return new KeyValuePair<string, string>(TestTraits.Category, TestTraits.UITest);
    }
}

/// <summary>
/// Marks a test for a specific platform.
/// </summary>
[TraitDiscoverer("Brinell.Core.Attributes.PlatformTraitDiscoverer", "Brinell.Core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PlatformAttribute : Attribute, ITraitAttribute
{
    public string Platform { get; }
    
    public PlatformAttribute(string platform)
    {
        Platform = platform;
    }
}

/// <summary>
/// Trait discoverer for PlatformAttribute.
/// </summary>
public class PlatformTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        var platform = traitAttribute.GetNamedArgument<string>("Platform");
        yield return new KeyValuePair<string, string>(TestTraits.Platform, platform);
    }
}

/// <summary>
/// Marks test priority.
/// </summary>
[TraitDiscoverer("Brinell.Core.Attributes.PriorityTraitDiscoverer", "Brinell.Core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PriorityAttribute : Attribute, ITraitAttribute
{
    public string Priority { get; }
    
    public PriorityAttribute(string priority)
    {
        Priority = priority;
    }
}

/// <summary>
/// Trait discoverer for PriorityAttribute.
/// </summary>
public class PriorityTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        var priority = traitAttribute.GetNamedArgument<string>("Priority");
        yield return new KeyValuePair<string, string>(TestTraits.Priority, priority);
    }
}
