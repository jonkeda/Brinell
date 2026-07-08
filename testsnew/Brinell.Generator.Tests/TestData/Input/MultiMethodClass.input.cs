namespace Brinell.Maui.Controls;

/// <summary>
/// Class with multiple Core methods.
/// </summary>
public abstract class MultiClickable<TScope> : Base<TScope>
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
    /// Hovers over the element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void HoverCore(IMauiElement element)
    {
        element.Hover();
    }
}
