using System.Runtime.InteropServices;

namespace Brinell.Uia;

/// <summary>
/// The client half: turns an automation element into a callable pattern.
/// </summary>
/// <remarks>
/// <para>
/// Takes the element as a delegate rather than as a typed parameter, and that is deliberate.
/// The only thing needed from the client library is "given a pattern id, fetch that pattern
/// off this element" - one call. Accepting it as a delegate keeps this project free of any
/// dependency on FlaUI, which matters because these same sources are compiled into the app
/// under test, where FlaUI has no business being.
/// </para>
/// <para>
/// The caller supplies <c>patternId =&gt; nativeElement.GetCurrentPattern(patternId)</c>; the
/// id lookup, the failure handling and the cast happen here so there is one copy of them.
/// </para>
/// </remarks>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class BrinellUiaClient
{
    /// <summary>
    /// This process's id for the pattern.
    /// </summary>
    /// <remarks>
    /// Meaningful only inside this process. The app under test has its own, usually different.
    /// </remarks>
    public static int PatternId => BrinellPatternRegistration.Current.PatternId;

    /// <summary>
    /// This process's id for the generated <c>IsBrinellAutomationPatternAvailable</c> property.
    /// </summary>
    /// <remarks>
    /// The cheap probe: reading this property answers "does this element carry the bridge"
    /// without a call into the app under test.
    /// </remarks>
    public static int IsPatternAvailablePropertyId
        => BrinellPatternRegistration.Current.IsPatternAvailablePropertyId;

    /// <summary>
    /// Fetches the Brinell pattern from an element, if it has one.
    /// </summary>
    /// <remarks>
    /// Returns false rather than throwing for every ordinary negative: the element has no
    /// bridge, the app is not instrumented, the element has gone away. A test asking whether an
    /// app supports the bridge is asking a question, not making a demand.
    /// </remarks>
    /// <param name="getCurrentPattern">
    /// Fetches a pattern by id from the element - normally
    /// <c>nativeElement.GetCurrentPattern</c>.
    /// </param>
    /// <param name="pattern">The pattern, when this returns true.</param>
    /// <returns>Whether the element carries the Brinell pattern.</returns>
    public static bool TryGetPattern(
        Func<int, object?> getCurrentPattern,
        out IBrinellAutomationPattern? pattern)
    {
        ArgumentNullException.ThrowIfNull(getCurrentPattern);

        pattern = null;

        if (!BrinellPatternRegistration.TryGetCurrent(out var registration, out _))
        {
            return false;
        }

        object? raw;
        try
        {
            raw = getCurrentPattern(registration!.PatternId);
        }
        catch (COMException)
        {
            // The element is gone, or the app closed mid-query. Not an error worth a stack.
            return false;
        }

        pattern = raw as IBrinellAutomationPattern;
        return pattern is not null;
    }
}

/// <summary>Turns an HRESULT from the bridge into something a test failure can say.</summary>
public static class BrinellVerbFailure
{
    /// <summary>Whether an HRESULT from the bridge means success.</summary>
    /// <param name="hresult">The value returned by a verb.</param>
    /// <returns>Whether it succeeded.</returns>
    public static bool Succeeded(int hresult) => hresult >= 0;

    /// <summary>
    /// Explains an HRESULT in terms of what the caller should do about it.
    /// </summary>
    /// <remarks>
    /// The distinction that matters is retryable versus not.
    /// <see cref="HResults.UIA_E_ELEMENTNOTAVAILABLE"/> means re-resolve and try again;
    /// <see cref="HResults.UIA_E_NOTSUPPORTED"/> means this will never work and the test should
    /// use another route.
    /// </remarks>
    /// <param name="verb">The verb that was attempted.</param>
    /// <param name="hresult">What it returned.</param>
    /// <returns>A sentence naming the verb, the cause and the remedy.</returns>
    public static string Describe(BrinellVerb verb, int hresult) => hresult switch
    {
        HResults.S_OK => $"{verb} succeeded.",
        HResults.S_FALSE => $"{verb} succeeded but did nothing.",

        HResults.UIA_E_NOTSUPPORTED =>
            $"{verb} is not supported by this element. Either the app under test declares no "
            + $"such capability for it, or the app was built against a contract that predates "
            + $"{verb}. This will not succeed on retry.",

        HResults.UIA_E_ELEMENTNOTAVAILABLE =>
            $"{verb} could not run: the element is no longer available. It was unloaded, or its "
            + "window closed, between being found and being used. Re-resolve the element and "
            + "retry.",

        HResults.UIA_E_TIMEOUT =>
            $"{verb} did not complete in the time the bridge allows. The app's UI thread is "
            + "blocked or the verb's own work did not finish.",

        HResults.E_INVALIDARG =>
            $"{verb} rejected its arguments. The verb number or an argument is malformed, which "
            + "means the client and the app under test disagree about the contract.",

        _ => $"{verb} failed with HRESULT 0x{hresult:X8}.",
    };
}
