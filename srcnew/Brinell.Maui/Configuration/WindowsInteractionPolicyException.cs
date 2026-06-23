namespace Brinell.Maui.Configuration;

/// <summary>
/// Thrown when a Windows automation action is blocked by the configured interaction policy.
/// </summary>
public sealed class WindowsInteractionPolicyException : InvalidOperationException
{
    public WindowsInteractionPolicyException(string message)
        : base(message)
    {
    }
}
