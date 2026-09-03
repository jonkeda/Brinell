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
    private string TestName => "Test";
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
        int? timeoutMs = null, Func<IMauiElement?>? resolve = null,
        [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return true;
        }

        return RunPoll(null, () => coreOperation((resolve ?? TryFindElement)()), timeoutMs, caller);
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
        string? message = null, int? timeoutMs = null, Func<IMauiElement?>? resolve = null,
        [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        RunPoll(null, () =>
        {
            var actual = getActual((resolve ?? TryFindElement)());
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
        var element = ResolveReadyElement(timeoutMs, doEnsureVisible, caller);
        coreOperation(element);
        return ContainingScope;
    }

    /// <summary>
    /// Polls until the control is ready to be acted on, and returns its element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Poll to get ready, then act once.</b> Resolution is safe to repeat — finding an
    /// element, checking visibility, checking enabled — so it is what the retry loop covers.
    /// The action is not safe to repeat, so it runs after the loop, exactly once.
    /// </para>
    /// <para>
    /// An exception from the action propagates rather than being retried: once the action has
    /// been attempted, retrying can only compound the damage — a driver that acts and then
    /// throws would otherwise replay it, silently doubling a click.
    /// </para>
    /// </remarks>
    private IMauiElement ResolveReadyElement(int? timeoutMs, bool doEnsureVisible, string? caller)
    {
        IMauiElement? ready = null;

        RunPoll(null, () =>
        {
            var element = FindElement();
            if (doEnsureVisible)
            {
                EnsureVisible(element, DefaultTimeoutMs);
            }
            EnsureReadyForActionCore(element);
            ready = element;
            return true;
        }, timeoutMs, caller);

        return ready ?? FindElement();
    }

    /// <summary>
    /// Checks any additional readiness this control requires before it can be acted on.
    /// </summary>
    /// <remarks>
    /// Runs inside the readiness poll, so a control that becomes ready a moment late is waited
    /// for rather than failed against. <see cref="ViewBase{TScope}"/> requires nothing beyond
    /// being present and visible; <c>ClickableControlBase</c> adds "enabled".
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    protected virtual void EnsureReadyForActionCore(IMauiElement element)
    {
    }

    protected TScope RunSetWithElement<T>(T? value, Action<IMauiElement> coreOperation,
         int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (value == null)
        {
            return ContainingScope;
        }
        // Same split as RunDoWithElement: setting a value is an action, not a query, so it
        // must not be replayed by the readiness retry.
        var element = ResolveReadyElement(timeoutMs, doEnsureVisible: true, caller);
        coreOperation(element);
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
        // Resolve once, then re-read the value each tick. The element rarely changes identity
        // while an assertion waits for its value to settle, but re-finding it every 100 ms is
        // the single largest source of traffic in an Android run — 811 lookups for 34 tests,
        // 78 s. A stale handle drops back to re-resolving, which is the case that made
        // re-finding look necessary in the first place.
        IMauiElement? element = null;
        RunPoll(null, () =>
        {
            if (element == null)
            {
                element = FindElement();
                EnsureVisible(element, DefaultTimeoutMs);
            }

            T? actual;
            try
            {
                actual = getActual(element);
            }
            catch (StaleElementReferenceException)
            {
                element = null;
                return false;
            }

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
    /// Resolves the element anywhere on the page, scrolling to it if it is not on screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The difference from <see cref="TryFindElement"/> matters only on Android, which publishes
    /// an accessibility node only for content inside the viewport: a control scrolled out of a
    /// <c>ScrollView</c> still exists and is laid out, but a plain lookup answers "no such
    /// element". Windows keeps the same element with <c>IsOffscreen=true</c> and answers yes.
    /// </para>
    /// <para>
    /// The scope scrolls to it on Android and does nothing extra on Windows, so both platforms
    /// give a test the same answer. It does not poll: the caller has already established that a
    /// plain lookup finds nothing.
    /// </para>
    /// </remarks>
    /// <returns>The element, or null when it is genuinely not on the page.</returns>
    protected IMauiElement? TryFindElementAfterScroll()
    {
        return _mauiScope.TryFindElementAfterScroll(Locator);
    }

    /// <summary>
    /// A resolver that scrolls to look at most once, then falls back to plain lookups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For use inside a poll, where resolving through <see cref="TryFindElementAfterScroll"/>
    /// on every tick would sweep the container repeatedly — a sweep costs orders of magnitude
    /// more than a plain lookup. One sweep answers the question it exists for: if the element is
    /// on the page the sweep leaves it on screen, and if it is not, sweeping again will not
    /// change that.
    /// </para>
    /// </remarks>
    /// <returns>A resolver to hand to a polling helper.</returns>
    protected Func<IMauiElement?> ScrollingOnceResolver()
    {
        var swept = false;
        return () =>
        {
            if (swept)
            {
                return TryFindElement();
            }

            swept = true;
            return TryFindElementAfterScroll();
        };
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
    /// For compound controls whose template wraps a native child. The child is looked for
    /// beneath the control's own element first; failing that, the scope is searched and
    /// candidates filtered by position, because some MAUI handlers reparent the native child out
    /// of the logical subtree.
    /// </para>
    /// <para>
    /// <c>protected virtual</c> rather than a shared helper: which child a compound control
    /// activates is knowledge about that control.
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

    /// <summary>
    /// Checks whether the element can be seen at all, scrolling to it when it is not already
    /// on screen.
    /// </summary>
    /// <remarks>
    /// <c>IsVisible</c> answers "on screen right now"; this answers "could the user see it at
    /// all", which requires scrolling — no property distinguishes a control scrolled out of view
    /// from one that is not rendered. Prefer this when a test means "the page shows this
    /// control", since whether something sits above the fold depends on window size and so
    /// differs between platforms.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True when visible, scrolling to it first if needed; null when absent.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsVisibleAfterScrollCore(IMauiElement? element)
    {
        if (element != null && IsVisibleCore(element) == true)
        {
            return true;
        }

        // A null element does not mean "not on the page". Android drops an off-screen view from
        // the accessibility tree entirely, so the very control this method exists to reach is
        // absent until something scrolls to it — and the plain lookup that produced this
        // argument does not scroll. Resolving through FindElement does: it falls back to
        // UiScrollable on Android, and is an ordinary lookup on Windows, where the element was
        // in the tree all along.
        var resolved = element;
        if (resolved == null)
        {
            try
            {
                resolved = FindElement();
            }
            catch (ElementNotFoundException)
            {
                return null;
            }
        }

        ScrollIntoViewCore(resolved);
        return IsVisibleCore(resolved);
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

    /// <summary>
    /// Whether the control is on the page, scrolling to it if it is not on screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "On the page", not "in the accessibility tree right now": Android publishes a node only
    /// for content inside the viewport, so the second reading answers no for a control Windows
    /// answers yes for. Deliberately not split into two methods the way visibility is — only
    /// one existence question is real, and naming the platform artifact would invite tests to
    /// depend on it.
    /// </para>
    /// <para>
    /// The cost lands on absence: <c>AssertExists(false)</c> must exhaust a scroll of the
    /// container before it can answer, which is the honest price of "is it really not there?"
    /// and is paid only when the element is not found.
    /// </para>
    /// </remarks>
    public bool IsExists()
    {
        return IsExistsBase(TryFindElementAfterScroll()) == true;
    }

    /// <summary>
    /// Waits until the element's presence matches <paramref name="expected"/>.
    /// </summary>
    /// <remarks>
    /// Resolves optionally, so <c>WaitExists(false)</c> reports the absence it is asking about
    /// instead of raising <c>ElementNotFoundException</c>, and scrolls to look — see
    /// <see cref="IsExists"/>.
    /// </remarks>
    public bool WaitExists(bool? expected = true, int? timeoutMs = null)
    {
        return RunWaitWithOptionalElement(expected,
            element => IsExistsBase(element) == expected!.Value,
            timeoutMs, ScrollingOnceResolver());
    }

    /// <summary>
    /// Asserts the element's presence, returning the scope for chaining.
    /// </summary>
    /// <remarks>
    /// Resolves optionally, so <c>AssertExists(false)</c> passes for a missing element rather
    /// than throwing, and scrolls to look — see <see cref="IsExists"/>.
    /// </remarks>
    public TScope AssertExists(bool? expected = true, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithOptionalElement(expected,
             IsExistsBase, (actual, expected1) => (actual == expected1),
            message ?? $"Expected Exists to be '{expected}'. Locator: {Locator}", timeoutMs,
            ScrollingOnceResolver());
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
