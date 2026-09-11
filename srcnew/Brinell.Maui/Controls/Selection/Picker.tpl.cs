namespace Brinell.Maui.Controls.Selection;

/// <summary>
/// MAUI Picker control for dropdown selection.
/// Inherits SelectByText, SelectByIndex, GetSelectedText from SelectorControlBase.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Picker<TScope> : Base.SelectorControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new picker control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the picker element.</param>
    public Picker(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new picker control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Picker(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Flyout - Core Methods

    // Opening the dropdown is now a thing a test asks for, rather than something selection did
    // on its way past. The two were the same call before: to select an item you opened the
    // popup, and so every selection test was also, silently, a flyout test - and a genuine
    // flyout test could not be told apart from one that only wanted the value changed.
    //
    // This half stays on the ExpandCollapse pattern rather than becoming a verb. MAUI has no
    // public API to open a Picker's dropdown - it belongs to the platform control - so an app
    // could only answer such a verb by reaching into WinUI, while the pattern is exactly the
    // supported way to ask a combo box to expand. There is nothing to gain by moving it.

    /// <summary>Opens the picker's dropdown.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void OpenFlyoutCore(IMauiElement element, int? timeoutMs = null)
    {
        if (element is not IExpandCollapsePatternElement<IMauiElement> flyout
            || !flyout.SupportsExpandCollapse)
        {
            throw new BrinellException(
                $"This picker cannot be expanded: its platform element publishes no "
                + $"ExpandCollapse pattern. Locator: {Locator}");
        }

        if (!flyout.Expand())
        {
            throw new BrinellException(
                $"The picker's dropdown did not open. Locator: {Locator}");
        }
    }

    /// <summary>Closes the picker's dropdown.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void CloseFlyoutCore(IMauiElement element, int? timeoutMs = null)
    {
        if (element is IExpandCollapsePatternElement<IMauiElement> flyout
            && flyout.SupportsExpandCollapse)
        {
            flyout.Collapse();
        }
    }

    /// <summary>Whether the picker's dropdown is showing.</summary>
    /// <remarks>
    /// False where the platform publishes no ExpandCollapse pattern, which is the honest answer:
    /// nothing is open, and nothing can be.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>Whether the dropdown is open.</returns>
    protected virtual bool? IsFlyoutOpenCore(IMauiElement? element)
        => element is IExpandCollapsePatternElement<IMauiElement> { SupportsExpandCollapse: true, IsExpanded: true };

    /// <summary>
    /// What the open dropdown is showing.
    /// </summary>
    /// <remarks>
    /// <b>Not the same question as <c>GetItemTexts</c>, and the difference is the point.</b> This
    /// is what the popup has put into the accessibility tree, which for a long list is the
    /// visible handful rather than everything the picker holds - so it answers "what can be seen
    /// right now", while the app answers "what is there". Reading the second out of the first is
    /// what step 24 stopped doing.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The realized item texts, or null where the platform has no dropdown to read.</returns>
    protected virtual IReadOnlyList<string>? GetDropdownItemTextsCore(IMauiElement? element)
    {
        if (element is not IExpandCollapsePatternElement<IMauiElement> flyout
            || !flyout.SupportsExpandCollapse)
        {
            return null;
        }

        return flyout.GetExpandedItems()?
            .Select(item => item.Text ?? string.Empty)
            .ToList();
    }

    #endregion

    #region Selection state - Core Methods

    // Four reads the app answers for itself. Each one used to open the dropdown, read the popup
    // and close it again - a read that changed what it was reading, so asking a picker what it
    // held twice in a row was two different journeys through the app, and a test that only
    // wanted to check a value left the UI somewhere it had not been.

    /// <inheritdoc />
    protected override string? GetSelectedTextCore(IMauiElement? element)
    {
        if (element is not { SupportsStateReads: true })
        {
            return base.GetSelectedTextCore(element);
        }

        var selected = element.ReadState("SelectedItem");
        return selected.Length == 0 ? null : selected;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>This was derived, and the derivation was wrong for a picker holding two items that
    /// read alike.</b> It read the selected text and returned the position of the first item
    /// matching it, so selecting the second of two identical entries reported the first. The app
    /// holds the number.
    /// </remarks>
    protected override int? GetSelectedIndexCore(IMauiElement? element)
    {
        if (element is not { SupportsStateReads: true })
        {
            return base.GetSelectedIndexCore(element);
        }

        var reported = element.ReadState("SelectedIndex");

        if (!int.TryParse(
                reported,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var index))
        {
            throw new BrinellException(
                $"The picker answered SelectedIndex with '{reported}', which is not a number. "
                + $"The two ends of the bridge disagree about the format. Locator: {Locator}");
        }

        // -1 is MAUI's "nothing is selected", and null is this API's. Neither is an error.
        return index < 0 ? null : index;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The count travels with the items so that a lie is detectable: item texts are arbitrary
    /// user strings, one of them may contain a newline, and a list silently one item longer than
    /// the app's would be compared against an expectation without anyone being told.
    /// </remarks>
    protected override IReadOnlyList<string>? GetItemTextsCore(IMauiElement? element)
    {
        if (element is not { SupportsStateReads: true })
        {
            return base.GetItemTextsCore(element);
        }

        var reported = element.ReadState("Items");
        var lines = reported.Split('\n');

        if (!int.TryParse(
                lines[0],
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var count)
            || lines.Length - 1 != count)
        {
            throw new BrinellException(
                $"The picker reported {lines.Length - 1} item(s) after announcing '{lines[0]}'. "
                + "Either an item text contains a newline or the two ends of the bridge disagree "
                + $"about the format. Locator: {Locator}");
        }

        return lines.Skip(1).ToList();
    }

    /// <inheritdoc />
    protected override int? GetItemCountCore(IMauiElement? element)
    {
        if (element is not { SupportsStateReads: true })
        {
            return base.GetItemCountCore(element);
        }

        var reported = element.ReadState("ItemCount");

        if (!int.TryParse(
                reported,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var count))
        {
            throw new BrinellException(
                $"The picker answered ItemCount with '{reported}', which is not a number. "
                + $"Locator: {Locator}");
        }

        return count;
    }

    #endregion

    #region Title - Core Methods

    /// <summary>
    /// Gets the picker title from the element.
    /// </summary>
    /// <param name="element">The picker element (may be null).</param>
    /// <returns>The title text.</returns>
    protected virtual string? GetTitleCore(IMauiElement? element)
    {
        if (element == null) return null;

        // The accessible name, which is what a picker's Title becomes once rendered. Not the
        // MAUI Title property - nothing publishes that - so a picker whose name is set from
        // something else reports that instead.
        return element.Name;
    }

    #endregion
}
