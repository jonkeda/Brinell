namespace Brinell.Maui.Containers;

/// <summary>
/// Answers whether an element the caller already holds matches a locator.
/// </summary>
/// <remarks>
/// <para>
/// A locator is normally handed to the driver to <i>search</i> with. A collection needs the
/// other direction: it has the item roots already and must say which one the caller meant.
/// Reusing <see cref="Locator"/> for that keeps one vocabulary - <c>ByAutomationId</c>,
/// <c>ByText</c>, <c>ByControlType</c> - instead of inventing a second way to name a thing.
/// </para>
/// <para>
/// Identifiers are compared exactly; captions leniently. An <c>AutomationId</c> is written by
/// the app author on both sides of the comparison, so a difference in case is a mistake worth
/// surfacing. A caption is rendered by the platform - Android cases button text to suit its
/// theme - so an exact match there would pass on one platform and fail on the other.
/// </para>
/// </remarks>
public static class ElementMatch
{
    /// <summary>
    /// Whether <paramref name="element"/> answers to <paramref name="locator"/>.
    /// </summary>
    /// <param name="element">The element to test.</param>
    /// <param name="locator">What the caller is looking for.</param>
    /// <returns>True when the element matches.</returns>
    /// <exception cref="NotSupportedException">
    /// The locator strategy cannot be evaluated against an element in hand. Searching by it
    /// may still work; only matching an element already found is unsupported.
    /// </exception>
    public static bool Matches(IMauiElement element, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(locator);

        try
        {
            return locator.Strategy switch
            {
                LocatorStrategy.AutomationId or LocatorStrategy.Id or LocatorStrategy.AccessibilityId
                    => Exactly(element.AutomationId, locator.Value),
                LocatorStrategy.Text => Loosely(element.Text, locator.Value),
                LocatorStrategy.Name => Loosely(element.Name, locator.Value),
                LocatorStrategy.ControlType => MatchesControlType(element, locator.Value),
                _ => throw new NotSupportedException(
                    $"A '{locator.Strategy}' locator cannot be matched against an element that is " +
                    "already found. Supported: AutomationId, Id, AccessibilityId, Text, Name, ControlType.")
            };
        }
        catch (StaleElementReferenceException)
        {
            // A dead element matches nothing; the caller re-resolves and asks again.
            return false;
        }
    }

    /// <summary>
    /// Whether the element is of the given control type.
    /// </summary>
    /// <remarks>
    /// Compared against the last segment of the platform's own type name, so
    /// <c>ByControlType("Button")</c> matches Windows' <c>Button</c> and Android's
    /// <c>android.widget.Button</c>. Type <i>names</i> still differ between platforms - a MAUI
    /// Entry is <c>Edit</c> on Windows and <c>EditText</c> on Android - so a control-type key is
    /// only portable where the platforms happen to agree. Prefer an id or a caption.
    /// </remarks>
    private static bool MatchesControlType(IMauiElement element, string controlType)
    {
        var tagName = element.TagName;
        if (string.IsNullOrEmpty(tagName)) return false;

        var lastSegment = tagName[(tagName.LastIndexOf('.') + 1)..];
        return string.Equals(lastSegment, controlType.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Exactly(string? actual, string expected)
        => string.Equals(actual, expected, StringComparison.Ordinal);

    private static bool Loosely(string? actual, string expected)
        => string.Equals(actual?.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
}
