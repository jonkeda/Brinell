namespace Bruinet.Uat;

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
