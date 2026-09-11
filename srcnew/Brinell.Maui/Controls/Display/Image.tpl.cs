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
    /// Whether the image has finished loading a bitmap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This used to be "it occupies space", and that assertion passed for the wrong
    /// reason.</b> A broken image occupies exactly as much space as a working one, because the
    /// layout reserves it either way - so a test asserting an image had loaded was asserting
    /// that MAUI had done arithmetic. The old comment was right that nothing else was
    /// observable; it was observing from outside the app, where the source does not reach.
    /// </para>
    /// <para>
    /// The app answers now where it can. The size check remains for platforms with no bridge,
    /// stated as the weaker thing it is rather than as the definition.
    /// </para>
    /// </remarks>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if loaded, false otherwise, null if not found.</returns>
    protected virtual bool? IsLoadedCore(IMauiElement? element)
    {
        if (element == null) return null;

        if (element.SupportsStateReads)
        {
            // Loaded means: a source to load, and not still loading it. Either alone is the
            // wrong question - an image with no source is never loading and never loaded.
            var hasSource = !string.IsNullOrEmpty(element.ReadState("Source"));
            var loading = bool.TryParse(element.ReadState("IsLoading"), out var busy) && busy;

            return hasSource && !loading;
        }

        var size = element.Size;
        return size.Width > 0 && size.Height > 0;
    }

    /// <summary>
    /// Where the image's bitmap comes from, as the app declared it.
    /// </summary>
    /// <remarks>
    /// Only the app can answer this: an image source never reaches the accessibility tree on any
    /// platform. Null rather than empty where there is no bridge, so "no source" and "cannot
    /// tell" stay distinguishable.
    /// </remarks>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The source, or null when it cannot be read.</returns>
    protected virtual string? GetSourceCore(IMauiElement? element)
        => element is { SupportsStateReads: true } ? element.ReadState("Source") : null;

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
