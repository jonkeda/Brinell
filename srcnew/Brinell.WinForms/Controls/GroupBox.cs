namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms GroupBox container control.
/// Provides scoped child element finding within the group box.
/// </summary>
public sealed class GroupBox<TScope> : ControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public GroupBox(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public GroupBox(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }

    /// <summary>
    /// Finds a child element within this GroupBox by automation ID.
    /// </summary>
    public IWinFormsElement? FindChild(string automationId, int? timeoutMs = null)
    {
        try
        {
            var element = FindElement(timeoutMs);
            var childLocator = Locator.ByAutomationId(automationId);
            return element.FindElement(childLocator);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all child elements within this GroupBox.
    /// </summary>
    public IReadOnlyList<IWinFormsElement>? GetChildren(int? timeoutMs = null)
    {
        try
        {
            var element = FindElement(timeoutMs);
            return element.FindElements(Locator.ByXPath("*"));
        }
        catch
        {
            return null;
        }
    }
}
