namespace Brinell.Maui.Controls;

/// <summary>
/// Simple clickable class with one Core method.
/// </summary>
public abstract class SimpleClickable<TScope> : Base<TScope>
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
}
