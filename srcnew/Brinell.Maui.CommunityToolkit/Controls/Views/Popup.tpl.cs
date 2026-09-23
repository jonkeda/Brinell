using Brinell.Maui.Containers;

namespace Brinell.Maui.CommunityToolkit.Controls.Views;

/// <summary>
/// CommunityToolkit.Maui <c>Popup</c>: a view shown modally over the page, scoped as a container.
/// </summary>
/// <remarks>
/// <para>
/// <b>Found from the app, not the page.</b> The toolkit shows a popup as a modal page, so its
/// root is not inside the page that raised it. On Windows it is still in the main window's tree,
/// with its own AutomationId, and its buttons answer Invoke - no bridge is involved.
/// </para>
/// <para>
/// <b>Not cached.</b> A popup opens and closes during a test, so every lookup finds it again.
/// </para>
/// <para>
/// Subclass it for a popup with known content, as with <c>ContentView</c>, or use
/// <see cref="Popup{TParent}"/> with <c>Label(id)</c> and <c>Button(id)</c>.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The scope that raises the popup, usually the page.</typeparam>
/// <typeparam name="TSelf">The popup type itself (self-referencing for fluent returns).</typeparam>
public partial class Popup<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : Popup<TParent, TSelf>
{
    /// <summary>
    /// Creates a popup container that the given scope raises.
    /// </summary>
    public Popup(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a popup container using the scope's default locator strategy.
    /// </summary>
    public Popup(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }

    /// <inheritdoc />
    protected override bool CacheContainerRoot => false;

    /// <inheritdoc />
    protected override bool AsksParent => false;

    /// <inheritdoc />
    protected override IMauiElement FindContainerRootElement()
        => Context.AppElement.TryFindElement(Locator)
            ?? throw new ElementNotFoundException($"No open popup was found by '{Locator}'.");

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Whether the popup is on screen.
    /// </summary>
    /// <param name="element">The popup root, or null when it is not open.</param>
    /// <returns>True when open, false when not.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsOpenCore(IMauiElement? element) => element != null;

    #endregion

    #region Hand-written: across parts

    /// <summary>
    /// Presses a button inside the popup that closes it, then waits for the popup to go.
    /// </summary>
    /// <remarks>
    /// Across two parts: the button, and the popup root that should disappear. Two calls in
    /// sequence, each its own unit of work.
    /// </remarks>
    /// <param name="buttonAutomationId">The AutomationId of the closing button.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds, for each of the two steps.</param>
    /// <returns>This popup, for chaining (its members now report it closed).</returns>
    /// <exception cref="TimeoutException">The popup was still open after the press.</exception>
    public TSelf CloseWith(string buttonAutomationId, int? timeoutMs = null)
    {
        Button(buttonAutomationId).Click(timeoutMs);

        if (!WaitOpen(false, timeoutMs))
        {
            throw new TimeoutException(
                $"Popup '{Locator.Value}' was still open after pressing '{buttonAutomationId}'.");
        }

        return Self;
    }

    #endregion
}

/// <summary>
/// A <see cref="Popup{TParent, TSelf}"/> for use where no popup-specific subclass is needed.
/// </summary>
/// <typeparam name="TParent">The scope that raises the popup.</typeparam>
public sealed partial class Popup<TParent> : Popup<TParent, Popup<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates a popup container that the given scope raises.</summary>
    public Popup(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>Creates a popup container using the scope's default locator strategy.</summary>
    public Popup(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}
