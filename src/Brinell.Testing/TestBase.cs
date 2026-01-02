using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Testing;

/// <summary>
/// Generic test base class with context support and lifecycle management.
/// Provides logging, lifecycle hooks, and common test utilities.
/// </summary>
/// <typeparam name="TContext">The context type (e.g., MockRepository, DbContext, UITestContext).</typeparam>
public abstract class TestBase<TContext> : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    protected TContext Context { get; private set; } = default!;

    /// <summary>
    /// Test name for logging.
    /// </summary>
    public string TestName { get; private set; } = string.Empty;

    /// <summary>
    /// Stopwatch for timing test execution.
    /// </summary>
    protected Stopwatch Timer { get; } = new();

    protected TestBase(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Create and configure the context (template method).
    /// Override in derived classes to create specific context type.
    /// </summary>
    protected abstract TContext CreateContext();

    /// <summary>
    /// Initialize test context asynchronously (template method).
    /// Override to perform async setup.
    /// </summary>
    protected virtual Task InitializeContextAsync() => Task.CompletedTask;

    /// <summary>
    /// Cleanup test context asynchronously (template method).
    /// Override to perform async teardown.
    /// </summary>
    protected virtual Task CleanupContextAsync() => Task.CompletedTask;

    /// <summary>
    /// xUnit lifecycle: Initialize test.
    /// </summary>
    public async Task InitializeAsync()
    {
        Context = CreateContext();
        await InitializeContextAsync();
    }

    /// <summary>
    /// xUnit lifecycle: Dispose test.
    /// </summary>
    public async Task DisposeAsync()
    {
        await CleanupContextAsync();
        if (Context is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Context is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Logging

    /// <summary>
    /// Log a message.
    /// </summary>
    protected void Log(string message)
    {
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
    }

    /// <summary>
    /// Log an action.
    /// </summary>
    protected void LogAction(string action, string details = "")
    {
        var msg = string.IsNullOrEmpty(details) 
            ? $"[ACTION] {action}" 
            : $"[ACTION] {action}: {details}";
        Log(msg);
    }

    /// <summary>
    /// Log an assertion.
    /// </summary>
    protected void LogAssertion(string assertion, string expected, string actual, bool passed)
    {
        var status = passed ? "PASS" : "FAIL";
        Log($"[ASSERT {status}] {assertion} | Expected: {expected} | Actual: {actual}");
    }

    /// <summary>
    /// Log arrange phase.
    /// </summary>
    protected void LogArrange(string details)
    {
        Log($"[ARRANGE] {details}");
    }

    /// <summary>
    /// Log act phase.
    /// </summary>
    protected void LogAct(string details)
    {
        Log($"[ACT] {details}");
    }

    /// <summary>
    /// Log assert phase.
    /// </summary>
    protected void LogAssert(string details)
    {
        Log($"[ASSERT] {details}");
    }

    #endregion

    #region Timing

    /// <summary>
    /// Measure action execution time.
    /// </summary>
    protected void MeasureAction(string name, Action action)
    {
        Timer.Restart();
        action();
        Timer.Stop();
        LogAction(name, $"{Timer.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Measure async action execution time.
    /// </summary>
    protected async Task MeasureActionAsync(string name, Func<Task> action)
    {
        Timer.Restart();
        await action();
        Timer.Stop();
        LogAction(name, $"{Timer.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Assert that an action throws a specific exception.
    /// </summary>
    protected T AssertThrows<T>(Action action) where T : Exception
    {
        var ex = Assert.Throws<T>(action);
        LogAssertion($"AssertThrows<{typeof(T).Name}>", typeof(T).Name, ex.GetType().Name, true);
        return ex;
    }

    /// <summary>
    /// Assert that an async action throws a specific exception.
    /// </summary>
    protected async Task<T> AssertThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        var ex = await Assert.ThrowsAsync<T>(action);
        LogAssertion($"AssertThrowsAsync<{typeof(T).Name}>", typeof(T).Name, ex.GetType().Name, true);
        return ex;
    }

    /// <summary>
    /// Assert that a collection contains an item.
    /// </summary>
    protected void AssertContains<T>(IEnumerable<T> collection, T item)
    {
        Assert.Contains(item, collection);
        LogAssertion("AssertContains", item?.ToString() ?? "null", "found", true);
    }

    /// <summary>
    /// Assert that a collection does not contain an item.
    /// </summary>
    protected void AssertDoesNotContain<T>(IEnumerable<T> collection, T item)
    {
        Assert.DoesNotContain(item, collection);
        LogAssertion("AssertDoesNotContain", item?.ToString() ?? "null", "not found", true);
    }

    #endregion
}
