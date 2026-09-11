namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// One tab in a <see cref="TabMenu{TParent}"/>.
/// </summary>
/// <remarks>
/// A tab is not one element: it is a surface holding a button and a caption, and which of
/// those carries the command or the text differs by platform. The tab is rooted at the
/// surface, so the item scopes both, and each member below reaches for the part that knows.
/// </remarks>
/// <typeparam name="TParent">The scope the tab menu belongs to.</typeparam>
public partial class TabItem<TParent> : Base.SelectableItemBase<TabMenu<TParent>, TabItem<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates a tab bound to a root the tab menu has already found.
    /// </summary>
    /// <param name="tabMenu">The owning tab menu.</param>
    /// <param name="itemRoot">The tab's root element.</param>
    /// <param name="index">The tab's zero-based position.</param>
    public TabItem(TabMenu<TParent> tabMenu, IMauiElement itemRoot, int index)
        : base(tabMenu, itemRoot, index)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Invokes the tab's button surface, falling back to the tab itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tab's root is a layout, and a layout has no command. The button inside it does.
    /// </para>
    /// <para>
    /// <b>Invoked rather than selected, unlike the rest of
    /// <see cref="Base.SelectableItemBase{TCollection, TSelf}"/>.</b> A tab is conceptually one
    /// of a group, but this tab bar is built from plain buttons - see
    /// <c>TabMenu_PlainButtonBar_ReportsNoSelection</c> - and a button exposes Invoke and no
    /// SelectionItem. Selection is reported separately rather than performed by the click.
    /// </para>
    /// <para>
    /// Measured, not assumed: inheriting <c>Select</c> failed with "'TabMenuView_Button' does not
    /// expose the UI Automation SelectionItemPattern". Under the activation ladder this control
    /// worked by falling through from SelectionItem to Invoke, so the mismatch was real all along
    /// and simply never said so.
    /// </para>
    /// </remarks>
    /// <param name="element">The tab's root element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        var target = TabMenuMarkup.ButtonWithin(element) ?? element;

        EnsureClickableCore(target);
        target.Invoke();
    }

    /// <summary>
    /// Reads the tab's caption from its caption label, then its button, then the tab itself.
    /// </summary>
    /// <remarks>
    /// Named <c>GetText</c> like every other item rather than <c>GetCaption</c>: one word for
    /// one idea across item types is worth more than a word that reads better on a tab. Where
    /// the text lives is the tab's business, not the caller's - the tab's own element reports
    /// an empty string on Windows.
    /// </remarks>
    /// <param name="element">The tab's root element.</param>
    protected override string? GetTextCore(IMauiElement element)
    {
        var caption = TabMenuMarkup.CaptionWithin(element)?.Text;
        if (!string.IsNullOrEmpty(caption)) return caption;

        var button = TabMenuMarkup.ButtonWithin(element)?.Text;
        if (!string.IsNullOrEmpty(button)) return button;

        return element.Text;
    }

    /// <summary>
    /// Whether this is the current tab.
    /// </summary>
    /// <remarks>
    /// Asked of the tab and then of its button, since either may be the surface the platform
    /// marks. A tab bar built from plain buttons marks neither and answers false - see
    /// <see cref="Base.SelectableItemBase{TCollection, TSelf}.IsMarkedSelected"/>.
    /// </remarks>
    /// <param name="element">The tab's root element (may be null).</param>
    protected override bool? IsSelectedCore(IMauiElement? element)
    {
        if (element == null) return null;

        return IsMarkedSelected(element)
               || IsMarkedSelected(TabMenuMarkup.ButtonWithin(element));
    }

    #endregion
}
