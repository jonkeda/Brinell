namespace Brinell.Maui.UITests.Containers2;

/// <summary>
/// Inner container nested inside OuterContainer.
/// Demonstrates nested container patterns.
/// </summary>
public class InnerContainer : ContainerBase<OuterContainer, InnerContainer>
{
    public InnerContainer(IMauiScope<OuterContainer> parentScope, string automationId)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, automationId))
    {
    }

    /// <summary>
    /// Title label in the inner container.
    /// </summary>
    public Label<InnerContainer> InnerTitle => new(this,"InnerTitle");

    /// <summary>
    /// The entry field in the inner container.
    /// </summary>
    public Entry<InnerContainer> InnerEntry => new(this,"InnerEntry");

    /// <summary>
    /// The action button in the inner container.
    /// </summary>
    public Button<InnerContainer> InnerButton => new(this,"InnerButton");
}
