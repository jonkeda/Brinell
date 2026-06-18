using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Containers2;

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
    public Label<OuterContainer> OuterTitle => new(this,"OuterTitle");

    /// <summary>
    /// Outer action button.
    /// </summary>
    public Button<OuterContainer> OuterButton => new(this,"OuterButton");

    /// <summary>
    /// The inner nested container.
    /// </summary>
    public InnerContainer InnerBorder => new(this, "InnerBorder");
}
