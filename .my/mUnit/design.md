# MUnit design (agent document)

The exact names, API and planned skeletons. The why is in [proposal.md](proposal.md).

## Projects

| Project | Path | TFM | References |
| --- | --- | --- | --- |
| `MUnit` (PackageId `MUnit.Maui`) | `srcnew/MUnit` | net10.0 | `Microsoft.Maui.Controls` 10.0.110, `Microsoft.Extensions.DependencyInjection` |
| `Brinell.Maui.Munit` | `srcnew/Brinell.Maui.Munit` | net10.0 | `Brinell.Maui`, `MUnit` |
| `MUnit.Samples.App` | `samples/MUnit.Samples.App` | net10.0, `UseMaui` | `Microsoft.Maui.Controls` |
| `MUnit.Tests` | `testsnew/MUnit.Tests` | net10.0, xUnit 2.9.3 | `MUnit`, samples |
| `MUnit.Tests.NUnit` | `testsnew/MUnit.Tests.NUnit` | NUnit 4.3.2 + adapter 5.0.0 | `MUnit`, samples |
| `MUnit.Tests.MSTest` | `testsnew/MUnit.Tests.MSTest` | MSTest 3.9.3 | `MUnit`, samples |
| `Brinell.Maui.Munit.Tests` | `testsnew/Brinell.Maui.Munit.Tests` | net10.0, `UseMaui`, xUnit | head, `Brinell.Samples.Shared`; links the sample app's Buttons and Toggle XAML and view models, and UITests' `ButtonsTestPage` and `ToggleTestPage` |

## Dependency analysis

