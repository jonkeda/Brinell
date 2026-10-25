namespace Bruinell.Maui.CommunityToolkit.Controls.Views;

/// <summary>
/// CommunityToolkit.Maui <c>RatingView</c>: a row of shapes the user taps to give a rating.
/// </summary>
/// <remarks>
/// <para>
/// <b>A range control whose steps are stars.</b> Value is the rating, Maximum is
/// <c>MaximumRating</c>, Minimum is 0 and Step is 1. Setting a value taps the star with that
/// number, as a user would; a rating of 0 has no star to tap and is refused.
/// </para>
/// <para>
/// <b>Windows needs the app's sink.</b> The view publishes no RangeValue pattern and its stars
/// have neither ids nor patterns. The app declares <c>SelectIndex,GetState</c> on it and attaches
/// a sink that taps star <c>i</c> and answers <c>Rating</c>, <c>MaximumRating</c> and
/// <c>IsReadOnly</c>; see <c>ToolkitVerbSink</c> in the sample app.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class RatingView<TScope> : Bruinell.Maui.Controls.Base.RangeControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a rating view control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the rating view element.</param>
    public RatingView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a rating view control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID).</param>
    public RatingView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Method Overrides

    /// <inheritdoc />
    protected override double? GetValueCore(IMauiElement? element)
        => ReadNumber(element, "Rating");

    /// <inheritdoc />
    protected override double? GetMinimumCore(IMauiElement? element)
        => element == null ? null : 0;

    /// <inheritdoc />
    protected override double? GetMaximumCore(IMauiElement? element)
        => ReadNumber(element, "MaximumRating");

    /// <inheritdoc />
    protected override double? GetStepCore(IMauiElement? element)
        => element == null ? null : 1;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">The value is not a star that can be tapped.</exception>
    protected override void SetValueCore(IMauiElement element, double? value, int? timeoutMs = null)
    {
        if (value == null)
        {
            return;
        }

        EnsureSettableCore(element);

        var maximum = GetMaximumCore(element);
        var star = (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
        if (star < 1 || (maximum.HasValue && star > maximum.Value) || Math.Abs(value.Value - star) > 0.001)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value,
                $"RatingView '{Locator.Value}' can be set only by tapping a star: a whole number "
                + $"from 1 to {maximum?.ToString(CultureInfo.InvariantCulture) ?? "its maximum"}.");
        }

        TapStarCore(element, star, timeoutMs);
    }

    #endregion

    #region RatingView-Specific Core Methods

    /// <summary>
    /// Reads whether the rating is fixed and ignores taps.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True when read-only, null where the app does not report it.</returns>
    protected virtual bool? IsReadOnlyCore(IMauiElement? element)
    {
        if (element?.ReadState("IsReadOnly") is not { } reported)
        {
            return null;
        }

        return bool.TryParse(reported, out var readOnly)
            ? readOnly
            : throw new BruinellException(
                $"The rating view answered IsReadOnly with '{reported}', which is not a boolean. "
                + $"Locator: {Locator}");
    }

    /// <summary>
    /// Taps the star with the given number, counting from 1, and waits for the rating to match.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="star">The star to tap. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void TapStarCore(IMauiElement element, int? star, int? timeoutMs = null)
    {
        if (star == null)
        {
            return;
        }

        EnsureSettableCore(element);

        if (Context.Driver?.DrawsRatingStars == true)
        {
            TapStarOnScreen(element, star.Value);

            // Android draws the stars and publishes no rating, so there is nothing to confirm
            // against; the page's own text is what a test reads there.
            if (GetValueCore(element) is null)
            {
                return;
            }
        }
        else
        {
            element.SelectIndex(star.Value - 1);
        }

        var confirmation = Confirm(() => GetValueCore(element),
                actual => actual.HasValue && Math.Abs(actual.Value - star.Value) < 0.001,
                timeoutMs);
        if (!confirmation.IsConfirmed)
        {
            throw confirmation.Failure(Locator, "the tap", lastError => new TimeoutException(
                $"RatingView '{Locator.Value}' did not reach a rating of {star}.", lastError));
        }
    }

    #endregion

    /// <summary>Taps the star itself, as a person does.</summary>
    /// <remarks>
    /// <c>SelectIndex</c> is a picker's route on Appium - it taps the control and looks for a
    /// dropdown - and failed here as "shows 0 item(s)". On Android the stars are the rating view's
    /// grandchildren, behind one layout node; UiAutomator2 roots an element's XPath at the element,
    /// so they are "./*/*/*" (measured on the sample: five star nodes, 53 px wide).
    /// </remarks>
    private void TapStarOnScreen(IMauiElement element, int star)
    {
        var stars = element.FindElements(Locator.ByXPath("./*/*/*"));
        if (star < 1 || star > stars.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(star), star, $"RatingView '{Locator.Value}' shows {stars.Count} star(s); there is no star {star}.");
        }

        stars[star - 1].Click();
    }

    #region Guards

    /// <inheritdoc />
    protected override void EnsureSettableCore(IMauiElement element)
    {
        base.EnsureSettableCore(element);

        if (IsReadOnlyCore(element) == true)
        {
            throw new InvalidOperationException(
                $"RatingView '{Locator.Value}' is read-only and ignores taps.");
        }
    }

    #endregion

    #region Helpers

    private double? ReadNumber(IMauiElement? element, string property)
    {
        if (element?.ReadState(property) is not { } reported)
        {
            return null;
        }

        return double.TryParse(reported, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw new BruinellException(
                $"The rating view answered {property} with '{reported}', which is not a number. "
                + $"Locator: {Locator}");
    }

    #endregion
}
