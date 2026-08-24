namespace Brinell.Maui.Extensions.Controls.Buttons;

/// <summary>
/// Command button rendered with the shared RoundButtonView template.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class RoundButton<TScope> : ClickableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private const string NativeButtonId = "RoundButtonView_NativeButton";
    private const string LegacyClickableContainerId = "RoundButtonView_btn1";

    /// <summary>
    /// Creates a round button within the specified scope.
    /// </summary>
    public RoundButton(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a round button within the specified scope using a string locator value.
    /// </summary>
    public RoundButton(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    /// <inheritdoc />
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);

        var target = ElementSearch.FindChildByAutomationId(MauiScope, element, NativeButtonId)
            ?? ElementSearch.FindChildByAutomationId(MauiScope, element, LegacyClickableContainerId)
            ?? element;

        if (!ElementClicker.TryClick(target))
        {
            throw new InvalidOperationException($"Could not activate round button. Locator: {Locator}");
        }
    }
}
