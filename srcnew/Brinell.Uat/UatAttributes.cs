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

/// <summary>
/// On a page's control property: tapping the control always goes to this page, so the binder knows
/// the page after it without an <c>I should be on ...</c> step.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UatNavigatesToAttribute : Attribute
{
    /// <param name="page">The page object type the control goes to.</param>
    public UatNavigatesToAttribute(Type page)
    {
        ArgumentNullException.ThrowIfNull(page);
        Page = page;
    }

    /// <summary>The page object type the control goes to.</summary>
    public Type Page { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class UatPhraseClassAttribute : TestScenarioServiceAttribute
{
    /// <summary>
    /// The folder under the scenario root whose scenarios, and those below it, see this class's
    /// phrases (<c>Invoicing</c>, <c>Invoicing/Credit notes</c>). Null takes the assembly's
    /// <see cref="UatNamespaceAttribute"/>, and without one every scenario sees them.
    /// </summary>
    public string? Namespace { get; init; }
}

/// <summary>
/// The namespace of every phrase class in the assembly that does not name its own
/// (<see cref="UatPhraseClassAttribute.Namespace"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class UatNamespaceAttribute : Attribute
{
    /// <param name="name">A folder under the scenario root, with <c>/</c> between folders.</param>
    public UatNamespaceAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>The namespace.</summary>
    public string Name { get; }
}

public abstract class UatPhraseClassBase : TestScenarioServiceBase
{
}
