namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI CollectionView control wrapper for root-level collection interactions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class CollectionView<TScope> : ControlBase<TScope>
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
