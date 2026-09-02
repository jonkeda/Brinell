namespace Brinell.Core.Configuration;

/// <summary>
/// Framework-level configuration options.
/// </summary>
public class FrameworkOptions
{

    /// <summary>
    /// Automation framework settings.
    /// </summary>
    public AutomationOptions Automation { get; set; } = new();
}
