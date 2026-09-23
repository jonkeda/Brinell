namespace Brinell.Uia;

/// <summary>
/// The frozen identifiers of the Brinell UI Automation contract.
/// </summary>
/// <remarks>
/// <para>
/// <b>These values are the wire format.</b> A UI Automation pattern id is allocated per
/// process at registration time and differs between the app under test and the test
/// assembly; the GUID is what actually identifies the pattern, and the two processes agree
/// only because both compile this file. Changing a GUID here is a breaking change that
/// requires the app under test and the test assembly to be rebuilt and deployed together.
/// </para>
/// <para>
/// <b>The method table is frozen too.</b> UI Automation takes the method list at
/// registration and dispatches by index thereafter, so inserting a method renumbers every
/// method after it and silently misroutes calls from an older peer. New capability is added
/// as a new <see cref="BrinellVerb"/>, never as a new method.
/// </para>
/// </remarks>
public static class BrinellUiaIds
{
    /// <summary>Identifies the pattern itself. The contract between the two processes.</summary>
    public static readonly Guid PatternGuid = new("e1ff558b-b42c-400f-9597-d3ed8d84fda4");

    /// <summary>IID of the provider-side interface, <see cref="IBrinellAutomationProvider"/>.</summary>
    public const string ProviderInterfaceIid = "d890790c-64e5-47fb-8539-c1a73c307c46";

    /// <summary>IID of the client-side interface, <see cref="IBrinellAutomationPattern"/>.</summary>
    public const string ClientInterfaceIid = "1a5456c2-a3a3-4175-9679-ec46b85915a8";

    /// <summary>The name UI Automation shows for the pattern in Inspect.exe and friends.</summary>
    public const string PatternProgrammaticName = "BrinellAutomationPattern";

    /// <summary>
    /// Bumped when the meaning of an existing verb changes, which should be never.
    /// </summary>
    /// <remarks>
    /// Adding a verb does not bump this: an older peer answers an unknown verb with
    /// <see cref="HResults.UIA_E_NOTSUPPORTED"/>, which is the negotiation. The version exists
    /// for the case that cannot be negotiated - a verb whose arguments were reinterpreted.
    /// </remarks>
    public const int ProtocolVersion = 1;

    /// <summary>Class name of the window that hosts the bridge, and of its fragment root.</summary>
    /// <remarks>
    /// Deliberately unmistakable. Anyone finding this in Inspect.exe on a machine that should
    /// not have instrumentation on it needs to be able to search for it and get one answer.
    /// </remarks>
    public const string BridgeClassName = "BrinellUiaBridge";

    /// <summary>
    /// The <c>AutomationId</c> the fragment root publishes.
    /// </summary>
    /// <remarks>
    /// A client must find the bridge by this, not by <see cref="BridgeClassName"/>. The window
    /// itself reports that class name to UI Automation whether or not our provider ever answered
    /// <c>WM_GETOBJECT</c>, so matching on it cannot distinguish a working bridge from an empty
    /// window that merely looks like one - and the difference is the whole diagnosis.
    /// </remarks>
    public const string BridgeAutomationId = "BrinellUiaBridge.Root";

    /// <summary>Class name reported by each element that carries the pattern.</summary>
    public const string TargetClassName = "BrinellUiaTarget";

    /// <summary>
    /// Prefix of the <c>AutomationId</c> published by a bridge element, so a client can find
    /// the element standing for a given MAUI <c>AutomationId</c> in one query.
    /// </summary>
    public const string TargetAutomationIdPrefix = "BrinellUia.";

    /// <summary>What a bridge element reports as its provider description.</summary>
    /// <remarks>
    /// Named as instrumentation on purpose. A shipping build contains no bridge at all
    /// (see step 27), but anything that does get inspected should announce what it is.
    /// </remarks>
    public const string ProviderDescription = "Brinell UI Automation bridge (test instrumentation)";

    /// <summary>The <c>AutomationId</c> a bridge element publishes for the named target.</summary>
    /// <param name="targetAutomationId">The <c>AutomationId</c> of the MAUI element.</param>
    /// <returns>The bridge element's own <c>AutomationId</c>.</returns>
    public static string TargetAutomationIdFor(string targetAutomationId)
        => TargetAutomationIdPrefix + targetAutomationId;
}
