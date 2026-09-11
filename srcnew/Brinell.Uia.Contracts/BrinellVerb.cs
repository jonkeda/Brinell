namespace Brinell.Uia;

/// <summary>
/// Everything the bridge can be asked to do, as wire values.
/// </summary>
/// <remarks>
/// <para>
/// <b>Append-only.</b> These numbers cross a process boundary between two independently
/// built assemblies, so a value that has ever shipped keeps its meaning forever. Add at the
/// end of a range; never renumber, never reuse a retired value.
/// </para>
/// <para>
/// <b>Ranges are the versioning story.</b> Each block of a hundred is one capability area,
/// which lets a provider answer "I implement gestures but not text" by range rather than by
/// enumerating verbs, and leaves room for a range to grow without colliding with the next.
/// </para>
/// <para>
/// Not every verb here is implemented yet. The enum is the whole planned vocabulary because
/// the numbering has to be decided once; <see cref="BrinellVerbRange"/> and the provider's
/// own capability set say what is actually live.
/// </para>
/// </remarks>
public enum BrinellVerb
{
    /// <summary>Not a verb. Rejected with <see cref="HResults.E_INVALIDARG"/>.</summary>
    None = 0,

    // ---- 1-99: meta ----------------------------------------------------------------

    /// <summary>Returns the supported verbs as a comma-separated list of numbers.</summary>
    GetCapabilities = 1,

    /// <summary>Returns <see cref="BrinellUiaIds.ProtocolVersion"/> as a decimal string.</summary>
    GetProtocolVersion = 2,

    /// <summary>Succeeds and does nothing. Proves the round trip without side effects.</summary>
    Ping = 3,

    // ---- 100-199: gestures ---------------------------------------------------------

    /// <summary>Single tap.</summary>
    Tap = 100,

    /// <summary>Double tap.</summary>
    DoubleTap = 101,

    /// <summary>Press and hold.</summary>
    LongPress = 102,

    /// <summary>Swipe towards decreasing x.</summary>
    SwipeLeft = 103,

    /// <summary>Swipe towards increasing x.</summary>
    SwipeRight = 104,

    /// <summary>Swipe towards decreasing y.</summary>
    SwipeUp = 105,

    /// <summary>Swipe towards increasing y.</summary>
    SwipeDown = 106,

    /// <summary>Pan by <c>arg1</c> and <c>arg2</c> device-independent pixels.</summary>
    Pan = 107,

    /// <summary>Pinch by <c>arg1</c> percent, where 100 is no change.</summary>
    Pinch = 108,

    // ---- 200-299: focus ------------------------------------------------------------

    /// <summary>Gives the element logical focus without touching the foreground window.</summary>
    Focus = 200,

    /// <summary>Removes logical focus from the element.</summary>
    Unfocus = 201,

    /// <summary>Returns <c>true</c> or <c>false</c>.</summary>
    IsFocused = 202,

    // ---- 300-399: text -------------------------------------------------------------

    /// <summary>Replaces the element's text with the argument.</summary>
    SetText = 300,

    /// <summary>Appends the argument to the element's text.</summary>
    AppendText = 301,

    /// <summary>Empties the element's text.</summary>
    ClearText = 302,

    /// <summary>Raises the element's completion action, as the Enter key would.</summary>
    Submit = 303,

    /// <summary>Returns the element's text.</summary>
    GetText = 304,

    // ---- 400-499: scrolling --------------------------------------------------------

    /// <summary>Scrolls the named descendant into view.</summary>
    ScrollTo = 400,

    /// <summary>Scrolls the item at the given index into view.</summary>
    ScrollToIndex = 401,

    /// <summary>Returns the scroll offset and extent.</summary>
    ScrollPosition = 402,

    // ---- 500-599: navigation -------------------------------------------------------

    /// <summary>Pops the navigation stack.</summary>
    NavigateBack = 500,

    /// <summary>Navigates to the given route.</summary>
    NavigateTo = 501,

    /// <summary>Returns the current route.</summary>
    CurrentRoute = 502,

    // ---- 600-699: state reads ------------------------------------------------------

    /// <summary>Returns a named property of the element that a user could perceive.</summary>
    GetState = 600,

    /// <summary>Returns <c>true</c> once the dispatcher queue has drained.</summary>
    IsIdle = 601,

    // ---- 700-799: date and time ----------------------------------------------------

    /// <summary>Sets a date from a round-trip formatted string.</summary>
    SetDate = 700,

    /// <summary>Sets a time from a round-trip formatted string.</summary>
    SetTime = 701,

