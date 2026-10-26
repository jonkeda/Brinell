using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Bruinet.Bdd.VisualStudio;

/// <summary>The Scraper tool window: records an app's pages into a UI test project; the page is the language server's.</summary>
[Guid(PackageIds.ScraperWindow)]
public sealed class ScraperWindow : ToolWindowPane
{
    /// <summary>Creates the window.</summary>
    public ScraperWindow()
        : base(null)
    {
        Caption = "Scraper";
        Control = new ScenarioPreviewControl("Scraper");
        Content = Control;
    }

    /// <summary>Fills the window: also when Visual Studio restores it at start-up, when no command shows it.</summary>
    public override void OnToolWindowCreated()
    {
        base.OnToolWindowCreated();
        (Package as BruinetBddPackage)?.Attach(this);
    }

    /// <summary>The window's content: the same WebView2 host as the preview's.</summary>
    public ScenarioPreviewControl Control { get; }
}
