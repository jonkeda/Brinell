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
    public string InstanceKey => $"declared:{_automationId}";

    /// <inheritdoc />
    public string? ReadState(string property)
        => DeclaresStateReads ? _driver.ReadState(_automationId, property) : null;

    private bool DeclaresStateReads
    {
        get
        {
            try
            {
                return _target.SupportedVerbs().Contains(BrinellVerb.GetState);
            }
            catch (Exception) when (!_driver.AppHasExited)
            {
                // The page republished its bridge since the lookup: nothing to ask.
                return false;
            }
            catch (Exception error)
            {
                throw new AppUnavailableException("the application process has exited.", error);
            }
        }
    }

    /// <inheritdoc />
    public void PerformGesture(MauiGesture gesture)
        => _driver.PerformGesture(_automationId, gesture);

    #endregion

    #region What it cannot answer

    private RouteUnavailableException NotInTheTree(string member)
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
    public void ScrollIntoView(int timeoutMs) => throw NotInTheTree("position");

    /// <inheritdoc />
    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
        => throw NotInTheTree("bounds; use PerformGesture");

    /// <inheritdoc />
    public string? GetAttribute(string name) => throw NotInTheTree("attributes");

    /// <inheritdoc />
    public IMauiElement? TryFindElement(Locator locator) => throw NotInTheTree("descendants");

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
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
