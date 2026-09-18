# Page objects

Page objects own the page's structure: which controls it has, where they are, and page-level
intent. They live in `testsnew/Brinell.Maui.UITests/Pages/`; containers and collections they
use live in `testsnew/Brinell.Maui.UITests/Containers/`.

## Shape

```csharp
using ToolkitMedia = Brinell.Maui.CommunityToolkit.Controls.Media;

namespace Brinell.Maui.UITests.Pages;

/// <summary>Page object for the CommunityToolkit MediaElement sample page.</summary>
public class CommunityToolkitMediaTestPage : PageObjectBase<CommunityToolkitMediaTestPage>
{
    public CommunityToolkitMediaTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "CommunityToolkitMediaPage";

    public ToolkitMedia.MediaElement<CommunityToolkitMediaTestPage> TestMediaElement => new(this, "TestMediaElement");
}
```

## Rules

- `public class FooPage : PageObjectBase<FooPage>`, constructor `(IMauiTestContext context)`.
  `Name` is the page root's `AutomationId`.
- **Controls are expression-bodied properties** that create a fresh object on each access:
  `public Switch<FooPage> TestSwitch => new(this, "TestSwitch");`. Never cache an element or a
  control instance in a field. (`GridCollectionDemoPage` still creates its containers once in
  the constructor; that is debt, not a model.)
- Components and framework containers are declared the same way:
  `public Popup<FooPage> TestPopup => new(this, "TestPopup");`.
- **Locators live here and only here.** Prefer the `AutomationId` string. Use
  `Locator.ByAccessibilityId` or `ByName` only where the platform gives nothing else. Never XPath.
- A page with a busy sentinel overrides `BusySignalPolicy => BusySignalPolicy.Required` and, if
  its sentinel is not the default, `BusyAutomationId`.
- **Window chrome outside every page** (toolbar items, Shell flyout, native alerts, menu bar,
  platform date pickers) is scoped to `AppRoot`, not the page: a control scoped to a page cannot
  be resolved while that page is not loaded, which for a back button is exactly when it is
  needed.

  ```csharp
  private readonly AppRoot _appRoot = new(context);

  public ToolbarButton<AppRoot> BackToHub => new(_appRoot, Locator.ByAccessibilityId("BackToHub"));
  ```

- **Page-level intent methods** (`FillProduct`, `Reset`) compose controls, wait for the
  resulting state, and return the page, or the next page for navigation. They contain no
  recovery loops, cleanup loops or back-navigation.
- **Regions become containers, repeated rows become collections**, in `Containers/`, derived
  from `ContainerObjectBase` / `CollectionObjectBase` or a framework container. See the
  `maui-control` skill's container and collection references for their rules; a page declares
  them as properties: `public ProductFormContainer Form => new(this, "ProductFormContainer");`.
- Public members of a page take ids and values, not `Locator`s, and return pages, containers,
  controls or values, never `IMauiElement`.

## Opening the page

Tests open pages through the fixture: `_fixture.Open(SamplePage.X)` navigates from the hub and
waits for the page to arrive. A new sample page needs the same `SamplePage` entry in the sample
app (`Navigation/SamplePage.cs`, registered in `Navigation/SamplePages.cs`) and in the test
project (`Pages/HubPage.cs`); the hub button's id, `Open_<SamplePage>`, follows from the name.
