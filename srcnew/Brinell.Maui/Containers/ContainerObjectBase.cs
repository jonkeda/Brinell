using System.Runtime.CompilerServices;
using Brinell.Core.Utilities;
using Brinell.Maui.Calls;

namespace Brinell.Maui.Containers;

/// <summary>
/// Shared behavior for scopes rooted at an element rather than at the driver.
/// </summary>
/// <remarks>
/// <para>
/// Pages and child containers both inherit this behavior. Parent navigation belongs only
/// to <see cref="ContainerObjectBase{TParent, TSelf}"/>.
/// </para>
/// <para>
/// By default, searches are scoped strictly to <see cref="ContainerRoot"/>: when a child is not
/// found within the container, the search does <b>not</b> fall back to the parent scope.
/// Container scoping means elements must be within the container. A component whose platform
/// deliberately flattens its children may override the element-finding members explicitly.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The container type itself (self-referencing for fluent returns).</typeparam>
/// <typeparam name="TSetResult">The scope returned by generated set operations.</typeparam>
public abstract class RootedScopeBase<TSelf, TSetResult>
    : ObjectBase, IMauiScope<TSelf>
    where TSelf : RootedScopeBase<TSelf, TSetResult>
{
    private IMauiElement? _cachedRoot;
    private bool _rootCacheValid;

    /// <summary>
    /// The locator that finds this scope's root element.
    /// </summary>
    protected abstract Locator Locator { get; }

    /// <inheritdoc />
    public TSelf Self => (TSelf)this;

    /// <inheritdoc />
    public abstract IMauiPage? Page { get; }

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;

    /// <summary>
    /// Whether this container keeps its resolved root between operations.
    /// </summary>
    protected virtual bool CacheContainerRoot => true;

    /// <summary>Checks whether a cached root still represents this scope.</summary>
    /// <remarks>
    /// By default: the root is still there. Reading its <see cref="IMauiElement.InstanceKey"/> is
    /// a live read, and a removed root answers it with <see cref="StaleElementException"/> on every
    /// platform (<c>.my/stale-readiness/design.md</c>, R6). A page adds "and shown".
    /// </remarks>
    protected virtual bool IsCachedRootValid(IMauiElement root)
    {
        _ = root.InstanceKey;
        return true;
    }

    #region Container root

    /// <inheritdoc />
    public IMauiElement ContainerRoot
    {
        get
        {
            if (!CacheContainerRoot)
                return FindContainerRootElement();

            if (_rootCacheValid && _cachedRoot != null)
            {
                try
                {
                    if (IsCachedRootValid(_cachedRoot))
                    {
                        return _cachedRoot;
                    }

                    InvalidateCache();
                }
                catch (StaleElementException)
                {
                    InvalidateCache();
                }
            }

            _cachedRoot = FindContainerRootElement();
            _rootCacheValid = true;
            return _cachedRoot;
        }
    }

    /// <summary>
    /// Finds the container's root element. Override to search elsewhere, for example a
    /// popup window for a dialog that lives outside the normal scope chain.
    /// </summary>
    /// <exception cref="ElementNotFoundException">Thrown when the root is not found.</exception>
    protected abstract IMauiElement FindContainerRootElement();

    /// <inheritdoc />
    public void InvalidateCache()
    {
        _rootCacheValid = false;
        _cachedRoot = null;
    }

    /// <summary>
    /// Gets this container's own element, or null when it is absent.
    /// </summary>
    /// <remarks>
    /// For a container this is its root.
    /// </remarks>
    protected IMauiElement? TryFindElement() => TryGetContainerRoot();

    /// <summary>
    /// Gets the container root without throwing when it is absent.
    /// </summary>
    protected IMauiElement? TryGetContainerRoot()
    {
        try
        {
            return ContainerRoot;
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Runs <paramref name="read"/> against the root. When the root turns out to be gone, forgets
    /// it, finds it again once, and runs <paramref name="read"/> on the new one.
    /// </summary>
    /// <remarks>
    /// The one place the scope layer handles a stale root (<c>.my/stale-readiness/design.md</c>,
    /// section 7.3; F9). A second stale answer propagates: the call's poll decides what to do
    /// with it.
    /// </remarks>
    /// <param name="read">What to do with the root.</param>
    /// <param name="whenAbsent">The answer when there is no root.</param>
    /// <returns>What <paramref name="read"/> returned, or <paramref name="whenAbsent"/>.</returns>
    protected T WithRoot<T>(Func<IMauiElement, T> read, T whenAbsent)
    {
        var root = TryGetContainerRoot();
        if (root == null) return whenAbsent;

        try
        {
            return read(root);
        }
        catch (StaleElementException)
        {
            InvalidateCache();

            root = TryGetContainerRoot();
            return root == null ? whenAbsent : read(root);
        }
    }

    #endregion

    #region Typed children (scoped to the container root)

    /// <summary>
    /// A child control of this container, as a control object rather than a raw element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The typed counterpart to <see cref="FindElement"/>, which returns a raw
    /// <c>IMauiElement</c>.
    /// </para>
    /// <para>
    /// Prefer declaring a named property on the container - <c>public Label&lt;ProductRow&gt; Name
    /// =&gt; new(this, "ProductNameLabel");</c>. Use this for children not worth naming.
    /// </para>
    /// </remarks>
    /// <typeparam name="TControl">The control type to resolve the child as.</typeparam>
    /// <param name="automationId">The child's automation id.</param>
    public TControl Child<TControl>(string automationId)
        where TControl : Controls.Base.ViewBase<TSelf>
    {
        ArgumentException.ThrowIfNullOrEmpty(automationId);

        return (TControl)Activator.CreateInstance(
            typeof(TControl),
            (IMauiScope<TSelf>)(TSelf)this,
            automationId)!;
    }

    /// <summary>A child <see cref="Controls.Display.Label{TScope}"/> of this container.</summary>
    public Controls.Display.Label<TSelf> Label(string automationId)
        => new((TSelf)this, automationId);

    /// <summary>A child <see cref="Controls.Buttons.Button{TScope}"/> of this container.</summary>
    public Controls.Buttons.Button<TSelf> Button(string automationId)
        => new((TSelf)this, automationId);

    /// <summary>A child <see cref="Controls.Text.Entry{TScope}"/> of this container.</summary>
    public Controls.Text.Entry<TSelf> Entry(string automationId)
        => new((TSelf)this, automationId);

    /// <summary>A child <see cref="Controls.Toggle.CheckBox{TScope}"/> of this container.</summary>
    public Controls.Toggle.CheckBox<TSelf> CheckBox(string automationId)
        => new((TSelf)this, automationId);

    #endregion

    #region Element finding (scoped to the container root)

    /// <inheritdoc />
    /// <remarks>
    /// None for a page or a plain scope: the driver picks the scrolling element on screen.
    /// A container passes its parent's answer on, and a container that scrolls itself answers
    /// with its own root.
    /// </remarks>
    public virtual IMauiElement? ScrollingRoot => null;

    /// <inheritdoc />
    public virtual bool AllowsScrollLookup => true;

    /// <inheritdoc />
    /// <remarks>
    /// One attempt, and no readiness check: a lookup answers what is there now. A call's attempt
    /// asks the scope chain first (<see cref="ProbeReadiness"/>), so a scope that is not ready is
    /// reported as that, not as a missing child.
    /// </remarks>
    public virtual IMauiElement? TryFindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        // Not found within the container is null. Do NOT fall back to the parent scope -
        // container scoping means elements must be within the container.
        return WithRoot(root => root.TryFindElement(locator), null);
    }

    /// <inheritdoc />
    public virtual IMauiElement FindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        return TryFindElement(locator) ?? throw DescribeMiss(locator);
    }

    /// <inheritdoc />
    public virtual ElementNotFoundException DescribeMiss(Locator locator)
        => new($"Element not found within {ScopeName}. Child locator: {locator}");

    /// <inheritdoc />
    public virtual IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        return WithRoot(root => root.FindElements(locator), []);
    }

    #endregion

    #region Readiness

    /// <summary>The scope's name in readiness answers and messages.</summary>
    protected virtual string ScopeName => $"{GetType().Name.Split('`')[0]} '{Locator}'";

    /// <inheritdoc />
    /// <remarks>
    /// The parent first (<see cref="ProbeParentReadiness"/>), returned unchanged when it is not
    /// ready; then this scope's root; then <see cref="ProbeContentReadiness"/> on it. A root that
    /// turns out to be gone is found once more; gone again, the answer is
    /// <see cref="ScopeReadinessState.StaleRoot"/>.
    /// </remarks>
    public virtual ScopeReadiness ProbeReadiness()
    {
        var parent = ProbeParentReadiness();
        return parent.IsReady ? ProbeOwnReadiness(rootReacquired: false) : parent;
    }

    /// <inheritdoc cref="IMauiElementScope.IsReady"/>
    public bool IsReady() => ProbeReadiness().IsReady;

    /// <summary>
    /// The readiness this scope inherits: its parent's when it asks the parent, otherwise ready.
    /// </summary>
    /// <remarks>None for a page, which has no parent.</remarks>
    protected virtual ScopeReadiness ProbeParentReadiness() => ScopeReadiness.Ready(ScopeName);

    /// <summary>
    /// What an attempt of this scope's own members checks first: the parent chain for a
    /// container (its own root is what the attempt then looks for), the whole probe for a page.
    /// </summary>
    protected virtual ScopeReadiness ProbeCallReadiness() => ProbeParentReadiness();

    /// <summary>
    /// This scope's own readiness beyond "the root is there": one attempt, no waiting.
    /// </summary>
    /// <remarks>
    /// Override for a scope whose content loads asynchronously, checking concrete UI state - a
    /// spinner gone, at least one row - and answering <see cref="ContentReady"/> or
    /// <see cref="ContentNotReady"/>. Never sleep here: the call's poll repeats the probe.
    /// </remarks>
    /// <param name="root">The scope's root, just found or checked.</param>
    protected virtual ScopeReadiness ProbeContentReadiness(IMauiElement root) => ContentReady();

    /// <summary>The answer of a scope that is ready.</summary>
    protected ScopeReadiness ContentReady() => ScopeReadiness.Ready(ScopeName);

    /// <summary>The answer of a scope whose content is not ready yet.</summary>
    /// <param name="detail">What it is waiting for, for the message.</param>
    protected ScopeReadiness ContentNotReady(string? detail = null)
        => new(ScopeName, ScopeReadinessState.ContentNotReady, detail);

    private ScopeReadiness ProbeOwnReadiness(bool rootReacquired)
    {
        try
        {
            var root = TryGetContainerRoot();
            if (root == null)
            {
                return new ScopeReadiness(
                    ScopeName,
                    rootReacquired ? ScopeReadinessState.StaleRoot : ScopeReadinessState.MissingRoot,
                    RootReacquired: rootReacquired);
            }

            var answer = ProbeContentReadiness(root);
            return rootReacquired ? answer with { RootReacquired = true } : answer;
        }
        catch (StaleElementException)
        {
            if (rootReacquired)
            {
                return new ScopeReadiness(ScopeName, ScopeReadinessState.StaleRoot, RootReacquired: true);
            }

            InvalidateCache();
            return ProbeOwnReadiness(rootReacquired: true);
        }
    }

    /// <summary>The budget <see cref="WaitReady"/> uses when the caller gives none.</summary>
    protected virtual int DefaultReadyTimeoutMs => DefaultTimeoutMs;

    /// <inheritdoc />
    /// <remarks>One call: polls <see cref="ProbeReadiness"/>. A misconfigured scope fails at once.</remarks>
    public virtual bool WaitReady(int? timeoutMs = null)
        => RunProbe(() => ScopeGate.Check(ProbeReadiness()) ?? Observation.Done(),
            timeoutMs ?? DefaultReadyTimeoutMs);

    /// <summary>
    /// A member of the scope itself as one call: polls <paramref name="probe"/> with no readiness
    /// step in front, because the probe is about the scope's own state.
    /// </summary>
    /// <param name="probe">One attempt.</param>
    /// <param name="budgetMs">The call's budget.</param>
    /// <param name="onTimeout">
    /// The failure to throw when the budget runs out, given the last readiness; null to answer
    /// false instead.
    /// </param>
    /// <param name="caller">The public member.</param>
    /// <returns>True when the probe reported done within the budget.</returns>
    private protected bool RunProbe(Func<Observation> probe, int budgetMs,
        Func<ScopeReadiness, Exception>? onTimeout = null,
        [CallerMemberName] string? caller = null)
        => Call.Run(caller ?? nameof(RunProbe), null, budgetMs, AnimationMs, context =>
        {
            if (Poll(context, _ => probe()))
            {
                return true;
            }

            if (context.Log.Last is { Kind: ObservationKind.Failed, Error: { } error })
            {
                var failure = context.Log.Unexpected(error, ScopeName, budgetMs);
                if (ReferenceEquals(failure, error))
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                }

                throw failure;
            }

            return onTimeout == null ? false : throw onTimeout(ProbeReadiness());
        }, succeeded: met => met);

    #endregion

    #region State

    /// <summary>
    /// Whether the container's root element exists, now. <see cref="WaitExists"/> waits.
    /// </summary>
    public bool IsExists() => TryGetContainerRoot() != null;

    /// <summary>
    /// Whether the container's root element is visible, now; null when it does not exist.
    /// <see cref="WaitVisible"/> waits.
    /// </summary>
    public bool? IsVisible() => TryGetContainerRoot()?.Visible;

    /// <summary>
    /// Waits until the container's existence matches <paramref name="expected"/>.
    /// A null expectation is a skip and returns true.
    /// </summary>
    /// <remarks>One call; each attempt checks the parent chain first.</remarks>
    public bool WaitExists(bool? expected, int? timeoutMs = null)
        => RunWaitWithOptionalElement(expected, root => (root != null) == expected!.Value, timeoutMs);

    /// <summary>
    /// Waits until the container's visibility matches <paramref name="expected"/>.
    /// A null expectation is a skip and returns true.
    /// </summary>
    /// <remarks>One call; each attempt checks the parent chain first.</remarks>
    public bool WaitVisible(bool? expected, int? timeoutMs = null)
        => RunWaitWithOptionalElement(expected, root => root?.Visible == expected!.Value, timeoutMs);

    /// <summary>
    /// Asserts the container's existence, returning the container so a chain stays inside it.
    /// </summary>
    public TSelf AssertExists(bool? expected = true, string? message = null, int? timeoutMs = null)
        => RunAssertWithOptionalElement(
            expected,
            root => (bool?)(root != null),
            (actual, wanted) => actual == wanted,
            message ?? $"Expected container {(expected == true ? "to exist" : "not to exist")}. Locator: {Locator}",
            timeoutMs);

    /// <summary>
    /// Asserts the container's visibility, returning the container so a chain stays inside it.
    /// </summary>
    public TSelf AssertVisible(bool? expected = true, string? message = null, int? timeoutMs = null)
        => RunAssertWithOptionalElement(
            expected,
            root => root?.Visible,
            (actual, wanted) => actual == wanted,
            message ?? $"Expected container visibility '{expected}'. Locator: {Locator}",
            timeoutMs);

    #endregion

    #region Run helpers

    // Mirrors the helper surface on the control base, but returning TSelf instead of the
    // parent scope. Identical names and shapes let the generator emit one body for both
    // hierarchies. The element these operate on is the container root.
    //
    // Every public member is one call (.my/stale-readiness/design.md, R1): one log pair, one
    // budget, one poll, whose attempts check the page and then use the root. An action runs once.

    private protected ControlCall Call => new(Context, PageName, ControlId);

    private protected int Budget(int? timeoutMs) => timeoutMs ?? DefaultTimeoutMs;

    private protected int AnimationMs => Context.Timeouts.Animation;

    /// <summary>
    /// What is left of the running call's budget, for a wait below the call (a scroll); the
    /// default wait outside a call. Nothing inside a call starts a budget of its own (R2, R3).
    /// </summary>
    protected int CallRemainingMs => AttemptContext.RemainingOr(DefaultTimeoutMs);

    /// <summary>The failure of a phase that ran out of budget, from what it last saw.</summary>
    private protected Exception Failure(AttemptContext attempt, string caller, int budgetMs)
        => attempt.Log.ToException(
            Locator,
            budgetMs,
            RootNotFound,
            readiness => ScopeGate.NotReady(readiness, caller, $"container '{Locator}'", budgetMs));

    /// <summary>
    /// The error for this scope's root not found, in the words of whatever it is looked up in.
    /// Builds the message only: it does not look again.
    /// </summary>
    protected virtual ElementNotFoundException RootNotFound() => new(Locator);

    /// <summary>One attempt against the root: the scope chain, then the root, then <paramref name="body"/>.</summary>
    private protected Observation RootAttempt(Func<IMauiElement, Observation> body)
    {
        if (ScopeGate.Check(ProbeCallReadiness()) is { } notReady)
        {
            return notReady;
        }

        var root = TryGetContainerRoot();
        if (root == null)
        {
            return Observation.Missing();
        }

        try
        {
            return body(root);
        }
        catch (StaleElementException error)
        {
            InvalidateCache();
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

    /// <summary>A poll of <paramref name="attempt"/> as one call; false when it never reported Done.</summary>
    private protected bool Poll(AttemptContext context, Func<AttemptContext, Observation> attempt,
        Func<Observation, bool>? stop = null)
        => Poller.Until(attempt, context, PollingIntervalMs, stop);

    /// <summary>
    /// Waits, inside a Core method, for the effect of an action the method has already done on the
    /// root.
    /// </summary>
    /// <remarks>
    /// The container counterpart of the control base's <c>Confirm</c>: no readiness check, no log
    /// entry, never a repeat, because the generated wrapper did the rest. A stale read ends the wait
    /// as <see cref="Controls.Base.ConfirmationResult.Replaced"/> at once.
    /// </remarks>
    /// <param name="read">Reads the value.</param>
    /// <param name="done">Whether the value is the one waited for.</param>
    /// <param name="timeoutMs">Maximum time to wait; null for the default.</param>
    /// <returns>How the wait ended, with the last value and error.</returns>
    protected Controls.Base.Confirmation<T> Confirm<T>(Func<T?> read, Func<T?, bool> done, int? timeoutMs)
        => Confirmer.Run(read, done, timeoutMs ?? DefaultTimeoutMs, PollingIntervalMs);

    /// <summary>Polls an arbitrary condition.</summary>
    protected bool RunWait(Func<bool> operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunWait);
        return Call.Run(caller, null, budget, AnimationMs, context =>
            Poll(context, _ => ScopeGate.Check(ProbeCallReadiness()) ?? (operation() ? Observation.Done() : Observation.Pending()))
            || (context.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(context, caller, budget)),
            succeeded: met => met);
    }

    /// <summary>Polls a condition evaluated against the container root.</summary>
    protected bool RunWaitWithElement<T>(T? expected, Func<IMauiElement, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null) return true;

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunWaitWithElement);
        return Call.Run(caller, null, budget, AnimationMs, context =>
            Poll(context, _ => RootAttempt(root => coreOperation(root) ? Observation.Done() : Observation.Pending()))
            || (context.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(context, caller, budget)),
            succeeded: met => met);
    }

    /// <summary>Runs an action once, as one call, after the scope chain is ready.</summary>
    protected TSelf RunDo(Action operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunDo);
        return Call.Run(caller, null, budget, AnimationMs, context =>
        {
            if (!Poll(context, _ => ScopeGate.Check(ProbeCallReadiness()) ?? Observation.Done()))
            {
                throw Failure(context, caller, budget);
            }

            operation();
            return Self;
        });
    }

    /// <summary>Runs an action against the container root, once, as one call.</summary>
    protected TSelf RunDoWithElement(Action<IMauiElement> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunDoWithElement);
        return Call.Run(caller, null, budget, AnimationMs, context =>
        {
            ActOnceOnRoot(context, caller, budget, coreOperation);
            return Self;
        });
    }

    /// <summary>Reads a value from the container root.</summary>
    protected T? RunGetWithElement<T>(Func<IMauiElement, T> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunGetWithElement);
        return Call.Run(caller, null, budget, AnimationMs, context =>
        {
            var value = default(T);
            if (!Poll(context, _ => RootAttempt(root =>
                {
                    value = coreOperation(root);
                    return Observation.Done();
                })))
            {
                throw Failure(context, caller, budget);
            }

            return value;
        });
    }

    /// <summary>
    /// Reads a value that is meaningful when the container root is absent: once, with the root
    /// resolved optionally.
    /// </summary>
    /// <remarks>
    /// Used by generated <c>Get*</c> members whose Core method carries <c>[AbsenceTolerant]</c>.
    /// </remarks>
    protected T? RunGetWithOptionalElement<T>(Func<IMauiElement?, T> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunGetWithOptionalElement);
        return Call.Run(caller, null, budget, AnimationMs, context =>
        {
            if (!Poll(context, _ => ScopeGate.Check(ProbeCallReadiness()) ?? Observation.Done()))
            {
                throw Failure(context, caller, budget);
            }

            return coreOperation(TryGetContainerRoot());
        });
    }

    /// <summary>Sets a value on the container, returning the parent scope.</summary>
    protected TSetResult RunSetWithElement<T>(T? value, Action<IMauiElement> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (value == null)
        {
            return SetResult;
        }

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunSetWithElement);
        return Call.Run(caller, value.ToString(), budget, AnimationMs, context =>
        {
            ActOnceOnRoot(context, caller, budget, coreOperation);
            return SetResult;
        });
    }

    /// <summary>The scope returned by a generated set operation.</summary>
    protected abstract TSetResult SetResult { get; }

    /// <summary>
    /// Polls a predicate that is meaningful when the container root is absent.
    /// </summary>
    /// <remarks>
    /// Used by generated members whose Core method carries <c>[AbsenceTolerant]</c>: the root may
    /// be null, because the predicate may be asking about absence.
    /// </remarks>
    protected bool RunWaitWithOptionalElement<T>(T? expected,
        Func<IMauiElement?, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null) return true;

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunWaitWithOptionalElement);
        return Call.Run(caller, null, budget, AnimationMs, context =>
            Poll(context, _ => ScopeGate.Check(ProbeCallReadiness())
                ?? (coreOperation(TryGetContainerRoot()) ? Observation.Done() : Observation.Pending()))
            || (context.Log.Last.Kind == ObservationKind.Pending ? false : throw Failure(context, caller, budget)),
            succeeded: met => met);
    }

    /// <summary>
    /// Asserts a value that is meaningful when the container root is absent.
    /// </summary>
    /// <remarks>
    /// Resolves the root optionally so a missing container fails the comparison rather than
    /// raising <c>ElementNotFoundException</c>.
    /// </remarks>
    protected TSelf RunAssertWithOptionalElement<T>(T? expected,
        Func<IMauiElement?, T?> getActual, Func<T?, T?, bool> compare,
        string? message = null, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        if (expected == null) return Self;

        return RunAssertCore(expected, () => getActual(TryGetContainerRoot()), compare, message, timeoutMs,
            caller ?? nameof(RunAssertWithOptionalElement));
    }

    /// <summary>Asserts a value, returning the container for chaining.</summary>
    protected TSelf RunAssert<T>(T? expected, Func<T?> getActual,
        Func<T?, T?, bool> compare, string? message = null,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null) return Self;

        return RunAssertCore(expected, getActual, compare, message, timeoutMs, caller ?? nameof(RunAssert));
    }

    /// <summary>Asserts a value read from the container root.</summary>
    protected TSelf RunAssertWithElement<T>(T? expected, Func<IMauiElement, T?> getActual,
        Func<T?, T?, bool> compare, string? message = null,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null) return Self;

        var budget = Budget(timeoutMs);
        caller ??= nameof(RunAssertWithElement);
        return Call.Run(caller, null, budget, AnimationMs, context =>
        {
            if (!Poll(context, _ => RootAttempt(root =>
                {
                    var actual = getActual(root);
                    return compare(actual, expected)
                        ? Observation.Done()
                        : Observation.Mismatch(new AssertionException(message ?? "Assert exception", expected, actual), null);
                })))
            {
                throw Failure(context, caller, budget);
            }

            return Self;
        });
    }

    private TSelf RunAssertCore<T>(T expected, Func<T?> getActual, Func<T?, T?, bool> compare,
        string? message, int? timeoutMs, string caller)
    {
        var budget = Budget(timeoutMs);
        return Call.Run(caller, null, budget, AnimationMs, context =>
        {
            if (!Poll(context, _ =>
                {
                    if (ScopeGate.Check(ProbeCallReadiness()) is { } notReady)
                    {
                        return notReady;
                    }

                    var actual = getActual();
                    return compare(actual, expected)
                        ? Observation.Done()
                        : Observation.Mismatch(new AssertionException(message ?? "Assert exception", expected, actual), null);
                }))
            {
                throw Failure(context, caller, budget);
            }

            return Self;
        });
    }

    /// <summary>
    /// What an action or a set on this scope requires of its root beyond being there: one check,
    /// inside the call's poll, throwing <see cref="ElementNotReadyException"/> when not met.
    /// </summary>
    /// <remarks>Nothing by default; a clickable item adds "enabled".</remarks>
    /// <param name="root">The root, just found.</param>
    protected virtual void EnsureReadyForActionCore(IMauiElement root)
    {
    }

    /// <summary>
    /// Resolves the root, then runs <paramref name="act"/> on it: once, unless it reports that it
    /// did not act.
    /// </summary>
    /// <remarks>
    /// As the control base's <c>ActOnce</c>: a Core method throws <see cref="ElementNotReadyException"/>
    /// only before it acts, so resolving again and asking again within the budget repeats nothing
    /// (R0). Any other exception ends the call at once.
    /// </remarks>
    private void ActOnceOnRoot(AttemptContext context, string caller, int budgetMs, Action<IMauiElement> act)
    {
        while (true)
        {
            var root = ResolveReadyRoot(context, caller, budgetMs);
            try
            {
                act(root);
                return;
            }
            catch (ElementNotReadyException error) when (context.Deadline.RemainingMs > 0)
            {
                context.Log.Add(Observation.NotReady(error, null), context.Deadline.ElapsedMs);
                WaitHelper.Pause(Math.Max(1, Math.Min(PollingIntervalMs, context.Deadline.RemainingMs)));
            }
        }
    }

    /// <summary>Polls until the scope chain is ready and the root there, and returns the root.</summary>
    private IMauiElement ResolveReadyRoot(AttemptContext context, string caller, int budgetMs)
    {
        IMauiElement? ready = null;
        if (!Poll(context, _ => RootAttempt(root =>
            {
                EnsureReadyForActionCore(root);
                ready = root;
                return Observation.Done();
            })))
        {
            throw Failure(context, caller, budgetMs);
        }

        return ready!;
    }

    #endregion

    #region Logging identity

    private string PageName => Page?.GetType().Name ?? "Unknown";
    private string ControlId => Locator.Value;

    #endregion
}

