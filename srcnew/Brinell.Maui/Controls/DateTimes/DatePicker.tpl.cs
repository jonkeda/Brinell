using Brinell.Core;
using Brinell.Core.Utilities;
using System.Linq;

namespace Brinell.Maui.Controls.DateTimes;

/// <summary>
/// MAUI DatePicker control for date selection.
/// Provides GetDate, SetDate, and date assertion methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class DatePicker<TScope> : Base.FocusableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new date picker control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the date picker element.</param>
    public DatePicker(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new date picker control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public DatePicker(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Format

    private string? _format;
    private System.Globalization.CultureInfo? _culture;

    /// <summary>
    /// Declares the format this control's date is rendered and entered in.
    /// </summary>
    /// <remarks>
    /// Overrides <see cref="DateTimeFormats.Date"/> for this control only. When a format is
    /// declared, reads parse with it exactly and a string that does not match is an error rather
    /// than a null - see <see cref="ParseDate"/>.
    /// </remarks>
    /// <param name="format">A .NET date format string, e.g. <c>dd-MMM-yy</c>.</param>
    /// <param name="culture">Culture for the format. Defaults to <see cref="DateTimeFormats.Culture"/>.</param>
    /// <returns>This control, for chaining onto a locator expression.</returns>
    public DatePicker<TScope> WithFormat(string format, System.Globalization.CultureInfo? culture = null)
    {
        _format = format ?? throw new ArgumentNullException(nameof(format));
        _culture = culture;
        return this;
    }

    /// <summary>Gets the format in force: the control's own, else the suite default.</summary>
    protected string Format => _format ?? DateTimeFormats.Date;

    /// <summary>Gets the culture in force: the control's own, else the suite default.</summary>
    protected System.Globalization.CultureInfo Culture => _culture ?? DateTimeFormats.Culture;

    /// <summary>
    /// Parses a rendered date using the declared format.
    /// </summary>
    /// <remarks>
    /// When the control declares a format through <see cref="WithFormat"/>, that format is the
    /// only one tried, and a mismatch throws naming both what was expected and what arrived. When
    /// no format is declared the suite default is tried first and the culture's own patterns
    /// second, which keeps an unconfigured suite working without ever falling back to the
    /// locale-ambiguous guessing this replaced.
    /// </remarks>
    protected System.DateTime? ParseDate(string? text)
    {
        var cleaned = DateTimeFormats.Clean(text);
        if (cleaned.Length == 0) return null;

        if (System.DateTime.TryParseExact(cleaned, Format, Culture,
                System.Globalization.DateTimeStyles.None, out var exact))
            return exact;

        if (_format != null)
        {
            throw new BrinellException(
                $"Could not read a date from '{cleaned}' using format '{Format}' " +
                $"({Culture.Name}). Locator: {Locator}");
        }

        return System.DateTime.TryParse(cleaned, Culture,
            System.Globalization.DateTimeStyles.None, out var loose)
            ? loose
            : null;
    }

    #endregion

    #region Date - Core Methods

    // Named ReadDate rather than GetDateCore so the generated exact-equality
    // trio lands on DateValue. AssertDate/WaitDate compare whole days only, which the
    // generated equality comparison cannot express, so those stay hand-written below
    // and keep their original signatures.

    /// <summary>
    /// Reads the date the control is showing.
    /// </summary>
    /// <remarks>
    /// The Value pattern is the authoritative source on Windows: it answers '07-Sep-26' on the
    /// picker itself, so there is nothing to search for. It is read-only there, which is why it
    /// appears here and not in <see cref="SetDateCore"/>. The DateText child is the fallback for
    /// platforms that publish no Value pattern.
    /// </remarks>
    protected virtual System.DateTime? ReadDate(IMauiElement? element)
    {
        if (element == null) return null;

        var valueDate = ParseDate(element.Value);
        if (valueDate != null) return valueDate;

        foreach (var child in element.FindElements(Locator.ByAutomationId("DateText")))
        {
            var childDate = ParseDate(child.Name) ?? ParseDate(child.Text);
            if (childDate != null) return childDate;
        }

        return ParseDate(element.Name) ?? ParseDate(element.Text);
    }

    /// <summary>
    /// Sets the date.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The verb, or a refusal that names it.</b> If the app under test declares the
    /// <c>SetDate</c> verb, it sets its own <c>DatePicker.Date</c> and says what the control then
    /// holds. If it does not, this throws.
    /// </para>
    /// <para>
    /// <b>There was a second route, and step 107 removed it.</b> For an app without the bridge,
    /// the control opened WinUI's calendar flyout by Invoke and walked it by pattern - month
    /// header, Previous and Next, one DataItem per day - by the automation ids of WinUI's own
    /// template. That was Windows internals written into a cross-platform control, it did nothing
    /// on Android, and the <c>SetDate</c> verb has replaced it since step 20.
    /// </para>
    /// <para>
    /// <b>This was a three-rung ladder</b>, and the rungs were tried in order until one appeared
    /// to work: a writable Value pattern, then the calendar, then typing. Two of the three were
    /// dead on the only platform that runs them - WinUI advertises the Value pattern and refuses
    /// the write, and a <c>CalendarDatePicker</c> hosts no text to type into - so every call paid
    /// for two failures to reach the one that worked, and a genuine breakage in the calendar
    /// route would have been reported as "could not set the date" with three suspects. Asking
    /// first costs one capability lookup and names the failure.
    /// </para>
    /// </remarks>
    protected virtual void SetDateCore(IMauiElement element, System.DateTime? date, int? timeoutMs = null)
    {
        if (date == null) return;

        if (element.SupportsSetDate)
        {
            element.SetDate(date.Value);
            return;
        }

        throw new BrinellException(
            $"Could not set date {date.Value:yyyy-MM-dd}. The app under test does not declare the "
            + "SetDate verb, and it is the only route: declaring it is one attribute in the app's "
            + $"markup - see GestureAutomation.Verbs. Locator: {Locator}");
    }

    #endregion

    #region Hand-written Convenience Members

    // Whole-day comparison (.Date): the control holds a day, and a caller passing
    // DateTime.Now should not fail against a picker showing today.

    /// <summary>
    /// Gets the currently selected date.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>The selected date, or null if element not found.</returns>
    public System.DateTime? GetDate(int? timeoutMs = null)
        => RunGetWithElement(element => ReadDate(element), timeoutMs);

    /// <summary>
    /// Waits for the date to match the expected value, comparing whole days.
    /// </summary>
    /// <param name="expected">Expected date. Null skips the wait.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    public bool WaitDate(System.DateTime? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return RunWaitWithElement(expected,
            e =>
            {
                var actual = ReadDate(e);
                return actual.HasValue && actual.Value.Date == expected.Value.Date;
            },
            timeoutMs);
    }

    /// <summary>
    /// Asserts the date matches the expected value, comparing whole days.
    /// </summary>
    /// <param name="expected">Expected date. Null skips the assertion.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertDate(System.DateTime? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssertWithElement(expected,
            ReadDate,
            (actual, exp) => actual.HasValue && exp.HasValue && actual.Value.Date == exp.Value.Date,
            message ?? $"Expected date {expected:yyyy-MM-dd}. Locator: {Locator}", timeoutMs);
    }

    #endregion
}
