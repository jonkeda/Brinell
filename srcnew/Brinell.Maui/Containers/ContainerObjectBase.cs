using System.Runtime.CompilerServices;
using Brinell.Core.Utilities;

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
/// Searches are scoped strictly to <see cref="ContainerRoot"/>: when a child is not
/// found within the container, the search does <b>not</b> fall back to the parent scope.
/// Container scoping means elements must be within the container.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The container type itself (self-referencing for fluent returns).</typeparam>
/// <typeparam name="TSetResult">The scope returned by generated set operations.</typeparam>
public abstract class RootedScopeBase<TSelf, TSetResult>
    : ObjectBase, IMauiScope<TSelf>, IContainerObject<IMauiElement>
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
    public abstract IPageObject? Page { get; }

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;

    /// <summary>
    /// Whether this container keeps its resolved root between operations.
    /// </summary>
    protected virtual bool CacheContainerRoot => true;

    /// <summary>Checks whether a cached root still represents this scope.</summary>
    protected virtual bool IsCachedRootValid(IMauiElement root)
    {
        _ = root.TagName;
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
                catch
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
    /// The container's answer to "the element for this object", which for a container is its
    /// root. Named to match <c>ViewBase.TryFindElement()</c> so a generated member reads the
    /// same on both bases — the generator emits one call shape and each base decides what its
    /// own element is.
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

    #endregion

    #region Typed children (scoped to the container root)

    /// <summary>
    /// A child control of this container, as a control object rather than a raw element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The container is already a scope, so a child resolves within it and nothing else needs to
    /// know where the container sits. This is the typed counterpart to <see cref="FindElement"/>:
    /// that one hands back an <c>IMauiElement</c>, which is where a test stops chaining and starts
    /// doing automation by hand.
    /// </para>
    /// <para>
    /// Prefer declaring a named property on the container - <c>public Label&lt;ProductRow&gt; Name
    /// =&gt; new(this, "ProductNameLabel");</c> - which reads better and gives the child a name.
    /// This exists for children not worth naming, and for tests that would otherwise reach for a
    /// locator.
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

    /// <summary>
    /// Whether this scope may resolve children now.
    /// </summary>
    protected virtual bool CanResolveElements(bool wait = false) => true;

    /// <summary>
    /// Creates the error raised when child resolution is blocked by scope readiness.
    /// </summary>
    protected virtual ElementNotFoundException CreateScopeNotReadyException(Locator locator)
        => new($"Container is not ready. Container locator: {Locator}, Child locator: {locator}");

    /// <inheritdoc />
    /// <inheritdoc />
    /// <remarks>
    /// The same lookup as <see cref="TryFindElement"/>: a container scopes its search to its own
    /// root, and scrolling that root is the container's own concern — <c>ScrollHelper</c> — not
    /// something an element lookup should trigger as a side effect. Overridden by a container
    /// that can scroll itself to reach a child.
    /// </remarks>
    public virtual IMauiElement? TryFindElementAfterScroll(Locator locator) => TryFindElement(locator);

    public IMauiElement? TryFindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        if (!CanResolveElements()) return null;

        var root = TryGetContainerRoot();
        if (root == null) return null;

        try
        {
            return root.FindElement(locator, timeoutMs: 0);
        }
        catch (ElementNotFoundException)
        {
            // Not found within the container. Do NOT fall back to the parent scope -
            // container scoping means elements must be within the container.
            return null;
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();

            root = TryGetContainerRoot();
            if (root == null) return null;

            try
            {
                return root.FindElement(locator, timeoutMs: 0);
            }
            catch (ElementNotFoundException)
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        if (!CanResolveElements()) throw CreateScopeNotReadyException(locator);

        return TryFindElement(locator)
            ?? throw new ElementNotFoundException(
                $"Element not found within container. Container locator: {Locator}, Child locator: {locator}");
    }

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        if (!CanResolveElements()) return [];

        var root = TryGetContainerRoot();
        if (root == null) return [];

        try
        {
            return root.FindElements(locator, timeoutMs: 0);
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();

            root = TryGetContainerRoot();
            return root == null ? [] : root.FindElements(locator, timeoutMs: 0);
        }
    }

    #endregion

    #region Readiness

    /// <inheritdoc />
    public virtual bool IsReady(int? timeoutMs = null)
    {
        if (!IsParentReady(timeoutMs)) return false;
        if (TryGetContainerRoot() == null) return false;

        return WaitContentReadyCore(timeoutMs);
    }

    /// <inheritdoc />
    public virtual bool WaitReady(int? timeoutMs = null)
    {
        if (!WaitParentReady(timeoutMs)) return false;
        if (!WaitExists(true, timeoutMs)) return false;

        return WaitContentReady(timeoutMs);
    }

    /// <summary>Checks readiness outside this rooted scope.</summary>
    protected virtual bool IsParentReady(int? timeoutMs = null) => true;

    /// <summary>Waits for readiness outside this rooted scope.</summary>
    protected virtual bool WaitParentReady(int? timeoutMs = null) => true;

    /// <summary>
    /// Waits for readiness beyond the root element merely existing.
    /// </summary>
    public bool WaitContentReady(int? timeoutMs = null)
        => WaitContentReadyCore(timeoutMs);

    /// <summary>
    /// Extra readiness beyond "the root element exists". Override for a container whose
    /// content loads asynchronously, waiting on concrete UI state - a spinner clearing,
    /// a count becoming non-zero - never a fixed sleep.
    /// </summary>
    protected virtual bool WaitContentReadyCore(int? timeoutMs = null) => true;

    #endregion

    #region State

    /// <summary>
    /// Whether the container's root element exists.
    /// </summary>
    public bool IsExists(int? timeoutMs = null)
    {
        if (timeoutMs is > 0)
            return Poll(() => TryGetContainerRoot() != null, timeoutMs);

        return TryGetContainerRoot() != null;
    }

    /// <summary>
    /// Whether the container's root element is visible; null when it does not exist.
    /// </summary>
    public bool? IsVisible(int? timeoutMs = null)
    {
        var root = TryGetContainerRoot();
        return root?.Visible;
    }

    /// <summary>
    /// Waits until the container's existence matches <paramref name="expected"/>.
    /// A null expectation is a skip and returns true.
    /// </summary>
    public bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return Poll(() => IsExists() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <summary>
    /// Waits until the container's visibility matches <paramref name="expected"/>.
    /// A null expectation is a skip and returns true.
    /// </summary>
    public bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return Poll(() => IsVisible() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <summary>
    /// Asserts the container's existence, returning the container so a chain stays inside it.
    /// </summary>
    public TSelf AssertExists(bool? expected = true, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return Self;

        if (!WaitExists(expected, timeoutMs))
        {
            throw new AssertionException(
                message ?? $"Expected container {(expected.Value ? "to exist" : "not to exist")}. Locator: {Locator}");
        }

        return Self;
    }

    /// <summary>
    /// Asserts the container's visibility, returning the container so a chain stays inside it.
    /// </summary>
    public TSelf AssertVisible(bool? expected = true, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return Self;

        if (!WaitVisible(expected, timeoutMs))
        {
            var actual = IsVisible();
            throw new AssertionException(
                message ?? $"Expected container visibility '{expected}' but got '{actual}'. Locator: {Locator}");
        }

        return Self;
    }

    #endregion

    #region Run helpers

    // Mirrors the helper surface on the control base, but returning TSelf instead of the
    // parent scope. Identical names and shapes let the generator emit one body for both
    // hierarchies. The element these operate on is the container root.

    /// <summary>
    /// Polls a condition, logging entry and exit, and rethrowing the last transient
    /// failure when the condition never held.
    /// </summary>
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
                $"Page '{Page.Name}' did not become ready for {caller ?? "operation"} on container '{Locator}' within {timeout} ms. " +
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

    /// <summary>Polls an arbitrary condition.</summary>
    protected bool RunWait(Func<bool> operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        return RunPoll(null, operation, timeoutMs, caller);
    }

    /// <summary>Polls a condition evaluated against the container root.</summary>
    protected bool RunWaitWithElement<T>(T? expected, Func<IMauiElement, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null) return true;

        return RunPoll(null, () => coreOperation(ContainerRoot), timeoutMs, caller);
    }

    /// <summary>Runs an action, returning the container for chaining.</summary>
    protected TSelf RunDo(Action operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        if (Page != null && !Page.WaitReady(timeout))
        {
            var snapshot = Page.ProbeReadiness();
            throw new PageLoadException(
                $"Page '{Page.Name}' did not become ready for {caller ?? "operation"} on container '{Locator}' within {timeout} ms. " +
                $"Last readiness state: {snapshot.State}; busy value: '{snapshot.BusySignalValue ?? "(none)"}'.");
        }
        operation();
        return Self;
    }

    /// <summary>Runs an action against the container root, returning the container.</summary>
    protected TSelf RunDoWithElement(Action<IMauiElement> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var root = ResolveReadyRoot(timeoutMs, caller);
        coreOperation(root);
        return Self;
    }

    /// <summary>Reads a value from the container root.</summary>
    protected T? RunGetWithElement<T>(Func<IMauiElement, T> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        var value = default(T);
        RunPoll(null, () => { value = coreOperation(ContainerRoot); return true; }, timeoutMs, caller);
        return value;
    }

    /// <summary>Sets a value on the container, returning the parent scope.</summary>
    protected TSetResult RunSetWithElement<T>(T? value, Action<IMauiElement> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (value == null)
        {
            return SetResult;
        }
        var root = ResolveReadyRoot(timeoutMs, caller);
        coreOperation(root);
        return SetResult;
    }

    /// <summary>The scope returned by a generated set operation.</summary>
    protected abstract TSetResult SetResult { get; }

    /// <summary>
    /// Polls a predicate that is meaningful when the container root is absent.
    /// </summary>
    /// <remarks>
    /// The counterpart of <see cref="RunDoWithElement"/> for <c>[AbsenceTolerant]</c> Core
    /// methods: the root is resolved with <see cref="TryGetContainerRoot"/> and may be null,
    /// because the predicate may be asking about absence. Mirrors
    /// <c>ViewBase.RunWaitWithOptionalElement</c> — generated members use whichever helper
    /// their base class provides, so both bases must offer the pair.
    /// </remarks>
    protected bool RunWaitWithOptionalElement<T>(T? expected,
        Func<IMauiElement?, bool> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null) return true;

        return RunPoll(null, () => coreOperation(TryGetContainerRoot()), timeoutMs, caller);
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

        RunPoll(null, () =>
        {
            var actual = getActual(TryGetContainerRoot());
            if (!compare(actual, expected))
            {
                throw new AssertionException(message ?? "Assert exception", expected, actual);
            }
            return true;
        }, timeoutMs, caller);

        return Self;
    }

    /// <summary>Asserts a value, returning the container for chaining.</summary>
    protected TSelf RunAssert<T>(T? expected, Func<T?> getActual,
        Func<T?, T?, bool> compare, string? message = null,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null) return Self;

        RunPoll(null, () =>
        {
            var actual = getActual();
            if (!compare(actual, expected))
            {
                throw new AssertionException(message ?? "Assert exception", expected, actual);
            }
            return true;
        }, timeoutMs, caller);

        return Self;
    }

    /// <summary>Asserts a value read from the container root.</summary>
    protected TSelf RunAssertWithElement<T>(T? expected, Func<IMauiElement, T?> getActual,
        Func<T?, T?, bool> compare, string? message = null,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        if (expected == null) return Self;

        RunPoll(null, () =>
        {
            var actual = getActual(ContainerRoot);
            if (!compare(actual, expected))
            {
                throw new AssertionException(message ?? "Assert exception", expected, actual);
            }
            return true;
        }, timeoutMs, caller);

        return Self;
    }

    private IMauiElement ResolveReadyRoot(int? timeoutMs, string? caller)
    {
        IMauiElement? ready = null;
        RunPoll(null, () =>
        {
            ready = ContainerRoot;
            return true;
        }, timeoutMs, caller);

        return ready ?? ContainerRoot;
    }

    #endregion

    #region Logging identity

    private string TestName => "Test";
    private string PageName => Page?.GetType().Name ?? "Unknown";
    private string ControlId => Locator.Value;
    private ITestLogger? Logger => Context.Logger;

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
    public override IPageObject? Page => _parentScope.Page;

    protected override IMauiElement FindContainerRootElement()
        => _parentScope.FindElement(Locator);

    protected override bool IsParentReady(int? timeoutMs = null)
        => _parentScope.IsReady(timeoutMs);

    protected override bool WaitParentReady(int? timeoutMs = null)
        => _parentScope.WaitReady(timeoutMs);

    protected override TParent SetResult => Parent;
}
