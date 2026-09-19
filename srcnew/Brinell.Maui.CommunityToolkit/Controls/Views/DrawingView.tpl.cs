namespace Brinell.Maui.CommunityToolkit.Controls.Views;

/// <summary>
/// CommunityToolkit.Maui <c>DrawingView</c>: a surface the user draws lines on.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not in the Windows automation tree by id.</b> The view renders as an unnamed image, so this
/// control resolves it through the app's bridge declaration instead
/// (<see cref="IMauiElement.TryFindDeclared"/>). That element answers state reads and gestures
/// only, which is all this control asks of it.
/// </para>
/// <para>
/// <b>A stroke on Windows is arranged, not drawn.</b> The app declares <c>Pan,GetState</c> and a
/// sink adds one straight line through the view's own completion entry point, so the page sees a
/// finished line. A test of real drawing belongs on Android, where the pan is touch input.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class DrawingView<TScope> : Brinell.Maui.Controls.Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a drawing view control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the drawing view element.</param>
    public DrawingView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a drawing view control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID).</param>
    public DrawingView(IMauiScope<TScope> scope, string locatorValue)
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
        => new($"DrawingView was not found by '{Locator}' in the tree or among the app's bridge "
               + "declarations. On Windows the app must declare GetState on it.");

    #endregion

    #region Core Method Overrides

    /// <inheritdoc />
    /// <remarks>Asks the app first: the declared element has no tree visibility to read.</remarks>
    protected override bool? IsVisibleCore(IMauiElement? element)
        => ReadBool(element, "IsVisible") ?? base.IsVisibleCore(element);

    /// <inheritdoc />
    protected override bool? IsEnabledCore(IMauiElement? element)
        => ReadBool(element, "IsEnabled") ?? base.IsEnabledCore(element);

    #endregion

    #region DrawingView-Specific Core Methods

    /// <summary>
    /// Reads how many lines the view holds.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The line count, or null where the app does not report it.</returns>
    protected virtual int? GetLineCountCore(IMauiElement? element)
    {
        if (element?.ReadState("LineCount") is not { } reported)
        {
            return null;
        }

        return int.TryParse(reported, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
            ? count
            : throw new BrinellException(
                $"The drawing view answered LineCount with '{reported}', which is not a number. "
                + $"Locator: {Locator}");
    }

    /// <summary>
    /// Draws one straight line from the centre of the view by the given offset, and waits for the
    /// view to hold one more line.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="deltaX">Horizontal length in device-independent pixels.</param>
    /// <param name="deltaY">Vertical length in device-independent pixels.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void DrawLineCore(IMauiElement element, int deltaX, int deltaY, int? timeoutMs = null)
    {
        var before = GetLineCountCore(element);

        Context.Driver.PerformGesture(Locator.Value, MauiGesture.Pan, deltaX, deltaY);

        if (before is null)
        {
            return;
        }

        var confirmation = Confirm(() => GetLineCountCore(element), actual => actual > before, timeoutMs);
        if (!confirmation.IsConfirmed)
        {
            throw confirmation.Failure(Locator, "the stroke", lastError => new TimeoutException(
                $"DrawingView '{Locator.Value}' did not gain a line after the stroke.", lastError));
        }
    }

    #endregion

    #region Helpers

    private bool? ReadBool(IMauiElement? element, string property)
        => element?.ReadState(property) is { } reported && bool.TryParse(reported, out var value)
            ? value
            : null;

    #endregion
}
