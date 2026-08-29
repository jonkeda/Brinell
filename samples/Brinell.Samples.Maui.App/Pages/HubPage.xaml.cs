using Brinell.Samples.Maui.App.Navigation;

namespace Brinell.Samples.Maui.App;

/// <summary>
/// The sample app's entry point: one button per registered page.
/// </summary>
public partial class HubPage : ContentPage
{
    public HubPage()
    {
        InitializeComponent();
        BuildPageList();
    }

    /// <summary>
    /// Creates one button per registry entry.
    /// </summary>
    /// <remarks>
    /// Built in code rather than bound to a CollectionView: a bound item template would
    /// give every button the same AutomationId, which is right for testing item scoping but
    /// wrong here — each button needs its own id so a test can open one page directly.
    /// </remarks>
    private void BuildPageList()
    {
        foreach (var entry in SamplePages.All)
        {
            PageList.Add(CreateButton(entry));
        }
    }

    private Button CreateButton(SamplePageEntry entry)
    {
        var button = new Button
        {
            // Derived from the enum member, never hand-written, so the app and the tests
            // cannot drift apart.
            AutomationId = entry.AutomationId,
            Text = entry.Title,
            HorizontalOptions = LayoutOptions.Fill,
        };

        button.Clicked += async (_, _) => await OpenAsync(entry);

        return button;
    }

    /// <summary>
    /// Opens a page by pushing it onto the navigation stack.
    /// </summary>
    /// <remarks>
    /// The page is constructed here, per open, so nothing carries over from a previous visit.
    /// Shell reused page instances, which is why the container tests needed an explicit reset
    /// to be order-independent (see rca-001).
    /// </remarks>
    private async Task OpenAsync(SamplePageEntry entry)
    {
        if (Navigation == null) return;

        var page = entry.Create();
        AddBackToHub(page);

        await Navigation.PushAsync(page);
    }

    /// <summary>
    /// Adds a "Back to hub" toolbar item to a page being opened.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Added here rather than in each page's XAML so a page stays unaware of how it was
    /// reached, and so a new page gets the affordance for free.
    /// </para>
    /// <para>
    /// It exists because returning to the hub must be a *click*. The driver's back navigation
    /// falls back to Alt+Left on Windows, which is global keyboard input and blocked by the
    /// interaction policy — and a synthesized shortcut is not what a user does anyway. A
    /// toolbar item carrying an AutomationId is one addressable element on every platform.
    /// </para>
    /// </remarks>
    private void AddBackToHub(ContentPage page)
    {
        page.ToolbarItems.Add(new ToolbarItem
        {
            Text = "Back",
            AutomationId = BackToHubAutomationId,
            Command = new Command(async () =>
            {
                if (page.Navigation != null)
                {
                    await page.Navigation.PopAsync();
                }
            })
        });
    }

    /// <summary>
    /// The AutomationId of the "back to hub" item present on every opened page.
    /// </summary>
    public const string BackToHubAutomationId = "BackToHub";
}
