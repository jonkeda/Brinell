namespace Brinell.Samples.Todo.UITests.Containers;

/// <summary>
/// The Todo app's <c>SectionCard</c>: a bordered card with a header around content the page
/// chooses.
/// </summary>
/// <remarks>
/// <para>
/// <b>A container.</b> The card fixes only its header (<c>SectionHeader</c>); the rest is the
/// page's. Lookups are strictly scoped to the card, which is why the detail page can name a label
/// <c>DueValue</c> inside one card without it being unique on the page.
/// </para>
/// <para>
/// Generic over its parent because both the detail and the edit page use it. The card is a
/// <c>ContentView</c>, so on Windows it is addressable only because the app registers the Brinell
/// automation handlers (probed 2026-09-18: each card is a group carrying its AutomationId).
/// </para>
/// </remarks>
/// <typeparam name="TParent">The page holding the card.</typeparam>
public sealed class SectionCard<TParent> : ContainerObjectBase<TParent, SectionCard<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates the card within the page.</summary>
    /// <param name="parentScope">The page.</param>
    /// <param name="automationId">The card's AutomationId.</param>
    public SectionCard(IMauiScope<TParent> parentScope, string automationId)
        : base(parentScope, automationId)
    {
    }

    /// <summary>The card's header.</summary>
    public Label<SectionCard<TParent>> Header => new(this, "SectionHeader");
}
