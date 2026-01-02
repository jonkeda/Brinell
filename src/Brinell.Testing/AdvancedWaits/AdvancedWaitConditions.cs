using System.Diagnostics;

namespace Brinell.Testing.AdvancedWaits;

/// <summary>
/// Advanced wait conditions for complex UI scenarios.
/// Handles animations, transitions, DOM stability, and custom predicates.
/// </summary>
public class AdvancedWaitConditions
{
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Wait for animation to complete (reduced opacity or transform).
    /// </summary>
    public async Task WaitForAnimationCompleteAsync(
        Func<Task<bool>> isAnimationCompleteFunc,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < effectiveTimeout)
        {
            if (await isAnimationCompleteFunc())
            {
                return;
            }
            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Animation did not complete within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for CSS transition to complete.
    /// </summary>
    public async Task WaitForTransitionCompleteAsync(
        string selector,
        Func<string, Task<string>> getComputedStyleFunc,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();
        var previousStyle = await getComputedStyleFunc(selector);

        while (watch.Elapsed < effectiveTimeout)
        {
            var currentStyle = await getComputedStyleFunc(selector);
            if (currentStyle == previousStyle)
            {
                // Style hasn't changed, transition complete
                return;
            }
            previousStyle = currentStyle;
            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Transition did not complete within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for DOM to stabilize (no changes for specified duration).
    /// </summary>
    public async Task WaitForDOMStabilityAsync(
        Func<Task<int>> getElementCountFunc,
        int stabilityDurationMs = 500,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();
        var lastCount = await getElementCountFunc();
        var stabilityWatch = Stopwatch.StartNew();

        while (watch.Elapsed < effectiveTimeout)
        {
            var currentCount = await getElementCountFunc();

            if (currentCount == lastCount)
            {
                if (stabilityWatch.ElapsedMilliseconds >= stabilityDurationMs)
                {
                    return;
                }
            }
            else
            {
                lastCount = currentCount;
                stabilityWatch.Restart();
            }

            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"DOM did not stabilize within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for element to be visible (opacity > 0, height > 0).
    /// </summary>
    public async Task WaitForElementVisibleAsync(
        Func<Task<bool>> isVisibleFunc,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < effectiveTimeout)
        {
            if (await isVisibleFunc())
            {
                return;
            }
            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Element did not become visible within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for element to be hidden.
    /// </summary>
    public async Task WaitForElementHiddenAsync(
        Func<Task<bool>> isHiddenFunc,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < effectiveTimeout)
        {
            if (await isHiddenFunc())
            {
                return;
            }
            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Element did not become hidden within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for custom condition with predicate.
    /// </summary>
    public async Task WaitForConditionAsync(
        Func<Task<bool>> conditionFunc,
        string conditionDescription = "condition",
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < effectiveTimeout)
        {
            try
            {
                if (await conditionFunc())
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                // Condition might throw during polling, continue
                if (watch.Elapsed >= effectiveTimeout)
                {
                    throw new WaitConditionException(
                        $"Condition '{conditionDescription}' failed after {effectiveTimeout.TotalSeconds}s: {ex.Message}", ex);
                }
            }

            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Condition '{conditionDescription}' not met within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for multiple elements to load.
    /// </summary>
    public async Task WaitForElementsLoadedAsync(
        Func<Task<int>> getElementCountFunc,
        int expectedCount,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < effectiveTimeout)
        {
            var count = await getElementCountFunc();
            if (count >= expectedCount)
            {
                return;
            }
            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Expected {expectedCount} elements but got less within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for text content to appear.
    /// </summary>
    public async Task WaitForTextAsync(
        Func<Task<string>> getTextFunc,
        string expectedText,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < effectiveTimeout)
        {
            var text = await getTextFunc();
            if (text?.Contains(expectedText) == true)
            {
                return;
            }
            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Text '{expectedText}' did not appear within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for network activity to complete (no pending requests).
    /// </summary>
    public async Task WaitForNetworkIdleAsync(
        Func<Task<int>> getPendingRequestsFunc,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();
        var idleDuration = TimeSpan.FromMilliseconds(500);
        var lastIdleTime = DateTime.UtcNow;

        while (watch.Elapsed < effectiveTimeout)
        {
            var pendingRequests = await getPendingRequestsFunc();

            if (pendingRequests == 0)
            {
                if (DateTime.UtcNow - lastIdleTime >= idleDuration)
                {
                    return;
                }
            }
            else
            {
                lastIdleTime = DateTime.UtcNow;
            }

            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Network did not become idle within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for element to receive focus.
    /// </summary>
    public async Task WaitForElementFocusedAsync(
        Func<Task<bool>> isFocusedFunc,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < effectiveTimeout)
        {
            if (await isFocusedFunc())
            {
                return;
            }
            await Task.Delay(_pollInterval);
        }

        throw new WaitConditionException(
            $"Element did not receive focus within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Set custom timeout for future waits.
    /// </summary>
    public AdvancedWaitConditions WithTimeout(TimeSpan timeout)
    {
        var clone = new AdvancedWaitConditions();
        // Would need to store and apply custom timeout in future calls
        return clone;
    }

    /// <summary>
    /// Assert that condition completes within budget.
    /// </summary>
    public async Task AssertCompletesWithinAsync(
        Func<Task> actionFunc,
        long maxMilliseconds)
    {
        var watch = Stopwatch.StartNew();
        await actionFunc();
        watch.Stop();

        if (watch.ElapsedMilliseconds > maxMilliseconds)
        {
            throw new WaitConditionException(
                $"Operation took {watch.ElapsedMilliseconds}ms but should complete within {maxMilliseconds}ms");
        }
    }
}

/// <summary>
/// Builder for complex wait conditions.
/// </summary>
public class WaitBuilder
{
    private readonly AdvancedWaitConditions _waiter = new();
    private TimeSpan? _timeout;

    /// <summary>
    /// Set timeout for wait condition.
    /// </summary>
    public WaitBuilder Timeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Wait for animation completion.
    /// </summary>
    public async Task AnimationCompleteAsync(Func<Task<bool>> isCompleteFunc)
    {
        await _waiter.WaitForAnimationCompleteAsync(isCompleteFunc, _timeout);
    }

    /// <summary>
    /// Wait for transition completion.
    /// </summary>
    public async Task TransitionCompleteAsync(
        string selector,
        Func<string, Task<string>> getStyleFunc)
    {
        await _waiter.WaitForTransitionCompleteAsync(selector, getStyleFunc, _timeout);
    }

    /// <summary>
    /// Wait for DOM stability.
    /// </summary>
    public async Task DOMStableAsync(Func<Task<int>> getCountFunc)
    {
        await _waiter.WaitForDOMStabilityAsync(getCountFunc, 500, _timeout);
    }

    /// <summary>
    /// Wait for custom condition.
    /// </summary>
    public async Task ForAsync(
        Func<Task<bool>> conditionFunc,
        string description = "condition")
    {
        await _waiter.WaitForConditionAsync(conditionFunc, description, _timeout);
    }
}

/// <summary>
/// Exception for wait condition failures.
/// </summary>
public class WaitConditionException : Exception
{
    public WaitConditionException(string message) : base(message) { }
    public WaitConditionException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Extension methods for advanced waits.
/// </summary>
public static class WaitExtensions
{
    /// <summary>
    /// Create wait builder.
    /// </summary>
    public static WaitBuilder Until() => new();

    /// <summary>
    /// Create advanced waiter.
    /// </summary>
    public static AdvancedWaitConditions CreateWaiter() => new();
}
