namespace Brinell.Maui.Controls.Display;

/// <summary>
/// MAUI Image control for displaying images with source and dimension access.
/// Provides IsLoaded(), GetSource(), GetWidth(), GetHeight(), and image assertions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Image<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new image control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the image element.</param>
    public Image(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new image control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Image(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Source - Core Methods


    #endregion

    #region IsLoaded - Core Methods

    /// <summary>
    /// Checks if image is loaded using pre-found element.
    /// An image is considered loaded if it occupies space.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if loaded, false otherwise, null if not found.</returns>
    protected virtual bool? IsLoadedCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Rendered size is the whole of what can be observed: the source is app state that
        // never reaches the accessibility tree, so an image that occupies space has loaded
        // something and one that does not has not.
        var size = element.Size;
        return size.Width > 0 && size.Height > 0;
    }

    #endregion

    #region Dimensions - Core Methods

    /// <summary>
    /// Gets the rendered width of the image from the pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>Width in pixels, or null if element not found.</returns>
    protected virtual int? GetWidthCore(IMauiElement? element)
    {
        return element?.Size.Width;
    }

    /// <summary>
    /// Gets the rendered height of the image from the pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>Height in pixels, or null if element not found.</returns>
    protected virtual int? GetHeightCore(IMauiElement? element)
    {
        return element?.Size.Height;
    }

    #endregion

    #region Hand-written Convenience Members

    #endregion
}