/// <summary>
/// A rooted scope with an explicit parent. Pages inherit <see cref="RootedScopeBase{TSelf, TSetResult}"/>
/// directly because a root page has no parent relationship.
/// </summary>
public abstract class ContainerObjectBase<TParent, TSelf>
    : RootedScopeBase<TSelf, TParent>, IMauiContainerObject<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : ContainerObjectBase<TParent, TSelf>
{
    private readonly IMauiScope<TParent> _parentScope;
    private readonly Locator _locator;

    /// <summary>Creates a container within the given parent scope.</summary>
    protected ContainerObjectBase(IMauiScope<TParent> parentScope, Locator locator)
    {
        _parentScope = parentScope ?? throw new ArgumentNullException(nameof(parentScope));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    /// <summary>Creates a container using the parent scope's default locator strategy.</summary>
    protected ContainerObjectBase(IMauiScope<TParent> parentScope, string locatorValue)
        : this(parentScope,
               new Locator(
                   parentScope?.DefaultLocatorStrategy ?? LocatorStrategy.AutomationId,
                   locatorValue ?? throw new ArgumentNullException(nameof(locatorValue))))
    {
        if (locatorValue.Length == 0)
            throw new ArgumentException("Locator value must not be empty.", nameof(locatorValue));
    }

    protected override Locator Locator => _locator;

    /// <inheritdoc />
    public TParent Parent => _parentScope.Self;

    /// <inheritdoc />
    public override IMauiTestContext Context => _parentScope.Context;

    /// <inheritdoc />
    public override IMauiPage? Page => _parentScope.Page;

    /// <inheritdoc />
    /// <remarks>
    /// Whatever scrolls for the scope this container sits in: a <c>Border</c> inside a
    /// <c>ScrollView</c> is scrolled by the <c>ScrollView</c>.
    /// </remarks>
    public override IMauiElement? ScrollingRoot => _parentScope.ScrollingRoot;

    /// <inheritdoc />
    /// <remarks>A container inside a row is confined as the row is.</remarks>
    public override bool AllowsScrollLookup => _parentScope.AllowsScrollLookup;

    protected override IMauiElement FindContainerRootElement()
        => _parentScope.TryFindElement(Locator) ?? throw RootNotFound();

    /// <inheritdoc />
    protected override ElementNotFoundException RootNotFound()
        => _parentScope.DescribeMiss(Locator) ?? new ElementNotFoundException(Locator);

    /// <summary>
    /// Whether this container inherits its parent's readiness. False for a scope shown over its
    /// parent (a popup, a dialog), which is usable while the page under it is busy or gone.
    /// </summary>
    protected virtual bool AsksParent => true;

    /// <inheritdoc />
    protected override ScopeReadiness ProbeParentReadiness()
        => AsksParent ? _parentScope.ProbeReadiness() : ContentReady();

    protected override TParent SetResult => Parent;

    // Element-object members, so a container can declare a control capability
    // (IRefreshableControlObject, ISwipeableControlObject) the way a view does.

    /// <summary>Reads a named attribute from the container's root, in the platform's own vocabulary, now.</summary>
    /// <remarks>
    /// <paramref name="timeoutMs"/> is not used. It is there because Core's <c>IControlObject</c>
    /// declares it, and a container that declares a control capability implements that interface
    /// (R9: Core is not changed by this work; see <c>move-down.md</c>).
    /// </remarks>
    public string? GetAttribute(string name, int? timeoutMs = null)
        => TryGetContainerRoot()?.GetAttribute(name);
}
