using System.Drawing;
using Brinell.Uia;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Maui.FlaUI.Bridge;

/// <summary>
/// Finds the bridge element that acts for a given <c>AutomationId</c>.
/// </summary>
internal static class BrinellBridgeLookup
{
    /// <summary>Finds the bridge element for an <c>AutomationId</c>, if the app publishes one.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session, for its raw-view walker.</param>
    /// <param name="automationId">The <c>AutomationId</c> of the element being acted on.</param>
    /// <param name="near">
    /// The real element's screen rectangle, read only when several targets share the
    /// <c>AutomationId</c>. Without it the newest published target on screen is used.
    /// </param>
    /// <returns>The bridge element, or null.</returns>
    internal static AutomationElement? Find(
        AutomationElement root,
        UIA3Automation automation,
        string automationId,
        Func<Rectangle>? near = null)
    {
        var walker = automation.TreeWalkerFactory.GetRawViewWalker();

        var bridge = FindBridgeRoot(walker, root);
        if (bridge is null)
        {
            return null;
        }

        var wanted = BrinellUiaIds.TargetAutomationIdFor(automationId);
        var matches = new List<AutomationElement>();

        FirstChildWhere(walker, bridge, child =>
        {
            if (string.Equals(ReadAutomationId(child), wanted, StringComparison.Ordinal))
            {
                matches.Add(child);
            }

            return false;
        });

        if (matches.Count > 0)
        {
            PublishedIds.GetOrCreateValue(root).TryAdd(automationId, 0);
        }

        return matches.Count switch
        {
            0 => null,
            1 => matches[0],
            _ => Nearest(root, matches, near),
        };
    }

    /// <summary>
    /// Among targets sharing an <c>AutomationId</c> with nothing to place them by, the newest
    /// that has bounds on screen, else the newest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The first used to be taken, and the first is the oldest.</b> A page pushed in place of
    /// one with the same ids publishes its targets while the outgoing page's are still there:
    /// they go when that page unloads, a moment later. A verb sent by id in that moment reached
    /// the old page - the Gestures tests saw two <c>GestureTapTarget</c>s and a tap that changed
    /// nothing on screen, in about one run in five, more often the faster the fixture navigates.
    /// </para>
    /// <para>
    /// The old page's element is off screen, so it answers no bounds or empty ones; that settles
    /// it. When neither or both have bounds, the app appends each registration, so the newest is
    /// the last.
    /// </para>
    /// </remarks>
    private static AutomationElement NewestOnScreen(List<AutomationElement> matches)
    {
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            if (TryReadBounds(matches[i], out _, out _, out var width, out var height)
                && width > 0
                && height > 0)
            {
                return matches[i];
            }
        }

