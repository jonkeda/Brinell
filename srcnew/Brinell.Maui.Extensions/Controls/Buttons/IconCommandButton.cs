using Brinell.Maui.Containers;
using Brinell.Maui.Controls.Buttons;

namespace Brinell.Maui.Extensions.Controls.Buttons;

/// <summary>
/// Command button rendered with the shared IconLabelButtonView template.
/// </summary>
/// <remarks>
/// A container with named parts - the native button and the icon button the template hosts -
/// rather than a clickable control that searched itself for them (step 101a). See
/// <see cref="RoundButton{TScope}"/>.
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class IconCommandButton<TScope> : ContainerObjectBase<TScope, IconCommandButton<TScope>>
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

    /// <summary>The native button the template wraps, which carries the command.</summary>
    public Button<IconCommandButton<TScope>> NativeButton => Button(NativeButtonId);

    /// <summary>The icon button, for a version of the template without a native button.</summary>
    public Button<IconCommandButton<TScope>> IconButton => Button(IconButtonId);

    /// <summary>
    /// Presses the button, through whichever part this version of the template has.
    /// </summary>
    /// <remarks>
    /// One question - does the native button exist - then one route. The template root is never
    /// invoked: it is not clickable, which is why the parts exist.
    /// </remarks>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope, for chaining.</returns>
    public TScope Click(int? timeoutMs = null)
    {
        var part = NativeButton.IsExists() ? NativeButton : IconButton;
        part.Click(timeoutMs);
        return Parent;
    }
}
