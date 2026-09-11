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

        if (element is IValuePatternElement value && value.SupportsValuePattern)
        {
            var patternDate = ParseDate(value.GetValuePattern());
            if (patternDate != null) return patternDate;
        }

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
    /// <b>One question, then one route.</b> If the app under test declares the <c>SetDate</c>
    /// verb, it sets its own <c>DatePicker.Date</c> and says what the control then holds. If it
    /// does not, the calendar is opened and the day selected by pattern - the route for an app
    /// carrying no instrumentation, and still no pointer anywhere in it.
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

        if (!TrySetByCalendar(element, date.Value, timeoutMs))
        {
            throw new BrinellException(
                $"Could not set date {date.Value:yyyy-MM-dd}. The app under test does not declare "
                + "the SetDate verb, and walking the calendar flyout did not reach the day. "
                + $"Declaring the verb is one attribute in the app's markup. Locator: {Locator}");
        }
    }

    #endregion

    #region Calendar flyout navigation

    /// <summary>
    /// Rung 2: open the calendar by Invoke and select the day by pattern.
    /// </summary>
    /// <remarks>
    /// Measured against WinUI's CalendarDatePicker: Invoke opens a CalendarView carrying a header
    /// button ('September 2026'), Previous and Next buttons - all Invoke-able - and one DataItem
    /// per day, each with a SelectionItem pattern. No coordinates are involved at any step.
    /// </remarks>
    private bool TrySetByCalendar(IMauiElement element, System.DateTime date, int? timeoutMs)
    {
        if (element is not IInvokePatternElement invoke || !invoke.SupportsInvokePattern)
            return false;

        if (!invoke.InvokePattern())
            return false;

        var calendar = WaitForCalendar(timeoutMs);
        if (calendar == null)
            return false;

        try
        {
            if (!NavigateToMonth(calendar, date))
                return false;

            var day = FindDayItem(calendar, date);
            if (day is not ISelectionItemPatternElement selectable
                || !selectable.SupportsSelectionItemPattern)
                return false;

            if (!selectable.SelectItemPattern())
                return false;
        }
        finally
        {
            WaitHelper.Pause(PollingIntervalMs);
        }

        // Selecting reports success even when the control declines to take the value, so the rung
        // is only honest if it reads the date back. A picker constrained by MinimumDate or
        // MaximumDate is the case that matters: the cell can exist and still not commit.
        return WaitForDate(date);
    }

    /// <summary>Polls until the control reports the date, or the wait runs out.</summary>
    private bool WaitForDate(System.DateTime date)
    {
        var deadline = System.DateTime.UtcNow.AddMilliseconds(DefaultTimeoutMs);
        do
        {
            var element = MauiScope.TryFindElement(Locator);
            if (element != null && ReadDate(element)?.Date == date.Date)
                return true;

            WaitHelper.Pause(PollingIntervalMs);
        }
        while (System.DateTime.UtcNow < deadline);

        return false;
    }

    private IMauiElement? WaitForCalendar(int? timeoutMs)
    {
        var deadline = System.DateTime.UtcNow.AddMilliseconds(timeoutMs ?? DefaultTimeoutMs);
        while (System.DateTime.UtcNow < deadline)
        {
            var calendar = Context.TryFindElement(Locator.ByAutomationId("CalendarView"));
            if (calendar != null) return calendar;
            WaitHelper.Pause(PollingIntervalMs);
        }

        return null;
    }

    /// <summary>
    /// Walks the calendar to the month holding <paramref name="date"/> using Previous/Next.
    /// </summary>
    /// <remarks>
    /// The header button doubles as the month label, so the current month is read from its Name
    /// rather than tracked. The loop stops when the header stops changing, which is how a picker
    /// constrained by MinimumDate/MaximumDate reports that it will not go further - Next simply
    /// does nothing at the boundary.
    /// </remarks>
    private bool NavigateToMonth(IMauiElement calendar, System.DateTime date)
    {
        var target = new System.DateTime(date.Year, date.Month, 1);

        for (var guard = 0; guard < 120; guard++)
        {
            var header = ReadHeaderMonth(calendar);
            if (header == null) return true; // Unreadable header: let the day search decide.
            if (header.Value == target) return true;

            var button = FindCalendarButton(calendar, header.Value < target ? "Next" : "Previous");
            if (button is not IInvokePatternElement step || !step.InvokePattern())
                return false;

            WaitHelper.Pause(PollingIntervalMs);
            if (ReadHeaderMonth(calendar) == header)
                return false; // Refused to move - the target is outside Minimum/MaximumDate.
        }

        return false;
    }

    private System.DateTime? ReadHeaderMonth(IMauiElement calendar)
    {
        foreach (var button in calendar.FindElements(Locator.ByControlType("Button")))
        {
            var name = DateTimeFormats.Clean(button.Name);
            if (name.Length == 0 || name == "Previous" || name == "Next") continue;

            if (System.DateTime.TryParse("1 " + name, Culture,
                    System.Globalization.DateTimeStyles.None, out var month))
                return new System.DateTime(month.Year, month.Month, 1);
        }

        return null;
    }

    private static IMauiElement? FindCalendarButton(IMauiElement calendar, string name)
    {
        foreach (var button in calendar.FindElements(Locator.ByControlType("Button")))
        {
            if (string.Equals(DateTimeFormats.Clean(button.Name), name, StringComparison.OrdinalIgnoreCase))
                return button;
        }

        return null;
    }

    /// <summary>
    /// Finds the cell for <paramref name="date"/> among the calendar's day items.
    /// </summary>
    /// <remarks>
    /// Day cells are named by number alone ('8'), or '7, today' for today, and the grid spans two
    /// months - a measured September view ran 7..30 then 1..7 of October, so '7' appeared twice.
    /// Matching on the number alone would pick the wrong month. The cells are in chronological
    /// order, so each descent in the sequence is a month boundary; counting those identifies which
    /// run a cell belongs to.
    /// </remarks>
    private static IMauiElement? FindDayItem(IMauiElement calendar, System.DateTime date)
    {
        var items = calendar.FindElements(Locator.ByControlType("DataItem"));
        if (items.Count == 0) return null;

        var run = 0;
        var previousDay = 0;
        IMauiElement? firstRunMatch = null;

        foreach (var item in items)
        {
            if (!TryReadDayNumber(item, out var day)) continue;

            if (day < previousDay) run++;
            previousDay = day;

            if (day != date.Day) continue;

            // Run 0 is the header month; the calendar opens on the month being displayed, which
            // NavigateToMonth has already made the target month.
            if (run == 0) return item;
            firstRunMatch ??= item;
        }

        return firstRunMatch;
    }

    private static bool TryReadDayNumber(IMauiElement item, out int day)
    {
        day = 0;
        var name = DateTimeFormats.Clean(item.Name);
        if (name.Length == 0) return false;

        // '7, today' and '8' both start with the number.
        var digits = new string(name.TakeWhile(char.IsDigit).ToArray());
        return digits.Length > 0 && int.TryParse(digits, out day);
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