    /// <summary>Opens the element's flyout or picker.</summary>
    OpenFlyout = 702,

    /// <summary>Closes the element's flyout or picker.</summary>
    CloseFlyout = 703,

    // ---- 800-899: selection --------------------------------------------------------

    /// <summary>Selects the item at the given index.</summary>
    SelectIndex = 800,

    /// <summary>Selects the item with the given text.</summary>
    SelectByText = 801,

    // ---- 900-999: dialogs and menus ------------------------------------------------

    /// <summary>Returns the current alert's title, message and buttons.</summary>
    CurrentAlert = 900,

    /// <summary>Dismisses the current alert with the named result.</summary>
    DismissAlert = 901,

    /// <summary>Invokes a menu item by id without opening the popup.</summary>
    InvokeMenuItem = 902,
}

/// <summary>The capability area a verb belongs to, derived from its numeric range.</summary>
public enum BrinellVerbRange
{
    /// <summary>Not a recognised range.</summary>
    Unknown = 0,

    /// <summary>Verbs 1-99: discovery and liveness.</summary>
    Meta = 1,

    /// <summary>Verbs 100-199.</summary>
    Gestures = 100,

    /// <summary>Verbs 200-299.</summary>
    Focus = 200,

    /// <summary>Verbs 300-399.</summary>
    Text = 300,

    /// <summary>Verbs 400-499.</summary>
    Scrolling = 400,

    /// <summary>Verbs 500-599.</summary>
    Navigation = 500,

    /// <summary>Verbs 600-699.</summary>
    State = 600,

    /// <summary>Verbs 700-799.</summary>
    DateAndTime = 700,

    /// <summary>Verbs 800-899.</summary>
    Selection = 800,

    /// <summary>Verbs 900-999.</summary>
    DialogsAndMenus = 900,
}

/// <summary>Which of the contract's two methods a verb travels on.</summary>
/// <remarks>
/// <para>
/// <b>Part of the contract, and as fixed as the numbers.</b> The method table has exactly two
/// entries and a verb uses one of them; a client that sends a verb down the other reaches a
/// provider that has never heard of it, and is refused for a reason that has nothing to do with
/// the element. That failure is indistinguishable from "this element does not support that",
/// which is what makes it worth writing down rather than remembering.
/// </para>
/// <para>
/// The values match the method indexes in the frozen table.
/// </para>
/// </remarks>
public enum BrinellVerbTransport
{
    /// <summary><c>Invoke(verb, arg1, arg2)</c> - three ints in, nothing out.</summary>
    Invoke = 0,

    /// <summary><c>Exchange(verb, argument, out result)</c> - a string in, a string out.</summary>
    Exchange = 1,
}

/// <summary>Helpers over <see cref="BrinellVerb"/> that both ends of the wire rely on.</summary>
public static class BrinellVerbs
{
    /// <summary>
    /// Which of the two contract methods a verb travels on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rule is what it looks like:</b> a verb goes on <c>Exchange</c> if it carries a
    /// string in either direction, and on <c>Invoke</c> otherwise. It is written out verb by
    /// verb anyway, because the rule is a description of the answers rather than a way of
    /// deriving them - <c>Submit</c> and <c>ClearText</c> carry no strings and still belong with
    /// the rest of the text range, since the provider answers a range on one path.
    /// </para>
    /// <para>
    /// <b>Append-only, like the numbers.</b> A verb that has shipped keeps its transport: moving
    /// one would silently break every client built before the move, with no version to check.
    /// Verbs this build does not implement yet are listed all the same, for the same reason the
    /// numbers are - deciding once is what stops the decision from being made twice differently.
    /// </para>
    /// </remarks>
    /// <param name="verb">The verb.</param>
    /// <returns>The method it is sent on.</returns>
    public static BrinellVerbTransport TransportFor(BrinellVerb verb) => verb switch
    {
        // Meta. Ping is answered on both, and is listed here so that asking about it gives one
        // answer rather than an arbitrary one.
        BrinellVerb.GetCapabilities => BrinellVerbTransport.Exchange,
        BrinellVerb.GetProtocolVersion => BrinellVerbTransport.Exchange,
        BrinellVerb.Ping => BrinellVerbTransport.Invoke,

        // Focus. IsFocused answers with a string, the other two do not.
        BrinellVerb.IsFocused => BrinellVerbTransport.Exchange,

        // Text, entire. Including the two that carry nothing.
        BrinellVerb.SetText => BrinellVerbTransport.Exchange,
        BrinellVerb.AppendText => BrinellVerbTransport.Exchange,
        BrinellVerb.ClearText => BrinellVerbTransport.Exchange,
        BrinellVerb.Submit => BrinellVerbTransport.Exchange,
        BrinellVerb.GetText => BrinellVerbTransport.Exchange,

        // Scrolling: to a named descendant, or reporting a position.
        BrinellVerb.ScrollTo => BrinellVerbTransport.Exchange,
        BrinellVerb.ScrollPosition => BrinellVerbTransport.Exchange,

        // Navigation by route.
        BrinellVerb.NavigateTo => BrinellVerbTransport.Exchange,
        BrinellVerb.CurrentRoute => BrinellVerbTransport.Exchange,

        // State reads, both of which answer with a string.
        BrinellVerb.GetState => BrinellVerbTransport.Exchange,
        BrinellVerb.IsIdle => BrinellVerbTransport.Exchange,

        // Dates and times travel as round-trip formatted strings.
        BrinellVerb.SetDate => BrinellVerbTransport.Exchange,
        BrinellVerb.SetTime => BrinellVerbTransport.Exchange,

        BrinellVerb.SelectByText => BrinellVerbTransport.Exchange,

        BrinellVerb.CurrentAlert => BrinellVerbTransport.Exchange,
        BrinellVerb.DismissAlert => BrinellVerbTransport.Exchange,
        BrinellVerb.InvokeMenuItem => BrinellVerbTransport.Exchange,

        // Everything else is ints or nothing: the gestures, Focus and Unfocus, the flyouts,
        // going back, scrolling to an index, selecting by index.
        _ => BrinellVerbTransport.Invoke,
    };

