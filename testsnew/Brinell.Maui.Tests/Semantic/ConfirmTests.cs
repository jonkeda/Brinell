using System.Diagnostics;
using Brinell.Maui.Controls.Base;

namespace Brinell.Maui.Tests.Semantic;

/// <summary>
/// <c>Confirm</c>: the one way a Core method waits. It reads the effect of an action the method
/// has already done, never repeats anything, and does nothing a generated wrapper has already done.
/// </summary>
public class ConfirmTests : SemanticControlTestsBase
{
    [Fact]
    public void Confirm_ReadThatSucceedsLate_IsConfirmed()
    {
        var reads = 0;

        var result = new ConfirmLabel(Page).Wait(() => ++reads, count => count >= 3, 1000);

        Assert.Equal(ConfirmationResult.Confirmed, result.Result);
        Assert.Equal(3, reads);
        Assert.Null(result.LastError);
    }

    [Fact]
    public void Confirm_ReadThatKeepsFailing_IsNotConfirmedWithTheLastError()
    {
        var result = new ConfirmLabel(Page).Wait<string>(
            () => throw new ElementNotFoundException("gone"), _ => true, 30);

        Assert.Equal(ConfirmationResult.NotConfirmed, result.Result);
        var error = Assert.IsType<ElementNotFoundException>(result.LastError);
        Assert.Equal("gone", error.Message);
    }

    [Fact]
    public void Confirm_ReadThatSucceedsWithTheWrongValue_IsNotConfirmedWithNoError()
    {
        var result = new ConfirmLabel(Page).Wait(() => 1, value => value == 2, 30);

        Assert.Equal(ConfirmationResult.NotConfirmed, result.Result);
        Assert.Equal(1, result.LastValue);
        Assert.Null(result.LastError);
    }

    /// <summary>
    /// A stale read means the element was replaced: the wait ends at once, and does not re-find
    /// an element the action never touched.
    /// </summary>
    [Fact]
    [Trait("Pin", "step6")]
    public void Confirm_StaleRead_IsReplacedAtOnce()
    {
        var stopwatch = Stopwatch.StartNew();

        var result = new ConfirmLabel(Page).Wait<bool>(
            () => throw new StaleElementException(), value => value, 5000);

        Assert.Equal(ConfirmationResult.Replaced, result.Result);
        Assert.IsType<StaleElementException>(result.LastError);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"took {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void Confirm_OnAControl_NeitherProbesReadinessNorLogs()
    {
        var label = new ConfirmLabel(Page);
        Context.Invocations.Clear();

        label.Wait(() => 1, value => value == 2, 30);

        // The page root is found through the context; readiness would look it up.
        Context.Verify(c => c.FindElements(It.IsAny<Locator>()), Times.Never);
        Context.Verify(c => c.Logger, Times.Never);
    }

    [Fact]
    public void Confirm_OnAContainer_NeitherProbesReadinessNorLogs()
    {
        var container = new ConfirmContainer(Page);
        Context.Invocations.Clear();

        var result = container.Wait(() => 1, value => value == 2, 30);

        Assert.False(result.IsConfirmed);
        Context.Verify(c => c.FindElements(It.IsAny<Locator>()), Times.Never);
        Context.Verify(c => c.Logger, Times.Never);
    }

    /// <summary>A control that exposes its protected <c>Confirm</c> for the tests.</summary>
    private sealed class ConfirmLabel(IMauiScope<TestPage> scope) : Label<TestPage>(scope, "Probe")
    {
        public Confirmation<T> Wait<T>(Func<T?> read, Func<T?, bool> done, int timeoutMs)
            => Confirm(read, done, timeoutMs);
    }

    /// <summary>A container that exposes its protected <c>Confirm</c> for the tests.</summary>
    private sealed class ConfirmContainer(IMauiScope<TestPage> scope)
        : ContainerObjectBase<TestPage, ConfirmContainer>(scope, "Probe")
    {
        public Confirmation<T> Wait<T>(Func<T?> read, Func<T?, bool> done, int timeoutMs)
            => Confirm(read, done, timeoutMs);
    }
}
