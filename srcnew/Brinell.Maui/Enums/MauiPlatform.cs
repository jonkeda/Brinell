namespace Brinell.Maui.Enums;

/// <summary>
/// Represents the target platform for MAUI tests.
/// </summary>
public enum MauiPlatform
{
    /// <summary>
    /// Windows platform (WinUI/WinAppDriver).
    /// </summary>
    Windows,
    
    /// <summary>
    /// Android platform (UiAutomator2).
    /// </summary>
    Android,
    
    /// <summary>
    /// iOS platform (XCUITest).
    /// </summary>
    iOS
}
