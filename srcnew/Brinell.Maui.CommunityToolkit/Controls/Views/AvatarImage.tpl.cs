using Brinell.Maui.Controls.Display;

namespace Brinell.Maui.CommunityToolkit.Controls.Views;

/// <summary>
/// The image an <see cref="AvatarView{TScope}"/> shows when an image source is set.
/// </summary>
/// <remarks>
/// Without an image source the avatar has no image element at all, and that is an answer, not a
/// failure: <c>IsShown</c> is false for a missing image. Plain <c>IsVisible</c> reads null for a
/// missing element, so <c>AssertVisible(false)</c> would never pass here.
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class AvatarImage<TScope> : Image<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates the image part within the specified scope.
    /// </summary>
    /// <param name="scope">The scope providing element finding, normally the avatar.</param>
    /// <param name="locator">The locator for the image.</param>
    public AvatarImage(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates the image part within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope providing element finding, normally the avatar.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID).</param>
    public AvatarImage(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Reads whether the image is shown.
    /// </summary>
    /// <param name="element">The pre-found element, or null when there is none.</param>
    /// <returns>True when shown; false when hidden or absent.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsShownCore(IMauiElement? element) => element?.Visible == true;

    #endregion
}
