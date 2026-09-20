using Brinell.Core.Utilities;
using Brinell.Maui.Calls;
using System.Runtime.CompilerServices;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for all MAUI controls implementing the Is/Wait/Assert pattern with fluent chaining.
/// Controls find elements within their scope (page, container, or list item).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract partial class ViewBase<TScope> : IElementObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly IMauiScope<TScope> _mauiScope;

    /// <summary>
    /// Creates a new control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator used to find the control element.</param>
    protected ViewBase(IMauiScope<TScope> scope, Locator locator)
    {
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _mauiScope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    /// <summary>
    /// Creates a new control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    protected ViewBase(IMauiScope<TScope> scope, string locatorValue)
        : this(scope, new Locator(scope?.DefaultLocatorStrategy ?? LocatorStrategy.AutomationId, locatorValue))
    {
        if (string.IsNullOrEmpty(locatorValue))
            throw new ArgumentNullException(nameof(locatorValue));
    }

    /// <summary>
    /// The locator that finds this control within its scope.
    /// </summary>
    protected Locator Locator { get; }

    /// <summary>
    /// The page this control belongs to, or null when its scope is not in a page.
    /// </summary>
    protected IMauiPage? Page => _mauiScope.Page;

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

    #region Calls

    // Every public member is one call (.my/stale-readiness/design.md, R1): one log pair, one
    // budget, one poll. Each attempt of the poll checks the page, finds the element again, makes
    // one visibility check, and then reads, compares or confirms readiness. Nothing inside a call
    // waits with a budget of its own (R2). An action runs once, after the poll (R0).

    private string PageName => Page?.GetType().Name ?? "Unknown";
    private string ControlId => Locator.Value;

    private ControlCall Call => new(Context, PageName, ControlId);

    private int Budget(int? timeoutMs) => timeoutMs ?? DefaultTimeoutMs;

    private int AnimationMs => Context.Timeouts.Animation;

    /// <summary>
    /// What is left of the running call's budget, for a wait below the call (a scroll); the
    /// default wait outside a call. Nothing inside a call starts a budget of its own (R2, R3).
    /// </summary>
    protected int CallRemainingMs => AttemptContext.RemainingOr(DefaultTimeoutMs);

    /// <summary>The failure of a phase that ran out of budget, from what it last saw.</summary>
    private Exception Failure(AttemptContext attempt, string caller, int budgetMs)
        => attempt.Log.ToException(
            Locator,
            budgetMs,
            NotFound,
            readiness => ScopeGate.NotReady(readiness, caller, $"control '{Locator}'", budgetMs));

    /// <summary>
    /// One attempt against the element: the scope chain, then a fresh lookup, then (optionally) one
    /// visibility check, then <paramref name="body"/>.
    /// </summary>
    private Observation Attempt(AttemptContext attempt, bool ensureVisible, Func<IMauiElement, Observation> body)
    {
        if (ScopeGate.Check(_mauiScope) is { } notReady)
        {
            return notReady;
        }

        var element = Locate(attempt);
        if (element == null)
        {
            return Observation.Missing();
        }

        try
        {
            if (ensureVisible)
            {
                EnsureVisible(element, attempt);
            }

            var observation = body(element);
            return observation.Kind == ObservationKind.Done && attempt.Log.Attempts == 0
                ? observation
                : observation with { InstanceKey = KeyOf(element) };
        }
        catch (StaleElementException error)
        {
            return Observation.Stale(error, null);
        }
        catch (ElementNotReadyException error)
        {
            return Observation.NotReady(error, KeyOf(element));
        }
        catch (AssertionException error)
        {
            return Observation.Mismatch(error, KeyOf(element));
        }
    }

    /// <summary>The element's identity, or null when it is already gone.</summary>
    private static string? KeyOf(IMauiElement element)
    {
        try
        {
            return element.InstanceKey;
        }
        catch (StaleElementException)
        {
            return null;
        }
    }

    /// <summary>Polls the page alone until it is ready, or fails the call.</summary>
    private void AwaitScope(AttemptContext attempt, string caller, int budgetMs)
    {
        if (!Poller.Until(_ => ScopeGate.Check(_mauiScope) ?? Observation.Done(), attempt, PollingIntervalMs))
        {
            throw Failure(attempt, caller, budgetMs);
        }
    }

    /// <summary>
    /// Runs an operation once as one call: waits for the page, then runs it, with one log pair.
    /// </summary>
    protected TResult Run<TValue, TResult>(
        string action,
        TValue? value,
        Func<TResult> operation,
        int? timeoutMs = null)
    {
        var budget = Budget(timeoutMs);
        return Call.Run(action, value?.ToString(), budget, AnimationMs, attempt =>
        {
            AwaitScope(attempt, action, budget);
            return operation();
        });
    }

    protected bool RunWait(Func<bool> operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunWait);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (Poller.Until(_ => ScopeGate.Check(_mauiScope) ?? (operation() ? Observation.Done() : Observation.Pending()),
                    attempt, PollingIntervalMs))
            {
                return true;
            }

            return attempt.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(attempt, caller, budget);
        }, succeeded: met => met);
    }

    protected bool RunWaitWithElement<T>(T? expected, Func<IMauiElement, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return true;
        }

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunWaitWithElement);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (Poller.Until(a => Attempt(a, ensureVisible: true,
                    element => coreOperation(element) ? Observation.Done() : Observation.Pending()),
                    attempt, PollingIntervalMs))
            {
                return true;
            }

            // A predicate that stayed false answers false; a call that never got to ask fails.
            return attempt.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(attempt, caller, budget);
        }, succeeded: met => met);
    }

    /// <summary>
    /// Polls a predicate that is meaningful when the element is absent.
    /// </summary>
    protected bool RunWaitWithOptionalElement<T>(T? expected,
        Func<IMauiElement?, bool> coreOperation,
        int? timeoutMs = null, Func<IMauiElement?>? resolve = null,
        [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return true;
        }

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunWaitWithOptionalElement);
        var find = resolve ?? TryFindElement;
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (Poller.Until(_ => ScopeGate.Check(_mauiScope)
                    ?? (coreOperation(find()) ? Observation.Done() : Observation.Pending()),
                    attempt, PollingIntervalMs))
            {
                return true;
            }

            return attempt.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(attempt, caller, budget);
        }, succeeded: met => met);
    }

    /// <summary>
    /// Asserts a value that is meaningful when the element is absent.
    /// </summary>
    protected TScope RunAssertWithOptionalElement<T>(T? expected,
        Func<IMauiElement?, T?> getActual, Func<T?, T?, bool> compare,
        string? message = null, int? timeoutMs = null, Func<IMauiElement?>? resolve = null,
        [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunAssertWithOptionalElement);
        var find = resolve ?? TryFindElement;
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (!Poller.Until(_ =>
                {
                    if (ScopeGate.Check(_mauiScope) is { } notReady)
                    {
                        return notReady;
                    }

                    var actual = getActual(find());
                    return compare(actual, expected)
                        ? Observation.Done()
                        : Observation.Mismatch(new AssertionException(message ?? "Assert exception", expected, actual), null);
                }, attempt, PollingIntervalMs))
            {
                throw Failure(attempt, caller, budget);
            }

            return ContainingScope;
        });
    }

    /// <summary>
    /// Runs an operation as one call: waits for the page, then runs it once.
    /// </summary>
    protected TScope RunDo(Action operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunDo);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            AwaitScope(attempt, caller, budget);
            operation();
            return ContainingScope;
        });
    }

    /// <summary>
    /// Whether an action waits for the element to be visible before it runs.
    /// </summary>
    protected virtual bool RequiresVisibilityForAction => true;

    /// <summary>
    /// An action as one call: polls until the control is ready to be acted on, then acts once.
    /// </summary>
    protected TScope RunDoWithElement(Action<IMauiElement> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunDoWithElement);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            ActOnce(attempt, RequiresVisibilityForAction, caller, budget, coreOperation);
            return ContainingScope;
        });
    }

    /// <summary>
    /// Resolves the control, then runs <paramref name="act"/> on it: once, unless it reports that
    /// it did not act.
    /// </summary>
    private void ActOnce(AttemptContext attempt, bool ensureVisible, string caller, int budgetMs,
        Action<IMauiElement> act)
    {
        while (true)
        {
            var element = ResolveReady(attempt, ensureVisible, caller, budgetMs);
            try
            {
                act(element);
                return;
            }
            catch (ElementNotReadyException error) when (attempt.Deadline.RemainingMs > 0)
            {
                attempt.Log.Add(Observation.NotReady(error, null), attempt.Deadline.ElapsedMs);
                WaitHelper.Pause(Math.Max(1, Math.Min(PollingIntervalMs, attempt.Deadline.RemainingMs)));
            }
        }
    }

    /// <summary>
    /// Polls until the control is ready to be acted on, and returns its element.
    /// </summary>
    private IMauiElement ResolveReady(AttemptContext attempt, bool ensureVisible, string caller, int budgetMs)
    {
        IMauiElement? ready = null;
        if (!Poller.Until(a => Attempt(a, ensureVisible, element =>
            {
                EnsureReadyForActionCore(element);
                ready = element;
                return Observation.Done();
            }), attempt, PollingIntervalMs))
        {
            throw Failure(attempt, caller, budgetMs);
        }

        return ready!;
    }

    /// <summary>
    /// Waits, inside a Core method, for the effect of an action the method has already done.
    /// </summary>
    /// <param name="read">Reads the value, typically a Core query on the held element.</param>
    /// <param name="done">Whether the value is the one waited for.</param>
    /// <param name="timeoutMs">Maximum time to wait; null for the default.</param>
    /// <returns>How the wait ended, with the last value and error.</returns>
    protected Confirmation<T> Confirm<T>(Func<T?> read, Func<T?, bool> done, int? timeoutMs)
        => Confirmer.Run(read, done, timeoutMs ?? DefaultTimeoutMs, PollingIntervalMs);

    /// <summary>
    /// Checks any additional readiness this control requires before it can be acted on.
    /// </summary>
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
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunSetWithElement);
        return Call.Run(caller, value.ToString(), budget, AnimationMs, attempt =>
        {
            ActOnce(attempt, ensureVisible: true, caller, budget, coreOperation);
            return ContainingScope;
        });
    }

    /// <summary>
    /// Reads a value that is meaningful when the element is absent: once, with the element
    /// resolved optionally.
    /// </summary>
    protected T? RunGetWithOptionalElement<T>(Func<IMauiElement?, T> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
        => Run(caller ?? nameof(RunGetWithOptionalElement), (object?)null,
            () => coreOperation(TryFindElement()), timeoutMs);

    protected T? RunGetWithElement<T>(Func<IMauiElement, T> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunGetWithElement);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            var value = default(T);
            if (!Poller.Until(a => Attempt(a, ensureVisible: true, element =>
                {
                    value = coreOperation(element);
                    return Observation.Done();
                }), attempt, PollingIntervalMs))
            {
                throw Failure(attempt, caller, budget);
            }

            return value;
        });
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

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunAssert);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (!Poller.Until(_ =>
                {
                    if (ScopeGate.Check(_mauiScope) is { } notReady)
                    {
                        return notReady;
                    }

                    var actual = getActual();
                    return compare(actual, expected)
                        ? Observation.Done()
                        : Observation.Mismatch(new AssertionException(message ?? "Assert exception", expected, actual), null);
                }, attempt, PollingIntervalMs))
            {
                throw Failure(attempt, caller, budget);
            }

            return ContainingScope;
        });
    }

    /// <summary>
    /// An assertion as one call: finds the element again on every attempt, then compares.
    /// </summary>
    protected TScope RunAssertWithElement<T>(T? expected, Func<IMauiElement, T?> getActual,
        Func<T?, T?, bool> compare, string? message = null,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunAssertWithElement);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (!Poller.Until(a => Attempt(a, ensureVisible: true, element =>
                {
                    var actual = getActual(element);
                    return compare(actual, expected)
                        ? Observation.Done()
                        : Observation.Mismatch(
                            new AssertionException(message ?? "Assert exception", expected, actual), null);
                }), attempt, PollingIntervalMs))
            {
                throw Failure(attempt, caller, budget);
            }

            return ContainingScope;
        });
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
    /// <param name="lookup">Whether to scroll to look.</param>
    /// <returns>The element, or null when it is genuinely not on the page.</returns>
    protected IMauiElement? TryFindElement(ScrollLookup lookup)
    {
        var element = TryFindElement();
        if (element != null || lookup == ScrollLookup.None || !_mauiScope.AllowsScrollLookup)
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
    /// Finds the element in one attempt, sweeping the scope's scroller once if the plain lookup
    /// finds nothing, or throws <see cref="NotFound"/>.
    /// </summary>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    protected IMauiElement FindElement()
        => TryFindElement()
           ?? (_mauiScope.AllowsScrollLookup ? ScrollingElement()?.TryFindByScrolling(Locator) : null)
           ?? throw NotFound();

    /// <summary>
    /// The error for a control that could not be found.
    /// </summary>
    protected virtual ElementNotFoundException NotFound()
        => _mauiScope.DescribeMiss(Locator) ?? new ElementNotFoundException(Locator);

    /// <summary>
    /// The lookup of one attempt: a plain lookup, then a sweep of the scroller when the scope
    /// allows one and the call has not swept within the last Animation interval (F3).
    /// </summary>
    private IMauiElement? Locate(AttemptContext attempt)
    {
        var element = TryFindElement();
        if (element != null || !_mauiScope.AllowsScrollLookup || !attempt.MaySweep())
        {
            return element;
        }

        return ScrollingElement()?.TryFindByScrolling(Locator);
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
    /// Checks whether the element can be seen at all, scrolling to it when it is not already
    /// on screen.
    /// </summary>
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

    /// <summary>
    /// One attempt at "visible": visible now, or scroll it into view (throttled) and look once
    /// more. Throws <see cref="ElementNotReadyException"/> when it is still not visible.
    /// </summary>
    private void EnsureVisible(IMauiElement element, AttemptContext attempt)
    {
        if (IsVisibleCore(element) == true)
        {
            return;
        }

        if (attempt.MayScrollIntoView(KeyOf(element) ?? Locator.ToString()))
        {
            element.ScrollIntoView(attempt.Deadline.RemainingMs);

            if (IsVisibleCore(element) == true)
            {
                return;
            }
        }

        throw new ElementNotReadyException(Locator, NotReadyReason.NotVisible);
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
    public bool IsExists()
    {
        return IsExistsBase(TryFindElement(ScrollLookup.Once)) == true;
    }

    /// <summary>
    /// Waits until the element's presence matches <paramref name="expected"/>.
    /// </summary>
    public bool WaitExists(bool? expected = true, int? timeoutMs = null)
    {
        return RunWaitWithOptionalElement(expected,
            element => IsExistsBase(element) == expected!.Value,
            timeoutMs, Resolver(ScrollLookup.Once));
    }

    /// <summary>
    /// Asserts the element's presence, returning the scope for chaining.
    /// </summary>
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
    protected virtual void ScrollIntoViewCore(IMauiElement element) => element.ScrollIntoView(CallRemainingMs);

    #endregion

}
