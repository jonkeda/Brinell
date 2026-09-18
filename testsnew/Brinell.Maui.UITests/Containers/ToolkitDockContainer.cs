using Brinell.Maui.CommunityToolkit.Controls.Layouts;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// The CommunityToolkit <c>DockLayout</c> on the toolkit sample page, with its docked children
/// named.
/// </summary>
/// <remarks>
/// Every child resolves under the dock's root only, so each property is proof that the dock
/// scopes the search.
/// </remarks>
public class ToolkitDockContainer : DockLayout<CommunityToolkitTestPage, ToolkitDockContainer>
{
    public ToolkitDockContainer(IMauiScope<CommunityToolkitTestPage> parentScope, string automationId)
        : base(parentScope, automationId)
    {
    }

    /// <summary>The label docked to the top edge.</summary>
    public Label<ToolkitDockContainer> HeaderLabel => new(this, "DockHeaderLabel");

    /// <summary>The button docked to the left edge.</summary>
    public Button<ToolkitDockContainer> ActionButton => new(this, "DockActionButton");

    /// <summary>The label that fills the remaining space (the last child).</summary>
    public Label<ToolkitDockContainer> CenterLabel => new(this, "DockCenterLabel");

    /// <summary>The status label docked to the bottom edge; echoes the action button.</summary>
    public Label<ToolkitDockContainer> StatusLabel => new(this, "DockStatusLabel");
}
