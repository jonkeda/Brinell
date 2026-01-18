namespace Brinell.Maui.UITests;

/// <summary>
/// Constants for UI test configuration.
/// </summary>
public static class TestConstants
{
    /// <summary>
    /// Default timeout for individual tests in milliseconds.
    /// Tests exceeding this duration will be cancelled.
    /// </summary>
    public const int DefaultTestTimeoutMs = 5_000;
    
    /// <summary>
    /// Short timeout for fast tests in milliseconds.
    /// </summary>
    public const int ShortTestTimeoutMs = 10_000;
    
    /// <summary>
    /// Long timeout for complex tests in milliseconds.
    /// </summary>
    public const int LongTestTimeoutMs = 60_000;
}
