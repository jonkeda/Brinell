using Brinell.Maui.AppSupport.Uia;
using Brinell.Uia;

namespace Brinell.Samples.Maui.ShellApp;

public partial class AppShell : Shell
{
    /// <summary>The route a detail page pushes, and the one the stack tests pop.</summary>
    public const string DetailSubRoute = "sub";

    public AppShell()
    {
        InitializeComponent();

        // Pushed rather than declared: a route registered here is not part of the tab or
        // flyout structure, which is what makes it a stack entry to pop.
        Routing.RegisterRoute(DetailSubRoute, typeof(Pages.DetailSubPage));

        // The flyout is one property on this object and a chrome button nothing can find: MAUI
        // gives the hamburger no AutomationId, so every route to it from outside was a guess -
        // by name, by control type, or by clicking where it usually is. Declared on the Shell
        // rather than on a page because the flyout belongs to the Shell.
        AutomationId = "AppShell";
        GestureAutomation.SetVerbs(
            this,
            $"{nameof(BrinellVerb.OpenFlyout)},{nameof(BrinellVerb.CloseFlyout)},"
            + $"{nameof(BrinellVerb.GetState)}");

        HandlerChanged += (_, _) => PublishAutomationIdToThePlatformView();
    }

    /// <summary>
    /// Copies the Shell's <c>AutomationId</c> onto the view that draws it, where UI Automation looks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Stage G step 32: all thirteen Shell tests failed before their bodies ran</b>, with
    /// <i>"Page 'ShellSamplePage' is not loaded"</i>. The page object scopes every lookup under a
    /// root element, and measured, no element in this app's tree carried that id - nor
    /// <c>AppShell</c>, though the Shell sets it above. MAUI does not copy a Shell's
    /// <c>AutomationId</c> to its platform view, so the tab strip, the flyout and every page were
    /// all findable from the window and none of them from the root the page object asked for.
    /// </para>
    /// <para>
    /// The WinUI view has an automation peer of its own; it only lacks the id. Setting it is the
    /// whole fix, and it does not replace any peer - which is the move that collapses the tree.
    /// </para>
    /// </remarks>
    private void PublishAutomationIdToThePlatformView()
    {
#if WINDOWS
        if (Handler?.PlatformView is Microsoft.UI.Xaml.DependencyObject view)
        {
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(view, AutomationId);
        }
#endif
    }
}
