namespace Brinell.Maui.Controls.Buttons;

/// <summary>
/// MAUI ImageButton control for clickable images.
/// Combines clickable behavior with image properties.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class ImageButton<TScope> : Base.ClickableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates an ImageButton control with locator.
    /// </summary>
    public ImageButton(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates an ImageButton control with automation ID.
    /// </summary>
    public ImageButton(IMauiScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    #region Image Properties - Core Methods

    /// <summary>
    /// Gets the source/path of the image from the pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The image source, or null if not available.</returns>
    protected virtual string? GetSourceCore(IMauiElement element)
    {
        var source = element.GetAttribute("Source");
        if (!string.IsNullOrEmpty(source))
            return source;

        source = element.GetAttribute("src");
        if (!string.IsNullOrEmpty(source))
            return source;

        return null;
    }

    /// <summary>
    /// Gets the aspect ratio of the image from the pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The aspect ratio string, or null if not available.</returns>
    protected virtual string? GetAspectCore(IMauiElement element)
    {
        return GetAttributeCore(element, "Aspect");
    }

    #endregion
}
