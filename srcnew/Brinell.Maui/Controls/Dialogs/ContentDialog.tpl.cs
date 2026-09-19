using Brinell.Maui.Containers;
using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Text;

namespace Brinell.Maui.Controls.Dialogs;

/// <summary>
/// MAUI ContentDialog control for WinUI3 popups produced by DisplayAlert and
/// DisplayPromptAsync.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public partial class ContentDialog<TParent> : ContainerObjectBase<TParent, ContentDialog<TParent>>
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

    #region Parts

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

    #endregion

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// The dialog's title, as the platform publishes it.
    /// </summary>
    /// <remarks>Read from the dialog's accessible name.</remarks>
    /// <param name="element">The dialog root.</param>
    /// <returns>The title.</returns>
    protected virtual string? GetTitleCore(IMauiElement? element)
    {
        if (element == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(element.Name))
        {
            return element.Name;
        }

        // Android publishes no name on the alert's root; the title is its alertTitle text, under
        // the app's package for MAUI's AppCompat alert and android: for a framework one.
        return element.TryFindElement(AndroidAlertTitle, out var title, 0) ? title?.Text : null;
    }

    /// <summary>An Android alert's title, whichever package its ids carry.</summary>
    private static readonly Locator AndroidAlertTitle = Locator.ByXPath(".//*[contains(@resource-id, ':id/alertTitle')]");

    /// <summary>
    /// The text on every button the dialog is offering.
    /// </summary>
    /// <remarks>
    /// Worth asserting on its own: a confirmation with the wrong buttons still passes a test that
    /// presses a button by name.
    /// </remarks>
    /// <param name="element">The dialog root.</param>
    /// <returns>The button texts, in tree order.</returns>
    [GenerateComparisons(Comparison.SequenceEquals | Comparison.HasItem | Comparison.Count)]
    protected virtual IReadOnlyList<string>? GetButtonTextsCore(IMauiElement? element)
        => element == null
            ? null
            : [.. element.FindElements(Locator.ByControlType("button"), 0)
                .Select(button => button.Name ?? string.Empty)];

    /// <summary>
    /// The question the dialog is asking, as the app phrased it.
    /// </summary>
    /// <remarks>
    /// Asked of the app rather than read from the dialog, so the app must report its alerts. The
    /// dialog root is still resolved first, so the question is read while the dialog is open.
    /// </remarks>
    /// <param name="element">The dialog root.</param>
    /// <returns>The message.</returns>
    /// <exception cref="NotSupportedException">
    /// The app under test does not report its alerts.
    /// </exception>
    protected virtual string? GetMessageCore(IMauiElement? element)
        => Context.AppElement.ReadAlert()?.Message
           ?? throw new NotSupportedException(
               "The app under test does not report what its alerts ask. Raise them through "
               + "BrinellAlerts in the app - one line per call site - or assert on the title and "
               + "the buttons, which the platform publishes without any cooperation.");

    #endregion
}
