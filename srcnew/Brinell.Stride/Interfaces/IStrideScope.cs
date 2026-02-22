using Brinell.Core.Interfaces;

namespace Brinell.Stride.Interfaces;

/// <summary>
/// Stride-specific element scope providing access to the test context
/// and self-referencing fluent return type.
/// Parallel to IMauiScope&lt;TScope&gt; but for Stride pipe-based communication.
/// </summary>
/// <typeparam name="TScope">The scope type itself (self-referencing for fluent returns).</typeparam>
public interface IStrideScope<TScope> : IElementScope
    where TScope : IStrideScope<TScope>
{
    /// <summary>
    /// Gets this scope for fluent chaining.
    /// Pages return themselves. Containers return themselves.
    /// </summary>
    TScope Self { get; }

    /// <summary>
    /// Gets the Stride test context for this scope.
    /// </summary>
    IStrideTestContext StrideContext { get; }
}
