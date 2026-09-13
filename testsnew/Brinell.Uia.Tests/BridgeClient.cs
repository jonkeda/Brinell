using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Uia.Tests;

/// <summary>Fetches the Brinell pattern off a FlaUI element, and walks trees by view.</summary>
/// <remarks>
/// The FlaUI-facing half of the client, kept in one place so the spikes and the eventual
/// <c>Brinell.Maui.FlaUI</c> extension agree about how it is done. All it needs from FlaUI is
/// the native element, which <c>UIA3FrameworkAutomationElement</c> exposes publicly - no
/// reflection and no fork.
/// </remarks>
internal static class BridgeClient
{
    /// <summary>Gets the pattern from an element, if it carries one.</summary>
    /// <param name="element">The element to ask.</param>
    /// <param name="pattern">The pattern, when this returns true.</param>
    /// <returns>Whether the element carries the Brinell pattern.</returns>
    internal static bool TryGetPattern(
        AutomationElement element, out IBrinellAutomationPattern? pattern)
    {
        var framework = (UIA3FrameworkAutomationElement)element.FrameworkAutomationElement;
        var native = framework.NativeElement;

        return BrinellUiaClient.TryGetPattern(native.GetCurrentPattern, out pattern);
    }

    /// <summary>
    /// Walks a tree looking for a class name, using the given view.
    /// </summary>
    /// <remarks>
    /// A hand-written walk rather than <c>FindFirstDescendant</c> because the question these
    /// spikes ask is precisely which view an element appears in, and a search that picks its
    /// own view cannot answer it.
    /// </remarks>
    /// <param name="walker">The view to walk.</param>
    /// <param name="root">Where to start.</param>
    /// <param name="className">The class name to match.</param>
    /// <param name="maxDepth">How deep to go before giving up.</param>
    /// <returns>The first match, or null.</returns>
    internal static AutomationElement? FindByClassName(
        ITreeWalker walker, AutomationElement root, string className, int maxDepth = 8)
    {
        if (maxDepth < 0)
        {
            return null;
        }

        var child = walker.GetFirstChild(root);

        while (child is not null)
        {
            if (string.Equals(SafeClassName(child), className, StringComparison.Ordinal))
            {
                return child;
            }

            var match = FindByClassName(walker, child, className, maxDepth - 1);
            if (match is not null)
            {
                return match;
            }

            child = walker.GetNextSibling(child);
        }

        return null;
    }

    /// <summary>
    /// Finds a direct child by <c>AutomationId</c>, using the given view.
    /// </summary>
    /// <remarks>
    /// Children only, not descendants: the bridge is deliberately one level deep, so a
    /// recursive search here would only be able to find something that should not exist.
    /// </remarks>
    /// <param name="walker">The view to walk.</param>
    /// <param name="parent">The element whose children to search.</param>
    /// <param name="automationId">The id to match, already in bridge form.</param>
    /// <returns>The match, or null.</returns>
    internal static AutomationElement? FindByAutomationId(
        ITreeWalker walker, AutomationElement parent, string automationId)
    {
        var child = walker.GetFirstChild(parent);

        while (child is not null)
        {
            if (string.Equals(
                    child.Properties.AutomationId.ValueOrDefault,
                    automationId,
                    StringComparison.Ordinal))
            {
                return child;
            }

            child = walker.GetNextSibling(child);
        }

        return null;
    }

    /// <summary>
    /// Attaches to a window that has only just been created, waiting for it to become one UI
    /// Automation will resolve.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A freshly created window is not immediately addressable, and the failure is not a
    /// null.</b> <c>UIA3Automation.FromHandle</c> throws
    /// <c>Win32Exception: "Unexpected HRESULT has been returned from a call to a COM
    /// component"</c> for a handle it cannot resolve yet, so there is nothing to test for and
    /// nothing that reads as "not ready".
    /// </para>
    /// <para>
    /// <b>Latent until step 28 put three hosts in a run.</b> With one host per run it never
    /// showed; with three it failed roughly one run in three, and it failed in the constructor,
    /// so it took a whole class down at once and looked like the bridge being broken rather than
    /// a window being young.
    /// </para>
    /// </remarks>
    /// <param name="automation">The client session.</param>
    /// <param name="hwnd">The window to attach to.</param>
    /// <param name="timeoutMs">How long to keep trying.</param>
    /// <returns>The window as an automation element.</returns>
    internal static AutomationElement AttachToWindow(
        UIA3Automation automation, IntPtr hwnd, int timeoutMs = 10_000)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        Exception? last = null;

        while (elapsed.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                return automation.FromHandle(hwnd);
            }
            catch (Exception ex)
            {
                last = ex;
                Thread.Sleep(20);
            }
        }

        throw new InvalidOperationException(
            $"UI Automation would not resolve window 0x{hwnd:X} within {timeoutMs} ms.", last);
    }

    /// <summary>Reads a class name without letting a vanished element fail the walk.</summary>
    internal static string SafeClassName(AutomationElement element)
    {
        try
        {
            return element.Properties.ClassName.ValueOrDefault ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
