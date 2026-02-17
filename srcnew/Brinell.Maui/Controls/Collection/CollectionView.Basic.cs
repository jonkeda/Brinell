namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI CollectionView control wrapper for root-level collection interactions.
/// Inherits scroll capability since CollectionView is inherently scrollable.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class CollectionView<TScope> : ScrollableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a CollectionView control using an explicit locator.
    /// </summary>
    public CollectionView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a CollectionView control using the scope default locator strategy.
    /// </summary>
    public CollectionView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}
