using Brinell.Core.Interfaces;
using System.Drawing;
using Brinell.Maui.FlaUI.Bridge;
using Brinell.Maui.Interfaces;
using Brinell.Uia;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// An element the app declared by <c>AutomationId</c> on the Brinell bridge, standing in for a
/// control the UI Automation tree does not show.
/// </summary>
/// <remarks>
/// <para>
/// <b>What <see cref="IMauiElement.TryFindDeclared"/> returns on Windows.</b> A MAUI <c>Stepper</c>
/// has no tree node of its own - only its two buttons - and a <c>SwipeView</c> publishes no
/// <c>AutomationId</c>, yet the bridge addresses both by id. This element answers what the bridge
/// can: state reads and gestures.
/// </para>
/// <para>
/// <b>Everything else throws.</b> It has no bounds, no visibility and no text, and inventing
/// defaults for them would let it pass a visibility check or read as empty. Asking it one of those
/// questions means a control took the wrong element.
/// </para>
/// </remarks>
internal sealed class FlaUIDeclaredElement : IMauiElement
{
    private readonly string _automationId;
    private readonly AutomationElement _target;
    private readonly FlaUIMauiDriver _driver;

    /// <summary>Creates the stand-in for a bridge target.</summary>
    /// <param name="automationId">The <c>AutomationId</c> the app declared.</param>
    /// <param name="target">The bridge element acting for it, found once by the caller.</param>
    /// <param name="driver">The driver that owns the bridge session.</param>
    internal FlaUIDeclaredElement(string automationId, AutomationElement target, FlaUIMauiDriver driver)
    {
        _automationId = automationId;
        _target = target;
        _driver = driver;
    }

    #region What the bridge answers

    /// <inheritdoc />
    public string? AutomationId => _automationId;

    /// <inheritdoc />
    /// <remarks>Read from the target found at lookup, so asking costs no second walk of the bridge.</remarks>
    public bool SupportsStateReads
    {
        get
        {
            try
            {
                return _target.SupportedVerbs().Contains(BrinellVerb.GetState);
            }
            catch
            {
                // The page republished its bridge since the lookup: nothing to ask.
                return false;
            }
        }
    }

    /// <inheritdoc />
    public string ReadState(string property) => _driver.ReadState(_automationId, property);

    /// <inheritdoc />
    public bool SupportsGesture(MauiGesture gesture)
        => GestureRunner.Supports(_driver.RootElement, _driver.Automation, _automationId, gesture);

    /// <inheritdoc />
    public void PerformGesture(MauiGesture gesture)
        => GestureRunner.Perform(_driver.RootElement, _driver.Automation, _automationId, gesture);

    #endregion

    #region What it cannot answer

    private NotSupportedException NotInTheTree(string member)
        => new(
            $"'{_automationId}' is reached through the app's bridge declaration, not through the UI "
            + $"Automation tree, so it has no {member}. It answers state reads and gestures only.");

    /// <inheritdoc />
    public bool Visible => throw NotInTheTree(nameof(Visible));

    /// <inheritdoc />
    public bool Enabled => throw NotInTheTree(nameof(Enabled));

    /// <inheritdoc />
    public bool Selected => throw NotInTheTree(nameof(Selected));

    /// <inheritdoc />
    public string? Text => throw NotInTheTree(nameof(Text));

    /// <inheritdoc />
    public string? TagName => throw NotInTheTree(nameof(TagName));

    /// <inheritdoc />
    public Point Location => throw NotInTheTree(nameof(Location));

    /// <inheritdoc />
    public Size Size => throw NotInTheTree(nameof(Size));

    /// <inheritdoc />
    public Rectangle Rect => throw NotInTheTree(nameof(Rect));

    /// <inheritdoc />
    public string? Name => throw NotInTheTree(nameof(Name));

    /// <inheritdoc />
    public bool Focused => throw NotInTheTree(nameof(Focused));

    /// <inheritdoc />
    public string? Hint => throw NotInTheTree(nameof(Hint));

    /// <inheritdoc />
    public void Click() => throw NotInTheTree("click route; use PerformGesture");

    /// <inheritdoc />
    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys)
        => throw NotInTheTree("text input");

    /// <inheritdoc />
    public void Clear() => throw NotInTheTree("text input");

    /// <inheritdoc />
    public void DoubleClick() => throw NotInTheTree("pointer route; use PerformGesture");

    /// <inheritdoc />
    public void RightClick() => throw NotInTheTree("pointer route");

    /// <inheritdoc />
    public void Hover() => throw NotInTheTree("pointer route");

    /// <inheritdoc />
    public void LongPress(int durationMs = 1000) => throw NotInTheTree("pointer route; use PerformGesture");

    /// <inheritdoc />
    public void ScrollIntoView(int timeoutMs = 5000) => throw NotInTheTree("position");

    /// <inheritdoc />
    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
        => throw NotInTheTree("bounds; use PerformGesture");

    /// <inheritdoc />
    public string? GetAttribute(string name) => throw NotInTheTree("attributes");

    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator, int timeoutMs = 5000) => throw NotInTheTree("descendants");

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator, int timeoutMs = 0)
        => throw NotInTheTree("descendants");

    /// <inheritdoc />
    public bool TryFindElement(Locator locator, out IMauiElement? element, int timeoutMs = 0)
        => throw NotInTheTree("descendants");

    /// <inheritdoc />
    public string? GetDomAttribute(string attributeName) => throw NotInTheTree("DOM");

    /// <inheritdoc />
    public string? GetDomProperty(string propertyName) => throw NotInTheTree("DOM");

    /// <inheritdoc />
    public string? GetCssValue(string propertyName) => throw NotInTheTree("DOM");

    /// <inheritdoc />
    public void Submit() => throw NotInTheTree("form");

    #endregion
}
