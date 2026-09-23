using System.Runtime.InteropServices;

namespace Brinell.Uia;

/// <summary>
/// The frozen method table, implemented by the app under test.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two methods, and it stays two methods.</b> UI Automation dispatches to a custom pattern
/// by index into the table given at registration, so adding a third method here renumbers
/// nothing today but guarantees a mismatch the moment a client and a provider are built from
/// different revisions. New capability arrives as a new <see cref="BrinellVerb"/>.
/// </para>
/// <para>
/// <b>Never throw.</b> Both methods return an HRESULT from <see cref="HResults"/>. An
/// exception escaping into COM loses its message and can leave UI Automation holding a broken
/// connection, after which every query against the app blocks until it times out.
/// </para>
/// </remarks>
[ComVisible(true)]
[Guid(BrinellUiaIds.ProviderInterfaceIid)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IBrinellAutomationProvider
{
    /// <summary>Performs a verb described entirely by numbers.</summary>
    /// <remarks>
    /// The cheap path. Gestures, focus and index-based selection need nothing else, and
    /// keeping them off the string method avoids a BSTR allocation per call in the paths that
    /// run most often.
    /// </remarks>
    /// <param name="verb">A <see cref="BrinellVerb"/> value.</param>
    /// <param name="arg1">First argument, meaning defined per verb.</param>
    /// <param name="arg2">Second argument, meaning defined per verb.</param>
    /// <returns>An <see cref="HResults"/> value.</returns>
    [PreserveSig]
    int Invoke(int verb, int arg1, int arg2);

    /// <summary>Performs a verb that takes or returns text.</summary>
    /// <remarks>
    /// One method rather than a separate getter and setter, because a verb that only reads
    /// ignores the argument and a verb that only writes returns the empty string. That keeps
    /// the table at two entries, which is the property worth protecting.
    /// </remarks>
    /// <param name="verb">A <see cref="BrinellVerb"/> value.</param>
    /// <param name="argument">The argument, or the empty string.</param>
    /// <param name="result">The result, or the empty string. Never null.</param>
    /// <returns>An <see cref="HResults"/> value.</returns>
    [PreserveSig]
    int Exchange(
        int verb,
        [MarshalAs(UnmanagedType.BStr)] string argument,
        [MarshalAs(UnmanagedType.BStr)] out string result);
}

/// <summary>
/// The same two methods as the test assembly calls them.
/// </summary>
/// <remarks>
/// Structurally identical to <see cref="IBrinellAutomationProvider"/> and deliberately a
/// separate type with its own IID, because UI Automation requires a distinct client interface
/// id at registration. What the client actually receives is
/// <c>BrinellAutomationPatternClient</c>, built by the pattern handler.
/// </remarks>
[ComVisible(true)]
[Guid(BrinellUiaIds.ClientInterfaceIid)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IBrinellAutomationPattern
{
    /// <inheritdoc cref="IBrinellAutomationProvider.Invoke"/>
    [PreserveSig]
    int Invoke(int verb, int arg1, int arg2);

    /// <inheritdoc cref="IBrinellAutomationProvider.Exchange"/>
    [PreserveSig]
    int Exchange(
        int verb,
        [MarshalAs(UnmanagedType.BStr)] string argument,
        [MarshalAs(UnmanagedType.BStr)] out string result);
}
