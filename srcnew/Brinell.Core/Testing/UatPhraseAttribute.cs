namespace Brinell.Core.Testing;

/// <summary>
/// Base of the UAT step attributes <see cref="UatGivenAttribute"/>, <see cref="UatWhenAttribute"/>
/// and <see cref="UatThenAttribute"/>. On a control method the step is a control verb; on a
/// fixture or phrase-class method it is a phrase. See AD-011.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class UatPhraseAttribute : Attribute
{
    /// <param name="keyword">The Given/When/Then the step binds to.</param>
    /// <param name="phrase">The phrase, e.g. <c>I set {control} to {value}</c>.</param>
    protected UatPhraseAttribute(UatEffectiveStepKeyword keyword, string phrase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phrase);
        Keyword = keyword;
        Phrase = phrase.Trim();
    }

    /// <summary>The Given/When/Then the step binds to.</summary>
    public UatEffectiveStepKeyword Keyword { get; }

    /// <summary>The phrase text, with <c>{control}</c> and value placeholders on a control verb.</summary>
    public string Phrase { get; }

    /// <summary>
    /// Literal boolean argument passed to a control verb for phrase variants that carry
    /// no value placeholder (e.g. <c>should be visible</c> vs <c>should not be visible</c>).
    /// </summary>
    public bool Literal { get; init; }

    /// <summary>True when <see cref="Literal"/> is meaningful for this control verb.</summary>
    public bool HasLiteral { get; init; }

    /// <summary>Optional stable command id suffix for a control verb; defaults to the method name.</summary>
    public string? CommandId { get; init; }
}
