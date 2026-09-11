using System.Runtime.InteropServices;
using Brinell.Uia.Interop;

namespace Brinell.Uia.Provider;

/// <summary>
/// The window that carries the Brinell UI Automation provider, and the set of targets beneath
/// it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a window at all.</b> A custom UI Automation pattern can only be answered by a native
/// <c>IRawElementProviderSimple</c>, and the only supported way to get one into the tree is to
/// return it from <c>WM_GETOBJECT</c>. XAML's <c>AutomationPeer.GetPatternCore</c> takes a
/// closed enum and cannot express a custom pattern at all, so there is no route through the
/// UI framework - the provider has to hang off an HWND of our own. This one is one pixel,
/// disabled, transparent to input, and draws nothing.
/// </para>
/// <para>
/// <b>It does not touch the host's automation tree.</b> No handler is replaced and no
/// automation peer is overridden. That is the whole point of the design: overriding the peer
/// of a WinUI control was measured to collapse the entire tree for the app under test, leaving
/// every element unaddressable while the app kept rendering normally.
/// </para>
/// <para>
/// <b>Threading.</b> Create and dispose on the thread that owns the parent window. The window
/// procedure runs on that thread's message loop, so a bridge created on a thread with no pump
/// would never answer <c>WM_GETOBJECT</c> and would look, from the client, exactly like an app
/// with no instrumentation.
/// </para>
/// </remarks>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class BrinellUiaBridge : IDisposable
{
    private static readonly Lock ClassGate = new();
    private static bool _classRegistered;

    /// <summary>
    /// Held for the life of the process, and for the same reason the class registration is.
    /// </summary>
    /// <remarks>
    /// The window class stores a raw function pointer to this delegate. A collected delegate
    /// leaves the class pointing at freed memory, and the failure lands on whichever window
    /// receives the next message - not necessarily ours.
    /// </remarks>
    private static Win32.WndProc? _classWndProc;

    private readonly BridgeFragmentRoot _root;
    private readonly Lock _gate = new();
    private readonly uint _ownerThreadId;

    private readonly Dictionary<string, BridgeTargetProvider> _targets =
        new(StringComparer.Ordinal);

    private IntPtr _hwnd;
    private int _nextRuntimeId = 1;
    private bool _disposed;

    private BrinellUiaBridge(IntPtr hwnd, BridgeFragmentRoot root, uint ownerThreadId)
    {
        _hwnd = hwnd;
        _root = root;
        _ownerThreadId = ownerThreadId;
    }

    /// <summary>The bridge window. Diagnostics only.</summary>
    public IntPtr Handle => _hwnd;

    /// <summary>
    /// Creates a bridge as a child of the given window.
    /// </summary>
    /// <remarks>
    /// Call on the thread that owns <paramref name="parentHwnd"/>.
    /// </remarks>
    /// <param name="parentHwnd">The app window to attach to.</param>
    /// <returns>The bridge, which the caller owns and must dispose.</returns>
    /// <exception cref="BrinellUiaException">The window class or the window could not be created.</exception>
    public static BrinellUiaBridge Attach(IntPtr parentHwnd)
    {
        if (parentHwnd == IntPtr.Zero || !Win32.IsWindow(parentHwnd))
        {
            throw new BrinellUiaException(
                $"Cannot attach a Brinell bridge to 0x{parentHwnd:X}: not a window.");
        }

        // Registering the pattern here rather than lazily means a misconfigured contract fails
        // at startup, where it is one clear error, instead of on the first verb in the middle
        // of a test run.
        _ = BrinellPatternRegistration.Current;

        EnsureWindowClass();

        var hwnd = Win32.CreateWindowEx(
            exStyle: Win32.WS_EX_NOACTIVATE | Win32.WS_EX_TRANSPARENT,
            className: BrinellUiaIds.BridgeClassName,
            windowName: BrinellUiaIds.BridgeClassName,

            // Visible, because an invisible window is excluded from the automation tree and
            // the whole point is to be found. Disabled and transparent, because being found is
            // all it is for - it must never take input, focus or a hit test.
            style: Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_DISABLED,
            x: 0,
            y: 0,
            width: 1,
            height: 1,
            parent: parentHwnd,
            menu: IntPtr.Zero,
            instance: Win32.GetModuleHandle(null),
            param: IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
        {
            throw new BrinellUiaException(
                "Could not create the Brinell bridge window "
                + $"(Win32 error {Marshal.GetLastWin32Error()}).");
        }

        var root = new BridgeFragmentRoot(hwnd);
        var bridge = new BrinellUiaBridge(hwnd, root, Win32.GetCurrentThreadId());

        Registry.Add(hwnd, bridge);
        return bridge;
    }

    /// <summary>
    /// Publishes a target, replacing any previous one with the same <c>AutomationId</c>.
    /// </summary>
    /// <remarks>
    /// Replacement rather than rejection, because the common cause of a duplicate is the same
    /// element being loaded a second time after navigation. Failing that case would mean a
    /// bridge that works until the user goes back to a page.
    /// </remarks>
    /// <param name="target">The target to publish.</param>
    public void Register(IBrinellVerbTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            if (_targets.TryGetValue(target.AutomationId, out var existing))
            {
                Disconnect(existing);
            }

            _targets[target.AutomationId] = new BridgeTargetProvider(_root, target, _nextRuntimeId++);
            PublishChildren();
        }
    }

    /// <summary>Removes a target, if it is published.</summary>
    /// <param name="automationId">The <c>AutomationId</c> the target was published under.</param>
    /// <returns>Whether anything was removed.</returns>
    public bool Unregister(string automationId) => Unregister(automationId, _ => true);

    /// <summary>
    /// Removes a target only if the one currently published still satisfies
    /// <paramref name="stillTheSameTarget"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists because of navigation order.</b> A UI framework generally loads the
    /// incoming page before it unloads the outgoing one, so an element that registers on load
    /// and unregisters on unload produces: new element registers, old element unregisters - and
    /// the second call removes the first one's registration, because they share an
    /// <c>AutomationId</c>. The result is a bridge that works on the first visit to a page and
    /// is empty on every visit after, which reads as an intermittent fault rather than an
    /// ordering one.
    /// </para>
    /// <para>
    /// So an unregister has to prove it is removing its own registration, not its successor's.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The <c>AutomationId</c> the target was published under.</param>
    /// <param name="stillTheSameTarget">
    /// Given the currently published target, whether it is the one the caller means to remove.
    /// </param>
    /// <returns>Whether anything was removed.</returns>
    public bool Unregister(string automationId, Func<IBrinellVerbTarget, bool> stillTheSameTarget)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(stillTheSameTarget);

        if (_disposed)
        {
            return false;
        }

        lock (_gate)
        {
            if (!_targets.TryGetValue(automationId, out var published)
                || !stillTheSameTarget(published.Target))
            {
                return false;
            }

            _targets.Remove(automationId);
            Disconnect(published);
            PublishChildren();
            return true;
        }
    }

    /// <summary>The <c>AutomationId</c>s currently published.</summary>
    public IReadOnlyCollection<string> RegisteredTargets
    {
        get
        {
            lock (_gate)
            {
                return _targets.Keys.ToArray();
            }
        }
    }

    /// <summary>
    /// Disconnects every provider and destroys the window.
    /// </summary>
    /// <remarks>
    /// Order matters and the disconnect is not optional. UI Automation caches provider pointers
    /// across the process boundary, and a client left holding one blocks until its transaction
    /// times out rather than failing - so a leaked provider stalls every accessibility client
    /// on the desktop, not only the test that caused it.
    /// </remarks>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var provider in _targets.Values)
            {
                Disconnect(provider);
            }

            _targets.Clear();
            _root.SetChildren([]);
            Disconnect(_root);

            if (_hwnd != IntPtr.Zero)
            {
                Registry.Remove(_hwnd);

                // Destroying a window from a thread that does not own it is a no-op that
                // reports success, which would leave the window and its provider alive with
                // nothing referencing them.
                if (Win32.GetCurrentThreadId() == _ownerThreadId)
                {
                    Win32.DestroyWindow(_hwnd);
                }

                _hwnd = IntPtr.Zero;
            }
        }
    }

    private static void Disconnect(IRawElementProviderSimple provider)
    {
        try
        {
            UiaNativeMethods.UiaDisconnectProvider(provider);
        }
        catch (COMException)
        {
            // Already gone, or no client ever connected. Either way there is nothing to clean.
        }
    }

    /// <summary>Rebuilds the root's child snapshot and tells any listening client.</summary>
    /// <remarks>Called under <c>_gate</c>.</remarks>
    private void PublishChildren()
    {
        _root.SetChildren([.. _targets.Values]);

        if (!UiaNativeMethods.UiaClientsAreListening())
        {
            return;
        }

        try
        {
            UiaNativeMethods.UiaRaiseStructureChangedEvent(
                _root, StructureChangeType.ChildrenInvalidated, null, 0);
        }
        catch (COMException)
        {
            // An event nobody receives is not a reason to fail a registration.
        }
    }

    private static void EnsureWindowClass()
    {
        lock (ClassGate)
        {
            if (_classRegistered)
            {
                return;
            }

            _classWndProc = WindowProcedure;

            var windowClass = new Win32.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<Win32.WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_classWndProc),
                hInstance = Win32.GetModuleHandle(null),
                lpszClassName = BrinellUiaIds.BridgeClassName,
            };

            if (Win32.RegisterClassEx(ref windowClass) != 0)
            {
                _classRegistered = true;
                return;
            }

            var error = Marshal.GetLastWin32Error();

            // Already registered by an earlier load of these sources in this process - which is
            // expected, since the app under test and any library carrying the bridge compile
            // the same file.
            if (error == Win32.ERROR_CLASS_ALREADY_EXISTS)
            {
                _classRegistered = true;
                return;
            }

            _classWndProc = null;
            throw new BrinellUiaException(
                $"Could not register the Brinell bridge window class (Win32 error {error}).");
        }
    }

    private static IntPtr WindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == Win32.WM_GETOBJECT
            && (int)lParam == Win32.UiaRootObjectId
            && Registry.TryGet(hwnd, out var bridge))
        {
            return UiaNativeMethods.UiaReturnRawElementProvider(hwnd, wParam, lParam, bridge!._root);
        }

        return Win32.DefWindowProc(hwnd, message, wParam, lParam);
    }

    /// <summary>
    /// Maps a window back to its bridge.
    /// </summary>
    /// <remarks>
    /// The window procedure is a static function pointer shared by the whole class, so it needs
    /// a way from the HWND to the instance. A dictionary rather than <c>GWLP_USERDATA</c>
    /// because these sources may be compiled into more than one assembly in a process, and two
    /// copies writing the same window word would be a very quiet kind of corruption.
    /// </remarks>
    private static class Registry
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<IntPtr, BrinellUiaBridge> Bridges = [];

        internal static void Add(IntPtr hwnd, BrinellUiaBridge bridge)
        {
            lock (Gate)
            {
                Bridges[hwnd] = bridge;
            }
        }

        internal static void Remove(IntPtr hwnd)
        {
            lock (Gate)
            {
                Bridges.Remove(hwnd);
            }
        }

        internal static bool TryGet(IntPtr hwnd, out BrinellUiaBridge? bridge)
        {
            lock (Gate)
            {
                return Bridges.TryGetValue(hwnd, out bridge);
            }
        }
    }
}
