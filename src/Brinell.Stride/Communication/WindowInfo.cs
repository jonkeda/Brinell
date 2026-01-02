namespace Brinell.Stride.Communication;

/// <summary>
/// Information about the game window position and size.
/// </summary>
public class WindowInfo
{
    /// <summary>
    /// Window X position on screen (client area top-left).
    /// </summary>
    public int WindowX { get; set; }

    /// <summary>
    /// Window Y position on screen (client area top-left).
    /// </summary>
    public int WindowY { get; set; }

    /// <summary>
    /// Window client area width.
    /// </summary>
    public int WindowWidth { get; set; }

    /// <summary>
    /// Window client area height.
    /// </summary>
    public int WindowHeight { get; set; }

    /// <summary>
    /// UI resolution X (may differ from window size due to scaling).
    /// </summary>
    public int UiResolutionX { get; set; } = 1280;

    /// <summary>
    /// UI resolution Y (may differ from window size due to scaling).
    /// </summary>
    public int UiResolutionY { get; set; } = 720;
}
