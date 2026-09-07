using System.Globalization;

namespace Brinell.Maui.Controls.DateTimes;

/// <summary>
/// Suite-wide default formats and culture used to read and write date and time controls as text.
/// </summary>
/// <remarks>
/// <para>
/// A date that crosses the automation boundary is a string, and a string is only a date once you
/// know the format. Nothing on either platform publishes the format, so it has to be declared.
/// The alternative — inferring it — is what the old <c>TryParseDateString</c> did, and it resolved
/// <c>03/04/2025</c> as 3 April on a US machine and 4 March on a British one, silently.
/// </para>
/// <para>
/// Set these once at suite start-up. An individual control overrides them with
/// <c>WithFormat(...)</c> when one screen renders a date differently from the rest of the app.
/// </para>
/// </remarks>
public static class DateTimeFormats
{
    /// <summary>
    /// Format used to read and write dates when a control does not declare its own.
    /// </summary>
    /// <remarks>
    /// Defaults to the current culture's short-date pattern, because that is what the platform
    /// renders into the accessibility tree — WinUI showed <c>07-Sep-26</c> on the machine this was
    /// measured on, not an invariant <c>yyyy-MM-dd</c>. It is one declared pattern, not a list of
    /// candidates.
    /// </remarks>
    public static string Date { get; set; } = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

    /// <summary>
    /// Format used to read and write times when a control does not declare its own.
    /// </summary>
    public static string Time { get; set; } = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;

    /// <summary>
    /// Culture applied to <see cref="Date"/> and <see cref="Time"/>.
    /// </summary>
    /// <remarks>
    /// Note that <c>/</c> and <c>:</c> in a .NET format string are not literals — they stand for
    /// the culture's date and time separators. <c>ToString("MM/dd/yyyy")</c> produced
    /// <c>09-19-2026</c> on the machine this was measured on. Use
    /// <see cref="CultureInfo.InvariantCulture"/> when a format is meant literally.
    /// </remarks>
    public static CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>
    /// Restores the defaults. Intended for tests that change the statics.
    /// </summary>
    public static void Reset()
    {
        Date = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        Time = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
        Culture = CultureInfo.CurrentCulture;
    }

    /// <summary>
    /// Strips the Unicode formatting characters the platform embeds in rendered date strings.
    /// </summary>
    /// <remarks>
    /// WinUI writes left-to-right marks (U+200E) between every field, so the tree carries
    /// <c>'‎07‎-‎Sep‎-‎26'</c> where the screen shows <c>07-Sep-26</c>.
    /// Every parse has to remove them first.
    /// </remarks>
    public static string Clean(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(text, @"\p{Cf}", string.Empty).Trim();
    }
}
