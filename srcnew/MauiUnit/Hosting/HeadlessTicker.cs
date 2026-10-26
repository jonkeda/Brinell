using Microsoft.Maui.Animations;

namespace MauiUnit.Hosting;

/// <summary>
/// The ticker under the context's <see cref="IAnimationManager"/>: it reports the system's animations
/// off, so MAUI's own animation manager finishes every animation at once, as it does on a device whose
/// user turned animations off. A fade, a toolkit state change or a page transition never makes a test
/// wait, and ends exactly where it would have ended.
/// </summary>
internal sealed class HeadlessTicker : ITicker
{
    public bool IsRunning { get; private set; }

    public int MaxFps { get; set; } = 60;

    public Action? Fire { get; set; }

    public bool SystemEnabled => false;

    public void Start() => IsRunning = true;

    public void Stop() => IsRunning = false;
}
