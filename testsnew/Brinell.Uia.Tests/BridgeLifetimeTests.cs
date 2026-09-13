using System.Runtime.InteropServices;
using Brinell.Uia.TestHost;
using FlaUI.Core.AutomationElements;
using Xunit.Abstractions;

namespace Brinell.Uia.Tests;

/// <summary>
/// Step 28: what happens when bridges come and go, with a client watching.
/// </summary>
/// <remarks>
/// <para>
/// <b>A leaked provider is not a quiet leak.</b> UI Automation caches provider pointers across
/// the process boundary, and a client left holding a stale one blocks until its transaction
/// times out rather than failing. So the cost of getting this wrong is not memory: it is every
/// accessibility client on the desktop stalling on the app under test - Narrator, Voice Access,
/// Magnifier, and whatever else happens to walk past.
/// </para>
/// <para>
/// <b>Out of process, with the client connected between cycles.</b> Disposing a bridge nobody
/// ever talked to proves nothing - <c>UiaDisconnectProvider</c> is a no-op if no client holds a
/// pointer. Each cycle here walks the tree and calls a verb first, so there is something real
/// to disconnect.
/// </para>
/// <para>
/// <b>Its own host process, not the shared one.</b> These destroy and rebuild the very bridge
/// the other tests in this assembly are addressing, so sharing a fixture with them would make
/// their failures depend on this file's timing.
/// </para>
/// </remarks>
[Trait("Category", "Uia")]
public class BridgeLifetimeTests : IDisposable
{
    /// <summary>
    /// How many attach/destroy cycles the soak runs.
    /// </summary>
    /// <remarks>
    /// Enough that a leak of one provider per cycle is unmistakable rather than arguable, and
    /// few enough that the test stays a few seconds. The assertion is on the count returning to
    /// where it started, so the number only decides how loudly a leak shows up.
    /// </remarks>
    private const int SoakCycles = 25;

    private readonly LifetimeHost _host;
    private readonly ITestOutputHelper _output;

    public BridgeLifetimeTests(ITestOutputHelper output)
    {
        _output = output;
        _host = new LifetimeHost();
    }

    public void Dispose() => _host.Dispose();

    /// <summary>
    /// Attaching and destroying repeatedly leaves no bridge behind, and the last one works.
    /// </summary>
    /// <remarks>
    /// <b>Two assertions, and the second is the one people forget.</b> A teardown that leaks
    /// nothing because it also breaks the bridge would pass the count check and fail every test
    /// in the suite afterwards, so the cycle ends by using the bridge it just rebuilt.
    /// </remarks>
    [Fact]
    public void RepeatedAttachAndDispose_DoesNotAccumulateProviders()
    {
        Assert.Equal(1, _host.Send(HostCommands.CountBridges));

        for (var cycle = 0; cycle < SoakCycles; cycle++)
        {
            // The client has to be holding something for a disconnect to mean anything. Walking
            // to the target and calling a verb is what makes UI Automation cache a pointer to
            // the provider that is about to be destroyed - a soak over a bridge nobody talked to
            // would disconnect nothing and prove nothing.
            Assert.True(
                BridgeClient.TryGetPattern(Target(), out var pattern),
                $"The bridge was unusable on cycle {cycle}.");

            Assert.Equal(
                HResults.S_OK,
                pattern!.Exchange((int)BrinellVerb.SetText, $"cycle {cycle}", out _));

            Assert.Equal(1, _host.Run(HostCommands.CycleBridge));
        }

        _output.WriteLine(
            $"{SoakCycles} cycles: {_host.Send(HostCommands.CountBridges)} bridge live, "
            + $"{_host.Send(HostCommands.CountDisconnectFailures)} disconnect failures.");

        // Every teardown severed what it was holding. Counted rather than assumed, because a
        // refused disconnect is silent at the call site and leaves the provider connected -
        // which is the leak, arriving with a clean bill of health.
        Assert.Equal(0, _host.Send(HostCommands.CountDisconnectFailures));

        // Still usable, and still the bridge - not a husk that answers the count and nothing
        // else. A fresh target each cycle, so the text is written now rather than carried over.
        Assert.True(BridgeClient.TryGetPattern(Target(), out var final));
        Assert.Equal(HResults.S_OK, final!.Exchange((int)BrinellVerb.SetText, "after", out _));
        Assert.Equal(HResults.S_OK, final.Exchange((int)BrinellVerb.GetText, "", out var text));
        Assert.Equal("after", text);
    }

