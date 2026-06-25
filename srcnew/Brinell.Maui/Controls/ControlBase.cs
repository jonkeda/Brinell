using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Utilities;
using System.Runtime.CompilerServices;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for all MAUI controls implementing the Is/Wait/Assert pattern with fluent chaining.
/// Controls find elements within their scope (page, container, or list item).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract class ControlBase<TScope> : ControlObjectBase<TScope>, IControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly IMauiScope<TScope> _mauiScope;
    
    /// <summary>
    /// Creates a new control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator used to find the control element.</param>
    protected ControlBase(IMauiScope<TScope> scope, Locator locator)
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
    protected ControlBase(IMauiScope<TScope> scope, string locatorValue)
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

    #region RunPoll

    private bool RunPoll(string? value, Func<bool> condition,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var stopwatch = Stopwatch.StartNew();
         Logger?.LogEntry(TestName, PageName, ControlId, caller ?? string.Empty, value);

        var ok = false;
        Exception? lastException = null;
        while (stopwatch.ElapsedMilliseconds < (timeoutMs ?? DefaultTimeoutMs))
        {
            try
            {
                if (condition())
                {
                    ok = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                // Polling expects transient failures (stale elements, not-yet-rendered)
            }

            WaitHelper.Pause(PollingIntervalMs);
        }
        stopwatch.Stop();
        if (ok)
        {
            Logger?.LogExit(TestName, PageName, ControlId, caller ?? string.Empty,
                LogResult.Success, (int)stopwatch.ElapsedMilliseconds);

        }
        else
        {
            Logger?.LogExit(TestName, PageName, ControlId, caller ?? string.Empty,
                LogResult.Error, (int)stopwatch.ElapsedMilliseconds, lastException?.Message);

            if (lastException != null)
            {
                throw lastException;
            }
        }
        return ok;
    }

    protected bool RunWait(Func<bool> operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        return RunPoll(null, () =>
        {
            return operation();
        }, timeoutMs, caller);
    }

    protected bool RunWaitWithElement(Func<IMauiElement, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        return RunPoll(null, () =>
        {
            
            var element = FindElement();
            EnsureVisible(element);
            return coreOperation(element);
        }, timeoutMs, caller);
    }

    protected TScope RunDo(Action operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        RunPoll(null, () =>
        {
            operation();
            return true;
        }, timeoutMs, caller);
        return ContainingScope;
    }

    protected TScope RunDoWithElement(Action<IMauiElement> coreOperation,
        int? timeoutMs = null, bool doEnsureVisible = true, [CallerMemberName] string? caller = null)
    {
        RunPoll(null, () =>
        {

            var element = FindElement();
            if (doEnsureVisible)
            {
                EnsureVisible(element);
            }
            coreOperation(element);
            return true;
        }, timeoutMs, caller);
        return ContainingScope;
    }

    protected TScope RunSetWithElement<T>(T? value, Action<IMauiElement> coreOperation, 
         int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (value == null)
        {
            return ContainingScope;
        }
        RunPoll(value?.ToString(), () =>
        {
            var element = FindElement();
            EnsureVisible(element);
            coreOperation(element);
            return true;
        }, timeoutMs, caller);
        return ContainingScope;
    }

    protected T? RunGetWithElement<T>(Func<IMauiElement, T> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var value = default(T);
        RunPoll(null, () =>
        {
            var element = FindElement();
            EnsureVisible(element);
            value = coreOperation(element);
            return true;
        }, timeoutMs, caller);
        return value;
    }

    /// <summary>
    /// Run assertion with custom comparison function.
    /// </summary>
    protected TScope RunAssert<T>(T? expected, Func<T?> getActual,
        Func<T?, T?, bool> compare, string? message = null,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }
        RunPoll(null, () =>
        {
            var actual = getActual();
            if (!compare(actual, expected))
            {
                throw new AssertionException(message ?? "Assert exception", expected, actual);
            }
            return true;
        }, timeoutMs, caller);
        return ContainingScope;
    }

    protected TScope RunAssertWithElement<T>(T? expected, Func<IMauiElement, T?> getActual,
        Func<T?, T?, bool> compare, string? message = null,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }
        RunPoll(null, () =>
        {
            var element = FindElement();
            EnsureVisible(element);

            var actual = getActual(element);
            if (!compare(actual, expected))
            {
                throw new AssertionException(message ?? "Assert exception", expected, actual);
            }
            return true;
        }, timeoutMs, caller);
        return ContainingScope;
    }

    #endregion

    private void EnsureScopeReady(int? timeoutMs = null)
    {
        if (!ContainingScope.IsReady(timeoutMs))
        {
            throw new Exception();
        }
    }

    #region Element Finding

    /// <summary>
    /// Tries to find the element within the scope.
    /// </summary>
    /// <returns>The element if found, null otherwise.</returns>
    protected virtual IMauiElement? TryFindElement()
    {
        EnsureScopeReady();
        return _mauiScope.TryFindElement(Locator);
    }

    /// <summary>
    /// Finds the element within the scope.
    /// </summary>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    protected virtual IMauiElement FindElement()
    {
        EnsureScopeReady();
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
                    
                WaitHelper.Pause(PollingIntervalMs);
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
            
            WaitHelper.Pause(PollingIntervalMs);
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

    #region Basic Interactions

    protected void EnsureSettableCore(IMauiElement element)
    {
        EnsureEnabledCore(element);
    }

    /// <summary>
    /// Sends keyboard keys to the control. Uses framework's Run for logging.
    /// Optimized to find element once and reuse.
    /// </summary>
    /// <param name="keys">The keys to send.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public virtual TScope SendKeys(string keys, int? timeoutMs = null)
    {
        return RunSetWithElement(keys, element =>
        {
            SendKeysCore(element, keys);
        }, timeoutMs);
    }
    
    /// <summary>
    /// Core implementation of SendKeys using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="keys">The keys to send.</param>
    protected virtual void SendKeysCore(IMauiElement element, string keys)
    {
        element.SendKeys(keys);
    }

    #endregion

    #region Visible
    
    /// <summary>
    /// Checks if element is visible using pre-found element.
    /// No stale element handling - element is found once at operation start.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if visible, false otherwise.</returns>
    protected bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
    
    /// <inheritdoc />
    public bool? IsVisible()
    {
        return IsVisibleCore(TryFindElement());
    }

    /// <inheritdoc />
    public bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null)
            return true;

        return RunWaitWithElement(
            element => IsVisibleCore(element) == expected.Value,
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
        return RunPoll(null, () => IsVisibleCore(element) == expected, timeoutMs);
    }

    protected virtual void EnsureVisibleCore(IMauiElement element, int timeout)
    {
        if (IsVisibleCore(element) != true)
        {
            element.ScrollIntoView();

            if (!WaitVisibleCore(element, true, timeout))
            {
                throw new TimeoutException(
                    $"Element was not visible within {timeout}ms after scrolling into view. Locator: {Locator}");
            }
        }
    }

    /// <summary>
    /// Ensures element is scrolled into view before performing an action.
    /// Called automatically by interaction methods.
    /// </summary>
    /// <param name="element">The element to ensure visibility for.</param>
    protected void EnsureVisible(IMauiElement element)
    {
        EnsureVisibleCore(element, DefaultTimeoutMs);
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
        return RunAssertWithElement(expected,
            e => IsVisibleCore(e), (actual, expected1) =>  (actual == expected1),
            null, timeoutMs);
    }

    #endregion

    #region Enabled

    protected virtual void EnsureEnabledCore(IMauiElement element)
    {
        if (IsEnabledCore(element) != true)
        {
            throw new TimeoutException(
                $"Element was not enabled. Locator: {Locator}");
        }
    }

    /// <summary>
    /// Checks if element is enabled using pre-found element.
    /// No stale element handling - element is found once at operation start.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if enabled, false otherwise.</returns>
    protected bool? IsEnabledCore(IMauiElement? element)
    {
        return element?.Enabled;
    }
    
    public bool? IsEnabled()
    {
        return IsEnabledCore(TryFindElement());
    }

    /// <summary>
    /// Polls enabled state using pre-found element.
    /// </summary>
    /// <param name="expected">The expected enabled state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    /// <inheritdoc />
    public bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        return RunWaitWithElement(
            element => IsEnabledCore(element) == expected.Value,
            timeoutMs);
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
        return RunAssertWithElement(expected,
            IsEnabledCore, (actual, expected1) => (actual == expected1),
            null, timeoutMs);
    }

    #endregion

    #region Exists

    protected bool? IsExistsCore(IMauiElement? element)
    {
        return element != null;
    }

    /// <inheritdoc />
    public bool IsExists()
    {
        return IsExistsCore(TryFindElement()) == true;
    }

    /// <inheritdoc />
    public bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        return RunWaitWithElement(
            element => IsExistsCore(element) == expected!.Value,
            timeoutMs);
    }

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
        return RunAssertWithElement(expected,
            IsExistsCore, (actual, expected1) => (actual == expected1),
            null, timeoutMs);
    }

    #endregion
    
    #region Text
    
    /// <summary>
    /// Gets the text of the element using pre-found element.
    /// Override in derived classes for platform-specific text retrieval.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The element text, or null if element is null.</returns>
    protected virtual string? GetTextCore(IMauiElement element)
    {
        return element.Text;
    }
    
    public string? GetText(int? timeoutMs = null)
    {
        return RunGetWithElement(element => GetTextCore(element), timeoutMs);
    }
    
    public bool WaitText(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        return RunWaitWithElement(
            element => GetTextCore(element) == expected, timeoutMs);
    }
    
    public TScope AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithElement(expected,
            GetTextCore, (actual, expected1) => (actual == expected1),
            null, timeoutMs);
    }
    
    public TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithElement(expected,
            GetTextCore, (actual, expected1) => (actual?.Contains(expected!) == true),
            null, timeoutMs);
    }
    
    public TScope AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithElement(expected,
            GetTextCore, (actual, expected1) => (actual?.StartsWith(expected!) == true),
            null, timeoutMs);
    }
    
    public TScope AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithElement(expected,
            GetTextCore, (actual, expected1) => (actual?.EndsWith(expected!) == true),
            null, timeoutMs);
    }
    
    public TScope AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithElement<bool?>(expected,
            e => { return string.IsNullOrEmpty(GetTextCore(e)); },
            (actual, expected1) => actual == expected1, null, timeoutMs);
    }

    #endregion

    #region Attributes

    protected virtual string? GetAttributeCore(IMauiElement element, string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return element.GetAttribute(name);
    }

    public string? GetAttribute(string name, int? timeoutMs = null)
    {
        return RunGetWithElement(element => GetAttributeCore(element, name), timeoutMs);
    }

    public bool WaitAttribute(string name, string? expected, int? timeoutMs = null)
    {
        if (expected == null)
            return true;

        return RunWaitWithElement(
            element => GetAttributeCore(element, name) == expected, timeoutMs);
    }

    public TScope AssertAttribute(string name, string? expected, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithElement(expected,
            element => GetAttributeCore(element, name), (actual, expected1) => (actual == expected1),
            null, timeoutMs);
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
        return RunDoWithElement(element =>
        {
            ScrollIntoViewCore(element);
        }, timeoutMs, true);
    }

    /// <summary>
    /// Core scroll implementation. Uses element's ScrollIntoView method.
    /// </summary>
    /// <param name="element">The element to scroll into view.</param>
    protected virtual void ScrollIntoViewCore(IMauiElement element)
    {
        element.ScrollIntoView();
    }

    #endregion

}