    /// <summary>The capability area <paramref name="verb"/> falls in.</summary>
    /// <remarks>
    /// Works on unknown values on purpose. A client from a newer build can ask an older
    /// provider "do you do text at all" and get a useful answer about a verb the provider has
    /// never heard of.
    /// </remarks>
    /// <param name="verb">Any verb value, known or not.</param>
    /// <returns>The range, or <see cref="BrinellVerbRange.Unknown"/> outside 1-999.</returns>
    public static BrinellVerbRange RangeOf(int verb) => verb switch
    {
        >= 1 and < 100 => BrinellVerbRange.Meta,
        >= 100 and < 200 => BrinellVerbRange.Gestures,
        >= 200 and < 300 => BrinellVerbRange.Focus,
        >= 300 and < 400 => BrinellVerbRange.Text,
        >= 400 and < 500 => BrinellVerbRange.Scrolling,
        >= 500 and < 600 => BrinellVerbRange.Navigation,
        >= 600 and < 700 => BrinellVerbRange.State,
        >= 700 and < 800 => BrinellVerbRange.DateAndTime,
        >= 800 and < 900 => BrinellVerbRange.Selection,
        >= 900 and < 1000 => BrinellVerbRange.DialogsAndMenus,
        _ => BrinellVerbRange.Unknown,
    };

    /// <summary>The wire form of a capability set: verb numbers, comma separated, ascending.</summary>
    /// <param name="verbs">The verbs the provider implements for one element.</param>
    /// <returns>A string suitable for returning from <see cref="BrinellVerb.GetCapabilities"/>.</returns>
    public static string FormatCapabilities(IEnumerable<BrinellVerb> verbs)
    {
        ArgumentNullException.ThrowIfNull(verbs);

        return string.Join(
            ",",
            verbs.Select(v => (int)v).Distinct().Order());
    }

    /// <summary>Reads what <see cref="FormatCapabilities"/> wrote.</summary>
    /// <remarks>
    /// Unparseable and unknown entries are dropped rather than throwing. The list comes from
    /// another process which may be newer than this one, so an entry this build cannot name is
    /// expected traffic, not corruption.
    /// </remarks>
    /// <param name="capabilities">The string returned by the provider.</param>
    /// <returns>The verbs, ascending.</returns>
    public static IReadOnlyList<BrinellVerb> ParseCapabilities(string? capabilities)
    {
        if (string.IsNullOrWhiteSpace(capabilities))
        {
            return [];
        }

        var parsed = new List<BrinellVerb>();

        foreach (var part in capabilities.Split(',', StringSplitOptions.RemoveEmptyEntries
                                                     | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var value) && Enum.IsDefined((BrinellVerb)value))
            {
                parsed.Add((BrinellVerb)value);
            }
        }

        parsed.Sort();
        return parsed;
    }
}
