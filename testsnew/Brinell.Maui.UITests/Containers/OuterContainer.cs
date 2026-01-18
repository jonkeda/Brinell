using Brinell.Maui.Controls;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Outer container for nested container testing.
/// </summary>
public class OuterContainer : MauiContainerBase<ContainerDemoPage, OuterContainer>
{
    public OuterContainer(IMauiScope<ContainerDemoPage> parentScope, string automationId)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, automationId))
    {
    }

    /// <summary>
    /// Title label in the outer container.
    /// </summary>
    public MauiControlBase<OuterContainer> OuterTitle => new(this, "OuterTitle");

    /// <summary>
    /// Outer action button.
    /// </summary>
    public MauiButtonControl<OuterContainer> OuterButton => Button("OuterButton");

    /// <summary>
    /// The inner nested container.
    /// </summary>
    public InnerContainer InnerFrame => new(this, "InnerFrame");
}
