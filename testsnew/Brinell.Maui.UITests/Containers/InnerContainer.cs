using Brinell.Maui.Controls;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Inner container nested inside OuterContainer.
/// Demonstrates nested container patterns.
/// </summary>
public class InnerContainer : MauiContainerBase<OuterContainer, InnerContainer>
{
    public InnerContainer(IMauiScope<OuterContainer> parentScope, string automationId)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, automationId))
    {
    }

    /// <summary>
    /// Title label in the inner container.
    /// </summary>
    public MauiControlBase<InnerContainer> InnerTitle => new(this, "InnerTitle");

    /// <summary>
    /// The entry field in the inner container.
    /// </summary>
    public MauiEntryControl<InnerContainer> InnerEntry => Entry("InnerEntry");

    /// <summary>
    /// The action button in the inner container.
    /// </summary>
    public MauiButtonControl<InnerContainer> InnerButton => Button("InnerButton");
}
