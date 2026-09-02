namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// The pages the sample app's hub can open.
/// </summary>
/// <remarks>
/// Mirrors <c>Brinell.Samples.Maui.App.Navigation.SamplePage</c>. Duplicated rather than
/// shared because the UI tests drive the app through its UI and must not reference its
/// assembly — a test that could call into the app would stop being a UI test. The member
/// names are the contract, and <see cref="HubPage.AutomationIdFor"/> derives the same id the
/// app does, so a rename on either side fails visibly rather than drifting.
/// </remarks>
public enum SamplePage
{
    Buttons,
    DateTime,
    Display,
    Range,
    Selection,
    Text,
    Toggle,
    Container,
    Collection,
    GridCollection,
    Shapes,
    Dialogs,
    Navigation,
    Scroll,
    AutomationProbe
}

/// <summary>
/// Page object for the sample app's hub: the flat page list that replaced Shell navigation.
/// </summary>
/// <remarks>
/// Opening a page is one click on all three platforms. Shell's tab bar was not uniform —
/// Android hid tabs past the fifth behind an overflow menu and Windows exposed only nine —
/// and its pushed routes leaked navigation state between tests
/// (see <c>.my/maui/rca/rca-001-container-module-tests-navigation-stack.md</c>).
/// </remarks>
public class HubPage : PageObjectBase<HubPage>
{
    public HubPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "PageHub";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null) => Title.IsExists();

    /// <inheritdoc />
    /// <remarks>
    /// This page object resolves one element that is deliberately <em>not</em> on the hub:
    /// <see cref="BackToHub"/>, which lives on whichever page is currently open. Gating
    /// lookups on the hub being loaded would make <see cref="TryGoBack"/> unable to find the
    /// only control that gets back to it — the hub reports not-loaded precisely when the back
    /// button is needed. See
    /// <c>.my/maui/rca/rca-002-page-precondition-discarded-slow-failures.md</c> for the
    /// precondition this opts out of.
    /// </remarks>
    protected override bool RequiresLoadedPage => false;

    /// <summary>The hub's title label.</summary>
    public Label<HubPage> Title => new(this, "PageHubTitle");

    /// <summary>
    /// The AutomationId of the button that opens a page.
    /// </summary>
    /// <remarks>
    /// The same convention the app applies in <c>SamplePages.AutomationIdFor</c>. Derived on
    /// both sides rather than written twice, so the two cannot fall out of step silently.
    /// </remarks>
    public static string AutomationIdFor(SamplePage page) => $"Open_{page}";

    /// <summary>
    /// The button that opens the given page.
    /// </summary>
    public Button<HubPage> OpenButton(SamplePage page) => new(this, AutomationIdFor(page));

    /// <summary>
    /// The "back to hub" toolbar item the hub adds to every page it opens.
    /// </summary>
    /// <remarks>
    /// Located by accessibility id, not automation id. A <c>ToolbarItem</c> is rendered into
    /// native chrome rather than page content, and MAUI surfaces its AutomationId there as the
    /// accessibility label — on Android the node's <c>resource-id</c> is empty and the value
    /// appears in <c>content-desc</c>. AutomationId maps to <c>resource-id</c> on Android, so
    /// looking it up that way finds nothing. AccessibilityId is the same string on both
    /// platforms, so one locator serves all three.
    /// </remarks>
    public Button<HubPage> BackToHub => new(this, Locator.ByAccessibilityId("BackToHub"));

    /// <summary>
    /// Clicks "back to hub" if it is present.
    /// </summary>
    /// <remarks>
    /// Reports absence rather than throwing: the caller is unwinding to a known state and
    /// "there is nothing to go back from" is an answer, not a failure. Preferred over
    /// <c>IMauiDriver.NavigateBack</c>, whose Windows fallback is Alt+Left — global keyboard
    /// input, which the interaction policy blocks by default.
    /// </remarks>
    /// <param name="timeoutMs">How long to wait for the item to be present.</param>
    /// <returns>True when the item was found and clicked.</returns>
    public bool TryGoBack(int? timeoutMs = null)
    {
        if (!BackToHub.WaitExists(true, timeoutMs))
        {
            return false;
        }

        BackToHub.Click();
        return true;
    }
}
