using Brinell.Core.Composition;

namespace Brinell.Uat;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UatNameAttribute : Attribute
{
    public UatNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatActionAttribute : Attribute
{
    public UatActionAttribute(string actionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ActionName = actionName.Trim();
    }

    public string ActionName { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UatIgnoreAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class UatPhraseClassAttribute : TestScenarioServiceAttribute
{
}

public abstract class UatPhraseClassBase : TestScenarioServiceBase
{
}
