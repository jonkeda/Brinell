namespace Brinell.Core.Interfaces;

/// <summary>
/// Optional platform capability for elements that expose a UI Automation
/// LegacyIAccessible default action.
/// </summary>
public interface ILegacyIAccessiblePatternElement
{
    /// <summary>
    /// Gets whether the element supports the LegacyIAccessible pattern.
    /// </summary>
    bool SupportsLegacyIAccessiblePattern { get; }

    /// <summary>
    /// Performs the element's default LegacyIAccessible action.
    /// </summary>
    /// <returns>True when the pattern was available and the default action was invoked.</returns>
    bool DoDefaultActionPattern();
}
