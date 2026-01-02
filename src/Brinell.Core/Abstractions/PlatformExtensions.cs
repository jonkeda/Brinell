namespace Brinell.Core.Abstractions;

/// <summary>
/// Extension methods for the Platform enum.
/// Implements FR-001.2: Platform Detection.
/// </summary>
public static class PlatformExtensions
{
    /// <summary>
    /// Returns true if the platform is a mobile platform (Android, iOS).
    /// </summary>
    public static bool IsMobile(this Platform platform) =>
        platform is Platform.Android or Platform.iOS;
    
    /// <summary>
    /// Returns true if the platform is a desktop platform (Windows WPF, Windows MAUI).
    /// </summary>
    public static bool IsDesktop(this Platform platform) =>
        platform is Platform.Windows or Platform.WindowsMaui;
    
    /// <summary>
    /// Returns true if the platform is a web platform.
    /// </summary>
    public static bool IsWeb(this Platform platform) =>
        platform == Platform.Web;
    
    /// <summary>
    /// Returns true if the platform is a game engine platform (Stride).
    /// </summary>
    public static bool IsGameEngine(this Platform platform) =>
        platform == Platform.Stride;
    
    /// <summary>
    /// Returns true if the platform supports touch gestures.
    /// </summary>
    public static bool SupportsTouch(this Platform platform) =>
        platform.IsMobile();
    
    /// <summary>
    /// Returns true if the platform uses FlaUI for automation.
    /// Applies to: WPF, WinForms
    /// </summary>
    public static bool UsesFlaUI(this Platform platform) =>
        platform == Platform.Windows;
    
    /// <summary>
    /// Returns true if the platform uses Appium for automation.
    /// Applies to: MAUI Windows, Android, iOS
    /// </summary>
    public static bool UsesAppium(this Platform platform) =>
        platform is Platform.WindowsMaui or Platform.Android or Platform.iOS;
    
    /// <summary>
    /// Returns true if the platform uses Selenium for automation.
    /// </summary>
    public static bool UsesSelenium(this Platform platform) =>
        platform == Platform.Web;
    
    /// <summary>
    /// Returns true if the platform supports async control operations (Playwright).
    /// See AD-009 v3.2 for details.
    /// </summary>
    /// <remarks>
    /// Note: Web platform defaults to Selenium (sync). 
    /// Playwright is in Brinell.Html.Playwright which supports async.
    /// </remarks>
    public static bool SupportsAsyncOperations(this Platform platform) =>
        false; // Playwright uses its own async context, not Platform enum
    
    /// <summary>
    /// Get the display name for the platform.
    /// </summary>
    public static string GetDisplayName(this Platform platform) => platform switch
    {
        Platform.Windows => "Windows (WPF/WinForms)",
        Platform.WindowsMaui => "Windows (MAUI)",
        Platform.Android => "Android",
        Platform.iOS => "iOS",
        Platform.Web => "Web Browser",
        Platform.Stride => "Stride 3D Engine",
        _ => platform.ToString()
    };
    
    /// <summary>
    /// Get the automation library name for the platform.
    /// </summary>
    public static string GetAutomationLibrary(this Platform platform) => platform switch
    {
        Platform.Windows => "FlaUI (UIA3)",
        Platform.WindowsMaui => "Appium",
        Platform.Android => "Appium",
        Platform.iOS => "Appium",
        Platform.Web => "Selenium WebDriver",
        Platform.Stride => "Named Pipes",
        _ => "Unknown"
    };
}
