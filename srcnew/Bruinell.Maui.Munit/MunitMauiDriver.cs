using System.Text;
using Bruinell.Core.Exceptions;
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
    private static int s_nextHandle;

    private readonly IRenderedElement<MauiElement> _rendered;
    private readonly string _handle = "munit-" + Interlocked.Increment(ref s_nextHandle);

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

    /// <summary>
    /// The elements under <paramref name="root"/> that <paramref name="locator"/> names. An element
    /// with <c>IsVisible</c> false (or under one) is left out, as Windows, Android and iOS leave it
    /// out of their trees; one merely off screen, like a carousel's other cards, stays, as on
    /// Windows. With <paramref name="includeHidden"/> it is found anyway, for <c>TryFindDeclared</c>.
    /// </summary>
    internal IReadOnlyList<IMauiElement> Find(MauiElement root, Locator locator, bool includeHidden = false)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return MunitLocators.Match(Read(root), locator)
            .Where(node => includeHidden || !IsHidden(node.Element))
            .Select(node => (IMauiElement)new MunitMauiElement(this, node.Element))
            .ToList();
    }

    private static bool IsHidden(MauiElement element)
    {
        for (var walker = element; walker is not null; walker = walker.Parent)
        {
            if (walker is VisualElement { IsVisible: false })
                return true;
        }

        return false;
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

    /// <summary>Shell's tab bar and flyout, as MUnit's tree shows them (<see cref="ShellTabBar"/>, <see cref="ShellFlyoutList"/>).</summary>
    public ShellChromeLocators ShellChrome { get; } = new(
        TabHost: Locator.ByClassName(nameof(ShellTabBar)),
        Tab: Locator.ByClassName(nameof(ShellSection)),
        FlyoutHost: Locator.ByClassName(nameof(ShellFlyoutList)),
        FlyoutItem: Locator.ByClassName(nameof(ShellItem)));

    public IMauiElement AppElement => new MunitMauiElement(this, Window, isApp: true);

    public string Context { get; set; } = "NATIVE_APP";

    public IReadOnlyCollection<string> Contexts => ["NATIVE_APP"];

    // One per rendered window, so two apps in one process are told apart, as two windows are.
    public string CurrentWindowHandle => _handle;

    public IReadOnlyCollection<string> WindowHandles => [_handle];

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

    public void NavigateBack()
    {
        try
        {
            _rendered.Navigation.GoBack();
        }
        catch (InvalidOperationException nothingBehind)
        {
            throw new BruinellException(nothingBehind.Message);
        }
    }

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

    /// <summary>
    /// A menu bar or context menu item by its <c>AutomationId</c>, raised without opening a menu, as
    /// the Windows bridge raises it: MAUI shows neither menu in any tree a test could walk.
    /// </summary>
    public void InvokeMenuItem(string automationId)
    {
        var page = CurrentPage;
        var menus = page.MenuBarItems.SelectMany(Flatten)
            .Concat(Read(Window).DescendantsAndSelf()
                .Select(node => node.Element)
                .OfType<Microsoft.Maui.Controls.BindableObject>()
                .Select(FlyoutBase.GetContextFlyout)
                .OfType<MenuFlyout>()
                .SelectMany(Flatten));
        Raise(menus.OfType<MenuItem>().FirstOrDefault(item => item.AutomationId == automationId), automationId, "menu item");
    }

    /// <summary>A toolbar item of the current page or a page that contains it, by its <c>AutomationId</c>.</summary>
    internal void InvokeToolbarItem(string automationId)
    {
        var items = new List<ToolbarItem>();
        for (Element? walker = CurrentPage; walker is not null; walker = walker.Parent)
        {
            if (walker is Page level)
                items.AddRange(level.ToolbarItems);
        }

        Raise(items.FirstOrDefault(item => item.AutomationId == automationId), automationId, "toolbar item");
    }

    private void Raise(MenuItem? item, string automationId, string kind)
    {
        if (item is null)
            throw new BruinellException($"No {kind} on the page carries that AutomationId: '{automationId}'.");
        if (!item.IsEnabled)
            throw new ElementNotReadyException(Locator.ByAutomationId(automationId), NotReadyReason.Disabled, $"the {kind} is disabled");

        TestContext.Dispatcher.Run(((IMenuItemController)item).Activate);
    }

    private static IEnumerable<IMenuElement> Flatten(IEnumerable<IMenuElement> entries)
    {
        foreach (var entry in entries)
        {
            yield return entry;
            if (entry is MenuFlyoutSubItem submenu)
            {
                foreach (var deeper in Flatten(submenu))
                    yield return deeper;
            }
        }
    }

    #endregion

    #region Gestures

    public bool SupportsGesture(string automationId, MauiGesture gesture)
        => Declared(automationId) is { } element && element.SupportsGesture(gesture);

    public void PerformGesture(string automationId, MauiGesture gesture, int arg1, int arg2)
        => (Declared(automationId) ?? throw new BruinellException($"No element carries the AutomationId '{automationId}'."))
            .PerformGesture(gesture, arg1, arg2);

    private MunitMauiElement? Declared(string automationId)
        => Find(Window, Locator.ByAutomationId(automationId), includeHidden: true).FirstOrDefault() as MunitMauiElement;

    #endregion

    public void Dispose()
    {
    }

    public override string ToString() => new StringBuilder("MUnit: ").Append(CurrentPage.GetType().Name).ToString();
}
