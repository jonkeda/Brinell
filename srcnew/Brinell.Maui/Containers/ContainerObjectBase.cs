using System.Runtime.CompilerServices;
using Brinell.Core.Utilities;

namespace Brinell.Maui.Containers;

/// <summary>
/// Base class for container objects: scopes rooted at an element rather than at the
/// driver, holding controls and other containers.
/// </summary>
/// <remarks>
/// <para>
/// A container object is a peer of <see cref="Pages.PageObjectBase{TSelf}"/>, not a
/// control. It deliberately does <b>not</b> derive from a control base, because a
/// control base would make the container's own members return the parent scope and eject
/// the caller from the container mid-chain.
/// </para>
/// <para>
/// Searches are scoped strictly to <see cref="ContainerRoot"/>: when a child is not
/// found within the container, the search does <b>not</b> fall back to the parent scope.
/// Container scoping means elements must be within the container.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The container type itself (self-referencing for fluent returns).</typeparam>
public abstract class ContainerObjectBase<TParent, TSelf>
    : ObjectBase, IMauiContainerObject<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : ContainerObjectBase<TParent, TSelf>
{
    private readonly IMauiScope<TParent> _parentScope;
    private IMauiElement? _cachedRoot;
    private bool _rootCacheValid;

    /// <summary>
    /// Creates a container within the given parent scope.
    /// </summary>
    /// <param name="parentScope">The parent scope (page or container).</param>
    /// <param name="locator">The locator for the container's root element.</param>
    protected ContainerObjectBase(IMauiScope<TParent> parentScope, Locator locator)
    {
        _parentScope = parentScope ?? throw new ArgumentNullException(nameof(parentScope));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    /// <summary>
    /// Creates a container using the parent scope's default locator strategy.
    /// </summary>
    /// <param name="parentScope">The parent scope (page or container).</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID).</param>
    protected ContainerObjectBase(IMauiScope<TParent> parentScope, string locatorValue)
        : this(parentScope,
               new Locator(
                   parentScope?.DefaultLocatorStrategy ?? LocatorStrategy.AutomationId,
                   locatorValue ?? throw new ArgumentNullException(nameof(locatorValue))))
    {
        if (locatorValue.Length == 0)
            throw new ArgumentException("Locator value must not be empty.", nameof(locatorValue));
    }

    /// <summary>
    /// The locator that finds this container's root element.
    /// </summary>
    protected Locator Locator { get; }

    /// <inheritdoc />
    public TSelf Self => (TSelf)this;

    /// <inheritdoc />
    public TParent Parent => _parentScope.Self;

    /// <inheritdoc />
    public override IMauiTestContext Context => _parentScope.Context;

    /// <inheritdoc />
    public IPageObject? Page => _parentScope.Page;

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;

    #region Container root

    /// <inheritdoc />
    public IMauiElement ContainerRoot
    {
        get
        {
            if (_rootCacheValid && _cachedRoot != null)
            {
                try
                {
                    // Touch a property to detect a dead reference. Adapters differ in how
                    // they report one - a typed stale exception, a raw automation error -
                    // so any failure invalidates the cache.
                    _ = _cachedRoot.TagName;
                    return _cachedRoot;
                }
                catch
                {
                    _rootCacheValid = false;
                    _cachedRoot = null;
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
    protected virtual IMauiElement FindContainerRootElement()
        => _parentScope.FindElement(Locator);

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

    #region Element finding (scoped to the container root)

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

        return TryFindElement(locator)
            ?? throw new ElementNotFoundException(
                $"Element not found within container. Container locator: {Locator}, Child locator: {locator}");
    }

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

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
    public bool IsReady(int? timeoutMs = null)
    {
        if (!_parentScope.IsReady(timeoutMs)) return false;
        if (TryGetContainerRoot() == null) return false;

        return WaitContentReadyCore(timeoutMs);
    }

    /// <inheritdoc />
    public bool WaitReady(int? timeoutMs = null)
    {
        if (!_parentScope.WaitReady(timeoutMs)) return false;
        if (!WaitExists(true, timeoutMs)) return false;

        return WaitContentReady(timeoutMs);
    }

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

    /// <summary>Polls an arbitrary condition.</summary>
    protected bool RunWait(Func<bool> operation, int? timeoutMs = null,
        [CallerMemberName] string? caller = null)
        => RunPoll(null, operation, timeoutMs, caller);

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
        RunPoll(null, () => { operation(); return true; }, timeoutMs, caller);
        return Self;
    }

    /// <summary>Runs an action against the container root, returning the container.</summary>
    protected TSelf RunDoWithElement(Action<IMauiElement> coreOperation,
        int? timeoutMs = null, [CallerMemberName] string? caller = null)
    {
        RunPoll(null, () => { coreOperation(ContainerRoot); return true; }, timeoutMs, caller);
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

    #endregion

    #region Logging identity

    private string TestName => "Test";
    private string PageName => Page?.GetType().Name ?? "Unknown";
    private string ControlId => Locator.Value;
    private ITestLogger? Logger => Context.Logger;

    #endregion
}
