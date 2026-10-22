namespace Brinell.Core.Interfaces;

/// <summary>
/// Generic test context providing typed element finding from driver root.
/// TElement is the platform's native element type.
/// </summary>
public interface ITestContext<TElement> : ITestContext, IElementScope<TElement>
{
}
