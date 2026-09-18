namespace Brinell.Maui.Tests.Semantic;

/// <summary>
/// <c>Until</c>: the one way a Core method waits. It polls a read of an element the method
/// already holds, and does nothing a generated wrapper has already done.
/// </summary>
public class UntilTests : SemanticControlTestsBase
{
    [Fact]
    public void Until_ReadThatSucceedsLate_ReturnsTrue()
    {
        var reads = 0;

        var done = new UntilLabel(Page).Wait(() => ++reads, count => count >= 3, 1000, out var lastError);

        Assert.True(done);
        Assert.Equal(3, reads);
        Assert.Null(lastError);
    }

    [Fact]
    public void Until_ReadThatKeepsFailing_ReturnsFalseWithTheLastError()
    {
        var done = new UntilLabel(Page).Wait<string>(
            () => throw new ElementNotFoundException("gone"), _ => true, 30, out var lastError);

        Assert.False(done);
        var error = Assert.IsType<ElementNotFoundException>(lastError);
        Assert.Equal("gone", error.Message);
    }

    [Fact]
    public void Until_ReadThatSucceedsWithTheWrongValue_ReturnsFalseWithNoError()
    {
        var done = new UntilLabel(Page).Wait(() => 1, value => value == 2, 30, out var lastError);

        Assert.False(done);
        Assert.Null(lastError);
    }

    [Fact]
    public void Until_OnAControl_NeitherProbesPageReadinessNorLogs()
    {
        var label = new UntilLabel(Page);
        Context.Invocations.Clear();

        label.Wait(() => 1, value => value == 2, 30, out _);

        // The page root is found through the context; readiness would look it up.
        Context.Verify(c => c.FindElements(It.IsAny<Locator>()), Times.Never);
        Context.Verify(c => c.Logger, Times.Never);
    }

    [Fact]
    public void Until_OnAContainer_NeitherProbesPageReadinessNorLogs()
    {
        var container = new UntilContainer(Page);
        Context.Invocations.Clear();

        var done = container.Wait(() => 1, value => value == 2, 30, out _);

        Assert.False(done);
        Context.Verify(c => c.FindElements(It.IsAny<Locator>()), Times.Never);
        Context.Verify(c => c.Logger, Times.Never);
    }

    /// <summary>A control that exposes its protected <c>Until</c> for the tests.</summary>
    private sealed class UntilLabel(IMauiScope<TestPage> scope) : Label<TestPage>(scope, "Probe")
    {
        public bool Wait<T>(Func<T?> read, Func<T?, bool> done, int timeoutMs, out Exception? lastError)
            => Until(read, done, timeoutMs, out lastError);
    }

    /// <summary>A container that exposes its protected <c>Until</c> for the tests.</summary>
    private sealed class UntilContainer(IMauiScope<TestPage> scope)
        : ContainerObjectBase<TestPage, UntilContainer>(scope, "Probe")
    {
        public bool Wait<T>(Func<T?> read, Func<T?, bool> done, int timeoutMs, out Exception? lastError)
            => Until(read, done, timeoutMs, out lastError);
    }
}
