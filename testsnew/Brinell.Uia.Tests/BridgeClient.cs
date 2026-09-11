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
