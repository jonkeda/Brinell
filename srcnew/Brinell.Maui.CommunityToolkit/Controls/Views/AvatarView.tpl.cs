namespace Brinell.Maui.CommunityToolkit.Controls.Views;

/// <summary>
/// CommunityToolkit.Maui <c>AvatarView</c>: initials or an image in a bordered shape.
/// </summary>
/// <remarks>
/// Read from the automation tree alone, no bridge: on Windows the avatar is a group whose child
/// is a text element carrying the initials, or an image element when an image source is set.
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class AvatarView<TScope> : Brinell.Maui.Controls.Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates an avatar view control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the avatar element.</param>
    public AvatarView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates an avatar view control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID).</param>
    public AvatarView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Reads the text the avatar shows, typically initials.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The shown text, or null when the avatar shows no text.</returns>
    [GenerateComparisons(Comparison.Equals | Comparison.Empty)]
    protected virtual string? GetTextCore(IMauiElement? element)
    {
        if (element == null)
        {
            return null;
        }

        var text = element.FindElements(Locator.ByControlType("text")).FirstOrDefault();
        return text?.Name ?? text?.Text;
    }

    /// <summary>
    /// Reads whether the avatar shows an image rather than text.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True when an image is shown, null when the element is absent.</returns>
    protected virtual bool? IsShowingImageCore(IMauiElement? element)
        => element?.FindElements(Locator.ByControlType("image")).Count > 0;

    #endregion
}
