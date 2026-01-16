using Brinell.Core.Abstractions.Controls;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for all MAUI controls implementing the Is/Wait/Assert pattern with fluent chaining.
/// Controls find elements within their scope (page, container, or list item).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public class MauiControlBase<TScope> : ControlObjectBase<TScope>, IControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly IMauiScope<TScope> _mauiScope;
    
    /// <summary>
    /// Creates a new control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator used to find the control element.</param>
    public MauiControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(locator, scope)
    {
        _mauiScope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    /// <summary>
    /// Creates a new control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(new Locator(scope?.DefaultLocatorStrategy ?? LocatorStrategy.AutomationId, locatorValue), 
               scope!)
    {
        _mauiScope = scope ?? throw new ArgumentNullException(nameof(scope));
        if (string.IsNullOrEmpty(locatorValue))
            throw new ArgumentNullException(nameof(locatorValue));
    }

    /// <summary>
    /// Gets the containing scope for fluent chaining.
    /// </summary>
    protected TScope ContainingScope => _mauiScope.Self;
    
    /// <summary>
    /// Gets the MAUI test context.
    /// </summary>
    protected IMauiTestContext Context => _mauiScope.Context;
    
    /// <summary>
    /// Gets the default timeout in milliseconds.
    /// </summary>
    protected int DefaultTimeoutMs => Context.Timeouts.DefaultWait;
    
    /// <summary>
    /// Gets the polling interval in milliseconds.
    /// </summary>
    protected int PollingIntervalMs => Context.Timeouts.PollingInterval;
    
    #region Polling
    
    /// <summary>
    /// Polls a condition until it returns true or timeout is reached.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool Poll(Func<bool> condition, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions during polling, continue trying
            }
            
            Thread.Sleep(PollingIntervalMs);
        }
        
        // Final check after timeout
        try
        {
            return condition();
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
    
    #region Element Finding
    
    /// <summary>
    /// Tries to find the element within the scope.
    /// </summary>
    /// <returns>The element if found, null otherwise.</returns>
    protected IMauiElement? TryFindElement()
    {
        return _mauiScope.TryFindElement(Locator);
    }
    
    /// <summary>
    /// Finds the element within the scope.
    /// </summary>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    protected IMauiElement FindElement()
    {
        return _mauiScope.FindElement(Locator);
    }
    
    #endregion
    
    #region Logging Helpers
    
    /// <summary>
    /// Gets logging context information.
    /// </summary>
    private string TestName => "Test"; // TODO: Get from test context when available
    private string PageName => Page?.GetType().Name ?? "Unknown";
    private string ControlId => Locator.Value;
    private ITestLogger? Logger => Context.Logger;
    
    /// <summary>
    /// Run operation without a value parameter.
    /// </summary>
    protected void Run(string action, Action operation)
    {
        Run<object?>(action, null, operation);
    }
    
    /// <summary>
    /// Run operation with a typed value parameter.
    /// </summary>
    protected void Run<T>(string action, T? value, Action operation)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger?.LogEntry(TestName, PageName, ControlId, action, value?.ToString());
        
        try
        {
            operation();
            stopwatch.Stop();
            Logger?.LogExit(TestName, PageName, ControlId, action, 
                LogResult.Success, (int)stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger?.LogExit(TestName, PageName, ControlId, action, 
                LogResult.Error, (int)stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// Run operation that returns a value.
    /// </summary>
    protected TResult Run<TResult>(string action, Func<TResult> operation)
    {
        return Run<object?, TResult>(action, null, operation);
    }
    
    /// <summary>
    /// Run operation with a typed value parameter that returns a result.
    /// </summary>
    protected TResult Run<TValue, TResult>(string action, TValue? value, Func<TResult> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger?.LogEntry(TestName, PageName, ControlId, action, value?.ToString());
        
        try
        {
            var result = operation();
            stopwatch.Stop();
            Logger?.LogExit(TestName, PageName, ControlId, action, 
                LogResult.Success, (int)stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger?.LogExit(TestName, PageName, ControlId, action, 
                LogResult.Error, (int)stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// Run assertion with default equality comparison.
    /// </summary>
    protected TScope RunAssert<T>(string assertType, T? expected, Func<T?> getActual, string? message = null)
    {
        return RunAssert(assertType, expected, getActual, (actual, exp) => Equals(actual, exp), message);
    }
    
    /// <summary>
    /// Run assertion with custom comparison function.
    /// </summary>
    protected TScope RunAssert<T>(string assertType, T? expected, Func<T?> getActual, 
        Func<T?, T?, bool> compare, string? message = null)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger?.LogEntry(TestName, PageName, ControlId, assertType, expected?.ToString());
        
        var actual = getActual();
        stopwatch.Stop();
        
        if (compare(actual, expected))
        {
            Logger?.LogAssertExit(TestName, PageName, ControlId, assertType,
                actual?.ToString(), expected?.ToString(), LogResult.Success, (int)stopwatch.ElapsedMilliseconds);
            return ContainingScope;
        }
        else
        {
            Logger?.LogAssertExit(TestName, PageName, ControlId, assertType,
                actual?.ToString(), expected?.ToString(), LogResult.Fail, (int)stopwatch.ElapsedMilliseconds, message);
            throw new AssertionException(
                message ?? $"Expected '{expected}' but got '{actual}'");
        }
    }
    
    #endregion
    
    #region State (Is methods - immediate, no waiting)
    
    /// <inheritdoc />
    public bool IsExists()
    {
        return TryFindElement() != null;
    }
    
    /// <inheritdoc />
    public bool? IsVisible()
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            return element.Displayed;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public bool? IsEnabled()
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            return element.Enabled;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }
    
    #endregion
    
    #region Waiting (poll until condition or timeout)
    
    /// <inheritdoc />
    public bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        return Poll(
            () => IsExists() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        return Poll(
            () => IsVisible() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        return Poll(
            () => IsEnabled() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
    
    #region Assertions (throw on failure)
    
    /// <inheritdoc />
    public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return ContainingScope;
        
        if (!WaitExists(expected, timeoutMs))
        {
            var actual = IsExists();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to exist" : "not to exist")} but it {(actual ? "exists" : "does not exist")}. Locator: {Locator}");
        }
        
        return ContainingScope;
    }
    
    /// <inheritdoc />
    public TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return ContainingScope;
        
        if (!WaitVisible(expected, timeoutMs))
        {
            var actual = IsVisible();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be visible" : "not to be visible")} but visibility is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }
        
        return ContainingScope;
    }
    
    /// <inheritdoc />
    public TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return ContainingScope;
        
        if (!WaitEnabled(expected, timeoutMs))
        {
            var actual = IsEnabled();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be enabled" : "to be disabled")} but enabled state is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }
        
        return ContainingScope;
    }
    
    #endregion
    
    #region Text
    
    /// <inheritdoc />
    public string? GetText(int? timeoutMs = null)
    {
        // Optionally wait for element to exist first
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        var element = TryFindElement();
        if (element == null) return null;
        
        return element.Text;
    }
    
    /// <inheritdoc />
    public bool WaitText(string? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        return Poll(
            () => GetText() == expected,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public TScope AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return ContainingScope;
        
        if (!WaitText(expected, timeoutMs))
        {
            var actual = GetText();
            throw new AssertionException(
                message ?? $"Expected text '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }
        
        return ContainingScope;
    }
    
    /// <inheritdoc />
    public TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return ContainingScope;
        
        var passed = Poll(
            () => GetText()?.Contains(expected) == true,
            timeoutMs ?? DefaultTimeoutMs);
        
        if (!passed)
        {
            var actual = GetText();
            throw new AssertionException(
                message ?? $"Expected text to contain '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }
        
        return ContainingScope;
    }
    
    #endregion
    
    #region Attributes
    
    /// <inheritdoc />
    public string? GetAttribute(string name)
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            return element.GetAttribute(name);
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }
    
    #endregion
}
