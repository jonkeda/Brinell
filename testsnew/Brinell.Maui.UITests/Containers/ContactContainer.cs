using Brinell.Maui.Controls;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Container for a contact card.
/// Demonstrates indexed container without MauiListControl wrapper.
/// </summary>
public class ContactContainer : MauiContainerBase<ContainerDemoPage, ContactContainer>
{
    private readonly int _index;

    public ContactContainer(IMauiScope<ContainerDemoPage> parentScope, int index)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, $"Contact_{index}"))
    {
        _index = index;
    }

    /// <summary>
    /// Gets the 0-based index of this contact.
    /// </summary>
    public int Index => _index;

    /// <summary>
    /// The contact name label.
    /// </summary>
    public MauiControlBase<ContactContainer> NameLabel => new(this, "ContactName");

    /// <summary>
    /// The contact email label.
    /// </summary>
    public MauiControlBase<ContactContainer> EmailLabel => new(this, "ContactEmail");

    /// <summary>
    /// The call button.
    /// </summary>
    public MauiButtonControl<ContactContainer> CallButton => Button("ContactCallButton");
}
