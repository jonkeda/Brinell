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
    private ITestLogger? Logger => Context.Logger;

    private ControlCall Call => new(Logger, PageName, ControlId);

    private int Budget(int? timeoutMs) => timeoutMs ?? DefaultTimeoutMs;

    private int AnimationMs => Context.Timeouts.Animation;

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
    /// <remarks>
    /// Expected outcomes become observations; anything else escapes to the poll, which records and
    /// retries it (or, for a closed app or a misconfigured page, ends the call). The element's
    /// identity is read only when the attempt is not the first to succeed, so a call that succeeds
    /// at once costs no extra read.
    /// </remarks>
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
    /// <remarks>
    /// Unlike the <c>Run*WithElement</c> family this does not resolve an element - the operation
    /// owns that. Controls whose logic spans several elements use it to get one logged unit of work
    /// rather than one per lookup.
    /// </remarks>
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
    /// <remarks>
    /// Unlike <see cref="RunWaitWithElement{T}"/>, the element is resolved with
    /// <see cref="TryFindElement()"/> and may be null, and visibility is not forced — the
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
    /// <remarks>
    /// Once, never again (R0): an operation that fails after it took effect must not be repeated.
    /// </remarks>
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
    /// <remarks>
    /// True for anything a user acts on where it is shown. A control whose action does not go
    /// through the element on screen - it is raised by id through the bridge - overrides this to
    /// false, so that a replaced element is looked up again instead of being waited on.
    /// </remarks>
    protected virtual bool RequiresVisibilityForAction => true;

    /// <summary>
    /// An action as one call: polls until the control is ready to be acted on, then acts once.
    /// </summary>
    /// <remarks>
    /// The action and whatever it confirms are inside the call's log pair (R1), and the action is
    /// never repeated (R0). Its confirmation, if any, has a budget of its own (R3). See
    /// <see cref="ActOnce"/> for the one case where the Core method is called again.
    /// </remarks>
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
    /// <remarks>
    /// <para>
    /// A Core method throws <see cref="ElementNotReadyException"/> only <i>before</i> it acts: its
    /// guards (<c>Ensure*Core</c>) come first, and a driver raises it for an action it did not
    /// perform (a toolbar item the app's bridge did not answer for yet, or answered "disabled").
    /// Nothing happened, so resolving again and asking again within the budget is waiting for
    /// state, not repeating an action (R0). When the budget runs out, the exception is the call's
    /// failure.
    /// </para>
    /// <para>
    /// Any other exception from <paramref name="act"/> ends the call at once: the action may have
    /// taken effect.
    /// </para>
    /// </remarks>
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
    /// <remarks>
    /// Resolution is polled because it is safe to repeat; the action itself then runs exactly
    /// once, so a failing action is never replayed.
    /// </remarks>
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
    /// <remarks>
    /// <para>
    /// The one way to wait inside a Core method. The generated wrapper around the Core method has
    /// already checked the scope chain, resolved the element and opened the log entry, so this
    /// does none of that: it only reads. A <c>Run*</c> helper here would start a second unit of
    /// work with its own timeout and its own log entry.
    /// </para>
    /// <para>
    /// Never repeats anything (R0). A stale read ends the wait as
    /// <see cref="ConfirmationResult.Replaced"/> at once, not as a retry.
    /// </para>
    /// </remarks>
    /// <param name="read">Reads the value, typically a Core query on the held element.</param>
    /// <param name="done">Whether the value is the one waited for.</param>
    /// <param name="timeoutMs">Maximum time to wait; null for the default.</param>
    /// <returns>How the wait ended, with the last value and error.</returns>
    protected Confirmation<T> Confirm<T>(Func<T?> read, Func<T?, bool> done, int? timeoutMs)
        => Confirmer.Run(read, done, timeoutMs ?? DefaultTimeoutMs, PollingIntervalMs);

    /// <summary>
    /// Checks any additional readiness this control requires before it can be acted on.
    /// </summary>
    /// <remarks>
    /// One check, inside the call's poll, so a control that becomes ready a moment late is waited
    /// for rather than failed against. Throws <see cref="ElementNotReadyException"/> when it is not
    /// ready. <see cref="ViewBase{TScope}"/> requires nothing beyond being present and visible;
    /// <c>ClickableControlBase</c> adds "enabled".
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
    /// <remarks>
    /// Used by generated <c>Get*</c> members whose Core method carries <c>[AbsenceTolerant]</c>.
    /// It waits for the page as every member does, then reads once: a missing element is an
    /// answer (typically null), not something to wait for.
    /// </remarks>
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
    /// An assertion as one call: finds the element once, then re-reads it each attempt.
    /// </summary>
    /// <remarks>
    /// Holding the element saves a lookup per read (S1). It is found again when it turns out to be
    /// gone, or when it was found but not visible - a replaced element must not use up the call.
    /// </remarks>
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
        IMauiElement? held = null;
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (!Poller.Until(a =>
                {
                    if (ScopeGate.Check(_mauiScope) is { } notReady)
                    {
                        return notReady;
                    }

                    if (held == null)
                    {
                        var found = Locate(a);
                        if (found == null)
                        {
                            return Observation.Missing();
                        }

                        try
                        {
                            EnsureVisible(found, a);
                        }
                        catch (ElementNotReadyException error)
                        {
                            return Observation.NotReady(error, KeyOf(found));
                        }

                        held = found;
                    }

                    try
                    {
                        var actual = getActual(held);
                        return compare(actual, expected)
                            ? Observation.Done()
                            : Observation.Mismatch(
                                new AssertionException(message ?? "Assert exception", expected, actual), null);
                    }
                    catch (StaleElementException error)
                    {
                        held = null;
                        return Observation.Stale(error, null);
                    }
                }, attempt, PollingIntervalMs))
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
    /// <remarks>
    /// The difference from <see cref="TryFindElement()"/> matters only on Android, which publishes
    /// an accessibility node only for content inside the viewport. Windows keeps off-screen
    /// elements in the tree, so both platforms give a test the same answer. The scope names the
    /// element that scrolls through <see cref="IMauiElementScope.ScrollingRoot"/>.
    /// </remarks>
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
    /// <remarks>
    /// With <see cref="ScrollLookup.Once"/> the sweep happens on the first call only; later calls
    /// use a plain lookup.
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
    /// Finds the element in one attempt, sweeping the scope's scroller once if the plain lookup
    /// finds nothing, or throws <see cref="NotFound"/>.
    /// </summary>
    /// <remarks>
    /// Not virtual: where a control is is <see cref="TryFindElement()"/>, the one lookup a control
    /// overrides, and this derives from it (F5). Inside a call's poll, <see cref="Locate"/> is used
    /// instead, which throttles the sweep.
    /// </remarks>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    protected IMauiElement FindElement()
        => TryFindElement()
           ?? (_mauiScope.AllowsScrollLookup ? ScrollingElement()?.TryFindByScrolling(Locator) : null)
           ?? throw NotFound();

    /// <summary>
    /// The error for a control that could not be found.
    /// </summary>
    /// <remarks>
    /// By default the scope's own reason ("not loaded", "not within the container"), asked for
    /// once, at the moment of failure. A control whose <see cref="TryFindElement()"/> looks in more
    /// than one place overrides this to say where it looked.
    /// </remarks>
    protected virtual ElementNotFoundException NotFound()
    {
        try
        {
            _mauiScope.FindElement(Locator);
        }
        catch (ElementNotFoundException error)
        {
            return error;
        }

        return new ElementNotFoundException(Locator);
    }

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
    /// <remarks>
    /// <c>IsVisible</c> answers "on screen right now"; this answers "could the user see it at
    /// all". Prefer this when a test means "the page shows this control", since whether something
    /// sits above the fold depends on window size and differs between platforms.
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

    /// <summary>
    /// One attempt at "visible": visible now, or scroll it into view (throttled) and look once
    /// more. Throws <see cref="ElementNotReadyException"/> when it is still not visible.
    /// </summary>
    /// <remarks>
    /// Never waits (R2): the call's poll decides whether to try again, and finds the element again
    /// when it does - a replaced element must not use up the call. The scroll is given what is
    /// left of the call's budget, never a default of its own.
    /// </remarks>
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
    /// <remarks>
    /// <para>
    /// "On the page", not "in the accessibility tree right now": Android publishes a node only
    /// for content inside the viewport.
    /// </para>
    /// <para>
    /// <c>AssertExists(false)</c> scrolls the container before it can answer, so checking for
    /// absence is slower than checking for presence.
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
    /// An escape hatch that asks one platform a question in its own vocabulary. Windows answers
    /// automation id, name, class, control type, enabled, visible, help text and the scroll
    /// percentages; Android answers UiAutomator2's accessibility attributes - <c>text</c>,
    /// <c>content-desc</c>, <c>resource-id</c>, <c>checked</c>, <c>selected</c>, <c>focused</c>,
    /// <c>hint</c> and similar. Anything else returns null.
    /// </para>
    /// <para>
    /// Null means either "empty" or "no such attribute". MAUI bindable properties such as
    /// <c>Value</c> or <c>Source</c> are not automation attributes and always return null; prefer
    /// the members on <c>IMauiElement</c>.
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
