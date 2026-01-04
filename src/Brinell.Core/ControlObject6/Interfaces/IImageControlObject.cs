namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for image display controls.
/// Provides methods for checking image source, dimensions, and loading state.
/// </summary>
public interface IImageControlObject : IControlObject
{
    /// <summary>
    /// Gets the image source URL or path.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The image source, or null if not set.</returns>
    string? GetSource(int? timeoutMs = null);

    /// <summary>
    /// Checks if the image has a source set.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if source is set.</returns>
    bool HasSource(int? timeoutMs = null);

    /// <summary>
    /// Asserts that the image source matches the expected value.
    /// </summary>
    /// <param name="expected">Expected source value.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertSource(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the image dimensions.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>Tuple of (width, height).</returns>
    (int width, int height) GetDimensions(int? timeoutMs = null);

    /// <summary>
    /// Asserts that the image dimensions match expected values.
    /// </summary>
    /// <param name="expectedWidth">Expected width (or null to skip).</param>
    /// <param name="expectedHeight">Expected height (or null to skip).</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertDimensions(int? expectedWidth, int? expectedHeight, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Checks if the image is still loading.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if loading.</returns>
    bool IsLoading(int? timeoutMs = null);

    /// <summary>
    /// Waits for the image to finish loading.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if loaded within timeout.</returns>
    bool WaitLoaded(int? timeoutMs = null);

    /// <summary>
    /// Asserts that the image has finished loading.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertLoaded(string? message = null, int? timeoutMs = null);
}
