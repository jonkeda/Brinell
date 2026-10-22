using Brinell.Core.Calls;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Core.Utilities;
using Brinell.Html.Interfaces;
using System.Runtime.CompilerServices;

namespace Brinell.Html.Controls;

/// <summary>
/// Base class for every Html control: the Is/Wait/Assert pattern with fluent chaining. A control
/// finds its element within its scope (page, container, frame or row).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class ControlBase<TScope> : ObjectBase, IControlObject<TScope>
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

    /// <summary>The element, found in one attempt, or null.</summary>
    protected IHtmlElement? TryFindElement() => _scope.TryFindElement(Locator);

    /// <summary>The element, found in one attempt.</summary>
    /// <exception cref="ElementNotFoundException">It is not there.</exception>
    protected IHtmlElement FindElement() => TryFindElement() ?? throw NotFound();

    /// <summary>The error for a control that could not be found.</summary>
    protected virtual ElementNotFoundException NotFound() => new(Locator);

    #region Calls

    // Every public member is one call (.docs/contracts/call-model.md, R1): one log pair, one
    // budget, one poll. Each attempt checks the scope, finds the element again, makes one
    // visibility check, then reads, compares or checks readiness. Nothing inside a call waits
    // with a budget of its own (R2). An action runs once, after the poll (R0). The Run* names and
    // signatures are MAUI's (R8), so the generator's output compiles on both.

    private string PageName => _scope.Page?.GetType().Name ?? "Unknown";

    private string ControlId => Locator.Value;

    private ControlCall Call => new(Context.Logger, Context.Name, Context.NearMiss, PageName, ControlId);

    private int Budget(int? timeoutMs) => timeoutMs ?? DefaultTimeoutMs;

    private int AnimationMs => Context.Timeouts.Animation;

    /// <summary>
    /// What is left of the running call's budget, for a wait below the call (a scroll); the
    /// default wait outside a call.
    /// </summary>
    protected int CallRemainingMs => CallBudget.RemainingOr(DefaultTimeoutMs);

    private Exception Failure(AttemptContext attempt, string caller, int budgetMs)
        => attempt.Log.ToException(
            Locator,
            budgetMs,
            NotFound,
            readiness => ScopeGate.NotReady(readiness, caller, $"control '{Locator}'", budgetMs));

    // The scope chain, one probe per attempt (R4): the scope answers for itself and its parents.
    private Observation? CheckScope() => ScopeGate.Check(_scope.ProbeReadiness());

    /// <summary>
    /// One attempt against the element: the scope, then a fresh lookup, then (optionally) one
    /// visibility check, then <paramref name="body"/>.
    /// </summary>
    private Observation Attempt(AttemptContext attempt, bool ensureVisible, Func<IHtmlElement, Observation> body)
    {
        if (CheckScope() is { } notReady)
        {
            return notReady;
        }

        var element = TryFindElement();
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

            return body(element);
        }
        catch (StaleElementException error)
        {
            return Observation.Stale(error, null);
        }
        catch (ElementNotReadyException error)
        {
            return Observation.NotReady(error, null);
        }
        catch (AssertionException error)
        {
            return Observation.Mismatch(error, null);
        }
    }

    private void AwaitScope(AttemptContext attempt, string caller, int budgetMs)
    {
        if (!Poller.Until(_ => CheckScope() ?? Observation.Done(), attempt, PollingIntervalMs))
        {
            throw Failure(attempt, caller, budgetMs);
        }
    }

    protected bool RunWait(Func<bool> operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunWait);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (Poller.Until(_ => CheckScope() ?? (operation() ? Observation.Done() : Observation.Pending()),
                    attempt, PollingIntervalMs))
            {
                return true;
            }

            return attempt.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(attempt, caller, budget);
        }, succeeded: met => met);
    }

    protected bool RunWaitWithElement<T>(T? expected, Func<IHtmlElement, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return true;
        }

        return RunWaitObserving(element => coreOperation(element) ? Observation.Done() : Observation.Pending(),
            timeoutMs, caller ?? nameof(RunWaitWithElement));
    }

    /// <summary>
    /// Polls <paramref name="until"/> on the value <paramref name="read"/> gives, until it holds.
    /// The last value read is in the call's log when it never did.
    /// </summary>
    protected bool RunWaitForValue(Func<IHtmlElement, string?> read, Func<string?, bool> until,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        ArgumentNullException.ThrowIfNull(until);
        return RunWaitObserving(element =>
            {
                var value = read(element);
                return until(value) ? Observation.Done() : Observation.Pending($"value '{value}'");
            },
            timeoutMs, caller ?? nameof(RunWaitForValue));
    }

    private bool RunWaitObserving(Func<IHtmlElement, Observation> observe, int? timeoutMs, string caller)
    {
        var budget = Budget(timeoutMs);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (Poller.Until(a => Attempt(a, ensureVisible: true, observe),
                    attempt, PollingIntervalMs))
            {
                return true;
            }

            // A predicate that stayed false answers false; a call that never got to ask fails.
            return attempt.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(attempt, caller, budget);
        }, succeeded: met => met);
    }

    /// <summary>Polls a predicate that is meaningful when the element is absent.</summary>
    protected bool RunWaitWithOptionalElement<T>(T? expected, Func<IHtmlElement?, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return true;
        }

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunWaitWithOptionalElement);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (Poller.Until(_ => CheckScope() ?? (coreOperation(TryFindElement()) ? Observation.Done() : Observation.Pending()),
                    attempt, PollingIntervalMs))
            {
                return true;
            }

            return attempt.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(attempt, caller, budget);
        }, succeeded: met => met);
    }

    /// <summary>Asserts a value that is meaningful when the element is absent.</summary>
    protected TScope RunAssertWithOptionalElement<T>(T? expected,
        Func<IHtmlElement?, T?> getActual, Func<T?, T?, bool> compare,
        string? message = null, int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunAssertWithOptionalElement);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (!Poller.Until(_ =>
                {
                    if (CheckScope() is { } notReady)
                    {
                        return notReady;
                    }

                    var actual = getActual(TryFindElement());
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

    /// <summary>Runs an operation as one call: waits for the scope, then runs it once.</summary>
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

    /// <summary>Whether an action waits for the element to be visible before it runs.</summary>
    protected virtual bool RequiresVisibilityForAction => true;

    /// <summary>An action as one call: polls until the control is ready to be acted on, then acts once.</summary>
    protected TScope RunDoWithElement(Action<IHtmlElement> coreOperation,
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
    /// it did not act (<see cref="ElementNotReadyException"/>).
    /// </summary>
    private void ActOnce(AttemptContext attempt, bool ensureVisible, string caller, int budgetMs,
        Action<IHtmlElement> act)
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
            catch (ElementNotReadyException error) when (error.Locator != Locator)
            {
                // The element reported it by its own selector (Playwright's actionability); the call names the control's.
                throw new ElementNotReadyException(Locator, error.Reason, error.Detail);
            }
        }
    }

    private IHtmlElement ResolveReady(AttemptContext attempt, bool ensureVisible, string caller, int budgetMs)
    {
        IHtmlElement? ready = null;
        if (!Poller.Until(a => Attempt(a, ensureVisible: false, element =>
            {
                // An element whose actions wait for actionability themselves (Playwright's) is not checked twice.
                if (ensureVisible && !element.WaitsForActionability)
                {
                    EnsureVisible(element, a);
                }

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
    /// Waits, inside an action, for the effect of what the action has already done. Never repeats
    /// the action (R0).
    /// </summary>
    protected Confirmation<T> Confirm<T>(Func<T?> read, Func<T?, bool> done, int? timeoutMs)
        => Confirmer.Run(read, done, timeoutMs ?? DefaultTimeoutMs, PollingIntervalMs);

    /// <summary>
    /// Inside an action: runs <paramref name="act"/> on the element once, then confirms its effect
    /// with a budget of its own (R3). An effect that never shows fails the call, naming what was
    /// read; the action is never repeated (R0).
    /// </summary>
    /// <param name="action">The action's name, for the message ("Fill", "Check").</param>
    /// <param name="act">The action.</param>
    /// <param name="read">Reads the effect, typically from the element just acted on.</param>
    /// <param name="done">Whether the value read is the effect asked for.</param>
    /// <param name="expected">What was asked for, in words, for the message.</param>
    /// <param name="timeoutMs">The confirmation's budget; the caller's timeout, or the default.</param>
    protected void ActAndConfirm<T>(string action, Action act, Func<T?> read, Func<T?, bool> done,
        string expected, int? timeoutMs)
    {
        act();
        var confirmation = Confirm(read, done, timeoutMs);
        if (!confirmation.IsConfirmed)
        {
            throw confirmation.Failure(Locator, action, lastError => new InvalidOperationException(
                $"The element '{Locator}' accepted {action} and did not take it: expected {expected}, "
                + $"it reads '{confirmation.LastValue}'. The action ran once and is not repeated.",
                lastError));
        }
    }

    /// <summary>
    /// Checks any readiness this control needs before it is acted on. Throw
    /// <see cref="ElementNotReadyException"/> when it is not ready; the call asks again.
    /// </summary>
    protected virtual void EnsureReadyForActionCore(IHtmlElement element)
    {
    }

    protected TScope RunSetWithElement<T>(T? value, Action<IHtmlElement> coreOperation,
         int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (value == null)
        {
            return ContainingScope;
        }

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunSetWithElement);
        return Call.Run(caller, value.ToString(), budget, AnimationMs, attempt =>
        {
            ActOnce(attempt, ensureVisible: true, caller, budget, coreOperation);
            return ContainingScope;
        });
    }

    /// <summary>
    /// Reads a value that is meaningful when the element is absent: once, after the scope is
    /// ready, with the element resolved optionally.
    /// </summary>
    protected T? RunGetWithOptionalElement<T>(Func<IHtmlElement?, T> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunGetWithOptionalElement);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            AwaitScope(attempt, caller, budget);
            return coreOperation(TryFindElement());
        });
    }

    protected T? RunGetWithElement<T>(Func<IHtmlElement, T> coreOperation,
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

    /// <summary>Run assertion with custom comparison function.</summary>
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
                    if (CheckScope() is { } notReady)
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

    /// <summary>An assertion as one call: finds the element again on every attempt, then compares.</summary>
    protected TScope RunAssertWithElement<T>(T? expected, Func<IHtmlElement, T?> getActual,
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

    #region Keys

    /// <summary>Sends keyboard keys to the control.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="keys">The keys to send.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SendKeysCore(IHtmlElement element, string keys, int? timeoutMs = null)
    {
        element.SendKeys(keys);
    }

    /// <summary>Presses one key by its name ("ArrowRight", "Enter", "Escape"), as a keyboard does: <see cref="SendKeysCore"/> types its text instead.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="key">The key's name.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PressKeyCore(IHtmlElement element, string key, int? timeoutMs = null)
    {
        element.PressKey(key);
    }

    #endregion

    #region Visible

    /// <summary>Whether the control is visible; null when it is not there.</summary>
    /// <param name="element">The pre-found element, or null.</param>
    [AbsenceTolerant]
    protected virtual bool? IsVisibleCore(IHtmlElement? element)
    {
        return element?.Visible;
    }

    /// <summary>
    /// One attempt at "visible": visible now, or scroll it into view (throttled) and look once
    /// more. Throws <see cref="ElementNotReadyException"/> when it is still not visible.
    /// </summary>
    private void EnsureVisible(IHtmlElement element, AttemptContext attempt)
    {
        if (IsVisibleCore(element) == true)
        {
            return;
        }

        if (attempt.MayScrollIntoView(Locator.ToString()))
        {
            element.ScrollIntoView(Math.Max(1, attempt.Deadline.RemainingMs));

            if (IsVisibleCore(element) == true)
            {
                return;
            }
        }

        throw new ElementNotReadyException(Locator, NotReadyReason.NotVisible);
    }

    #endregion

    #region Enabled

    /// <summary>Throws <see cref="ElementNotReadyException"/> (Disabled) when the element is not enabled.</summary>
    protected virtual void EnsureEnabledCore(IHtmlElement element)
    {
        if (IsEnabledCore(element) != true)
        {
            throw new ElementNotReadyException(Locator, NotReadyReason.Disabled, "not enabled");
        }
    }

    /// <summary>Whether the control is enabled.</summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual bool? IsEnabledCore(IHtmlElement? element)
    {
        return element?.Enabled;
    }

    #endregion

    #region Exists

    // Hand-written, as on MAUI: IsExists answers bool, not bool?, and never fails on absence.
    private static bool? IsExistsBase(IHtmlElement? element) => element != null;

    /// <summary>Whether the control is on the page now. Does not wait (R7).</summary>
    public bool IsExists()
    {
        return IsExistsBase(TryFindElement()) == true;
    }

    /// <summary>Waits until the control's presence matches <paramref name="expected"/>.</summary>
    public bool WaitExists(bool? expected = true, int? timeoutMs = null)
    {
        return RunWaitWithOptionalElement(expected,
            element => IsExistsBase(element) == expected!.Value, timeoutMs);
    }

    /// <summary>Asserts the control's presence, returning the scope for chaining.</summary>
    public TScope AssertExists(bool? expected = true, string? message = null, int? timeoutMs = null)
    {
        return RunAssertWithOptionalElement(expected,
            IsExistsBase, (actual, expected1) => actual == expected1,
            message ?? $"Expected Exists to be '{expected}'. Locator: {Locator}", timeoutMs);
    }

    #endregion

    #region Text

    /// <summary>The control's text.</summary>
    /// <param name="element">The pre-found element.</param>
    [GenerateComparisons(Comparison.Equals | Comparison.Contains | Comparison.StartsWith
        | Comparison.EndsWith | Comparison.Empty)]
    protected virtual string? GetTextCore(IHtmlElement element)
    {
        return element.Text;
    }

    /// <summary>The text now, or <c>null</c> when the control is not there. Does not wait (R7).</summary>
    public string? TryGetText() => TryFindElement() is { } element ? GetTextCore(element) : null;

    /// <summary>Waits until the text equals <paramref name="expected"/>. An alias for <c>WaitText</c>.</summary>
    public bool WaitTextEquals(string? expected, int? timeoutMs = null) => WaitText(expected, timeoutMs);

    /// <summary>Waits until the text satisfies <paramref name="until"/> (a count above zero, a value that changed).</summary>
    public bool WaitText(Func<string?, bool> until, int? timeoutMs = null)
        => RunWaitForValue(element => GetTextCore(element), until, timeoutMs);

    #endregion

    #region Attributes

    /// <summary>A named attribute of the control.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="name">The attribute's name.</param>
    protected virtual string? GetAttributeCore(IHtmlElement element, string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return element.GetAttribute(name);
    }

    /// <summary>Waits until the attribute <paramref name="name"/> satisfies <paramref name="until"/>.</summary>
    public bool WaitAttribute(string name, Func<string?, bool> until, int? timeoutMs = null)
        => RunWaitForValue(element => GetAttributeCore(element, name), until, timeoutMs);

    #endregion

    #region ScrollIntoView

    /// <summary>
    /// Scrolls the element into the visible viewport, as one call. Scrolling is how the element
    /// becomes visible, so this call does not wait for visibility first.
    /// </summary>
    public TScope ScrollIntoView(int? timeoutMs = null)
    {
        var budget = Budget(timeoutMs);
        const string caller = nameof(ScrollIntoView);
        return Call.Run(caller, null, budget, AnimationMs, attempt =>
        {
            if (!Poller.Until(a => Attempt(a, ensureVisible: false, element =>
                {
                    ScrollIntoViewCore(element);
                    return Observation.Done();
                }), attempt, PollingIntervalMs))
            {
                throw Failure(attempt, caller, budget);
            }

            return ContainingScope;
        });
    }

    /// <summary>Scrolls the element into view, with what is left of the call's budget.</summary>
    /// <param name="element">The pre-found element.</param>
    [SkipGeneration("Hand-written above: scrolling is how the element becomes visible, so the call does not wait for visibility first.")]
    protected virtual void ScrollIntoViewCore(IHtmlElement element)
    {
        element.ScrollIntoView(CallRemainingMs);
    }

    #endregion
}
