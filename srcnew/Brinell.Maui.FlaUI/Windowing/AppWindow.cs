namespace Brinell.Maui.FlaUI.Windowing;

/// <summary>
/// The app's top-level window, as UI Automation hands it out - and handed out again when it goes stale.
/// </summary>
/// <remarks>
/// <para>
/// <b>The window handle is the identity; the element is a cache.</b> Two of eight full runs, and a
/// stress probe within forty page changes, ended with every later test failing in milliseconds:
/// bridge silent, window "unreadable". Captured in the act, the app was idle and responding, its
/// window handle unchanged - and the element the driver had held since launch answered
/// <c>UIA_E_ELEMENTNOTAVAILABLE</c>, while a fresh attach to the same handle worked at once. UI
/// Automation had retired the element; the driver never asked again, so one invalidation blinded
/// it for the rest of the run. See <c>.my/fix/rca-app-freeze-was-a-stale-root.md</c>.
/// </para>
/// <para>
/// Step 104 moved this out of <c>FlaUIMauiDriver</c>, where it sat among placement and foreground
/// code it has nothing to do with.
/// </para>
/// </remarks>
internal sealed class AppWindow
{
    private const int ElementNotAvailable = unchecked((int)0x80040201);

    private readonly UIA3Automation _automation;
    private readonly Lock _gate = new();
    private AutomationElement _element;
    private int _reattachments;

    internal AppWindow(UIA3Automation automation, AutomationElement element)
    {
        _automation = automation;
        _element = element;
        Handle = element.Properties.NativeWindowHandle.ValueOrDefault;
    }

    private AppWindow(UIA3Automation automation, AutomationElement element, IntPtr handle)
    {
        _automation = automation;
        _element = element;
        Handle = handle;
    }

    /// <summary>Attaches to a window by handle, waiting for UI Automation to resolve it.</summary>
    internal static AppWindow FromHandle(UIA3Automation automation, IntPtr windowHandle)
        => new(automation, Attach(automation, windowHandle), windowHandle);

    /// <summary>The native handle. Zero when UI Automation reported none.</summary>
    internal IntPtr Handle { get; }

    /// <summary>The window's element, re-attached by handle if UI Automation has retired it.</summary>
    /// <remarks>
    /// The check is one property read, cheap next to the tree searches every caller goes on to do.
    /// The re-attach is taken under a lock because the verb runners and the test thread can reach
    /// it together.
    /// </remarks>
    internal AutomationElement Element
    {
        get
        {
            var element = _element;

            if (IsStillAvailable(element) || Handle == IntPtr.Zero)
            {
                return element;
            }

            lock (_gate)
            {
                if (!ReferenceEquals(element, _element) && IsStillAvailable(_element))
                {
                    return _element;
                }

                _element = Attach(_automation, Handle);
                Interlocked.Increment(ref _reattachments);
                return _element;
            }
        }
    }

    /// <summary>How many times the element had gone stale and was attached again.</summary>
    internal int Reattachments => Volatile.Read(ref _reattachments);

    private static bool IsStillAvailable(AutomationElement element)
    {
        try
        {
            _ = element.Properties.ProcessId.Value;
            return true;
        }
        catch (System.Runtime.InteropServices.COMException stale)
            when (stale.HResult == ElementNotAvailable)
        {
            return false;
        }
    }

    /// <summary>
    /// Attaches to a window, waiting for UI Automation to be willing to resolve it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A window that exists is not always a window UI Automation will hand you.</b>
    /// <c>FromHandle</c> throws <c>Win32Exception: "Unexpected HRESULT has been returned from a
    /// call to a COM component"</c> for a handle it cannot resolve yet - not a null, so there is
    /// nothing to test for and nothing that reads as "not ready".
    /// </para>
    /// <para>
    /// <b>It fails in a constructor, which is what makes it worth handling here.</b> A fixture
    /// that cannot build takes its whole collection down at once, and the report is a COM error
    /// with no mention of a window - it reads as the automation stack being broken rather than a
    /// window being a few milliseconds young.
    /// </para>
    /// </remarks>
    private static AutomationElement Attach(UIA3Automation automation, IntPtr windowHandle)
    {
        const int timeoutMs = 10_000;
        const int pollMs = 20;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Exception? last = null;

        while (clock.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                return automation.FromHandle(windowHandle);
            }
            catch (Exception attaching)
            {
                last = attaching;
                Thread.Sleep(pollMs);
            }
        }

        throw new InvalidOperationException(
            $"UI Automation would not resolve window 0x{windowHandle:X} within {timeoutMs} ms. "
            + "The window exists but the automation stack will not hand it over, which is not "
            + "the same as the app being absent.",
            last);
    }
}
