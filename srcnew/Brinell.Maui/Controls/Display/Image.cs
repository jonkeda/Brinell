namespace Brinell.Maui.Controls.Display;

/// <summary>
/// MAUI Image control for displaying images with source and dimension access.
/// Provides IsLoaded(), GetSource(), GetWidth(), GetHeight(), and image assertions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Image<TScope> : ControlBase<TScope>
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

    /// <summary>
    /// Gets the image source from pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The image source path/URL, or null if not found.</returns>
    protected string? GetSourceCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try various source attributes
        return element.GetAttribute("Source")
            ?? element.GetAttribute("source")
            ?? element.GetAttribute("src");
    }

    /// <summary>
    /// Gets the image source path or URL.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>The source path/URL, or null if not found.</returns>
    public string? GetSource(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        return GetSourceCore(TryFindElement());
    }

    #endregion

    #region IsLoaded - Core Methods

    /// <summary>
    /// Checks if image is loaded using pre-found element.
    /// An image is considered loaded if it has a source and positive dimensions.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if loaded, false otherwise, null if not found.</returns>
    protected bool? IsLoadedCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Check if source is set
        var source = GetSourceCore(element);
        if (string.IsNullOrEmpty(source)) return false;

        // Check if element has positive dimensions
        var size = element.Size;
        return size.Width > 0 && size.Height > 0;
    }

    /// <summary>
    /// Checks if the image has been successfully loaded.
    /// </summary>
    /// <returns>True if loaded, false if not, null if element not found.</returns>
    public bool? IsLoaded()
    {
        return IsLoadedCore(TryFindElement());
    }

    #endregion

    #region Dimensions

    /// <summary>
    /// Gets the rendered width of the image.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>Width in pixels, or null if element not found.</returns>
    public int? GetWidth(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }

        var element = TryFindElement();
        return element?.Size.Width;
    }

    /// <summary>
    /// Gets the rendered height of the image.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>Height in pixels, or null if element not found.</returns>
    public int? GetHeight(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }

        var element = TryFindElement();
        return element?.Size.Height;
    }

    #endregion

    #region WaitLoaded

    /// <summary>
    /// Waits for image to be loaded.
    /// </summary>
    /// <param name="expected">The expected loaded state.</param>
    /// <param name="timeoutMs">Maximum time to wait.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return RunWait(
            () => IsLoaded() == expected.Value,
            timeoutMs);
    }

    #endregion

    #region AssertLoaded

    /// <summary>
    /// Asserts the image is loaded.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertLoaded(string? message = null, int? timeoutMs = null)
        => AssertLoaded(true, message, timeoutMs);

    /// <summary>
    /// Asserts the image loaded state.
    /// </summary>
    /// <param name="expected">The expected loaded state.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(nameof(AssertLoaded), expected, () =>
        {
            WaitLoaded(expected, timeoutMs);
            return IsLoaded();
        }, message ?? $"Expected image {(expected.Value ? "to be loaded" : "not to be loaded")}. Locator: {Locator}");
    }

    /// <summary>
    /// Asserts the image source matches expected.
    /// </summary>
    /// <param name="expected">The expected source path/URL.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertSource(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        var passed = RunWait(() => GetSource() == expected, timeoutMs);

        if (!passed)
        {
            var actual = GetSource();
            throw new AssertionException(
                message ?? $"Expected source '{expected}' but was '{actual}'. Locator: {Locator}");
        }

        return ContainingScope;
    }

    #endregion
}
