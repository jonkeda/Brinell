using Brinell.Core.Diagnostics;
using Xunit;

namespace Brinell.Maui.UITests.Tests.Background;

/// <summary>
/// Step 16: the desktop lease's two branches, both of them, in milliseconds.
/// </summary>
/// <remarks>
/// <para>
/// <b>The serialising branch is the one that needs a test.</b> Every run since stage B sets
/// <c>BRINELL_BACKGROUND_MODE</c>, which sends <see cref="DesktopGate"/> down the path that takes
/// nothing and blocks nobody. The other path - the one that keeps two apps from fighting over the
/// foreground of the machine someone is sitting at - is now the path the suite never exercises.
/// Code that only runs for other people is exactly the code to pin down here.
/// </para>
/// <para>
/// <b>No app, no desktop, no collection.</b> These take a gate of their own rather than the
/// suite's, so they contend with themselves instead of with the run they are part of, and they
/// need neither of the sample apps to be running. That is what makes it affordable to assert the
/// blocking behaviour directly instead of inferring it from a wall clock.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Stage", "Background")]
public class DesktopGateTests
{
    /// <summary>Long enough that a wait which returns is a real handover, not a race.</summary>
    private const int GenerousWaitMs = 30_000;

    /// <summary>Short enough that asserting a timeout costs nothing worth noticing.</summary>
    private const int ImpatientWaitMs = 50;

    /// <summary>
    /// In background mode nobody takes the desktop, so nobody waits for it.
    /// </summary>
    /// <remarks>
    /// The whole point of step 16: this is the branch that lets the hub app and the Shell app be
    /// on screen at the same time. Two acquisitions with no release between them is precisely
    /// what two collections starting together looks like.
    /// </remarks>
    [Fact]
    public void Refused_LetsBothCollectionsInAtOnce()
    {
        using var policy = PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused);
        var gate = new DesktopGate(ImpatientWaitMs);

        using var first = gate.Acquire();
        using var second = gate.Acquire();

        Assert.NotNull(second);
    }

    /// <summary>
    /// With physical input allowed, the second collection waits for the first to finish.
    /// </summary>
    /// <remarks>
    /// Asserted in both directions on purpose. That the second acquisition is still waiting while
    /// the first is held is the guarantee; that it completes once the first is released is what
    /// separates a working queue from a deadlock, and only one of those two facts is visible in a
    /// run that passes.
    /// </remarks>
    [Fact]
    public void Allowed_MakesTheSecondCollectionWaitForTheFirst()
    {
        using var policy = PhysicalInput.OverridePolicy(PhysicalInputPolicy.Allowed);
        var gate = new DesktopGate(GenerousWaitMs);

        var first = gate.Acquire();

        // A thread rather than a task, because the analyzers rightly object to blocking on one
        // and this test is about blocking. The policy override is an AsyncLocal, and a started
        // thread captures the execution context, so the waiter sees the same policy this does.
        IDisposable? second = null;
        using var secondAcquired = new ManualResetEventSlim(false);
        var waiter = new Thread(() =>
        {
            second = gate.Acquire();
            secondAcquired.Set();
        })
        {
            IsBackground = true,
            Name = nameof(Allowed_MakesTheSecondCollectionWaitForTheFirst),
        };
        waiter.Start();

        Assert.False(
            secondAcquired.Wait(250),
            "A second collection took the desktop while the first still held it.");

        first.Dispose();

        Assert.True(
            secondAcquired.Wait(GenerousWaitMs),
            "The desktop was released and the waiting collection was never let in.");

        second!.Dispose();
    }

    /// <summary>
    /// A fixture disposed twice releases the desktop once.
    /// </summary>
    /// <remarks>
    /// The failure this guards against is the one the lease exists to prevent, reached from the
    /// other side: a double release raises the semaphore's count, and from then on two
    /// collections hold a lease that is supposed to be exclusive. Asserted by showing the gate is
    /// still shut afterwards, because a count of two is invisible to anything that only asks
    /// whether one acquisition succeeds.
    /// </remarks>
    [Fact]
    public void Allowed_DisposingTwiceReleasesOnce()
    {
        using var policy = PhysicalInput.OverridePolicy(PhysicalInputPolicy.Allowed);
        var gate = new DesktopGate(ImpatientWaitMs);

        var first = gate.Acquire();
        first.Dispose();
        first.Dispose();

        using var second = gate.Acquire();

        Assert.Throws<TimeoutException>(() => gate.Acquire());
    }

    /// <summary>
    /// A lease nobody released fails the run with something a person can act on.
    /// </summary>
    /// <remarks>
    /// The alternative is a run that hangs until CI kills it, and a log whose last line is the
    /// name of a test that passed. The message has to name the way out, because the person
    /// reading it is not the person who wrote the lease.
    /// </remarks>
    [Fact]
    public void Allowed_ALeakedLeaseTimesOutAndSaysWhatToDo()
    {
        using var policy = PhysicalInput.OverridePolicy(PhysicalInputPolicy.Allowed);
        var gate = new DesktopGate(ImpatientWaitMs);

        using var leaked = gate.Acquire();

        var timeout = Assert.Throws<TimeoutException>(() => gate.Acquire());

        Assert.Contains("was not disposed", timeout.Message);
        Assert.Contains("BRINELL_BACKGROUND_MODE", timeout.Message);
    }
}
