namespace Brinell.WinForms.Interfaces;

/// <summary>
/// Strongly-typed scope interface with CRTP for fluent method chaining.
/// </summary>
/// <typeparam name="TScope">The concrete scope type (CRTP pattern).</typeparam>
public interface IWinFormsScope<TScope> : IWinFormsElementScope
    where TScope : IWinFormsScope<TScope>
{
    /// <summary>
    /// Gets a reference to self for fluent chaining.
    /// </summary>
    TScope Self { get; }
}
