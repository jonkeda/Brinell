namespace Brinell.Core.Testing;

/// <summary>Declares a <c>Given</c> UAT step on the method it decorates.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatGivenAttribute : UatPhraseAttribute
{
    /// <param name="phrase">The phrase, e.g. <c>the server also has todos</c>.</param>
    public UatGivenAttribute(string phrase)
        : base(UatEffectiveStepKeyword.Given, phrase)
    {
    }

    /// <summary>A <c>Given</c> step whose phrase is the method name, with parameters in capitals.</summary>
    public UatGivenAttribute()
        : base(UatEffectiveStepKeyword.Given, string.Empty)
    {
    }
}

/// <summary>Declares a <c>When</c> UAT step on the method it decorates.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatWhenAttribute : UatPhraseAttribute
{
    /// <param name="phrase">The phrase, e.g. <c>I set {control} to {value}</c>.</param>
    public UatWhenAttribute(string phrase)
        : base(UatEffectiveStepKeyword.When, phrase)
    {
    }

    /// <summary>A <c>When</c> step whose phrase is the method name, with parameters in capitals.</summary>
    public UatWhenAttribute()
        : base(UatEffectiveStepKeyword.When, string.Empty)
    {
    }
}

/// <summary>
/// Declares a <c>Then</c> UAT step on the method it decorates. A check written after
/// <c>And</c> or <c>But</c> takes the keyword before it, so it is declared here too.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatThenAttribute : UatPhraseAttribute
{
    /// <param name="phrase">The phrase, e.g. <c>{control} should be visible</c>.</param>
    public UatThenAttribute(string phrase)
        : base(UatEffectiveStepKeyword.Then, phrase)
    {
    }

    /// <summary>A <c>Then</c> step whose phrase is the method name, with parameters in capitals.</summary>
    public UatThenAttribute()
        : base(UatEffectiveStepKeyword.Then, string.Empty)
    {
    }
}
