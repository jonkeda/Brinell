using Brinell.Maui.Controls;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Outer container for nested container testing.
/// </summary>
public class OuterContainer : ContainerBase<ContainerDemoPage, OuterContainer>
{
    public OuterContainer(IMauiScope<ContainerDemoPage> parentScope, string automationId)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, automationId))
    {
    }

    /// <summary>
    /// Title label in the outer container.
    /// </summary>
    public Label<OuterContainer> OuterTitle => Label("OuterTitle");

    /// <summary>
    /// Outer action button.
    /// </summary>
    public Button<OuterContainer> OuterButton => Button("OuterButton");

    /// <summary>
    /// The inner nested container.
    /// </summary>
    public InnerContainer InnerFrame => new(this, "InnerFrame");
}
