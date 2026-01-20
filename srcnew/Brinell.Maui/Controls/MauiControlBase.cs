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
    /// Gets the MAUI-typed scope for element finding operations.
    /// </summary>
    protected IMauiScope<TScope> MauiScope => _mauiScope;
    
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
    
    #region ScrollIntoView
    
    /// <summary>
    /// Scrolls the element into the visible viewport if not already visible.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope ScrollIntoView(int? timeoutMs = null)
    {
        return RunWithElement(nameof(ScrollIntoView), timeoutMs, element =>
        {
            ScrollIntoViewCore(element);
        }, skipEnsureVisible: true);
    }
    
    /// <summary>
    /// Core scroll implementation. Uses element's ScrollIntoView method.
    /// </summary>
    /// <param name="element">The element to scroll into view.</param>
    protected virtual void ScrollIntoViewCore(IMauiElement element)
    {
        // Skip if already visible
        if (IsVisibleCore(element) == true)
        {
            return;
        }
        
        try
        {
            // Use the element's built-in ScrollIntoView which uses Selenium 4 API
            element.ScrollIntoView(Context.Driver);
            
            // Brief pause for scroll animation
            Thread.Sleep(100);
        }
        catch (Exception)
        {
            // Best effort - swallow scroll failures
        }
    }
    
    /// <summary>
    /// Ensures element is scrolled into view before performing an action.
    /// Called automatically by interaction methods.
    /// </summary>
    /// <param name="element">The element to ensure visibility for.</param>
    protected void EnsureVisible(IMauiElement element)
    {
        if (IsVisibleCore(element) != true)
        {
            ScrollIntoViewCore(element);
        }
    }
    
    #endregion
    
    #region Element Finding
    
    /// <summary>
    /// Tries to find the element within the scope.
    /// </summary>
    /// <returns>The element if found, null otherwise.</returns>
    protected virtual IMauiElement? TryFindElement()
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
    
    /// <summary>
    /// Finds element, waiting for it to exist if timeout is specified.
    /// Single entry point for element retrieval with optional wait.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout to wait for element to exist.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found within timeout.</exception>
    protected IMauiElement FindElementWithWait(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            var timeout = timeoutMs.Value;
            var stopwatch = Stopwatch.StartNew();
            
            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                var element = TryFindElement();
                if (element != null)
                    return element;
                    
                Thread.Sleep(PollingIntervalMs);
            }
        }
        
        return FindElement(); // Throws if not found
    }
    
    /// <summary>
    /// Polls with pre-found element reference.
    /// Element is found once at operation start, no re-finding needed.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="condition">The condition to check.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool PollWithElement(
        IMauiElement element,
        Func<IMauiElement, bool> condition,
        int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            if (condition(element))
                return true;
            
            Thread.Sleep(PollingIntervalMs);
        }
        
        // Final check
        return condition(element);
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
    
    /// <summary>
    /// Run operation that finds element first, then executes core logic.
    /// Logging wraps the entire operation including element finding.
    /// Automatically scrolls element into view before action.
    /// </summary>
    /// <param name="action">The action name for logging.</param>
    /// <param name="timeoutMs">Optional timeout for element finding.</param>
    /// <param name="coreOperation">The core operation to execute with the found element.</param>
    /// <param name="skipEnsureVisible">If true, skips automatic scroll into view (used by ScrollIntoView itself).</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    protected TScope RunWithElement(string action, int? timeoutMs, Action<IMauiElement> coreOperation, bool skipEnsureVisible = false)
    {
        Run(action, () =>
        {
            var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
            if (!skipEnsureVisible)
            {
                EnsureVisible(element);
            }
            coreOperation(element);
        });
        return ContainingScope;
    }
    
    /// <summary>
    /// Run operation with value that finds element first, then executes core logic.
    /// Automatically scrolls element into view before action.
    /// </summary>
    /// <typeparam name="TValue">The type of the value parameter.</typeparam>
    /// <param name="action">The action name for logging.</param>
    /// <param name="value">The value to log.</param>
    /// <param name="timeoutMs">Optional timeout for element finding.</param>
    /// <param name="coreOperation">The core operation to execute with the found element.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    protected TScope RunWithElement<TValue>(string action, TValue? value, int? timeoutMs, 
        Action<IMauiElement> coreOperation)
    {
        Run(action, value, () =>
        {
            var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
            EnsureVisible(element);
            coreOperation(element);
        });
        return ContainingScope;
    }
    
    /// <summary>
    /// Run operation that finds element first, then executes core logic returning a result.
    /// Automatically scrolls element into view before action.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="action">The action name for logging.</param>
    /// <param name="timeoutMs">Optional timeout for element finding.</param>
    /// <param name="coreOperation">The core operation to execute with the found element.</param>
    /// <returns>The result of the operation.</returns>
    protected TResult RunWithElement<TResult>(string action, int? timeoutMs, 
        Func<IMauiElement, TResult> coreOperation)
    {
        return Run(action, () =>
        {
            var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
            EnsureVisible(element);
            return coreOperation(element);
        });
    }
    
    #endregion
    
    #region Basic Interactions
    
    /// <summary>
    /// Sends keyboard keys to the control. Uses framework's Run for logging.
    /// Optimized to find element once and reuse.
    /// </summary>
    /// <param name="keys">The keys to send.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public virtual TScope SendKeys(string keys)
    {
        return RunWithElement(nameof(SendKeys), keys, null, element =>
        {
            SendKeysCore(element, keys);
        });
    }
    
    /// <summary>
    /// Core implementation of SendKeys using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="keys">The keys to send.</param>
    protected virtual void SendKeysCore(IMauiElement element, string keys)
    {
        element.Click(); // Focus the element first
        element.SendKeys(keys);
    }
    
    #endregion
    
    #region State (Is methods - immediate, no waiting)
    
    /// <inheritdoc />
    public bool IsExists()
    {
        return TryFindElement() != null;
    }
    
    /// <summary>
    /// Checks if element is visible using pre-found element.
    /// No stale element handling - element is found once at operation start.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if visible, false otherwise.</returns>
    protected bool? IsVisibleCore(IMauiElement? element)
    {
        if (element == null) return null;
        return element.Displayed;
    }
    
    /// <inheritdoc />
    public bool? IsVisible()
    {
        return IsVisibleCore(TryFindElement());
    }
    
    /// <summary>
    /// Checks if element is enabled using pre-found element.
    /// No stale element handling - element is found once at operation start.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if enabled, false otherwise.</returns>
    protected bool? IsEnabledCore(IMauiElement? element)
    {
        if (element == null) return null;
        return element.Enabled;
    }
    
    /// <inheritdoc />
    public bool? IsEnabled()
    {
        return IsEnabledCore(TryFindElement());
    }
    
    #endregion
    
    #region Waiting - Core Methods (Element-Aware)
    
    /// <summary>
    /// Polls enabled state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected enabled state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitEnabledCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsEnabledCore(e) == expected,
            timeoutMs);
    }
    
    /// <summary>
    /// Polls visible state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected visible state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitVisibleCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsVisibleCore(e) == expected,
            timeoutMs);
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
    
    /// <summary>
    /// Asserts the element exists. Throws if it doesn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertExists(string? message = null, int? timeoutMs = null)
        => AssertExists(true, message, timeoutMs);
    
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
    
    /// <summary>
    /// Asserts the element is visible. Throws if it isn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertVisible(string? message = null, int? timeoutMs = null)
        => AssertVisible(true, message, timeoutMs);
    
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
    
    /// <summary>
    /// Asserts the element is enabled. Throws if it isn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertEnabled(string? message = null, int? timeoutMs = null)
        => AssertEnabled(true, message, timeoutMs);
    
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
    
    /// <summary>
    /// Gets the text of the element using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The element text, or null if element is null.</returns>
    protected string? GetTextCore(IMauiElement? element)
    {
        if (element == null) return null;
        return element.Text;
    }
    
    /// <inheritdoc />
    public string? GetText(int? timeoutMs = null)
    {
        // Optionally wait for element to exist first
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return GetTextCore(TryFindElement());
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
    
    /// <inheritdoc />
    public TScope AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return ContainingScope;
        
        var passed = Poll(
            () => GetText()?.StartsWith(expected) == true,
            timeoutMs ?? DefaultTimeoutMs);
        
        if (!passed)
        {
            var actual = GetText();
            throw new AssertionException(
                message ?? $"Expected text to start with '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }
        
        return ContainingScope;
    }
    
    /// <inheritdoc />
    public TScope AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return ContainingScope;
        
        var passed = Poll(
            () => GetText()?.EndsWith(expected) == true,
            timeoutMs ?? DefaultTimeoutMs);
        
        if (!passed)
        {
            var actual = GetText();
            throw new AssertionException(
                message ?? $"Expected text to end with '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }
        
        return ContainingScope;
    }
    
    /// <inheritdoc />
    public TScope AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return ContainingScope;
        
        var passed = Poll(
            () => 
            {
                var text = GetText();
                var isEmpty = string.IsNullOrEmpty(text);
                return isEmpty == expected.Value;
            },
            timeoutMs ?? DefaultTimeoutMs);
        
        if (!passed)
        {
            var actual = GetText();
            throw new AssertionException(
                message ?? $"Expected text {(expected.Value ? "to be empty" : "not to be empty")} but got '{actual ?? "(null)"}'. Locator: {Locator}");
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
        
        return element.GetAttribute(name);
    }
    
    #endregion
}
