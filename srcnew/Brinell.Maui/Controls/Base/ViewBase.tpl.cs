using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Utilities;
using System.Runtime.CompilerServices;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for all MAUI controls implementing the Is/Wait/Assert pattern with fluent chaining.
/// Controls find elements within their scope (page, container, or list item).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract partial class ViewBase<TScope> : ControlObjectBase<TScope>, IElementObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly IMauiScope<TScope> _mauiScope;

    /// <summary>
    /// Creates a new control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator used to find the control element.</param>
    protected ViewBase(IMauiScope<TScope> scope, Locator locator)
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
    protected ViewBase(IMauiScope<TScope> scope, string locatorValue)
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

    /// <summary>
    /// Gets logging context information.
    /// </summary>
    private string TestName => "Test"; // TODO: Get from test context when available
    private string PageName => Page?.GetType().Name ?? "Unknown";
    private string ControlId => Locator.Value;
    private ITestLogger Logger => Context.Logger;


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

    /// <summary>
    /// Runs an operation once, with entry/exit logging, and returns its result.
    /// </summary>
    /// <remarks>
    /// Unlike the <c>Run*WithElement</c> family this does not poll and does not resolve an
    /// element — the operation owns both. Controls whose logic spans several elements use
    /// it to get one logged unit of work rather than one per lookup.
    /// </remarks>
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

    protected bool RunWait(Func<bool> operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        return RunPoll(null, () =>
        {
            return operation();
        }, timeoutMs, caller);
    }

    protected bool RunWaitWithElement<T>(T? expected, Func<IMauiElement, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return true;
        }

        return RunPoll(null, () =>
        {
            var element = FindElement();
            EnsureVisible(element, DefaultTimeoutMs);
            return coreOperation(element);
        }, timeoutMs, caller);
    }

    /// <summary>
    /// Polls a predicate that is meaningful when the element is absent.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="RunWaitWithElement{T}"/>, the element is resolved with
    /// <see cref="TryFindElement"/> and may be null, and visibility is not forced — the
    /// predicate may be asking about invisibility. Used by generated members whose Core
    /// method carries <c>[AbsenceTolerant]</c>.
    /// </remarks>
    protected bool RunWaitWithOptionalElement<T>(T? expected,
        Func<IMauiElement?, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return true;
        }

        return RunPoll(null, () => coreOperation(TryFindElement()), timeoutMs, caller);
    }

    /// <summary>
    /// Asserts a value that is meaningful when the element is absent.
    /// </summary>
    /// <remarks>
    /// The counterpart of <see cref="RunWaitWithOptionalElement{T}"/>: resolves the
    /// element optionally so a missing element fails the comparison rather than raising
    /// <c>ElementNotFoundException</c>.
    /// </remarks>
    protected TScope RunAssertWithOptionalElement<T>(T? expected,
        Func<IMauiElement?, T?> getActual, Func<T?, T?, bool> compare,
        string? message = null, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        RunPoll(null, () =>
        {
            var actual = getActual(TryFindElement());
            if (!compare(actual, expected))
            {
                throw new AssertionException(message ?? "Assert exception", expected, actual);
            }
            return true;
        }, timeoutMs, caller);

        return ContainingScope;
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
                EnsureVisible(element, DefaultTimeoutMs);
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
            EnsureVisible(element, DefaultTimeoutMs);
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
            EnsureVisible(element, DefaultTimeoutMs);
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
            EnsureVisible(element, DefaultTimeoutMs);

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
    protected virtual IMauiElement FindElement()
    {
        return _mauiScope.FindElement(Locator);
    }

    /// <summary>
    /// Finds a descendant of this control's element by automation id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For compound controls whose template wraps a native child — a round button hosting a
    /// platform button, an editable field hosting an entry. The child is looked for beneath
    /// the control's own element first; failing that, the scope is searched and candidates are
    /// filtered to those positioned inside the control. The second pass exists because some
    /// MAUI handlers reparent the native child out of the logical subtree, leaving position as
    /// the only reliable link.
    /// </para>
    /// <para>
    /// This is <c>protected virtual</c> on the control rather than a shared static helper:
    /// which child a compound control activates is knowledge about that control, and a custom
    /// control outside this assembly overrides it the same way the built-ins do.
    /// </para>
    /// </remarks>
    /// <param name="root">The control's own element.</param>
    /// <param name="automationId">The automation id of the child to find.</param>
    /// <returns>The child element, or null when no visible match exists.</returns>
    protected virtual IMauiElement? FindChildCore(IMauiElement root, string automationId)
    {
        ArgumentNullException.ThrowIfNull(root);

        var locator = Locator.ByAutomationId(automationId);

        var directChild = root.FindElements(locator).FirstVisible();
        if (directChild != null)
        {
            return directChild;
        }

        if (!root.HasUsableBounds())
        {
            return null;
        }

        return MauiScope.FindVisibleElements(locator)
            .FirstOrDefault(root.ContainsCenter);
    }

    /// <summary>
    /// Finds a descendant of this control's element by control type.
    /// </summary>
    /// <remarks>
    /// The by-id counterpart of <see cref="FindChildCore(IMauiElement, string)"/>, for templates
    /// whose inner part carries no automation id. Among positional candidates the smallest is
    /// taken, since a larger match is usually an ancestor that merely contains the control.
    /// </remarks>
    /// <param name="root">The control's own element.</param>
    /// <param name="controlType">The control type of the child to find.</param>
    /// <returns>The child element, or null when no visible match exists.</returns>
    protected virtual IMauiElement? FindChildByControlTypeCore(IMauiElement root, string controlType)
    {
        ArgumentNullException.ThrowIfNull(root);

        var locator = Locator.ByControlType(controlType);

        var directChild = root.FindElements(locator).FirstVisible();
        if (directChild != null)
        {
            return directChild;
        }

        if (!root.HasUsableBounds())
        {
            return null;
        }

        return MauiScope.FindVisibleElements(locator)
            .Where(root.ContainsCenter)
            .OrderBy(candidate => candidate.Area())
            .FirstOrDefault();
    }

    #endregion

    #region Visible

    /// <summary>
    /// Checks if element is visible using pre-found element.
    /// No stale element handling - element is found once at operation start.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if visible, false otherwise.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }

    /// <summary>
    /// Polls visible state using pre-found element.
    /// </summary>
    /// <remarks>
    /// Not generated: this is already a <c>Wait*</c>, and the generated
    /// <c>WaitVisible</c> comes from <see cref="IsVisibleCore"/>. Generating from this one
    /// too would collide on the name.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected visible state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    [SkipGeneration("Already a Wait* helper; WaitVisible is generated from IsVisibleCore.")]
    protected virtual bool WaitVisibleCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return RunPoll(null, () => IsVisibleCore(element) == expected, timeoutMs);
    }

    protected virtual void EnsureVisible(IMauiElement element, int timeout)
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

    #endregion

    #region Enabled

    /// <summary>
    /// Checks if element is enabled using pre-found element.
    /// No stale element handling - element is found once at operation start.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if enabled, false otherwise.</returns>
    protected virtual bool? IsEnabledCore(IMauiElement? element)
    {
        return element?.Enabled;
    }

    #endregion

    #region Exists

    protected virtual bool? IsExistsBase(IMauiElement? element)
    {
        return element != null;
    }

    public bool IsExists()
    {
        return IsExistsBase(TryFindElement()) == true;
    }

    /// <summary>
    /// Waits until the element's presence matches <paramref name="expected"/>.
    /// </summary>
    /// <remarks>
    /// Resolves the element optionally, so <c>WaitExists(false)</c> reports the absence it
    /// is asking about instead of raising <c>ElementNotFoundException</c>.
    /// </remarks>
    public bool WaitExists(bool? expected = true, int? timeoutMs = null)
    {
        return RunWaitWithOptionalElement(expected,
            element => IsExistsBase(element) == expected!.Value,
            timeoutMs);
    }

    /// <summary>
    /// Asserts the element's presence, returning the scope for chaining.
    /// </summary>
    /// <remarks>
    /// Resolves the element optionally, so <c>AssertExists(false)</c> passes for a missing
    /// element rather than throwing.
    /// </remarks>
    public TScope AssertExists(bool? expected = true, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithOptionalElement(expected,
             IsExistsBase, (actual, expected1) => (actual == expected1),
            message ?? $"Expected Exists to be '{expected}'. Locator: {Locator}", timeoutMs);
    }

    #endregion
    #region Attributes

    protected virtual string? GetAttributeCore(IMauiElement element, string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return element.GetAttribute(name);
    }

    #endregion

    #region ScrollIntoView

    /// <summary>
    /// Core scroll implementation. Uses element's ScrollIntoView method.
    /// </summary>
    /// <param name="element">The element to scroll into view.</param>
    protected virtual void ScrollIntoViewCore(IMauiElement element) => element.ScrollIntoView();

    #endregion

}