        return matches[^1];
    }

    /// <summary>
    /// Picks, among targets sharing an <c>AutomationId</c>, the one over the real element.
    /// </summary>
    private static AutomationElement Nearest(
        AutomationElement root, List<AutomationElement> matches, Func<Rectangle>? near)
    {
        Rectangle wanted;
        try
        {
            wanted = near?.Invoke() ?? Rectangle.Empty;
        }
        catch (Exception)
        {
            wanted = Rectangle.Empty;
        }

        if (wanted.IsEmpty)
        {
            return NewestOnScreen(matches);
        }

        // UI Automation gives this process logical pixels when it is not per-monitor DPI aware;
        // the app answers GetBounds in physical pixels.
        var (centreX, centreY) = ToPhysical(
            root, wanted.Left + (wanted.Width / 2.0), wanted.Top + (wanted.Height / 2.0));

        AutomationElement? best = null;
        var bestDistance = double.MaxValue;

        foreach (var candidate in matches)
        {
            if (!TryReadBounds(candidate, out var left, out var top, out var width, out var height))
            {
                continue;
            }

            var dx = left + (width / 2.0) - centreX;
            var dy = top + (height / 2.0) - centreY;
            var distance = (dx * dx) + (dy * dy);

            // A tie goes to the later, newer target. The outgoing page has the same layout as the
            // one pushed in its place and still answers bounds while it unloads, so the two sit at
            // distance 0 together: a full run logged it for every TimePicker, DatePicker and
            // Expander that had two, and keeping the first set the old page's control, which the
            // app confirmed and the screen never showed.
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best ?? matches[^1];
    }

    /// <summary>
    /// A bridge target's screen rectangle from its <c>GetBounds</c>, in physical pixels: for a
    /// recorded video's touch mark on a control the tree cannot show.
    /// </summary>
    /// <exception cref="InvalidOperationException">The app publishes no such target, or it does not answer.</exception>
    internal static Rectangle TargetBounds(AutomationElement root, UIA3Automation automation, string automationId)
    {
        if (Find(root, automation, automationId) is not { } target
            || !TryReadBounds(target, out var left, out var top, out var width, out var height))
        {
            throw new InvalidOperationException($"'{automationId}' answers no bounds on the bridge.");
        }

        return Rectangle.FromLTRB((int)Math.Round(left), (int)Math.Round(top), (int)Math.Round(left + width), (int)Math.Round(top + height));
    }

    private static (double X, double Y) ToPhysical(AutomationElement root, double x, double y)
    {
        var factor = PhysicalPerCaller(root);
        return (x * factor, y * factor);
    }

    // Physical pixels per pixel of this process: 1 for a per-monitor aware client.
    private static double PhysicalPerCaller(AutomationElement root)
    {
        try
        {
            var hwnd = root.Properties.NativeWindowHandle.ValueOrDefault;
            if (hwnd == IntPtr.Zero)
            {
                return 1;
            }

            var callerDpi = GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()) switch
            {
                Unaware => 96u,
                SystemAware => GetDpiForSystem(),
                _ => 0u,
            };

            var windowDpi = GetDpiForWindow(hwnd);
            if (callerDpi > 0 && windowDpi > 0)
            {
                return (double)windowDpi / callerDpi;
            }
        }
        catch (Exception)
        {
            // Unconverted is still right for a per-monitor aware client.
        }

        return 1;
    }

    private const int Unaware = 0;
    private const int SystemAware = 1;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetThreadDpiAwarenessContext();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    private static bool TryReadBounds(
        AutomationElement target, out double left, out double top, out double width, out double height)
    {
        left = top = width = height = 0;

        if (!target.TryGetBrinellPattern(out var pattern))
        {
            return false;
        }

        string answer;
        try
        {
            if (pattern!.Exchange((int)BrinellVerb.GetBounds, string.Empty, out answer) != HResults.S_OK)
            {
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        var parts = answer.Split(',');
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var number = System.Globalization.NumberStyles.Float;

        return parts.Length == 4
               && double.TryParse(parts[0], number, culture, out left)
               && double.TryParse(parts[1], number, culture, out top)
               && double.TryParse(parts[2], number, culture, out width)
               && double.TryParse(parts[3], number, culture, out height);
    }

    /// <summary>
    /// Every element currently published on the app's bridge.
    /// </summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session, for its raw-view walker.</param>
    /// <returns>The bridge's children, or empty if there is no bridge.</returns>
    internal static IReadOnlyList<AutomationElement> Targets(
        AutomationElement root, UIA3Automation automation)
    {
        var walker = automation.TreeWalkerFactory.GetRawViewWalker();

        var bridge = FindBridgeRoot(walker, root);
        if (bridge is null)
        {
            return [];
        }

        var targets = new List<AutomationElement>();

        FirstChildWhere(walker, bridge, child =>
        {
            targets.Add(child);

            // Never matches, so the walk visits every sibling. Collecting through the same
            // enumeration the single lookup uses is deliberate: one implementation of "walk the
            // bridge's children, tolerating a sibling that vanishes mid-walk", not two.
            return false;
        });

        return targets;
    }

    /// <summary>Whether the app under test publishes a bridge at all.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session, for its raw-view walker.</param>
    /// <returns>Whether a bridge window was found.</returns>
    internal static bool HasBridge(AutomationElement root, UIA3Automation automation)
        => FindBridgeRoot(automation.TreeWalkerFactory.GetRawViewWalker(), root) is not null;

    /// <summary>The ids each window's bridge has carried at some point in this session.</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        AutomationElement, System.Collections.Concurrent.ConcurrentDictionary<string, byte>> PublishedIds = new();

    /// <summary>Whether this window's bridge has carried a target for the id before.</summary>
    /// <remarks>
    /// Tells an id that is between publications - a page arriving, a control re-laid out - from
    /// one the app never instrumented: only the first is worth waiting for.
    /// </remarks>
    internal static bool HasPublished(AutomationElement root, string? automationId)
        => automationId is not null
           && PublishedIds.TryGetValue(root, out var ids)
           && ids.ContainsKey(automationId);

    /// <summary>The bridge root found under each window, until the window element is dropped.</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<AutomationElement, KnownBridge>
        BridgeRoots = new();

    /// <summary>A bridge root and the bridge's own window, which it lives exactly as long as.</summary>
    private sealed record KnownBridge(AutomationElement Root, IntPtr Handle);

    /// <summary>
    /// The fragment root among the window's immediate children, remembered per window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Walking the window's children to find it was three quarters of every bridge call</b>:
    /// 7 ms of 9.4 for a <c>CurrentRoute</c>, whose own exchange is 0.5 ms, and the fixture asks
    /// several per navigation.
    /// </para>
    /// <para>
    /// The app publishes one fragment root per bridge window and swaps only its children, so a
    /// root found once stays right while that window exists. Each use checks the window with
    /// <c>IsWindow</c>, a local call, rather than by reading the element: a read can be answered
    /// from UI Automation's side and pass for a provider that has gone. A bridge attached again
    /// is a new window, so it is found again; a new app window element is a new key.
    /// </para>
    /// </remarks>
    private static AutomationElement? FindBridgeRoot(ITreeWalker walker, AutomationElement window)
    {
        if (BridgeRoots.TryGetValue(window, out var known) && IsWindow(known.Handle))
        {
            return known.Root;
        }

        var found = WalkToBridgeRoot(walker, window);
        var handle = found is null ? IntPtr.Zero : ReadHandle(found);

        if (handle == IntPtr.Zero)
        {
            // Not published yet - a starting app - or a root without a window to check it by:
            // nothing to remember, so the next call walks again.
            BridgeRoots.Remove(window);
        }
        else
        {
            BridgeRoots.AddOrUpdate(window, new KnownBridge(found!, handle));
        }

        return found;
    }

    private static IntPtr ReadHandle(AutomationElement element)
    {
        try
        {
            return element.Properties.NativeWindowHandle.ValueOrDefault;
        }
        catch (Exception)
        {
            return IntPtr.Zero;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hwnd);

    /// <summary>
    /// Finds the fragment root among the window's immediate children.
    /// </summary>
    private static AutomationElement? WalkToBridgeRoot(ITreeWalker walker, AutomationElement window)
        => FirstChildWhere(
            walker,
            window,
            e => string.Equals(
                ReadAutomationId(e), BrinellUiaIds.BridgeAutomationId, StringComparison.Ordinal));

    /// <summary>
    /// The first immediate child matching a predicate.
    /// </summary>
    private static AutomationElement? FirstChildWhere(
        ITreeWalker walker, AutomationElement parent, Func<AutomationElement, bool> matches)
    {
        AutomationElement? child;
        try
        {
            child = walker.GetFirstChild(parent);
        }
        catch (Exception)
        {
            return null;
        }

        while (child is not null)
        {
            if (matches(child))
            {
                return child;
            }

            try
            {
                child = walker.GetNextSibling(child);
            }
            catch (Exception)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Describes the raw tree just below the app window.
    /// </summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="maxDepth">How far below the window to walk.</param>
    /// <returns>One line per element, indented by depth.</returns>
    internal static string Describe(
        AutomationElement root, UIA3Automation automation, int maxDepth = 3)
    {
        var walker = automation.TreeWalkerFactory.GetRawViewWalker();
        var report = new System.Text.StringBuilder();

        report.AppendLine(Line(root));
        Walk(walker, root, 1);

        return report.ToString();

        void Walk(ITreeWalker w, AutomationElement element, int depth)
        {
            if (depth > maxDepth)
            {
                return;
            }

            AutomationElement? child;
            try
            {
                child = w.GetFirstChild(element);
            }
            catch (Exception)
            {
                return;
            }

            while (child is not null)
            {
                report.AppendLine(new string(' ', depth * 2) + Line(child));

                // Only descend where a bridge could be. A real app's XAML tree is thousands of
                // elements and every node costs a cross-process call.
                if (Interesting(child))
                {
                    Walk(w, child, depth + 1);
                }

                try
                {
                    child = w.GetNextSibling(child);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        static bool Interesting(AutomationElement element)
        {
            var className = ReadClassName(element);

            return className.Contains("Brinell", StringComparison.Ordinal)
                   || className.Contains("Window", StringComparison.Ordinal)
                   || className.Contains("Island", StringComparison.Ordinal);
        }

        static string Line(AutomationElement element)
        {
            var automationId = ReadAutomationId(element);

            var mine = automationId == BrinellUiaIds.BridgeAutomationId
                       || automationId.StartsWith(
                           BrinellUiaIds.TargetAutomationIdPrefix, StringComparison.Ordinal);

            return $"{(mine ? "*** " : string.Empty)}class='{ReadClassName(element)}' "
                   + $"id='{automationId}' name='{Read(() => element.Properties.Name.ValueOrDefault)}'";
        }
    }

    private static string ReadClassName(AutomationElement element)
        => Read(() => element.Properties.ClassName.ValueOrDefault);

    private static string Read(Func<string?> read)
    {
        try
        {
            return read() ?? string.Empty;
        }
        catch (Exception)
        {
            return "(unreadable)";
        }
    }

    private static string ReadAutomationId(AutomationElement element)
    {
        try
        {
            return element.Properties.AutomationId.ValueOrDefault ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
