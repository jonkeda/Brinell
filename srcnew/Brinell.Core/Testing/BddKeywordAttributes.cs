namespace Brinell.Core.Testing;

/// <summary>Declares a <c>Given</c> UAT step on the method it decorates.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class BddGivenAttribute : BddStepAttribute
{
    /// <param name="phrase">The phrase, e.g. <c>the server also has todos</c>.</param>
    public BddGivenAttribute(string phrase)
        : base(UatEffectiveStepKeyword.Given, phrase)
    {
    }

    /// <summary>A <c>Given</c> step whose phrase is the method name, with parameters in capitals.</summary>
    public BddGivenAttribute()
        : base(UatEffectiveStepKeyword.Given, string.Empty)
    {
    }
}

/// <summary>Declares a <c>When</c> UAT step on the method it decorates.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class BddWhenAttribute : BddStepAttribute
{
    /// <param name="phrase">The phrase, e.g. <c>I set {control} to {value}</c>.</param>
    public BddWhenAttribute(string phrase)
        : base(UatEffectiveStepKeyword.When, phrase)
    {
    }

    /// <summary>A <c>When</c> step whose phrase is the method name, with parameters in capitals.</summary>
    public BddWhenAttribute()
        : base(UatEffectiveStepKeyword.When, string.Empty)
    {
    }
}

/// <summary>
/// Declares a <c>Then</c> UAT step on the method it decorates. A check written after
/// <c>And</c> or <c>But</c> takes the keyword before it, so it is declared here too.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class BddThenAttribute : BddStepAttribute
{
    /// <param name="phrase">The phrase, e.g. <c>{control} should be visible</c>.</param>
    public BddThenAttribute(string phrase)
        : base(UatEffectiveStepKeyword.Then, phrase)
    {
    }

    /// <summary>A <c>Then</c> step whose phrase is the method name, with parameters in capitals.</summary>
    public BddThenAttribute()
        : base(UatEffectiveStepKeyword.Then, string.Empty)
    {
    }
}
