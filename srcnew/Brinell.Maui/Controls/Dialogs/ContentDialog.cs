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

    /// <summary>
    /// The dialog's title, as the platform publishes it.
    /// </summary>
    /// <remarks>
    /// <b>The dialog's own accessible name</b>, not the first piece of text inside it. WinUI
    /// names a <c>ContentDialog</c> after its title and also renders that title as a text
    /// element in the content area, so reading the text would be picking a position out of a
    /// list and hoping; the name is the platform stating which it is.
    /// </remarks>
    /// <returns>The title.</returns>
    public string? GetTitle() => ContainerRoot.Name;

    /// <summary>
    /// The text on every button the dialog is offering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Worth asserting on its own, because the commonest dialog defect is the button list.</b>
    /// A confirmation that offers no way out, or that offers two where the destructive one is
    /// the default, is wrong in a way no assertion about the outcome can see: the test presses a
    /// button by name and passes either way.
    /// </para>
    /// <para>
    /// Read from the platform rather than from the app, because unlike the message these are
    /// published unambiguously - each is a Button element with its text as its name.
    /// </para>
    /// </remarks>
    /// <returns>The button texts, in tree order.</returns>
    public IReadOnlyList<string> GetButtonTexts()
        => [.. FindElements(Locator.ByControlType("button"))
            .Select(button => button.Name ?? string.Empty)];

    /// <summary>
    /// The question the dialog is asking, as the app phrased it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This one does not come from the dialog</b>, which is worth knowing before reading the
    /// exception it can throw. WinUI puts the message in the dialog's content area beside a
    /// second copy of the title, so from out here it can only be picked out as "the text that is
    /// not the title" - true until an app writes an alert whose message and title read alike, and
    /// wrong silently when one does. It is asked of the app instead, which passed the string.
    /// </para>
    /// <para>
    /// It sits on the dialog anyway because this is where someone looks for it. The title and
    /// the buttons beside it are read straight off the platform and need nothing from the app.
    /// </para>
    /// </remarks>
    /// <returns>The message.</returns>
    /// <exception cref="NotSupportedException">
    /// The app under test does not report its alerts.
    /// </exception>
    public string GetMessage()
        => Context.Driver.CurrentAlert()?.Message
           ?? throw new NotSupportedException(
               "The app under test does not report what its alerts ask. Raise them through "
               + "BrinellAlerts in the app - one line per call site - or assert on the title and "
               + "the buttons, which the platform publishes without any cooperation.");
}
