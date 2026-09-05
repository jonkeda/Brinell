using Brinell.Maui.Containers;
using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Text;

namespace Brinell.Maui.Controls.Dialogs;

/// <summary>
/// MAUI ContentDialog control for WinUI3 popups produced by DisplayAlert and
/// DisplayPromptAsync.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public class ContentDialog<TParent> : ContainerObjectBase<TParent, ContentDialog<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates a ContentDialog control in the current scope.
    /// </summary>
    /// <param name="parentScope">The parent scope that owns the dialog interaction.</param>
    public ContentDialog(IMauiScope<TParent> parentScope)
        : base(parentScope, Locator.ByClassName("ContentDialog"))
    {
    }

    /// <inheritdoc />
    protected override bool CacheContainerRoot => false;

    /// <inheritdoc />
    protected override IMauiElement FindContainerRootElement()
    {
        return Context.Driver.TryFindActiveDialogRoot()
            ?? throw new ElementNotFoundException("No active content dialog was found.");
    }

    /// <summary>
    /// Finds a dialog button by visible text.
    /// </summary>
    public Button<ContentDialog<TParent>> DialogButton(string buttonText)
        => new(this, Locator.ByName(buttonText));

    /// <summary>
    /// The text input field inside a DisplayPromptAsync dialog.
    /// </summary>
    public Entry<ContentDialog<TParent>> PromptInput
        => new(this, Locator.ByControlType("entry"));
}
