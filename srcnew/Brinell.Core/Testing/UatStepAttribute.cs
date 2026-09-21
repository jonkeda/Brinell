namespace Brinell.Core.Testing;

/// <summary>
/// Declares a UAT step phrase for the control method it decorates. The execution
/// engine discovers these across Brinell.Core and app control types, so the phrase
/// vocabulary lives next to the method instead of a hand-written catalog.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatStepAttribute : Attribute
{
    /// <param name="keyword">The Given/When/Then the step binds to.</param>
    /// <param name="phrase">The phrase, e.g. <c>I set {control} to {value}</c>.</param>
    public UatStepAttribute(UatEffectiveStepKeyword keyword, string phrase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phrase);
        Keyword = keyword;
        Phrase = phrase.Trim();
    }

    /// <summary>The Given/When/Then the step binds to.</summary>
    public UatEffectiveStepKeyword Keyword { get; }

    /// <summary>The phrase text with <c>{control}</c> and optional value placeholders.</summary>
    public string Phrase { get; }

    /// <summary>
    /// Literal boolean argument passed to the method for phrase variants that carry
    /// no value placeholder (e.g. <c>should be visible</c> vs <c>should not be visible</c>).
    /// </summary>
    public bool Literal { get; init; }

    /// <summary>True when <see cref="Literal"/> is meaningful for this step.</summary>
    public bool HasLiteral { get; init; }

    /// <summary>Optional stable command id suffix; defaults to the method name.</summary>
    public string? CommandId { get; init; }
}
