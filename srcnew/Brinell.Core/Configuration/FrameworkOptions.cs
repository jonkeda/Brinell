namespace Brinell.Core.Configuration;

/// <summary>
/// Framework-level configuration options.
/// </summary>
public class FrameworkOptions
{
    /// <summary>
    /// Windows desktop automation interaction policy.
    /// </summary>
    public WindowsInteractionConfig WindowsInteraction { get; set; } = new();

    /// <summary>
    /// Automation framework settings.
    /// </summary>
    public AutomationOptions Automation { get; set; } = new();
}
