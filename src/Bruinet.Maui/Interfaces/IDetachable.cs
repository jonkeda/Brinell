namespace Bruinet.Maui.Interfaces;

/// <summary>
/// An element that can outlive its place in the app's tree: removed from it, yet still answering
/// reads for a moment.
/// </summary>
/// <remarks>
/// On Windows, a page that Shell has navigated away from leaves the UI Automation tree, but the
/// element a test holds for it keeps answering for about a second: not stale, not off screen, its
/// full bounds. A page object that cached it went on reading the page underneath
/// (.my/rca/stacked-pages.md in the Kofflard repository). Whether it is still attached is the one
/// read that tells.
/// </remarks>
public interface IDetachable
{
    /// <summary>Whether the element is still in its app's tree; false once it was removed.</summary>
    bool IsAttached { get; }
}
