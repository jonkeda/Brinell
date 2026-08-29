namespace Brinell.Maui.Extensions.Controls.Buttons;

/// <summary>
/// Command button rendered with the shared RoundButtonView template.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class RoundButton<TScope> : Brinell.Maui.Controls.Base.ClickableControlBase<TScope>
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
    /// <remarks>
    /// The template wraps a native button, which is what carries the command; the template
    /// root only hosts it. Resolving the child here — rather than in a shared helper — is what
    /// keeps this control responsible for its own view.
    /// </remarks>
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);

        var target = FindChildCore(element, NativeButtonId)
            ?? FindChildCore(element, LegacyClickableContainerId)
            ?? element;

        if (!TryActivateByPattern(target))
        {
            target.Click();
        }
    }
}
