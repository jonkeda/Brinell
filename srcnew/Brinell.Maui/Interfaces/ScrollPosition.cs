namespace Brinell.Maui.Interfaces;

/// <summary>
/// Where a scroller is, and how much there is to scroll.
/// </summary>
/// <remarks>
/// <para>
/// <b>Six numbers rather than a percentage.</b> UI Automation offers a scroll percent, and a
/// percent cannot answer the question tests actually ask: "are we at the bottom" is true both
/// for a long list scrolled to its end and for a page too short to scroll at all, and those are
/// different facts about the app. Offset, viewport and content size together separate them.
/// </para>
/// <para>
/// Device-independent units, as MAUI reports them - not physical pixels. A test comparing these
/// to <c>IMauiElement.Rect</c>, which is in physical pixels, is comparing two different things.
/// </para>
/// </remarks>
/// <param name="X">Horizontal offset from the start of the content.</param>
/// <param name="Y">Vertical offset from the start of the content.</param>
/// <param name="ViewportWidth">Visible width.</param>
/// <param name="ViewportHeight">Visible height.</param>
/// <param name="ContentWidth">Total width of the content.</param>
/// <param name="ContentHeight">Total height of the content.</param>
public readonly record struct ScrollPosition(
    double X,
    double Y,
    double ViewportWidth,
    double ViewportHeight,
    double ContentWidth,
    double ContentHeight)
{
    /// <summary>Whether the content is taller than the viewport, so scrolling means anything.</summary>
    /// <remarks>
    /// The guard a test wants before asserting that scrolling moved something. A page that
    /// cannot scroll will report the same offset before and after, and that is not a failure of
    /// the scroll - it is the page being short.
    /// </remarks>
    public bool CanScrollVertically => ContentHeight > ViewportHeight;

    /// <summary>Whether the viewport is at the bottom of the content.</summary>
    /// <remarks>
    /// Compared with a one-unit tolerance. MAUI's offsets are doubles produced by layout
    /// arithmetic, so a scroller genuinely at its end commonly reports a value a fraction short
    /// of the exact difference.
    /// </remarks>
    public bool IsAtBottom => Y >= ContentHeight - ViewportHeight - 1;

    /// <summary>Whether the viewport is at the top of the content.</summary>
    public bool IsAtTop => Y <= 1;
}
