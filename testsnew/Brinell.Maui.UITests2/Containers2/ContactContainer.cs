using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Containers2;

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
    public Label<ContactContainer> NameLabel => new(this,"ContactName");

    /// <summary>
    /// The contact email label.
    /// </summary>
    public Label<ContactContainer> EmailLabel => new(this,"ContactEmail");

    /// <summary>
    /// The call button.
    /// </summary>
    public Button<ContactContainer> CallButton => new(this,"ContactCallButton");
}
