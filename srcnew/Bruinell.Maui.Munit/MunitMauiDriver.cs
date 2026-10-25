using System.Text;
using Bruinell.Core.Locators;
using Bruinell.Maui.Enums;
using Bruinell.Maui.Interfaces;
using Microsoft.Maui.Controls;
using MUnit;
using MUnit.Inspection;
using MUnit.Rendering;
using MUnit.Snapshots;
using MauiElement = Microsoft.Maui.Controls.Element;

namespace Bruinell.Maui.Munit;

/// <summary>
/// Bruinell's MAUI driver over a page rendered by MUnit: lookup in the window's tree, navigation
/// through MAUI's own NavigationPage and Shell, the tree as page source. The same page objects and
/// controls that drive the app on Windows, Android and iOS drive it here, in memory.
/// </summary>
/// <remarks>
/// <para>
/// It reports <see cref="MauiPlatform.Windows"/>: Bruinell's driver defaults are the UI Automation
/// answers, and MUnit's tree is like that one (off-screen elements are in it, finding is not
/// seeing). A <c>Headless</c> platform value is an open question for Bruinell (.my/mUnit/proposal.md).
/// </para>
/// <para>Every lookup first runs the work the context has queued, so a poll sees settled state.</para>
/// </remarks>
public sealed class MunitMauiDriver : IMauiDriver
{
    private readonly IRenderedElement<MauiElement> _rendered;

    public MunitMauiDriver(IRenderedElement<MauiElement> rendered)
    {
        ArgumentNullException.ThrowIfNull(rendered);
        _rendered = rendered;
    }

    /// <summary>The MUnit context the page was rendered in.</summary>
    public MauiTestContext TestContext => _rendered.Context;

    /// <summary>The window the page was rendered into.</summary>
    public Window Window => _rendered.Window;

    internal Page CurrentPage => _rendered.CurrentPage;

    #region Lookup

    public IReadOnlyList<IMauiElement> FindElements(Locator locator) => Find(Window, locator);

    internal IReadOnlyList<IMauiElement> Find(MauiElement root, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return MunitLocators.Match(Read(root), locator)
            .Select(node => (IMauiElement)new MunitMauiElement(this, node.Element))
            .ToList();
    }

    internal VisualNode Read(MauiElement root) => ElementQueries.Read(root);

    #endregion

    #region Session, platform, the app

    public void Close()
    {
    }

    public void Quit()
    {
    }

    // No pixels: a failure's page source carries the tree instead.
    public byte[] GetScreenshot() => [];

    public byte[] TakeScreenshot() => [];

    public MauiPlatform Platform => MauiPlatform.Windows;

    public IMauiElement AppElement => new MunitMauiElement(this, Window, isApp: true);

    public string Context { get; set; } = "NATIVE_APP";

    public IReadOnlyCollection<string> Contexts => ["NATIVE_APP"];

    public string CurrentWindowHandle => "munit";

    public IReadOnlyCollection<string> WindowHandles => ["munit"];

    public string GetPageSource() => TreeSnapshot.Format(Read(Window));

    public string GetAutomationTree() => GetPageSource();

    #endregion

    #region Navigation

    public void NavigateTo(string destination)
    {
        if (Window.Page is not Shell)
            throw new DeviceOnlyException($"{nameof(NavigateTo)} without a Shell");
        _rendered.Navigation.GoTo(destination);
    }

    public void NavigateBack() => _rendered.Navigation.GoBack();

    public int NavigationDepth() => _rendered.Navigation.Stack.Count + _rendered.Navigation.ModalStack.Count;

    public bool IsIdle(int timeoutMs = 2000)
    {
        TestContext.Dispatcher.RunPending();
        return !TestContext.Dispatcher.HasPendingWork;
    }

    // The top page's identity is its AutomationId, the id a caller then looks it up by
    // (PageNavigation.WaitArrived); its type name only where it has none.
    public string CurrentRoute()
        => _rendered.Navigation.Location
           ?? (string.IsNullOrEmpty(CurrentPage.AutomationId) ? CurrentPage.GetType().Name : CurrentPage.AutomationId);

    public void Refresh() => TestContext.Dispatcher.RunPending();

    public void ResetAppState() => throw new DeviceOnlyException(nameof(ResetAppState));

    public object? ExecuteScript(string script, params object[] args) => throw new DeviceOnlyException(nameof(ExecuteScript));

    public void InvokeMenuItem(string automationId)
        => new MunitMauiElement(this, CurrentPage).InvokeToolbarItem(automationId);

    #endregion

    public void Dispose()
    {
    }

    public override string ToString() => new StringBuilder("MUnit: ").Append(CurrentPage.GetType().Name).ToString();
}
