using Brinell.Uia;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// Raises the app's alerts and remembers what they asked, so a test can assert the question.
/// </summary>
/// <remarks>
/// <para>
/// <b>A test could always see that a dialog appeared; it could not see what it said.</b> The
/// assertions available were "something opened" and "the label afterwards says confirmed" -
/// both of which stay true if the app asks the wrong question, or asks a destructive one where
/// it meant to ask a harmless one. That is the class of green test step 23 was about.
/// </para>
/// <para>
/// <b>Why the app has to be the one to say.</b> On Windows the popup does publish its title and
/// its buttons, and those are read straight off the platform - see <c>ContentDialog</c>. The
/// message is the exception: WinUI puts it in the dialog's content area together with a second
/// copy of the title, so from outside it can only be identified as "the text that is not the
/// title", which is wrong for an alert whose message and title read alike. There is also no
/// supported way to observe <c>DisplayAlert</c> from outside the call: MAUI signals its own
/// platform layer through <c>MessagingCenter</c>, which is <c>internal</c> in MAUI 10, and
/// reflecting into it would turn a MAUI upgrade into a runtime failure found by a test rather
/// than a build failure found by a compiler.
/// </para>
/// <para>
/// <b>So the app announces, at the call site.</b> That is a real cost - an app under test that
/// cannot be modified keeps only the title and the buttons - and it is the same bargain the rest
/// of the bridge makes: one line in the app, in exchange for an answer that cannot be wrong.
/// </para>
/// </remarks>
public static class BrinellAlerts
{
    /// <summary>
    /// The alerts currently open, innermost last.
    /// </summary>
    /// <remarks>
    /// A stack rather than one slot, because an app can raise a second alert from the handler
    /// of the first. With one slot the inner alert's dismissal would clear the record while the
    /// outer one was still on screen, and the verb would then answer "nothing is open" about a
    /// dialog a user is looking at.
    /// </remarks>
    private static readonly List<string> Open = [];

    private static readonly object Gate = new();

    /// <summary>
    /// Shows a one-button alert and records it.
    /// </summary>
    /// <remarks>
    /// MAUI's single-button overload passes that button as the <i>dismissing</i> one, and WinUI
    /// renders it as the dialog's secondary button. This reports it the same way rather than
    /// promoting it: a caller asserting on an alert with nothing to accept should see an empty
    /// accept, not a fabricated one.
    /// </remarks>
    /// <param name="page">The page to raise it on.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancel">The only button's text.</param>
    /// <returns>A task completing when the alert is dismissed.</returns>
    public static async Task DisplayAlertAsync(
        Page page, string title, string message, string cancel)
    {
        ArgumentNullException.ThrowIfNull(page);

        var recorded = Record(title, message, accept: string.Empty, cancel);

        try
        {
            await page.DisplayAlertAsync(title, message, cancel);
        }
        finally
        {
            Forget(recorded);
        }
    }

    /// <summary>Shows a two-button alert and records it.</summary>
    /// <param name="page">The page to raise it on.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="accept">The accepting button's text.</param>
    /// <param name="cancel">The dismissing button's text.</param>
    /// <returns>Whether the accepting button was pressed.</returns>
    public static async Task<bool> DisplayAlertAsync(
        Page page, string title, string message, string accept, string cancel)
    {
        ArgumentNullException.ThrowIfNull(page);

        var recorded = Record(title, message, accept, cancel);

        try
        {
            return await page.DisplayAlertAsync(title, message, accept, cancel);
        }
        finally
        {
            Forget(recorded);
        }
    }

    /// <summary>Shows a prompt and records it.</summary>
    /// <param name="page">The page to raise it on.</param>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="accept">The accepting button's text.</param>
    /// <param name="cancel">The dismissing button's text.</param>
    /// <returns>What was typed, or null if it was dismissed.</returns>
    public static async Task<string?> DisplayPromptAsync(
        Page page, string title, string message, string accept, string cancel)
    {
        ArgumentNullException.ThrowIfNull(page);

        var recorded = Record(title, message, accept, cancel);

        try
        {
            return await page.DisplayPromptAsync(title, message, accept, cancel);
        }
        finally
        {
            Forget(recorded);
        }
    }

    /// <summary>
    /// What the alert on screen is asking, in wire form.
    /// </summary>
    /// <remarks>
    /// <see cref="AlertPayload.None"/> when nothing is open, which is an answer rather than a
    /// refusal - see that field for why the difference matters.
    /// </remarks>
    /// <returns>The innermost open alert, or <see cref="AlertPayload.None"/>.</returns>
    internal static string Current()
    {
        lock (Gate)
        {
            return Open.Count == 0 ? AlertPayload.None : Open[^1];
        }
    }

    private static string Record(string title, string message, string accept, string cancel)
    {
        var payload = AlertPayload.Format(title, message, accept, cancel);

        lock (Gate)
        {
            Open.Add(payload);
        }

        return payload;
    }

    /// <summary>
    /// Removes one recorded alert, by identity of what was recorded.
    /// </summary>
    /// <remarks>
    /// The last matching entry rather than the last entry: two alerts asking the same thing are
    /// indistinguishable here, and removing the newest of those is right in both orders. Removing
    /// blindly from the end would drop the wrong one when an inner alert outlives its opener,
    /// which a task continuation can arrange.
    /// </remarks>
    private static void Forget(string payload)
    {
        lock (Gate)
        {
            var last = Open.LastIndexOf(payload);
            if (last >= 0)
            {
                Open.RemoveAt(last);
            }
        }
    }
}
