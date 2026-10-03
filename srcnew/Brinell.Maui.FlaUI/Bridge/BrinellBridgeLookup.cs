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
    /// <c>AutomationId</c>. Without it the first published target is used.
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

        return matches.Count switch
        {
            0 => null,
            1 => matches[0],
            _ => Nearest(root, matches, near),
        };
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
            return matches[0];
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

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best ?? matches[0];
    }

    private static (double X, double Y) ToPhysical(AutomationElement root, double x, double y)
    {
        try
        {
            var hwnd = root.Properties.NativeWindowHandle.ValueOrDefault;
            if (hwnd == IntPtr.Zero)
            {
                return (x, y);
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
                var factor = (double)windowDpi / callerDpi;
                return (x * factor, y * factor);
            }
        }
        catch (Exception)
        {
            // Unconverted is still right for a per-monitor aware client.
        }

        return (x, y);
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

    /// <summary>
    /// Finds the fragment root among the window's immediate children.
    /// </summary>
    private static AutomationElement? FindBridgeRoot(ITreeWalker walker, AutomationElement window)
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
