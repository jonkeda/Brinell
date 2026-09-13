using Brinell.Maui.Containers;
using Brinell.Maui.Controls.Buttons;

namespace Brinell.Maui.Extensions.Controls.Buttons;

/// <summary>
/// Command button rendered with the shared RoundButtonView template.
/// </summary>
/// <remarks>
/// <para>
/// <b>A container with named parts, not a button that searches itself</b> (step 101a). The
/// template is a root hosting the native button that carries the command, so that is what this
/// models: <see cref="NativeButton"/> is a control of its own, found under this root.
/// </para>
/// <para>
/// It used to be a clickable control whose click looked for the native button, then the legacy
/// container, then invoked its own root - a three-rung ladder - and whose child lookup fell back
/// to searching the whole page for anything whose centre lay inside its bounds.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class RoundButton<TScope> : ContainerObjectBase<TScope, RoundButton<TScope>>
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

    /// <summary>The native button the template wraps, which carries the command.</summary>
    public Button<RoundButton<TScope>> NativeButton => Button(NativeButtonId);

    /// <summary>The clickable container an older version of the template used instead.</summary>
    public Button<RoundButton<TScope>> LegacyButton => Button(LegacyClickableContainerId);

    /// <summary>
    /// Presses the button, through whichever part this version of the template has.
    /// </summary>
    /// <remarks>
    /// One question - does the native button exist - then one route. Where neither part exists
    /// the legacy button's click throws, naming its locator, rather than invoking the template
    /// root, which carries no command.
    /// </remarks>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope, for chaining.</returns>
    public TScope Click(int? timeoutMs = null)
    {
        var part = NativeButton.IsExists() ? NativeButton : LegacyButton;
        part.Click(timeoutMs);
        return Parent;
    }
}
