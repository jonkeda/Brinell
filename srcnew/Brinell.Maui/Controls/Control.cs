namespace Brinell.Maui.Controls;

/// <summary>
/// Generic concrete MAUI control used when no specialized control type is required.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract class Control<TScope> : ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new generic control within the specified scope.
    /// </summary>
    protected Control(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new generic control within the specified scope using a string locator value.
    /// </summary>
    protected Control(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}