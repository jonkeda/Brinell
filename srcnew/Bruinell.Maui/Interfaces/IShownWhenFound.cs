namespace Bruinell.Maui.Interfaces;

/// <summary>
/// An element whose driver only finds what is on screen, so being found already says it is shown.
/// </summary>
/// <remarks>
/// UiAutomator2 leaves out of its tree whatever the user cannot see, unless asked otherwise, so an
/// Android element that was found, and is not stale, is visible to the user. Asking it again costs a
/// round trip: <see cref="MauiElementExtensions.HasUsableBounds"/> read <c>displayed</c> and then
/// <c>rect</c> on every readiness probe, and 1,092 <c>displayed</c> reads took 36 s of one
/// 8.5-minute Android slice. With this, it reads the bounds alone. Windows keeps off-screen and
/// hidden elements in its tree, so its elements do not say this.
/// </remarks>
public interface IShownWhenFound
{
    /// <summary>Whether this element can only have been found while shown.</summary>
    bool IsShownWhenFound { get; }
}
