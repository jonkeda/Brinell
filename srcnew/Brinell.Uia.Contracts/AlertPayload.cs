namespace Brinell.Uia;

/// <summary>
/// How an alert's four strings cross the wire, written once for both ends.
/// </summary>
/// <remarks>
/// <para>
/// <b>In the contract rather than in either end, because it is one.</b> The app formats and the
/// client parses; a disagreement between them would show up as an alert whose message is
/// mysteriously blank, which reads like a missing declaration rather than like a format bug.
/// This file is compiled into both, so there is nothing to keep in step.
/// </para>
/// <para>
/// <b>Line separated, with newlines escaped.</b> An alert's message is prose written by the app
/// and may well contain a line break - "Saved.\nThe file is in Documents." is an ordinary
/// message - and an unescaped one would split a field in two and silently shift everything
/// after it. Backslash is escaped for the same reason, so that a message ending in one cannot
/// consume the separator that follows.
/// </para>
/// </remarks>
public static class AlertPayload
{
    /// <summary>What <see cref="BrinellVerb.CurrentAlert"/> answers when no alert is open.</summary>
    /// <remarks>
    /// An answer, not a refusal. "Nothing is open" is a fact about the app and a perfectly good
    /// reply; refusing would make it indistinguishable from an app that has no bridge at all,
    /// which is the distinction the whole diagnostic vocabulary exists to keep.
    /// </remarks>
    public const string None = "";

    /// <summary>Packs an alert's strings.</summary>
    /// <param name="title">The alert's title.</param>
    /// <param name="message">Its message.</param>
    /// <param name="accept">The accepting button's text.</param>
    /// <param name="cancel">The dismissing button's text, or empty when it has none.</param>
    /// <returns>The wire form.</returns>
    public static string Format(string? title, string? message, string? accept, string? cancel)
        => string.Join(
            "\n",
            Escape(title), Escape(message), Escape(accept), Escape(cancel));

    /// <summary>Reads what <see cref="Format"/> wrote.</summary>
    /// <param name="payload">The wire form, or empty when no alert is open.</param>
    /// <param name="title">The alert's title.</param>
    /// <param name="message">Its message.</param>
    /// <param name="accept">The accepting button's text.</param>
    /// <param name="cancel">The dismissing button's text, empty when it has none.</param>
    /// <returns>Whether an alert was described. False for <see cref="None"/>.</returns>
    public static bool TryParse(
        string? payload,
        out string title,
        out string message,
        out string accept,
        out string cancel)
    {
        title = message = accept = cancel = string.Empty;

        if (string.IsNullOrEmpty(payload))
        {
            return false;
        }

        var parts = payload.Split('\n');
        if (parts.Length != 4)
        {
            return false;
        }

        title = Unescape(parts[0]);
        message = Unescape(parts[1]);
        accept = Unescape(parts[2]);
        cancel = Unescape(parts[3]);
        return true;
    }

    private static string Escape(string? value)
        => (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

    /// <summary>
    /// Reverses <see cref="Escape"/>, left to right.
    /// </summary>
    /// <remarks>
    /// Scanned once rather than done with three <c>Replace</c> calls in the other order: replacing
    /// <c>\\</c> last would turn the literal two characters <c>\</c> and <c>n</c> - written by an
    /// app whose message really does contain a backslash before an n - into a line break.
    /// </remarks>
    private static string Unescape(string value)
    {
        var built = new System.Text.StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 >= value.Length)
            {
                built.Append(value[i]);
                continue;
            }

            i++;
            built.Append(value[i] switch
            {
                'n' => '\n',
                'r' => '\r',
                '\\' => '\\',

                // Not something this end wrote. Kept as it arrived rather than dropped, so a
                // newer app's escape shows up in the assertion instead of vanishing from it.
                _ => value[i],
            });
        }

        return built.ToString();
    }
}
