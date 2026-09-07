namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for elements supporting the Value pattern (Windows UI Automation).
/// Implemented by platform-specific elements; a platform without the pattern reports
/// <see cref="SupportsValuePattern"/> as false rather than guessing.
/// </summary>
/// <remarks>
/// This is where a text field's editability actually lives. It was previously read as an
/// attribute named "readonly", "isReadOnly" or "editable" - none of which any platform
/// publishes - so the question always answered false. See
/// <c>.my/GetAttribute/audit-what-getattribute-can-answer.md</c>.
/// </remarks>
public interface IValuePatternElement
{
    /// <summary>
    /// Gets whether the element supports the Value UI Automation pattern.
    /// </summary>
    bool SupportsValuePattern { get; }

    /// <summary>
    /// Gets the value carried by the pattern.
    /// </summary>
    /// <returns>The value, or null when the pattern is unsupported.</returns>
    string? GetValuePattern();

    /// <summary>
    /// Gets whether the pattern reports the value as read-only.
    /// </summary>
    /// <returns>True or false when the pattern answers; null when it is unsupported.</returns>
    bool? IsValuePatternReadOnly();
}
