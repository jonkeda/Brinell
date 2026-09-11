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

    /// <summary>The call succeeded but did nothing. Used where "no such element" is not an error.</summary>
    public const int S_FALSE = 1;

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
