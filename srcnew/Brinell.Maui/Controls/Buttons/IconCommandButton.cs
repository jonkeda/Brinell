namespace Brinell.Maui.Controls.Buttons;

/// <summary>
/// Command button rendered with the shared IconLabelButtonView template.
/// Activates the native button/icon child before falling back to the template root.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class IconCommandButton<TScope> : ClickableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private const string NativeButtonId = "IconLabelButtonView_NativeButton";
    private const string IconButtonId = "IconLabelButtonView_btnIcon";

    /// <summary>
    /// Creates an icon command button within the specified scope.
    /// </summary>
    public IconCommandButton(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates an icon command button within the specified scope using a string locator value.
    /// </summary>
    public IconCommandButton(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    /// <inheritdoc />
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckClickableCore(element, timeoutMs);

        var target = ElementSearch.FindChildByAutomationId(MauiScope, element, NativeButtonId)
            ?? ElementSearch.FindChildByAutomationId(MauiScope, element, IconButtonId)
            ?? element;

        if (!ElementActivator.TryActivate(target))
        {
            throw new InvalidOperationException($"Could not activate icon command button. Locator: {Locator}");
        }
    }

    /// <summary>
    /// Attempts to activate the command button.
    /// </summary>
    public new bool TryClick(int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element == null)
        {
            return false;
        }

        EnsureVisible(element);
        return Run(nameof(TryClick), () =>
        {
            ClickCore(element, timeoutMs);
            return true;
        });
    }
}
