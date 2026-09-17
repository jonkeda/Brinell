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
        return Context.AppElement.TryFindActiveDialog()
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

    /// <summary>
    /// The dialog's title, as the platform publishes it.
    /// </summary>
    /// <remarks>Read from the dialog's accessible name.</remarks>
    /// <returns>The title.</returns>
    public string? GetTitle() => ContainerRoot.Name;

    /// <summary>
    /// The text on every button the dialog is offering.
    /// </summary>
    /// <remarks>
    /// Worth asserting on its own: a confirmation with the wrong buttons still passes a test that
    /// presses a button by name.
    /// </remarks>
    /// <returns>The button texts, in tree order.</returns>
    public IReadOnlyList<string> GetButtonTexts()
        => [.. FindElements(Locator.ByControlType("button"))
            .Select(button => button.Name ?? string.Empty)];

    /// <summary>
    /// The question the dialog is asking, as the app phrased it.
    /// </summary>
    /// <remarks>
    /// Asked of the app rather than read from the dialog, so the app must report its alerts.
    /// </remarks>
    /// <returns>The message.</returns>
    /// <exception cref="NotSupportedException">
    /// The app under test does not report its alerts.
    /// </exception>
    public string GetMessage()
        => Context.AppElement.ReadAlert()?.Message
           ?? throw new NotSupportedException(
               "The app under test does not report what its alerts ask. Raise them through "
               + "BrinellAlerts in the app - one line per call site - or assert on the title and "
               + "the buttons, which the platform publishes without any cooperation.");
}