- **MUnit:** MAUI Controls (plain net10.0 assemblies: Controls.Core, Maui.Core, Essentials, Graphics, Controls.Xaml) and MS DI. No test framework, no AI SDK, no Brinell.
- **Brinell.Maui.Munit:** Brinell.Maui, which brings Appium.WebDriver and xunit.extensibility.core (an existing Brinell.Maui cost, not the head's), plus MUnit.
- **An app's test project:** MUnit, its framework of choice, and a MAUI class library of the app's pages, view models and services on `net10.0`. A platform-only app project cannot be referenced; its pages move to a library, or are linked as `Brinell.Maui.Munit.Tests` does.
- **Packages added to the root `Directory.Packages.props`:** `NUnit`, `NUnit3TestAdapter`, `MSTest.TestFramework`, `MSTest.TestAdapter`.

## Namespaces and types

| Namespace | Public types | Internal types |
| --- | --- | --- |
| `MUnit` | `MauiTestContext`, `MauiTestContextOptions` | `ApplicationScope` (nested) |
| `MUnit.Hosting` | `TestDispatcher`, `TestServiceProvider`, `InMemoryPreferences`, `InMemorySecureStorage` | `TestDispatcherProvider`, `TestDispatcherTimer`, `HeadlessHandler`, `HeadlessMauiContext` |
| `MUnit.Rendering` | `IRenderedElement<out T>`, `RenderOptions`, `TestNavigator` | `RenderedElement<T>` |
| `MUnit.Inspection` | `VisualNode`, `VisualTree`, `IElementAdapter`, `DefaultElementAdapter`, `ElementQueries`, `Screen`, `ElementNotFoundException` | `ItemRealizer` |
| `MUnit.Interaction` | `Interactions`, `ElementNotInteractableException` | |
| `MUnit.Assertions` | `ShouldExtensions`, `ElementAssertions<T>`, `MUnitAssertionException` | |
| `MUnit.Snapshots` | `TreeSnapshot`, `SnapshotOptions` | |
| `MUnit.Internals` | | `MauiInternals` |
| `Brinell.Maui.Munit` | `MunitMauiDriver`, `MunitMauiElement`, `MunitHead`, `DeviceOnlyException` | `MunitLocators` |

## Public API

```csharp
// Host and renderer
using var ctx = new MauiTestContext(new MauiTestContextOptions {
    Application = sp => new App(),          // optional: the app's resources and styles
    SetApplicationCurrent = true,           // default
    WaitTimeout = TimeSpan.FromSeconds(2),  // wall-clock budget for real async only
});
ctx.Services.AddSingleton<IClock>(fake);    // before the first Render
ctx.Services.Replace<IApi>(mock.Object);
ctx.Preferences / ctx.SecureStorage / ctx.Dispatcher / ctx.Application
IRenderedElement<T> ctx.Render<T>(Action<RenderOptions>? configure = null)   // T from DI or ActivatorUtilities
IRenderedElement<T> ctx.Render<T>(Func<IServiceProvider, T> factory, ...)
IRenderedElement<T> ctx.Render<T>(T instance, ...)
RenderOptions: .WithNavigation() .WithBindingContext(o) .WithoutAppearing()
ctx.AdvanceTime(TimeSpan)

// Rendered element (the window it is in)
T Instance; Window Window; Page CurrentPage; VisualNode Tree; TestNavigator Navigation
TEl Find<TEl>(string? automationId = null)          // exactly one, else ElementNotFoundException with the tree
IReadOnlyList<TEl> FindAll<TEl>(string? automationId = null)
Element FindByAutomationId(string id)
TEl FindByText<TEl>(string text)
string ToSnapshot(SnapshotOptions? options = null)
void WaitForAssertion(Action assertion, TimeSpan? timeout = null)
void WaitForState(Func<bool> predicate, TimeSpan? timeout = null)
// The same queries on any Element: element.Find<Label>(), element.FindAll<Entry>()

// Navigation
Navigation.CurrentPage / Stack / ModalStack / Location
Navigation.GoBack(); Navigation.GoTo("//route"); TPage Navigation.ShouldBeOn<TPage>()

// Interactions (each returns its control)
Button/ImageButton/MenuItem.Click()   View.Tap(taps = 1)
InputView.SetText(s) / ClearText()    Entry.Complete()   SearchBar.Search()
Switch.Toggle() / SetToggled(b)       CheckBox.Toggle() / Check() / Uncheck() / SetChecked(b)
RadioButton.Select()                  Slider.SlideTo(v)  Stepper.StepTo(v)
DatePicker.SetDate(d)  TimePicker.SetTime(t)   Picker.Select(item) / SelectIndex(i)
SelectableItemsView.Select(item) / Deselect(item)   RefreshView.Refresh()
Interactions.Act(element, verb, action)   // the extension point for custom controls

// Assertions
el.Should().HaveText(s).And.ContainText(s).BeVisible().NotBeVisible().BeEnabled().BeDisabled()
   .HaveAutomationId(s).HaveBindingContext<TVm>(pred?).BeBoundTo(Label.TextProperty, "Path").Satisfy(pred, "why")
switch.Should().BeOn()/BeOff()   checkBox.Should().BeChecked()/NotBeChecked()   items.Should().HaveItemCount(n)

// Brinell head
IMauiTestContext ctx2 = ctx.Render<ButtonsPage>().AsBrinellContext(timeouts?);
new ButtonsTestPage(ctx2).TestButton.Click();
```

## Core interfaces

| Interface | Role | Implemented by |
| --- | --- | --- |
| `IRenderedElement<out T>` | A rendered element and its window: queries, navigation, waits, snapshot | `RenderedElement<T>` |
| `IElementAdapter` | Children and text of a kind of element; the extension point for third-party controls | `DefaultElementAdapter`; a user's own, via `Options.Adapters` |
| MAUI's `IDispatcher` | Queue and virtual clock | `TestDispatcher` |
| MAUI's `IDispatcherProvider` | Process-wide; returns the running context's dispatcher | `TestDispatcherProvider` |
| MAUI's `IPreferences`, `ISecureStorage` | Essentials storage | `InMemoryPreferences`, `InMemorySecureStorage` |
| MAUI's `IViewHandler`, `IMauiContext` | Carry services to Shell and the application; map nothing | `HeadlessHandler`, `HeadlessMauiContext` |
| Brinell's `IMauiDriver`, `IMauiElement` | The head | `MunitMauiDriver`, `MunitMauiElement` |

## Render sequence

1. `Services.Build()` fixes the registrations.
2. The `Application` is made once, under `ApplicationGate`. `Current` is put back afterwards, and the application gets a `HeadlessHandler`.
3. Inside `Dispatcher.Run`, with `Application.Current` set to the app, the element is made: `factory(Services)`, or `ActivatorUtilities.GetServiceOrCreateInstance<T>`.
4. `BindingContext` is set from the options.
5. A `View` is wrapped in a `ContentPage`. With `InNavigationPage`, the page is wrapped in a `NavigationPage`. A `Shell` gets a `HeadlessHandler`.
6. `new Window(page)` is created, registered in a `ConditionalWeakTable` against the context, and opened with `MauiInternals.AddWindow(app, window)`.
7. With `Appear`, `page.SendAppearing()` is called. MAUI forwards it to the current page, then the queue drains.
8. Dispose calls `SendDisappearing`, then `RemoveWindow`.

## Dispatcher rules

- `IsDispatchRequired` is always false.
- `Dispatch` enqueues. `DispatchDelayed` and timers schedule on the virtual clock.
- `Run(action)` sets `s_running` and a `SynchronizationContext` that posts to the queue. On the outermost entry it calls `OnEnter`, which takes the `Application.Current` scope. It runs the action, then drains.
- A drain runs queued work until the queue is empty; a nested drain is a no-op. Every query, interaction, assertion and wait drains first.
- `AdvanceTime(by)` runs scheduled entries in (due, sequence) order, draining after each.
- `WaitFor(cond, timeout)` drains and checks inside the scope, then waits on the signal outside it.

## Brinell head mapping

| `IMauiElement` member | On MUnit |
| --- | --- |
| `Visible`, `Enabled` | Effective `IsVisible` and `IsEnabled` up the ancestors |
| `Text` | The adapter's text: `Label` spans, `InputView`, `Button`, `Picker` item, date and time formatted, `MenuItem`, `Page.Title`, `IText` |
| `Rect`, `Size`, `Location` | 1x1 at the origin when visible, empty when hidden |
| `Click`, `Invoke`, `Select` | Button, ImageButton or MenuItem click; Switch or CheckBox toggle; RadioButton select; a realized row selects its item; otherwise a tap |
| `SendKeys(Keys or Paste)`, `AppendText` | Appends to `Text`; `SetValue` replaces it |
| `Clear`, `Submit` | `ClearText`; `Entry.Complete` or `SearchBar.Search` |
| `Checked`, `SetChecked`, `Toggle` | Switch, CheckBox, RadioButton |
| `RangeValue`, `RangeMinimum`, `RangeMaximum`, `SetRangeValue` | Slider, Stepper, and ProgressBar (read only) |
| `SelectIndex`, `SelectByText`, `ReadItemTexts`, `SelectedItemText` | Picker, `SelectableItemsView` rows, `CarouselView.Position` |
| `SetDate`, `SetTime` | DatePicker, TimePicker, with min and max checked |
| `DoubleClick` | Tap with `NumberOfTapsRequired = 2` |
| `GetAttribute(name)` | Any `{name}Property` bindable property, as invariant text |
| `ScrollIntoView`, `Focus`, `ClearFocus` | No-ops |
| `RightClick`, `Hover`, `LongPress`, `Swipe` | `DeviceOnlyException` (`IEndsCall`) |
| App element `ReadAlert`, `TryFindActiveDialog` | null, because there are no alerts yet (Phase 4) |
| App element flyout members | `Shell.FlyoutIsPresented` or `FlyoutPage.IsPresented` |
| `IMauiDriver.FindElements` | `MunitLocators` |
| `NavigateTo` | `Shell.GoToAsync` |
| `NavigateBack` | `TestNavigator.GoBack` |
| `GetPageSource` | `TreeSnapshot` |
| `GetScreenshot` | An empty byte array |

Locator strategies:
- `AutomationId`, `Id`, `AccessibilityId`, `DataAutomationId` and `DataTestId` match the `AutomationId`.
- `Name` matches the semantic description, else `AutomationProperties.Name`.
- `Text` and `LinkText` match the text exactly; `PartialLinkText` matches a substring.
- `ClassName`, `TagName` and `ControlType` match the type name or any of its base types.
- The XPath `./*` gives the children.
- Any other strategy throws `DeviceOnlyException`.

## MAUI internals: used, and to explore

| Member | Used for | Status |
| --- | --- | --- |
| `Application.AddWindow(Window)`, `RemoveWindow(Window)` | Lifecycle; `Shell.Current` | Used (reflection) |
| `TapGestureRecognizer.SendTapped(View, Func<IElement, Point?>)` | `Tap` | Used (reflection) |
| `BindableObject.GetContext(BindableProperty)` and `.Bindings` | `BeBoundTo` | Used (reflection) |
| `Page.SendAppearing` and `SendDisappearing`, `Button.SendClicked`, `SendPressed` and `SendReleased`, `Entry.SendCompleted`, `ISearchBarController`, `ISliderController`, `IMenuItemController`, `IShellContentController.GetOrCreateContent` | Interactions, lifecycle | Public (`EditorBrowsable(Never)`) |
| `Window.AlertManager` and the internal `IAlertManagerSubscription` | `DisplayAlert`, action sheets, prompts | Explore in Phase 4 |
| `BindingDiagnostics.BindingFailed` | Turning binding errors into test failures | Explore in Phase 3 |
| `GroupableItemsView` grouping and `ListView` `TemplatedItemsList` | Realizing grouped rows and cells | Explore in Phase 1 |
| `VisualElement.IsLoaded`, `SendLoaded` and `OnLoaded` | `Loaded` and `Unloaded` | Explore in Phase 6 |
| `MauiAppBuilder` without a platform | Running the app's `MauiProgram` composition in tests | Explore: `CreateMauiApp` calls platform extensions |
| `ShellNavigationQueryParameters` and `IQueryAttributable` | Shell parameter tests | Explore in Phase 4 |

## Planned skeletons

### Phase 4: alerts answered from the test

```csharp
public sealed class TestDialogs   // ctx.Dialogs
{
    public Queue<object?> Answers { get; } = new();    // bool for DisplayAlert, string for sheets and prompts
    public IReadOnlyList<DialogRequest> Shown { get; }  // title, message, buttons, in order
    public DialogRequest ShouldHaveShown(string title);
}
// Wiring: subscribe on Window.AlertManager through the internal IAlertManagerSubscription that
// MauiInternals resolves, completing each request's TaskCompletionSource with the next answer.
// With no answer queued, fail the test naming the dialog.
```

### Phase 5: snapshots

```csharp
public static class SnapshotExtensions
{
    public static void ShouldMatchSnapshot(this IRenderedElement<Element> rendered,
        SnapshotOptions? options = null, string? name = null,
        [CallerFilePath] string file = "", [CallerMemberName] string member = "")
    {
        var text = rendered.ToSnapshot(options);
        var path = Path.Combine(Path.GetDirectoryName(file)!, "__snapshots__", (name ?? member) + ".txt");
        // missing or different -> write <path>.received.txt and throw MUnitAssertionException with a unified diff;
        // MUNIT_ACCEPT=1 -> overwrite <path> instead. Line endings "\n", UTF-8 without a BOM.
    }
}
```

The format is `TreeSnapshot`: one element per line, two-space indent, `Type #AutomationId "text"
[state]`. It never contains hash codes, positions or culture-dependent numbers beyond the app's own
text.

### Phase 6: fake handlers with a sizer

```csharp
public interface IHeadlessSizer { Size Measure(IView view, double widthConstraint, double heightConstraint); }

internal sealed class HeadlessViewHandler(IHeadlessSizer sizer) : ViewHandler<IView, object>(ViewHandler.ViewMapper)
{
    protected override object CreatePlatformView() => new();
    public override Size GetDesiredSize(double w, double h) => sizer.Measure(VirtualView, w, h);
}
// Opt-in through MauiTestContextOptions.Layout = new TextLengthSizer(charWidth: 8, lineHeight: 20).
// Register it for every leaf type through a HeadlessMauiContext whose Handlers factory returns it.
// Layouts keep their own handlers' logic by calling ILayoutManager on Measure and Arrange.
// After rendering, CrossPlatformMeasure and Arrange run on the window's size; bounds are then real
// (approximate) and MunitMauiElement.Rect reports them.
```

## Tests in the proof of concept

| File | Tests |
| --- | --- |
| `MUnit.Tests/Examples/SettingsPageTests.cs` | Label text on appear; `SaveButton_UpdatesStatus`; save disabled until a name is typed; disabled click refused; busy while saving (gated service); failure message; two-way bindings; `BeBoundTo` |
| `MUnit.Tests/Examples/GreetingViewTests.cs` | Click handler on a ContentView; a bindable property set by the test |
| `MUnit.Tests/Examples/DependencyInjectionTests.cs` | Real service over in-memory preferences; `Replace`; services fixed after render |
| `MUnit.Tests/Examples/NavigationTests.cs` | Select, push, back; rows from the template; toolbar reload and empty view; Shell `GoToAsync` from the app's handler, back, `GoTo` |
| `MUnit.Tests/Framework/DispatcherTests.cs` | Timer on the virtual clock; delayed order; an async void exception fails the test; `WaitForAssertion` on a real delay; 8 contexts in parallel |
| `MUnit.Tests/Framework/TreeTests.cs` | Snapshot text; effective state; miss message; ambiguity; hidden input refused; assertion message; MaxLength; tap command |
| `MUnit.Tests.NUnit/SettingsPageTests.cs` | Save through SetUp and TearDown; MUnit failure message under NUnit |
| `MUnit.Tests.MSTest/GreetingAndShellTests.cs` | Click; Shell help route; method-level parallel |
| `Brinell.Maui.Munit.Tests/SamplePagesOnMunitTests.cs` | `ButtonsTestPage`: command and status; `ToggleTestPage`: check, switch, radio; page root found |
