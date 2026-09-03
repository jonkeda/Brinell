using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using Brinell.Core.Utilities;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Brinell.Html.Controls;

public abstract class ControlBase<TScope> : ObjectBase, IControlObject<TScope>, IHtmlAsyncControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    private readonly IHtmlScope<TScope> _scope;

    protected ControlBase(IHtmlScope<TScope> scope, Locator locator)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    protected ControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : this(scope, ResolveLocator(selectorOrId))
    {
    }

    protected Locator Locator { get; }

    protected TScope ContainingScope => _scope.Self;

    public override IHtmlTestContext Context => _scope.Context;

    protected IHtmlElement? TryFindElement() => _scope.TryFindElement(Locator);

    protected IHtmlElement FindElement() => _scope.FindElement(Locator);

    #region

    /// <summary>
    /// Gets logging context information.
    /// </summary>
    private string TestName => "Test";
    private string PageName => "Unknown";
    private string ControlId => Locator.Value;
    private ITestLogger? Logger => Context.Logger;


    #endregion
    
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

    protected bool RunWaitWithElement(Func<IHtmlElement, bool> coreOperation,
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

    protected TScope RunDoWithElement(Action<IHtmlElement> coreOperation,
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

    protected TScope RunSetWithElement<T>(T? value, Action<IHtmlElement> coreOperation,
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

    protected T? RunGetWithElement<T>(Func<IHtmlElement, T> coreOperation,
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

    protected TScope RunAssertWithElement<T>(T? expected, Func<IHtmlElement, T?> getActual,
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

    protected TScope RunWithElement(Action<IHtmlElement> action)
    {
        return RunDoWithElement(action);
    }

    protected TResult? RunWithElement<TResult>(Func<IHtmlElement, TResult> action)
    {
        return RunGetWithElement(action);
    }

    protected TScope RunAssert(Action<IHtmlElement> assertion)
    {
        var element = FindElement();
        assertion(element);
        return ContainingScope;
    }

    protected static Locator ResolveLocator(string selectorOrId)
    {
        if (string.IsNullOrWhiteSpace(selectorOrId))
        {
            throw new ArgumentNullException(nameof(selectorOrId));
        }

        return IsRawSelector(selectorOrId)
            ? Locator.ByCss(selectorOrId)
            : Locator.ByAutomationId(selectorOrId);
    }

    private static bool IsRawSelector(string value)
    {
        return value.StartsWith('#') ||
               value.StartsWith('.') ||
               value.StartsWith('[') ||
               value.Contains('#') ||
               value.Contains('[') ||
               value.Contains(':') ||
               value.Contains('>') ||
               value.Contains(' ');
    }

    #region Async helpers

    protected async Task<TScope> RunWithElementAsync(Func<IHtmlElement, Task> action)
    {
        var element = FindElement();
        await action(element).ConfigureAwait(false);
        return ContainingScope;
    }

    protected async Task<TResult> RunWithElementAsync<TResult>(Func<IHtmlElement, Task<TResult>> action)
    {
        var element = FindElement();
        return await action(element).ConfigureAwait(false);
    }

    protected IHtmlElement? TryFindAsyncElement() => TryFindElement();

    #endregion

    #region IHtmlAsyncControlObject<TScope> explicit implementation

    Task<bool> IHtmlAsyncControlObject<TScope>.IsExists()
        => Task.FromResult(IsExists());

    Task<bool?> IHtmlAsyncControlObject<TScope>.IsVisible()
        => Task.FromResult(IsVisible());

    Task<bool?> IHtmlAsyncControlObject<TScope>.IsEnabled()
        => Task.FromResult(IsEnabled());

    Task<bool> IHtmlAsyncControlObject<TScope>.WaitExists(bool? expected, int? timeoutMs)
        => Task.FromResult(WaitExists(expected, timeoutMs));

    Task<bool> IHtmlAsyncControlObject<TScope>.WaitVisible(bool? expected, int? timeoutMs)
        => Task.FromResult(WaitVisible(expected, timeoutMs));

    Task<bool> IHtmlAsyncControlObject<TScope>.WaitEnabled(bool? expected, int? timeoutMs)
        => Task.FromResult(WaitEnabled(expected, timeoutMs));

    Task<TScope> IHtmlAsyncControlObject<TScope>.AssertExists(bool? expected, string? message, int? timeoutMs)
        => Task.FromResult(AssertExists(expected, message, timeoutMs));

    Task<TScope> IHtmlAsyncControlObject<TScope>.AssertVisible(bool? expected, string? message, int? timeoutMs)
        => Task.FromResult(AssertVisible(expected, message, timeoutMs));

    Task<TScope> IHtmlAsyncControlObject<TScope>.AssertEnabled(bool? expected, string? message, int? timeoutMs)
        => Task.FromResult(AssertEnabled(expected, message, timeoutMs));

    Task<string?> IHtmlAsyncControlObject<TScope>.GetText(int? timeoutMs)
        => Task.FromResult(GetText(timeoutMs));

    Task<bool> IHtmlAsyncControlObject<TScope>.WaitText(string? expected, int? timeoutMs)
        => Task.FromResult(WaitText(expected, timeoutMs));

    Task<TScope> IHtmlAsyncControlObject<TScope>.AssertText(string? expected, string? message, int? timeoutMs)
        => Task.FromResult(AssertText(expected, message, timeoutMs));

    Task<TScope> IHtmlAsyncControlObject<TScope>.AssertTextContains(string? expected, string? message, int? timeoutMs)
        => Task.FromResult(AssertTextContains(expected, message, timeoutMs));

    Task<string?> IHtmlAsyncControlObject<TScope>.GetAttribute(string name)
        => Task.FromResult(GetAttribute(name, null));

    #endregion


    #region Basic Interactions

    protected void EnsureSettableCore(IHtmlElement element)
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
    protected virtual void SendKeysCore(IHtmlElement element, string keys)
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
    protected bool? IsVisibleCore(IHtmlElement? element)
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
    protected bool WaitVisibleCore(IHtmlElement element, bool expected, int timeoutMs)
    {
        return RunPoll(null, () => IsVisibleCore(element) == expected, timeoutMs);
    }

    protected virtual void EnsureVisibleCore(IHtmlElement element, int timeout)
    {
        if (!ContainingScope.IsReady(timeout))
        {
            throw new TimeoutException(
                $"Element was not visible within {timeout}ms. Locator: {Locator}");
        }
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
    protected void EnsureVisible(IHtmlElement element)
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
            e => IsVisibleCore(e), (actual, expected1) => (actual == expected1),
            null, timeoutMs);
    }

    #endregion

    #region Enabled

    protected virtual void EnsureEnabledCore(IHtmlElement element)
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
    protected bool? IsEnabledCore(IHtmlElement? element)
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
        if (expected == null)
            return true;

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

    protected bool? IsExistsCore(IHtmlElement? element)
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
    protected virtual string? GetTextCore(IHtmlElement element)
    {
        return element.Text;
    }

    public string? GetText(int? timeoutMs = null)
    {
        return RunGetWithElement(element => GetTextCore(element), timeoutMs);
    }

    public bool WaitText(string? expected, int? timeoutMs = null)
    {
        if (expected == null)
            return true;

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

    protected virtual string? GetAttributeCore(IHtmlElement element, string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
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
    protected virtual void ScrollIntoViewCore(IHtmlElement element)
    {
        element.ScrollIntoView();
    }

    #endregion

}
