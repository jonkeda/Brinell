using Brinell.Uia;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Maui.FlaUI.Bridge;

/// <summary>
/// Finds the bridge element that acts for a given <c>AutomationId</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The bridge element is not under the element it acts for.</b> It hangs off a sidecar
/// window belonging to the app's top-level window, deliberately, so that publishing it cannot
/// disturb the app's own automation tree. So the lookup starts at the window, not at the
/// element.
/// </para>
/// <para>
/// <b>And it must be an explicit raw walk.</b> Bridge elements set
/// <c>IsControlElement=false</c> so assistive technology never sees them, and
/// <c>FindFirstDescendant</c> was measured not to reach elements in that state - see
/// <c>OrdinarySearch_ReachesTheBridgeOrDoesNot</c> in <c>Brinell.Uia.Tests</c>. Using the
/// ordinary search here would report every app as having no bridge.
/// </para>
/// </remarks>
internal static class BrinellBridgeLookup
{
    /// <summary>Finds the bridge element for an <c>AutomationId</c>, if the app publishes one.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session, for its raw-view walker.</param>
    /// <param name="automationId">The <c>AutomationId</c> of the element being acted on.</param>
    /// <returns>The bridge element, or null.</returns>
    internal static AutomationElement? Find(
        AutomationElement root, UIA3Automation automation, string automationId)
    {
        var walker = automation.TreeWalkerFactory.GetRawViewWalker();

        var bridge = FindBridgeRoot(walker, root);
        if (bridge is null)
        {
            return null;
        }

        var wanted = BrinellUiaIds.TargetAutomationIdFor(automationId);

        return FirstChildWhere(
            walker, bridge, e => string.Equals(ReadAutomationId(e), wanted, StringComparison.Ordinal));
    }

    /// <summary>
    /// Every element currently published on the app's bridge.
    /// </summary>
    /// <remarks>
    /// For the verbs that are about the app rather than about one control - going back is the
    /// first of them - where the caller has no <c>AutomationId</c> to look up because the thing
    /// being asked is not a control at all. The caller picks by capability instead, which costs
    /// one round trip per target; there are as many targets as the app has declarations, which
    /// is a handful.
    /// </remarks>
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
    /// <remarks>
    /// <para>
    /// <b>One level, and only one.</b> The bridge parents its window to the app's top-level
    /// window, so the fragment root is always a direct child. An earlier version searched three
    /// levels deep to be tolerant, and that was a mistake twice over: it walked the app's entire
    /// XAML subtree - thousands of cross-process calls - before reaching a sibling that was
    /// there all along, and a transient failure anywhere in that subtree aborted the search. The
    /// result was a bridge that was found on one call and missing on the next.
    /// </para>
    /// <para>
    /// Matched on <c>AutomationId</c> rather than class name. The bridge's own window reports
    /// the class name to UI Automation whether or not the provider ever answered
    /// <c>WM_GETOBJECT</c>, so a class-name match cannot tell a working bridge from an empty
    /// window that looks like one.
    /// </para>
    /// </remarks>
    private static AutomationElement? FindBridgeRoot(ITreeWalker walker, AutomationElement window)
        => FirstChildWhere(
            walker,
            window,
            e => string.Equals(
                ReadAutomationId(e), BrinellUiaIds.BridgeAutomationId, StringComparison.Ordinal));

    /// <summary>
    /// The first immediate child matching a predicate.
    /// </summary>
    /// <remarks>
    /// A failure reading one sibling skips that sibling rather than ending the walk. Elements
    /// come and go while a walk is in progress - an app under test is a live application - and
    /// abandoning the search because one neighbour vanished is how a lookup becomes
    /// intermittent.
    /// </remarks>
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
    /// <remarks>
    /// For telling apart the three ways a gesture goes missing, which are indistinguishable from
    /// the test: no bridge window (the app was built without the automation sources), a bridge
    /// window with no fragment root (<c>WM_GETOBJECT</c> is not being answered), or a fragment
    /// root with no children (nothing declared verbs). Each wants a different fix.
    /// </remarks>
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
