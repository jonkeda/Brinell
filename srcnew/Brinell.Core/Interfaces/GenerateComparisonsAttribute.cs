namespace Brinell.Core.Interfaces;

/// <summary>
/// Comparison variants a generated Assert/Wait family can emit for a
/// <c>Get*Core</c> query.
/// </summary>
[Flags]
public enum Comparison
{
    /// <summary>Exact equality — <c>AssertText</c>, <c>WaitText</c>.</summary>
    Equals = 1,

    /// <summary>Substring containment — <c>AssertTextContains</c>, <c>WaitTextContains</c>.</summary>
    Contains = 2,

    /// <summary>Prefix match — <c>AssertTextStartsWith</c>.</summary>
    StartsWith = 4,

    /// <summary>Suffix match — <c>AssertTextEndsWith</c>.</summary>
    EndsWith = 8,

    /// <summary>Empty / non-empty check — <c>AssertTextEmpty</c>.</summary>
    Empty = 16
}

/// <summary>
/// Declares which comparison variants Brinell.Generator should emit for a
/// <c>Get*Core</c> method. Without this attribute only <see cref="Comparison.Equals"/>
/// is generated.
/// </summary>
/// <example>
/// <code>
/// [GenerateComparisons(Comparison.Equals | Comparison.Contains)]
/// protected virtual string? GetTextCore(IMauiElement element) => element.Text;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GenerateComparisonsAttribute : Attribute
{
    /// <summary>
    /// Creates the attribute with the comparison variants to generate.
    /// </summary>
    /// <param name="comparisons">The variants to emit.</param>
    public GenerateComparisonsAttribute(Comparison comparisons)
    {
        Comparisons = comparisons;
    }

    /// <summary>
    /// Gets the comparison variants to generate.
    /// </summary>
    public Comparison Comparisons { get; }
}
