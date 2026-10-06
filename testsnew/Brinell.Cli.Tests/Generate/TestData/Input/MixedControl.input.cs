namespace Brinell.Maui.Controls;

/// <summary>
/// Mixed control with an action Core method and an Is*Core state query.
/// </summary>
public abstract class MixedControl<TScope> : Base<TScope>
    where TScope : IScope<TScope>
{
    /// <summary>
    /// Performs click on pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }

    /// <summary>
    /// State query for whether the element is visible.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}
