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
public sealed class UatPhraseAttribute : Attribute
{
    public UatPhraseAttribute(string phrase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phrase);
        Phrase = phrase.Trim();
    }

    public UatPhraseAttribute(UatEffectiveStepKeyword keyword, string phrase)
        : this(phrase)
    {
        Keyword = keyword;
    }

    public string Phrase { get; }

    public UatEffectiveStepKeyword? Keyword { get; }
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
