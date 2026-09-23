namespace Brinell.Maui.Interfaces;

/// <summary>
/// Base scope interface for element finding with self-referencing fluent return.
/// Both pages and containers implement this interface.
/// </summary>
/// <typeparam name="TScope">The scope type itself (self-referencing for fluent returns).</typeparam>
public interface IMauiScope<TScope> : IMauiElementScope
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Gets this scope for fluent chaining.
    /// Pages return themselves. Containers return themselves.
    /// </summary>
    TScope Self { get; }
}
