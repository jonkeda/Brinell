using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Brinell.Bdd.VisualStudio;

/// <summary>The BDD Explorer tool window: a suite's steps and hooks, its pages, and its last results, from the language server.</summary>
[Guid(PackageIds.BddExplorerWindow)]
public sealed class BddExplorerWindow : ToolWindowPane
{
    /// <summary>Creates the window.</summary>
    public BddExplorerWindow()
        : base(null)
    {
        Caption = "BDD Explorer";
        Control = new ScenarioPreviewControl("BDD Explorer");
        Content = Control;
    }

    /// <summary>Fills the window: also when Visual Studio restores it at start-up, when no command shows it.</summary>
    public override void OnToolWindowCreated()
    {
        base.OnToolWindowCreated();
        (Package as BrinellBddPackage)?.Attach(this);
    }

    /// <summary>The window's content: the same WebView2 host as the preview's.</summary>
    public ScenarioPreviewControl Control { get; }
}