    /// <summary>
    /// A bridge whose window is destroyed without a Dispose tears itself down anyway.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is how it usually happens.</b> Nobody writes "destroy the bridge window"; an app
    /// window closes and Windows destroys its children, and whatever managed teardown the host
    /// wired up may not run first, or at all. Before step 28 the bridge only cleaned up in
    /// <c>Dispose</c>, so this path left the providers connected and left the registry holding
    /// a strong reference keyed on a handle Windows is free to reissue - which would have meant
    /// a later, unrelated window being answered by a dead bridge.
    /// </para>
    /// <para>
    /// <b>The window is destroyed while the client holds a pointer to the provider</b>, because
    /// that is the only arrangement in which the disconnect does any work.
    /// </para>
    /// </remarks>
    [Fact]
    public void WindowDestroyedWithoutDispose_StillDisconnectsAndDeregisters()
    {
        Assert.True(BridgeClient.TryGetPattern(Target(), out var pattern));
        Assert.Equal(HResults.S_OK, pattern!.Exchange((int)BrinellVerb.SetText, "held", out _));

        Assert.Equal(0, _host.Run(HostCommands.DestroyBridgeWindow));

        // The disconnect ran and UI Automation accepted it. This is the half the plan names -
        // "UiaDisconnectProvider runs on window close" - and until now the HRESULT was thrown
        // away, so it was a line of code rather than a fact.
        Assert.Equal(0, _host.Send(HostCommands.CountDisconnectFailures));

        // The bridge window is gone from the tree, which is what a client would see. Asserted
        // as well as the count because the count is the host's own bookkeeping and this is not.
        var raw = _host.Automation.TreeWalkerFactory.GetRawViewWalker();
        var bridge = BridgeClient.FindByClassName(
            raw, _host.Window, BrinellUiaIds.BridgeClassName);

        Assert.True(bridge is null, "The bridge window outlived its own destruction.");
    }

    /// <summary>
    /// The stale pattern from a destroyed bridge fails rather than hanging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The failure mode the disconnect exists to prevent, measured as a duration.</b> A
    /// client holding a pointer to a provider that was never disconnected does not get an
    /// error - it blocks until UI Automation's own transaction timeout, which is seconds, on
    /// every call. Asserting only that the call fails would pass just as well in the leaked
    /// case, some seconds later, so the assertion has to be about the clock.
    /// </para>
    /// <para>
    /// <b>Generous, because it is measuring the difference between milliseconds and a
    /// timeout.</b> UI Automation's default is around two seconds and this allows one; the test
    /// is not trying to pin down how fast a disconnected call fails, only that it is not waiting
    /// for a corpse.
    /// </para>
    /// </remarks>
    [Fact]
    public void CallingThroughADestroyedBridge_FailsPromptly()
    {
        Assert.True(BridgeClient.TryGetPattern(Target(), out var pattern));
        Assert.Equal(HResults.S_OK, pattern!.Exchange((int)BrinellVerb.SetText, "held", out _));

        Assert.Equal(0, _host.Run(HostCommands.DestroyBridgeWindow));

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var hr = CallAndCatch(() => pattern.Exchange((int)BrinellVerb.GetText, "", out _));
        clock.Stop();

        _output.WriteLine($"Stale call returned 0x{hr:X8} after {clock.ElapsedMilliseconds} ms.");

        Assert.False(
            BrinellVerbFailure.Succeeded(hr),
            "A verb sent through a destroyed bridge reported success.");

        Assert.True(
            clock.ElapsedMilliseconds < 1_000,
            $"The stale call took {clock.ElapsedMilliseconds} ms. A disconnected provider fails "
            + "at once; one that was never disconnected blocks until UI Automation's transaction "
            + "times out, which is what hangs every accessibility client on the desktop.");
    }

    /// <remarks>
    /// A dead provider can answer with an HRESULT or by throwing, depending on how far the call
    /// got before the marshalling noticed. Both are acceptable and neither is a hang, so the
    /// exception becomes its own HRESULT rather than failing the test.
    /// </remarks>
    private static int CallAndCatch(Func<int> call)
    {
        try
        {
            return call();
        }
        catch (COMException com)
        {
            return com.HResult;
        }
        catch (InvalidCastException)
        {
            // The proxy is gone entirely, which is the disconnect having worked.
            return HResults.UIA_E_ELEMENTNOTAVAILABLE;
        }
    }

    private AutomationElement Target()
    {
        var raw = _host.Automation.TreeWalkerFactory.GetRawViewWalker();
        var bridge = BridgeClient.FindByClassName(
            raw, _host.Window, BrinellUiaIds.BridgeClassName);

        Assert.True(bridge is not null, "The bridge window is not in the tree.");

        var wanted = BrinellUiaIds.TargetAutomationIdFor(HostTargets.Primary);
        var target = BridgeClient.FindByAutomationId(raw, bridge!, wanted);

        Assert.True(target is not null, $"No bridge element named {wanted}.");

        return target!;
    }
}
