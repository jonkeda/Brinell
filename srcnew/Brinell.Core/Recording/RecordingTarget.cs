using System.Drawing;

namespace Brinell.Core.Recording;

/// <summary>
/// What a screen recorder records: a window, a monitor, a browser page, a device. Handles, strings
/// and an opaque page, so no platform type reaches Brinell.Core.
/// </summary>
public abstract record RecordingTarget
{
    private RecordingTarget()
    {
    }

    /// <summary>A window from its handle as a driver writes it (a decimal number); null for none.</summary>
    /// <param name="handle">The handle's text.</param>
    public static RecordingTarget? ForWindow(string? handle)
        => long.TryParse(handle, out var value) && value != 0 ? new Window((nint)value) : null;

    /// <summary>A top-level window on Windows: a MAUI, WPF, WinForms or Stride app.</summary>
    /// <param name="Handle">The window's handle (HWND).</param>
    public sealed record Window(nint Handle) : RecordingTarget;

    /// <summary>A monitor, or an area of the screen.</summary>
    /// <param name="Handle">The monitor's handle (HMONITOR); zero for the whole virtual screen.</param>
    /// <param name="Crop">The area to keep, in screen coordinates; null for the whole monitor.</param>
    public sealed record Monitor(nint Handle, Rectangle? Crop) : RecordingTarget;

    /// <summary>A browser page.</summary>
    /// <param name="Page">The driver's page object (a Playwright <c>IPage</c>), kept opaque.</param>
    public sealed record BrowserPage(object Page) : RecordingTarget;

    /// <summary>An Android device or emulator.</summary>
    /// <param name="Serial">The device as <c>adb devices</c> names it.</param>
    public sealed record AndroidDevice(string Serial) : RecordingTarget;

    /// <summary>An iOS simulator.</summary>
    /// <param name="Udid">The simulator's UDID.</param>
    public sealed record IosSimulator(string Udid) : RecordingTarget;
}
