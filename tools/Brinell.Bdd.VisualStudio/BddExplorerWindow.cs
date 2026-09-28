using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Brinell.Bdd.VisualStudio;

/// <summary>The UAT Explorer tool window: a suite's steps and hooks, its pages, and its last results, from the language server.</summary>
[Guid(PackageIds.BddExplorerWindow)]
public sealed class BddExplorerWindow : ToolWindowPane
{
    /// <summary>Creates the window.</summary>
    public BddExplorerWindow()
        : base(null)
    {
        Caption = "UAT Explorer";
        Control = new ScenarioPreviewControl();
        Content = Control;
    }

    /// <summary>The window's content: the same WebView2 host as the preview's.</summary>
    public ScenarioPreviewControl Control { get; }
}
