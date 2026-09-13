using System.Drawing;
using Brinell.Core.Diagnostics;
using Brinell.Core.Utilities;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;

namespace Brinell.Maui.FlaUI.Windowing;

/// <summary>
/// The real mouse, and bringing the app to the front so the real keyboard reaches it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The one place this stack touches the desktop's input devices.</b> Every member records itself
/// with <see cref="PhysicalInput"/> first, so under a quiet run it throws before anything moves -
/// and <c>grep "Mouse\."</c> across <c>Brinell.Maui.FlaUI</c> finds this file and nothing else.
/// </para>
/// <para>
/// Step 103 moved these out of <c>FlaUIMauiDriver</c> and <c>FlaUIMauiElement</c>. They stay
/// because AD-008's second admission test - "is the physical path the thing under test?" -
/// sometimes answers yes, and those tests run under <c>PhysicalInputFact</c>.
/// </para>
/// </remarks>
internal sealed class PhysicalPointer
{
    private readonly AppWindow _window;

    internal PhysicalPointer(AppWindow window) => _window = window;

    /// <summary>
    /// Brings the app's window to the front, before input that goes wherever the foreground is.
    /// </summary>
    /// <remarks>
    /// Real input is positional and focus-relative, and FlaUI does not do this for you:
    /// <c>AutomationElement.Click()</c> is clickable-point plus mouse, with no activation, and
    /// <c>Keyboard.Type</c> reaches whichever window holds focus.
    /// </remarks>
    /// <param name="site">Who is asking, for the physical-input record.</param>
    /// <param name="replacement">What should be used instead, for the refusal message.</param>
    internal void BringAppToFront(string site, string replacement)
    {
        // Outside any try: a refusal that gets swallowed is not a refusal.
        PhysicalInput.Used(site, replacement);

        var root = _window.Element;
        try
        {
            if (root.Patterns.Window.IsSupported)
            {
                var windowPattern = root.Patterns.Window.Pattern;
                if (windowPattern.WindowVisualState.Value == WindowVisualState.Minimized)
                {
                    windowPattern.SetWindowVisualState(WindowVisualState.Normal);
                }
            }
        }
        catch
        {
            // Ignore window visual state failures and continue with focus.
        }

        try
        {
            root.SetForeground();
        }
        catch
        {
            // SetForeground can fail if the window is not top-level; fall back to Focus.
            try
            {
                root.Focus();
            }
            catch
            {
                // Ignore focus failures; interaction will proceed regardless.
            }
        }
    }

    internal void Click(AutomationElement element)
    {
        BringAppToFront("PhysicalPointer.Click", "the Invoke pattern, or the bridge's Tap verb (step 18)");
        element.Click();
    }

    internal void DoubleClick(AutomationElement element)
    {
        BringAppToFront("PhysicalPointer.DoubleClick", "the DoubleTap gesture verb (step 18)");
        element.DoubleClick();
    }

    /// <remarks>
    /// Still physical, and still right for what it is. A right-click means "show me the context
    /// menu", and no verb can stand in for a test about the menu appearing. Reaching a menu
    /// <i>item</i> goes through <c>IMauiDriver.InvokeMenuItem</c>.
    /// </remarks>
    internal void RightClick(AutomationElement element)
    {
        BringAppToFront(
            "PhysicalPointer.RightClick",
            "IMauiDriver.InvokeMenuItem, where the aim is the item rather than the menu");
        element.RightClick();
    }

    internal void Hover(AutomationElement element)
    {
        BringAppToFront("PhysicalPointer.Hover", "a pointer-enter verb (not yet planned)");
        Mouse.MoveTo(CenterOf(element));
    }

    internal void LongPress(AutomationElement element, int durationMs)
    {
        BringAppToFront("PhysicalPointer.LongPress", "the LongPress gesture verb (step 18)");
        Mouse.Position = CenterOf(element);
        Mouse.Down(MouseButton.Left);
        try
        {
            WaitHelper.Pause(durationMs);
        }
        finally
        {
            Mouse.Up(MouseButton.Left);
        }
    }

    /// <remarks>
    /// Steps the pointer by hand rather than calling <c>Mouse.Drag</c>, which assigns
    /// <c>Mouse.Position</c> twice and so teleports. WinUI only recognises a drag when it receives
    /// the moves in between, and <c>Mouse.Drag</c> has nowhere to put a duration either.
    /// </remarks>
    internal void Drag(Point start, Point end, int durationMs)
    {
        BringAppToFront("PhysicalPointer.Drag", "a Swipe gesture verb (step 18)");
        Mouse.MoveTo(start);
        Mouse.Down(MouseButton.Left);
        try
        {
            var steps = Math.Max(10, durationMs / 50);
            var dx = (end.X - start.X) / (double)steps;
            var dy = (end.Y - start.Y) / (double)steps;
            var stepDelay = durationMs / steps;

            for (var i = 1; i <= steps; i++)
            {
                Mouse.MoveTo(new Point((int)(start.X + dx * i), (int)(start.Y + dy * i)));
                WaitHelper.Pause(stepDelay);
            }
        }
        finally
        {
            Mouse.Up(MouseButton.Left);
        }
    }

    private static Point CenterOf(AutomationElement element)
    {
        var rect = element.BoundingRectangle;
        return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    }
}
