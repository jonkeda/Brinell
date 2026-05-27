namespace Brinell.Maui.Controls.Buttons;

/// <summary>
/// MAUI Button control with click capability and fluent method chaining.
/// Inherits from ClickableControlBase which provides all click functionality.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Button<TScope> : ClickableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new button control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the button element.</param>
    public Button(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new button control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Button(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    /// <inheritdoc />
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckClickableCore(element, timeoutMs);

        var target = FindActivationTarget(element);

        if (!ElementActivator.TryActivate(target))
        {
            throw new InvalidOperationException($"Could not activate button. Locator: {Locator}");
        }
    }

    /// <summary>
    /// Activates the button through keyboard input after focusing it.
    /// Useful for MAUI/WinUI button surfaces where UIA Invoke reports success
    /// without dispatching the app command.
    /// </summary>
    public TScope Press(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Press), timeoutMs, element =>
        {
            PressCore(element, timeoutMs);
        });
    }

    /// <summary>
    /// Attempts to activate the button through keyboard input if it exists.
    /// </summary>
    public bool TryPress(int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element == null)
        {
            return false;
        }

        EnsureVisible(element);
        return Run(nameof(TryPress), () =>
        {
            PressCore(element, timeoutMs);
            return true;
        });
    }

    private void PressCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckClickableCore(element, timeoutMs);
        FindActivationTarget(element).SendKeys(Keys.Space);
    }

    private IMauiElement FindActivationTarget(IMauiElement element)
        => ElementSearch.IsControlType(element, "Button")
            ? element
            : ElementSearch.FindChildByControlType(MauiScope, element, "Button")
              ?? element;
}
