using Brinell.Maui.Controls;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Container for a contact card.
/// Demonstrates indexed container without List wrapper.
/// </summary>
public class ContactContainer : ContainerBase<ContainerDemoPage, ContactContainer>
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
    public ControlBase<ContactContainer> NameLabel => new Control<ContactContainer>(this, "ContactName");

    /// <summary>
    /// The contact email label.
    /// </summary>
    public ControlBase<ContactContainer> EmailLabel => new Control<ContactContainer>(this, "ContactEmail");

    /// <summary>
    /// The call button.
    /// </summary>
    public Button<ContactContainer> CallButton => Button("ContactCallButton");
}
