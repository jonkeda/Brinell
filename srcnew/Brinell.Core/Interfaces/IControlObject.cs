namespace Brinell.Core.Interfaces;

/// <summary>
/// Base interface for all controls in the Brinell framework.
/// Provides identity, state querying, waiting, and assertion capabilities.
/// Action methods return TScope for fluent method chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public interface IControlObject<TScope> : IElementObject<TScope>
{
    // Attributes

    /// <summary>
    /// Get an attribute value from the element.
    /// Returns null if attribute or element doesn't exist.
    /// </summary>
    string? GetAttribute(string name, int? timeoutMs);
}
