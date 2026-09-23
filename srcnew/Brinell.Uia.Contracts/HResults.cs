namespace Brinell.Uia;

/// <summary>
/// The HRESULTs the bridge speaks.
/// </summary>
/// <remarks>
/// Every failure crossing the COM boundary is one of these. An exception must never be
/// allowed to propagate out of a provider method: the client receives a generic
/// <c>E_FAIL</c> with the message lost, and in the worst case UI Automation tears down the
/// connection and every subsequent query on the app hangs until it times out.
/// </remarks>
public static class HResults
{
    /// <summary>The call succeeded.</summary>
    public const int S_OK = 0;

    /// <summary>
    /// The call succeeded but did nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THIS DOES NOT REACH THE CLIENT. Never give it a meaning.</b> UI Automation's marshalling
    /// of a custom pattern reports every <i>success</i> HRESULT as <see cref="S_OK"/>; failure
    /// HRESULTs cross intact. Measured from both ends at once through the bridge log at step 26,
    /// on both <c>Invoke</c> and <c>Exchange</c>, after a test that would not go red.
    /// </para>
    /// <para>
    /// So a verb whose caller must be able to tell "did it" from "declined" cannot say so with
    /// this. Use <see cref="BRINELL_E_DECLINED"/>, or have the verb report what landed and let
    /// the caller compare - the second is why <c>SetDate</c>, <c>SetText</c> and
    /// <c>SelectByText</c> were never harmed by this while <c>NavigateBack</c> was.
    /// </para>
    /// <para>
    /// It remains for the cases where nothing depends on the difference and the app's own log is
    /// the only reader: a flyout asked to open twice, a scroll past the end of a list.
    /// </para>
    /// </remarks>
    public const int S_FALSE = 1;

    /// <summary>
    /// The verb was understood, refused, and nothing changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="S_FALSE"/>'s meaning, in a code that survives the wire.</b> A binding that
    /// declines an index, a command whose <c>CanExecute</c> is false, a page asked to go back
    /// when there is nothing to pop. All of these were <c>S_FALSE</c> and all of them arrived as
    /// success, which let a test assert an action the app had refused to perform.
    /// </para>
    /// <para>
    /// <b>A failure code for something that is not a fault.</b> That is the trade: the severity
    /// bit is what carries it across the boundary, and reporting a refusal as an error the caller
    /// must handle is better than reporting it as a success the caller believes. The
    /// customer-defined bit keeps it out of Microsoft's numbering space.
    /// </para>
    /// <para>
    /// <b>Distinct from <see cref="UIA_E_NOTSUPPORTED"/>, which means never.</b> This one means
    /// not now, and not because of the request: ask again in another state and it may well work.
    /// And distinct from <see cref="UIA_E_ELEMENTNOTAVAILABLE"/>, which says ask somebody else -
    /// a whole-app verb walking its candidates treats that as "try the next" and this as an
    /// answer.
    /// </para>
    /// </remarks>
    public const int BRINELL_E_DECLINED = unchecked((int)0xA0040001);

    /// <summary>Generic failure. Avoid: it tells the caller nothing.</summary>
    public const int E_FAIL = unchecked((int)0x80004005);

    /// <summary>The verb number is not a defined verb, or an argument is malformed.</summary>
    public const int E_INVALIDARG = unchecked((int)0x80070057);

    /// <summary>
    /// The element is gone - unloaded, or its window closed.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="UIA_E_NOTSUPPORTED"/> on purpose: this one is worth retrying
    /// after re-resolving the element, and the other never is.
    /// </remarks>
    public const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);

    /// <summary>
    /// This element does not implement this verb.
    /// </summary>
    /// <remarks>
    /// The negotiation with an older app under test. A client from a newer build asks for a
    /// verb the provider has never heard of and gets this, rather than a crash or a hang.
    /// </remarks>
    public const int UIA_E_NOTSUPPORTED = unchecked((int)0x80040204);

    /// <summary>The verb was understood but did not complete in the time allowed.</summary>
    public const int UIA_E_TIMEOUT = unchecked((int)0x80131505);
}
