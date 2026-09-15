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
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        Logger?.LogEntry(TestName, PageName, ControlId, caller ?? string.Empty, value);

        var ok = false;
        Exception? lastException = null;
        if (Page != null && !Page.WaitReady(timeout))
        {
            var snapshot = Page.ProbeReadiness();
            throw new PageLoadException(
                $"Page '{Page.Name}' did not become ready for {caller ?? "operation"} on control '{Locator}' within {timeout} ms. " +
                $"Last readiness state: {snapshot.State}; busy value: '{snapshot.BusySignalValue ?? "(none)"}'.");
        }

        do
        {
            try
            {
                if ((Page == null || Page.IsReady()) && condition())
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

            if (stopwatch.ElapsedMilliseconds >= timeout)
                break;

            WaitHelper.Pause(PollingIntervalMs);
        }
        while (stopwatch.ElapsedMilliseconds < timeout);
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
    protected TResult Run<TValue, TResult>(
        string action,
        TValue? value,
        Func<TResult> operation,
        int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        if (Page != null && !Page.WaitReady(timeout))
        {
            var snapshot = Page.ProbeReadiness();
            throw new PageLoadException(
                $"Page '{Page.Name}' did not become ready for {action} on control '{Locator}' within {timeout} ms. " +
                $"Last readiness state: {snapshot.State}; busy value: '{snapshot.BusySignalValue ?? "(none)"}'.");
        }

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
    /// Whether a lookup that finds nothing should scroll to look.
    /// </summary>
    protected enum ScrollLookup
    {
        /// <summary>A plain lookup.</summary>
        None,

        /// <summary>
        /// A plain lookup, then one sweep of the scope's scrolling element. Inside a poll, the
        /// sweep happens on the first tick only.
        /// </summary>
        Once,
    }

    /// <summary>
    /// Resolves the element, scrolling to look for it if the plain lookup finds nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The difference from <see cref="TryFindElement()"/> matters only on Android, which publishes
    /// an accessibility node only for content inside the viewport: a control scrolled out of a
    /// <c>ScrollView</c> still exists and is laid out, but a plain lookup answers "no such
    /// element". Windows keeps the same element with <c>IsOffscreen=true</c>, and its driver
    /// scrolls nothing - so both platforms give a test the same answer.
    /// </para>
    /// <para>
    /// <b>Done here, once, rather than by every scope.</b> Scopes used to implement
    /// <c>TryFindElementAfterScroll</c>, and the one containers and pages inherited did not scroll -
    /// so on Android nothing inside a container was ever scrolled to (step 100a). A scope now says
    /// which element scrolls, <see cref="IMauiElementScope.ScrollingRoot"/>, and nothing else.
    /// </para>
    /// </remarks>
    /// <param name="lookup">Whether to scroll to look.</param>
    /// <returns>The element, or null when it is genuinely not on the page.</returns>
    protected IMauiElement? TryFindElement(ScrollLookup lookup)
    {
        var element = TryFindElement();
        if (element != null || lookup == ScrollLookup.None)
        {
            return element;
        }

        return ScrollingElement()?.TryFindByScrolling(Locator);
    }

    /// <summary>
    /// The element to scroll when looking for this control: the scope's scroller, or the app,
    /// which lets the platform pick the scrolling container on screen.
    /// </summary>
    private IMauiElement? ScrollingElement() => _mauiScope.ScrollingRoot ?? Context.AppElement;

    /// <summary>
    /// A resolver for a polling helper, applying <paramref name="lookup"/>.
    /// </summary>
    /// <remarks>
    /// With <see cref="ScrollLookup.Once"/> the sweep happens on the first call and never again:
    /// a sweep costs orders of magnitude more than a plain lookup, and one answers the question it
    /// exists for. If the element is on the page the sweep leaves it on screen; if it is not,
    /// sweeping again will not change that. This was <c>ScrollingOnceResolver</c> (step 100b).
    /// </remarks>
    /// <param name="lookup">Whether to scroll to look.</param>
    /// <returns>A resolver to hand to a polling helper.</returns>
    protected Func<IMauiElement?> Resolver(ScrollLookup lookup)
    {
        if (lookup == ScrollLookup.None)
        {
            return () => TryFindElement();
        }

        var swept = false;
        return () =>
        {
            if (swept)
            {
                return TryFindElement();
            }

            swept = true;
            return TryFindElement(ScrollLookup.Once);
        };
    }

    /// <summary>
    /// Finds the element within the scope, sweeping the scope's scroller once if it is not found.
    /// </summary>
    /// <remarks>
    /// The route every action takes, so it needs the sweep as much as <see cref="IsExists"/> does:
    /// measured on Android at step 100a, a page's reset button below the fold failed every test
    /// in the class with "not found within container" before the test body ran. The scope polls
    /// for its find timeout first; the sweep is paid once, after that, and only on the way to
    /// failing.
    /// </remarks>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    protected virtual IMauiElement FindElement()
    {
        try
        {
            return _mauiScope.FindElement(Locator);
        }
        catch (ElementNotFoundException)
        {
            var swept = ScrollingElement()?.TryFindByScrolling(Locator);
            if (swept != null)
            {
                return swept;
            }

            throw;
        }
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
    /// <para>
    /// <c>IsVisible</c> answers "on screen right now"; this answers "could the user see it at
    /// all", which requires scrolling — no property distinguishes a control scrolled out of view
    /// from one that is not rendered. Prefer this when a test means "the page shows this
    /// control", since whether something sits above the fold depends on window size and so
    /// differs between platforms.
    /// </para>
    /// <para>
    /// <b>Takes the element it is given, and resolves nothing.</b> It used to resolve a missing
    /// element with <c>FindElement</c>, which polls for the whole find timeout before giving up -
    /// so <c>AssertVisibleAfterScroll(false)</c> paid that on every tick of its own poll. The trio
    /// below resolves with <see cref="ScrollLookup.Once"/> instead, the same lookup <c>Exists</c>
    /// uses, which is what makes an absent element cheap and an Android one findable (step 100c).
    /// </para>
    /// <para>
    /// Revealing is the element's business: on Windows its ScrollItem pattern, or the bridge's
    /// <c>ScrollTo</c> verb on the scroll view that holds it.
    /// </para>
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True when visible, scrolling to it first if needed; null when absent.</returns>
    [AbsenceTolerant]
    [SkipGeneration("Hand-written below: the trio resolves with ScrollLookup.Once, which the generated trio cannot express.")]
    protected virtual bool? IsVisibleAfterScrollCore(IMauiElement? element)
    {
        if (element == null)
        {
            return null;
        }

        if (IsVisibleCore(element) == true)
        {
            return true;
        }

        ScrollIntoViewCore(element);
        return IsVisibleCore(element);
    }

    /// <summary>Whether the user could see the control, scrolling to it if needed.</summary>
    /// <returns>True when visible after scrolling; false when not, or absent.</returns>
    public bool? IsVisibleAfterScroll()
        => IsVisibleAfterScrollCore(TryFindElement(ScrollLookup.Once)) == true;

    /// <summary>Waits until <see cref="IsVisibleAfterScroll"/> matches <paramref name="expected"/>.</summary>
    public bool WaitVisibleAfterScroll(bool? expected = true, int? timeoutMs = null)
        => RunWaitWithOptionalElement(expected,
            element => IsVisibleAfterScrollCore(element) == expected!.Value,
            timeoutMs, Resolver(ScrollLookup.Once));

    /// <summary>Asserts <see cref="IsVisibleAfterScroll"/>, returning the scope for chaining.</summary>
    public TScope AssertVisibleAfterScroll(bool? expected = true, string? message = null, int? timeoutMs = null)
        => RunAssertWithOptionalElement(expected,
            IsVisibleAfterScrollCore, (actual, expected1) => actual == expected1,
            message ?? $"Expected VisibleAfterScroll to be '{expected}'. Locator: {Locator}", timeoutMs,
            Resolver(ScrollLookup.Once));

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
        return IsExistsBase(TryFindElement(ScrollLookup.Once)) == true;
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
            timeoutMs, Resolver(ScrollLookup.Once));
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
            Resolver(ScrollLookup.Once));
    }

    #endregion
    #region Attributes

    /// <summary>
    /// Reads a named attribute straight from the platform.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The deliberate escape hatch, and the only place in the control library that asks for an
    /// attribute by name. You are asking <b>one platform a question in its own vocabulary</b>:
    /// Windows answers twelve names (automation id, name, class, control type, enabled, visible,
    /// help text and the scroll percentages) and null to everything else; Android answers
    /// UiAutomator2's accessibility attributes - <c>text</c>, <c>content-desc</c>,
    /// <c>resource-id</c>, <c>checked</c>, <c>selected</c>, <c>focused</c>, <c>hint</c> and
    /// friends - and null to the rest.
    /// </para>
    /// <para>
    /// A null answer therefore means either "empty" or "this platform has no such attribute",
    /// and nothing here can tell you which. That is why controls do not use it: a MAUI
    /// bindable property is not an automation attribute, so asking for <c>Value</c> or
    /// <c>Source</c> by name returns null on every device and reads as data. Everything a
    /// control needs comes from a member on <c>IMauiElement</c> or a pattern capability
    /// instead - see <c>.my/GetAttribute/</c>.
    /// </para>
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="name">The platform's own attribute name.</param>
    /// <returns>The value, or null when absent - or unsupported.</returns>
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
