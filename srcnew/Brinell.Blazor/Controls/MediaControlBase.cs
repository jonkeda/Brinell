using System.Globalization;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Controls;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public abstract class MediaControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected MediaControlBase(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    protected MediaControlBase(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    // Playback control
    public TScope Play() => RunWithElement(e => e.Evaluate("el => el.play()"));
    public TScope Pause() => RunWithElement(e => e.Evaluate("el => el.pause()"));

    // Playback state
    public bool IsPlaying() => RunWithElement(e =>
        !(e.GetDomProperty("paused") == "True" || e.GetDomProperty("paused") == "true")
        && !(e.GetDomProperty("ended") == "True" || e.GetDomProperty("ended") == "true"));
    public bool IsPaused() => RunWithElement(e =>
        e.GetDomProperty("paused") == "True" || e.GetDomProperty("paused") == "true");
    public bool IsEnded() => RunWithElement(e =>
        e.GetDomProperty("ended") == "True" || e.GetDomProperty("ended") == "true");

    // Time control
    public double GetCurrentTime() => RunWithElement(e =>
        double.Parse(e.GetDomProperty("currentTime") ?? "0", CultureInfo.InvariantCulture));
    public TScope Seek(double seconds) => RunWithElement(e =>
        e.Evaluate($"el => el.currentTime = {seconds.ToString(CultureInfo.InvariantCulture)}"));
    public double GetDuration() => RunWithElement(e =>
        double.Parse(e.GetDomProperty("duration") ?? "0", CultureInfo.InvariantCulture));

    // Volume control
    public double GetVolume() => RunWithElement(e =>
        double.Parse(e.GetDomProperty("volume") ?? "1", CultureInfo.InvariantCulture));
    public TScope SetVolume(double volume) => RunWithElement(e =>
        e.Evaluate($"el => el.volume = {Math.Clamp(volume, 0, 1).ToString(CultureInfo.InvariantCulture)}"));
    public bool IsMuted() => RunWithElement(e =>
        e.GetDomProperty("muted") == "True" || e.GetDomProperty("muted") == "true");
    public TScope Mute() => RunWithElement(e => e.Evaluate("el => el.muted = true"));
    public TScope Unmute() => RunWithElement(e => e.Evaluate("el => el.muted = false"));

    // Source
    public string? GetSource() => RunWithElement(e =>
        e.GetDomAttribute("src") ?? e.GetDomProperty("currentSrc"));

    // Assertions
    public TScope AssertPlaying(string? message = null) => RunAssert(e =>
    {
        var paused = e.GetDomProperty("paused");
        var ended = e.GetDomProperty("ended");
        if (paused == "True" || paused == "true" || ended == "True" || ended == "true")
            throw new AssertionException(message ?? "Expected media to be playing");
    });
    public TScope AssertPaused(string? message = null) => RunAssert(e =>
    {
        var paused = e.GetDomProperty("paused");
        if (paused != "True" && paused != "true")
            throw new AssertionException(message ?? "Expected media to be paused");
    });
}
