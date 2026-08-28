using Brinell.Maui.Containers;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the container module test view.
/// </summary>
/// <remarks>
/// Only the containers that are addressable on Windows get typed container objects.
/// <c>Frame</c>, <c>SwipeView</c>, and <c>RefreshView</c> are declared in the markup for
/// the planned Android/iOS phase but expose no <c>AutomationId</c> here, so they are
/// reached through <see cref="TryFindByAutomationId"/> and reported by the probe rather
/// than modelled as containers that would never resolve.
/// </remarks>
public class ContainerTestPage : PageObjectBase<ContainerTestPage>
{
    public ContainerTestPage(IMauiTestContext context)
        : base(context)
    {
        TestGrid = new Grid<ContainerTestPage>(this, "TestGrid");
        TestBorder = new Border<ContainerTestPage>(this, "TestBorder");
        TestContentView = new ContentView<ContainerTestPage>(this, "TestContentView");
        TestScrollView = new ScrollView<ContainerTestPage>(this, "TestScrollView");
    }

    /// <inheritdoc />
    public override string Name => "ContainerPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null) => PageTitle.IsExists();

    #region Observed state

    /// <summary>The page title label.</summary>
    public Label<ContainerTestPage> PageTitle => new(this, "ContainerPageTitle");

    /// <summary>The most recent container action.</summary>
    public Label<ContainerTestPage> Status => new(this, "ContainerStatusLabel");

    /// <summary>Reflects how many refreshes have completed.</summary>
    public Label<ContainerTestPage> RefreshText => new(this, "RefreshContentLabel");

    /// <summary>Completes a refresh without needing a pull gesture.</summary>
    public Button<ContainerTestPage> TriggerRefreshButton => new(this, "TriggerRefreshButton");

    /// <summary>Restores the initial state.</summary>
    public Button<ContainerTestPage> ResetButton => new(this, "ContainerResetButton");

    #endregion

    #region Containers

    /// <summary>A Grid holding four cells.</summary>
    public Grid<ContainerTestPage> TestGrid { get; }

    /// <summary>A Border wrapping a label and a button.</summary>
    public Border<ContainerTestPage> TestBorder { get; }

    /// <summary>A ContentView wrapping a label and a button.</summary>
    public ContentView<ContainerTestPage> TestContentView { get; }

    /// <summary>A bounded ScrollView that owns its own scrolling.</summary>
    public ScrollView<ContainerTestPage> TestScrollView { get; }

    /// <summary>The BoxView, which has no children by design.</summary>
    public Label<ContainerTestPage> TestBoxView => new(this, "TestBoxView");

    #endregion

    /// <summary>
    /// Resolves an element by automation id from page scope, or null.
    /// </summary>
    public IMauiElement? TryFindByAutomationId(string automationId)
    {
        try
        {
            return Context.TryFindElement(Locator.ByAutomationId(automationId));
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }
}
