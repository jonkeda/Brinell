namespace Brinell.Maui.Controls.Buttons;

/// <summary>
/// MAUI ImageButton control for clickable images.
/// Combines clickable behavior with image properties.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class ImageButton<TScope> : ClickableControlBase<TScope>
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

    #region Image Properties

    /// <summary>
    /// Gets the source/path of the image.
    /// </summary>
    /// <returns>The image source, or null if not available.</returns>
    public string? GetSource(int? timeoutMs = null)
    {
        return RunGetWithElement(
         element => {
             var source = element.GetAttribute("Source");
             if (!string.IsNullOrEmpty(source))
                 return source;

             source = element.GetAttribute("src");
             if (!string.IsNullOrEmpty(source))
                 return source;

             return null;

         }, timeoutMs);
    }

    /// <summary>
    /// Checks if the image button is pressed.
    /// </summary>
    /// <returns>True if pressed, false otherwise, null if unknown.</returns>
    public bool? IsPressed(int? timeoutMs = null)
    {
        return RunGetWithElement(element =>
        {
            var attr = element.GetAttribute("IsPressed");
            if (!string.IsNullOrEmpty(attr))
            {
                return attr.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }, timeoutMs);
    }

    /// <summary>
    /// Gets the aspect ratio of the image.
    /// </summary>
    /// <returns>The aspect ratio string, or null if not available.</returns>
    public string? GetAspect(int? timeoutMs)
    {
        return GetAttribute("Aspect", timeoutMs);
    }

    #endregion
}
