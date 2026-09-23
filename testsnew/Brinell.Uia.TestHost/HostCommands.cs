namespace Brinell.Uia.TestHost;

/// <summary>
/// The window messages a test can send the host to drive the bridge's lifetime.
/// </summary>
/// <remarks>
/// <para>
/// <b>Step 28 needs a bridge that comes and goes while a client is watching.</b> A leaked
/// provider is only a problem once a client has cached a pointer to it, so the interesting
/// sequence - attach, let the client walk the tree, destroy, repeat - has to be driven from
/// outside while the host keeps running. These are how.
/// </para>
/// <para>
/// <b>Queries are sent; commands are posted.</b> Both run on the thread that owns the window,
/// which is what creating and destroying a bridge requires. The difference is that a
/// <c>SendMessage</c> from another process is an <i>input-synchronous</i> call, and COM forbids
/// an outgoing call while one is being dispatched: <c>UiaDisconnectProvider</c> has to call out
/// to the client to revoke its interface, so it fails with
/// <c>RPC_E_CANTCALLOUT_ININPUTSYNCCALL</c> (0x8001010D) and the provider stays connected.
/// </para>
/// <para>
/// <b>Measured, after it broke the first draft of these tests.</b> Teardown driven by
/// <c>SendMessage</c> reported success and left every provider live, so a held pattern went on
/// answering. The harness was manufacturing the leak it was there to detect. Commands are
/// therefore posted - which is also the faithful arrangement, since a real window closes from
/// its own message loop - and the caller waits on
/// <see cref="CommandsHandled"/>, a query, which needs no callout.
/// </para>
/// <para>
/// Public so the tests name the same numbers the host answers.
/// </para>
/// </remarks>
public static class HostCommands
{
    private const uint WmApp = 0x8000;

    /// <summary>
    /// Disposes the current bridge and attaches a fresh one to the same window.
    /// </summary>
    /// <remarks>
    /// One soak cycle. Posted, not sent; wait for <see cref="CommandsHandled"/> to move.
    /// </remarks>
    public const uint CycleBridge = WmApp + 1;

    /// <summary>Returns <c>BrinellUiaBridge.ActiveCount</c>.</summary>
    public const uint CountBridges = WmApp + 2;

    /// <summary>
    /// Destroys the bridge's window behind its back, without disposing the bridge.
    /// </summary>
    /// <remarks>
    /// <b>The leak this exists to catch, staged deliberately.</b> In a real app nobody calls
    /// this - the app's window closes and Windows destroys its children for it, which is the
    /// same thing happening for a reason nobody wrote down. If the bridge only cleaned up in
    /// <c>Dispose</c>, its providers would stay connected and its registry entry would stay
    /// keyed on a handle Windows can hand to another window. Posted, not sent.
    /// </remarks>
    public const uint DestroyBridgeWindow = WmApp + 3;

    /// <summary>Returns <c>BrinellUiaBridge.DisconnectFailures</c>.</summary>
    /// <remarks>
    /// Should be zero after any amount of tearing down. A non-zero answer means the teardown ran
    /// and UI Automation declined to sever the provider, which is a leak that reported success.
    /// </remarks>
    public const uint CountDisconnectFailures = WmApp + 4;

    /// <summary>Returns <c>BrinellUiaBridge.LastDisconnectHResult</c>.</summary>
    public const uint LastDisconnectHResult = WmApp + 5;

    /// <summary>
    /// Returns how many posted commands this host has finished.
    /// </summary>
    /// <remarks>
    /// A query, so it is safe to send. It is how a caller waits for a posted command without
    /// sending one - see the note above about input-synchronous calls.
    /// </remarks>
    public const uint CommandsHandled = WmApp + 6;
}
