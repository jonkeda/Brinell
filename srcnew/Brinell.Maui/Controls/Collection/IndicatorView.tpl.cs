using System.Globalization;

namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI IndicatorView control for displaying carousel or collection indicators.
/// IndicatorView shows visual indicators (typically dots) representing positions in a collection.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not in the Windows automation tree at all</b> (probed 2026-09-18, MAUI 10): the dots are
/// drawn with no automation element, and the view itself publishes none either. This control
/// resolves it through the app's bridge declaration instead
/// (<see cref="IMauiElement.TryFindDeclared"/>); the app must declare
/// <c>uia:GestureAutomation.Verbs="GetState"</c> on it. The bridge answers <c>Position</c> (the
/// selected dot), <c>Count</c> (the number of dots), <c>IsVisible</c> and <c>IsEnabled</c>.
/// </para>
/// <para>
/// <b>Android.</b> Where the view is in the tree it is found there; no state read answers
/// <c>Position</c> or <c>Count</c>, so they are null.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class IndicatorView<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new IndicatorView control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the IndicatorView element.</param>
    public IndicatorView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new IndicatorView control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public IndicatorView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Element Finding

    /// <summary>
    /// The view from the tree where the platform shows it, otherwise the app's declaration of it.
    /// </summary>
    protected override IMauiElement? TryFindElement()
        => base.TryFindElement()
           ?? (Locator.Strategy == LocatorStrategy.AutomationId
               ? Context.AppElement.TryFindDeclared(Locator.Value)
               : null);

    /// <inheritdoc />
    protected override ElementNotFoundException NotFound()
        => new($"IndicatorView was not found by '{Locator}' in the tree or among the app's bridge "
               + "declarations. On Windows the app must declare GetState on it.");

    #endregion

    #region Core Method Overrides

    /// <inheritdoc />
    protected override bool? IsVisibleCore(IMauiElement? element)
        => ReadBool(element, "IsVisible") ?? base.IsVisibleCore(element);

    /// <inheritdoc />
    protected override bool? IsEnabledCore(IMauiElement? element)
        => ReadBool(element, "IsEnabled") ?? base.IsEnabledCore(element);

    #endregion

    #region IndicatorView-Specific Core Methods

    /// <summary>
    /// Gets the index of the selected indicator, counted from zero.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The selected index, or null where the platform does not publish it.</returns>
    protected virtual int? GetPositionCore(IMauiElement? element)
        => ReadInt(element, "Position") ?? ReadDots(element)?.Position;

    /// <summary>
    /// Gets the number of indicators shown.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The indicator count, or null where the platform does not publish it.</returns>
    protected virtual int? GetCountCore(IMauiElement? element)
        => ReadInt(element, "Count") ?? ReadDots(element)?.Count;

    #endregion

    #region Helpers

    /// <summary>
    /// The count and position as Android's own dots describe them, or null elsewhere.
    /// </summary>
    /// <remarks>
    /// Android publishes each dot with a description such as "Item 2 of 5", and ", selected" on the
    /// current one (measured on the sample's IndicatorView). That text is the platform's and
    /// follows the device's language; the emulator runs in English. Children are "./*/*": an
    /// element's XPath is rooted at the element on UiAutomator2.
    /// </remarks>
    private (int Count, int? Position)? ReadDots(IMauiElement? element)
    {
        if (element == null || Context.Driver.Platform != Enums.MauiPlatform.Android)
        {
            return null;
        }

        var count = 0;
        int? position = null;

        foreach (var dot in element.FindElements(Locator.ByXPath("./*/*")))
        {
            var match = DotDescription.Match(dot.Name ?? string.Empty);
            if (!match.Success)
            {
                continue;
            }

            count = int.Parse(match.Groups["count"].Value, CultureInfo.InvariantCulture);
            if (match.Groups["selected"].Success)
            {
                position = int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture) - 1;
            }
        }

        return count == 0 ? null : (count, position);
    }

    private static readonly System.Text.RegularExpressions.Regex DotDescription =
        new(@"^Item (?<index>\d+) of (?<count>\d+)(?<selected>, selected)?$");

    private bool? ReadBool(IMauiElement? element, string property)
        => element?.ReadState(property) is { } reported && bool.TryParse(reported, out var value)
            ? value
            : null;

    private int? ReadInt(IMauiElement? element, string property)
    {
        if (element?.ReadState(property) is not { } reported)
        {
            return null;
        }

        return int.TryParse(reported, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new BrinellException(
                $"The IndicatorView answered {property} with '{reported}', which is not a number. "
                + $"Locator: {Locator}");
    }

    #endregion
}
