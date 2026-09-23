namespace Brinell.Core.Interfaces;

/// <summary>
/// Declares that a <c>*Core</c> method should not produce a generated public member.
/// </summary>
/// <remarks>
/// <para>
/// Brinell.Generator emits a public wrapper for every <c>protected virtual *Core</c> method
/// whose first parameter is the platform element. A few Core methods legitimately should not
/// get one — usually because the generated member would expose a platform element type, or
/// because the generated comparison would be meaningless for the return type.
/// </para>
/// <para>
/// Before this attribute existed the only way to opt out was to drop <c>virtual</c>, which
/// made the method invisible to the generator. That worked, but it was indistinguishable from
/// forgetting the keyword, and it also blocked derived controls from overriding the method.
/// Declaring intent here keeps the method <c>virtual</c> and overridable, and lets the
/// generator treat a silent near-miss as an error rather than a maybe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [SkipGeneration("A public wrapper would leak IMauiElement into the control's API.")]
/// protected virtual IReadOnlyList&lt;IMauiElement&gt;? GetItemElementsCore(IMauiElement? element)
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SkipGenerationAttribute : Attribute
{
    /// <summary>
    /// Creates the attribute with the reason generation is skipped.
    /// </summary>
    /// <param name="reason">
    /// Why this Core method has no generated member. Required: the reason is the whole point
    /// of declaring the intent rather than implying it.
    /// </param>
    public SkipGenerationAttribute(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the reason generation is skipped for this method.
    /// </summary>
    public string Reason { get; }
}
