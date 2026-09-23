namespace Brinell.Maui.Interfaces;

/// <summary>
/// What the alert on screen is asking.
/// </summary>
/// <remarks>
/// <para>
/// <b>The question, not the outcome.</b> Assert on this to catch an app that asks the wrong
/// question but still reaches the expected result.
/// </para>
/// <para>
/// <b>A one-button alert has no <see cref="Accept"/>.</b> MAUI passes the single button as the
/// dismissing one, so it is reported in <see cref="Cancel"/> with <see cref="Accept"/> empty.
/// </para>
/// </remarks>
/// <param name="Title">The alert's title.</param>
/// <param name="Message">Its message, which only the app can report - see
/// <c>IMauiElement.ReadAlert</c>.</param>
/// <param name="Accept">The accepting button's text, empty when there is only one button.</param>
/// <param name="Cancel">The dismissing button's text.</param>
public readonly record struct AlertContents(
    string Title,
    string Message,
    string Accept,
    string Cancel)
{
    /// <summary>The buttons the alert offers, in the order MAUI was given them.</summary>
    /// <remarks>
    /// For asserting the choices without caring which is which, and for the common mistake of
    /// offering only one button where the app meant to offer a way out.
    /// </remarks>
    public IReadOnlyList<string> Buttons =>
        Accept.Length == 0 ? [Cancel] : [Accept, Cancel];
}
