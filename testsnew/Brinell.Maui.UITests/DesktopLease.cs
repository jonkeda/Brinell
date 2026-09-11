using Brinell.Core.Diagnostics;

namespace Brinell.Maui.UITests;

/// <summary>
/// Step 16: the thing two test collections actually contend for.
/// </summary>
/// <remarks>
/// <para>
/// <b>The resource is the desktop, not the test runner.</b> The assembly used to disable xUnit's
/// parallelism outright, and the comment explaining why named the real problem: two apps on
/// screen at once compete for the foreground, the mouse follows one of them, and keystrokes go
/// wherever focus last landed. None of that is about running two test collections - it is about
/// two collections both reaching for a machine there is only one of.
/// </para>
/// <para>
/// <b>So the gate is on the reaching, and it opens when nothing reaches.</b> With physical input
/// refused or audited, no test in the suite touches the foreground, the pointer or the
/// clipboard; two apps can then run side by side without noticing each other, which is what
/// stages 13 through 15 were for. With physical input allowed - the ordinary local run - this
/// serialises the collections exactly as the assembly attribute used to.
/// </para>
/// <para>
/// <b>Held for the fixture's life, not per test.</b> A fixture owns a running app for the whole
/// collection; taking the desktop for the duration is the same grain the old attribute worked
/// at, and it means no test body has to know the lease exists. xUnit builds a collection's
/// fixture when its first test runs and disposes it after the last, so the second collection
/// simply waits in its constructor.
/// </para>
/// <para>
/// <b>Android is not gated here.</b> Two Appium sessions share one emulator whatever the input
/// policy says, so the mobile head keeps the assembly-wide attribute - see its own
/// <c>AssemblyInfo.cs</c>, which is why that file is no longer shared.
/// </para>
/// </remarks>
internal static class DesktopLease
{
    /// <summary>
    /// How long a collection will wait for the other one to finish.
    /// </summary>
    /// <remarks>
    /// Long enough for a full collection - the Shell suite is minutes - and short enough that a
    /// lease which is never released fails the run with something readable instead of hanging
    /// until the CI job is killed.
    /// </remarks>
    private const int WaitMs = 10 * 60 * 1000;

    private static readonly DesktopGate Shared = new(WaitMs);

    /// <summary>
    /// Takes the desktop if this run needs it exclusively.
    /// </summary>
    /// <returns>
    /// A token to dispose when the caller is finished with the desktop. Disposing it is always
    /// safe, whether or not anything was actually taken.
    /// </returns>
    internal static IDisposable Acquire() => Shared.Acquire();
}

/// <summary>
/// The lease's mechanism, as an object rather than a static.
/// </summary>
/// <remarks>
/// <para>
/// <b>Separated so it can be tested at all.</b> The only interesting behaviour here is what
/// happens when two holders contend, and the one gate the suite runs on is held by whichever
/// fixture is live - a test that took it would be queuing behind the run it belongs to, and a
/// test that proved the queue works by blocking for ten minutes proves nothing anyone will wait
/// for. A test with a gate of its own contends with itself, deliberately, in milliseconds.
/// </para>
/// <para>
/// That matters more than it sounds: the serialising branch is the one that never runs in the
/// mode this suite now uses. Background mode takes the <see cref="PhysicalInputPolicy.Allowed"/>
/// path out of every ordinary run, so without <c>DesktopGateTests</c> the code protecting people
/// who run the suite in the foreground would be shipped unexecuted.
/// </para>
/// </remarks>
internal sealed class DesktopGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly int _waitMs;

    /// <param name="waitMs">
    /// How long <see cref="Acquire"/> waits before deciding a lease was leaked.
    /// </param>
    internal DesktopGate(int waitMs) => _waitMs = waitMs;

    /// <summary>
    /// Takes the desktop if this run needs it exclusively.
    /// </summary>
    /// <returns>
    /// A token to dispose when the caller is finished with the desktop. Disposing it is always
    /// safe, whether or not anything was actually taken.
    /// </returns>
    /// <exception cref="TimeoutException">
    /// Another collection has held the desktop for longer than any collection should take, which
    /// means a fixture was not disposed.
    /// </exception>
    internal IDisposable Acquire()
    {
        if (PhysicalInput.Policy != PhysicalInputPolicy.Allowed)
        {
            // Nothing in the suite will touch the desktop, so there is nothing to take. This is
            // the branch that lets two apps run at once.
            return NullLease.Instance;
        }

        if (!_gate.Wait(_waitMs))
        {
            throw new TimeoutException(
                $"Waited {_waitMs / 1000}s for another test collection to release the desktop. "
                + "A fixture holding the lease was not disposed, or a collection ran longer than "
                + "any collection should. Set BRINELL_BACKGROUND_MODE=1 to let collections run "
                + "side by side instead of queuing.");
        }

        return new HeldLease(_gate);
    }

    private sealed class HeldLease(SemaphoreSlim gate) : IDisposable
    {
        private bool _released;

        public void Dispose()
        {
            // Guarded because a fixture can be disposed more than once, and releasing a
            // semaphore twice would hand the desktop to two collections at the same time -
            // the exact failure this type exists to prevent, arrived at from the other side.
            if (_released)
            {
                return;
            }

            _released = true;
            gate.Release();
        }
    }

    private sealed class NullLease : IDisposable
    {
        internal static readonly NullLease Instance = new();

        public void Dispose()
        {
        }
    }
}
