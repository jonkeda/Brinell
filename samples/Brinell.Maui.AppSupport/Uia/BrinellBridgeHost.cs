using Brinell.Uia;
using Brinell.Uia.Provider;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// One bridge per top-level window, created on demand and torn down with the window.
/// </summary>
/// <remarks>
/// <para>
/// Keyed by window handle rather than by MAUI's window object, so an app with several windows
/// gets one bridge each, and an element always reaches the bridge belonging to the window it is
/// actually in.
/// </para>
/// <para>
/// <b>Nothing here starts a bridge on its own.</b> The first element that declares a verb
/// creates one; an app with no declarations never registers a pattern, never creates a window,
/// and is indistinguishable from an app without the bridge compiled in. That is what makes
/// leaving <c>UseBrinellUiaBridge</c> in a build a smaller decision than it sounds - though
/// step 27 removes the code from a shipping build regardless, because absence is the only real
/// control UI Automation offers.
/// </para>
/// </remarks>
public static class BrinellBridgeHost
{
    private static readonly Lock Gate = new();
    private static readonly Dictionary<IntPtr, BrinellUiaBridge> Bridges = [];

    /// <summary>Windows whose teardown is already subscribed, so it happens once.</summary>
    private static readonly HashSet<IntPtr> Watched = [];

    /// <summary>Whether any bridge exists in this process. Diagnostics and tests.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static bool IsActive
    {
        get
        {
            lock (Gate)
            {
                return Bridges.Count > 0;
            }
        }
    }

    /// <summary>
    /// Publishes an element to its window's bridge, creating the bridge if it is the first.
    /// </summary>
    /// <param name="element">The element, which must be realised and have an AutomationId.</param>
    /// <param name="verbs">What the element answers.</param>
    /// <param name="sink">An optional sink that gets first refusal.</param>
    internal static void Publish(
        VisualElement element,
        IReadOnlyCollection<BrinellVerb> verbs,
        IBrinellGestureSink? sink)
    {
        // The platform check the analyzer needs, and the one a reader needs too: everything
        // below this line is UI Automation, which exists only on Windows. Off Windows the
        // declaration is inert and gestures are real touch input.
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var hwnd = WindowHandles.RootOf(element);
        if (hwnd == IntPtr.Zero)
        {
            BridgeDiagnostics.Report(
                $"'{element.AutomationId}' declares verbs but its window could not be found, "
                + "so it was not published. The element is not realised on this platform.");
            return;
        }

        try
        {
            BridgeDiagnostics.Report(
                $"publishing '{element.AutomationId}' ({string.Join(",", verbs)}) on window 0x{hwnd:X}");

            var bridge = GetOrCreate(hwnd);
            BridgeDiagnostics.Report("bridge ready");

            bridge.Register(new MauiVerbTarget(element, element.AutomationId, verbs, sink));
            BridgeDiagnostics.Report($"registered '{element.AutomationId}'");

            WatchForClose(element, hwnd);
            BridgeDiagnostics.Report("teardown wired");
        }
        catch (Exception ex)
        {
            // Every exception, not only BrinellUiaException. This runs inside the app's Loaded
            // handler, so anything that escapes takes the app under test down with it - and an
            // app that will not start is a far worse outcome than an app without instrumentation.
            // Test-only code must never be able to break the thing it is meant to observe.
            BridgeDiagnostics.Report($"could not publish '{element.AutomationId}': {ex}");
        }
    }

    /// <summary>Removes an element from its window's bridge.</summary>
    /// <param name="element">The element being unloaded.</param>
    internal static void Withdraw(VisualElement element)
    {
        var automationId = element.AutomationId;
        if (string.IsNullOrWhiteSpace(automationId))
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Deliberately not resolved through the element: by Unloaded its handler may already be
        // gone, so the window handle is no longer reachable from it. Sweeping every bridge is
        // cheap - there is one per window - and correct in the case that matters.
        lock (Gate)
        {
            foreach (var bridge in Bridges.Values)
            {
                // Only if this element is still the one registered. MAUI loads the incoming page
                // before unloading the outgoing one, so by the time this runs the same
                // AutomationId may already belong to the new page's element - and removing it
                // would leave the bridge empty for every visit after the first.
                var removed = bridge.Unregister(
                    automationId, target => target is MauiVerbTarget mine && mine.Owns(element));

                BridgeDiagnostics.Report($"withdraw '{automationId}' -> {removed}");
            }
        }
    }

    /// <summary>
    /// Disposes every bridge in this process.
    /// </summary>
    /// <remarks>
    /// For an app that wants to shut instrumentation down explicitly, and for tests of the
    /// bridge's own lifetime. Ordinary teardown happens when each window closes.
    /// </remarks>
    public static void Shutdown()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        lock (Gate)
        {
            foreach (var bridge in Bridges.Values)
            {
                bridge.Dispose();
            }

            Bridges.Clear();
            Watched.Clear();
        }
    }

    /// <summary>
    /// Arranges for the window's bridge to be disposed when the window goes away.
    /// </summary>
    /// <remarks>
    /// Subscribed once per window, tracked by the same handle the bridge is keyed on.
    /// <c>Destroying</c> rather than the platform window's <c>Closed</c>, because by the time
    /// the native window is closing the element that led us here may have no handler left and
    /// there would be no way back to the MAUI window at all.
    /// </remarks>
    private static void WatchForClose(VisualElement element, IntPtr hwnd)
    {
        lock (Gate)
        {
            if (!Watched.Add(hwnd))
            {
                return;
            }
        }

        if (element.Window is not { } window)
        {
            // No MAUI window to hang the teardown off. The bridge still dies with the process,
            // and Shutdown remains available; what is lost is only the per-window cleanup.
            lock (Gate)
            {
                Watched.Remove(hwnd);
            }

            return;
        }

        window.Destroying += OnWindowDestroying;
        return;

        void OnWindowDestroying(object? sender, EventArgs e)
        {
            window.Destroying -= OnWindowDestroying;

            lock (Gate)
            {
                Watched.Remove(hwnd);
            }

            Close(hwnd);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static BrinellUiaBridge GetOrCreate(IntPtr hwnd)
    {
        lock (Gate)
        {
            if (Bridges.TryGetValue(hwnd, out var existing))
            {
                return existing;
            }

            var bridge = BrinellUiaBridge.Attach(hwnd);
            Bridges[hwnd] = bridge;
            return bridge;
        }
    }

    /// <summary>
    /// Tears down the bridge for a window that has closed.
    /// </summary>
    /// <remarks>
    /// Called from the platform window's <c>Closed</c> event. Skipping it leaks a provider, and
    /// a leaked provider is not a quiet leak: UI Automation caches provider pointers across
    /// processes and a client holding a stale one blocks until its transaction times out.
    /// </remarks>
    internal static void Close(IntPtr hwnd)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        lock (Gate)
        {
            if (Bridges.Remove(hwnd, out var bridge))
            {
                bridge.Dispose();
            }
        }
    }
}
