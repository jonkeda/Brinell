# MauiUnit design (agent document)

The exact names, API and planned skeletons. The why is in [proposal.md](proposal.md).

## Projects

| Project | Path | TFM | References |
| --- | --- | --- | --- |
| `MauiUnit` (PackageId `MauiUnit`) | `srcnew/MauiUnit` | net10.0 | `Microsoft.Maui.Controls` 10.0.110, `Microsoft.Extensions.DependencyInjection` |
| `Bruinet.Maui.MauiUnit` | `srcnew/Bruinet.Maui.MauiUnit` | net10.0 | `Bruinet.Maui`, `MauiUnit` |
| `MauiUnit.Samples.App` | `samples/MauiUnit.Samples.App` | net10.0, `UseMaui` | `Microsoft.Maui.Controls` |
| `MauiUnit.Tests` | `testsnew/MauiUnit.Tests` | net10.0, xUnit 2.9.3 | `MauiUnit`, samples |
| `MauiUnit.Tests.NUnit` | `testsnew/MauiUnit.Tests.NUnit` | NUnit 4.3.2 + adapter 5.0.0 | `MauiUnit`, samples |
| `MauiUnit.Tests.MSTest` | `testsnew/MauiUnit.Tests.MSTest` | MSTest 3.9.3 | `MauiUnit`, samples |
| `Bruinet.Maui.MauiUnit.Tests` | `testsnew/Bruinet.Maui.MauiUnit.Tests` | net10.0, `UseMaui`, xUnit | head, `Bruinet.Samples.Shared`; links the sample app's Buttons and Toggle XAML and view models, and UITests' `ButtonsTestPage` and `ToggleTestPage` |
| `Bruinet.Samples.Maui.App.Headless`, `.ShellApp.Headless` | `samples/` | net10.0, `UseMaui` | Both sample apps' sources and XAML linked, minus `Platforms\` and `MauiProgram`; AppSupport (now also `net10.0`), the toolkit |
| `Bruinet.Maui.UITests` (the device suite) | `testsnew/Bruinet.Maui.UITests` | net10.0-windows, xUnit | Also references `Bruinet.Maui.MauiUnit` and both headless apps; `BRUINET_HEAD=mauiunit` runs it in memory through the fixtures' `CreateInMemoryOptions` |

## Dependency analysis

- **MauiUnit:** MAUI Controls (plain net10.0 assemblies: Controls.Core, Maui.Core, Essentials, Graphics, Controls.Xaml) and MS DI. No test framework, no AI SDK, no Bruinet.
- **Bruinet.Maui.MauiUnit:** Bruinet.Maui, which brings Appium.WebDriver and xunit.extensibility.core (an existing Bruinet.Maui cost, not the head's), plus MauiUnit.
- **An app's test project:** MauiUnit, its framework of choice, and a MAUI class library of the app's pages, view models and services on `net10.0`. A platform-only app project cannot be referenced; its pages move to a library, or are linked as `Bruinet.Maui.MauiUnit.Tests` does.
- **Packages added to the root `Directory.Packages.props`:** `NUnit`, `NUnit3TestAdapter`, `MSTest.TestFramework`, `MSTest.TestAdapter`.

## Namespaces and types

| Namespace | Public types | Internal types |
| --- | --- | --- |
| `MauiUnit` | `MauiTestContext`, `MauiTestContextOptions` | `ApplicationScope` (nested) |
| `MauiUnit.Hosting` | `TestDispatcher`, `TestServiceProvider`, `InMemoryPreferences`, `InMemorySecureStorage` | `TestDispatcherProvider`, `TestDispatcherTimer`, `HeadlessHandler`, `HeadlessMauiContext` |
| `MauiUnit.Rendering` | `IRenderedElement<out T>`, `RenderOptions`, `TestNavigator` | `RenderedElement<T>` |
| `MauiUnit.Inspection` | `VisualNode`, `VisualTree`, `IElementAdapter`, `DefaultElementAdapter`, `ElementQueries`, `Screen`, `ShellTabBar`, `ShellFlyoutList`, `ElementNotFoundException` | `ItemRealizer`, `ShellChromeNodes` |
| `MauiUnit.Interaction` | `Interactions`, `ElementNotInteractableException`, `InteractionRefusal` | |
| `MauiUnit.Dialogs` | `TestDialog`, `DialogKind`, `DialogRequests` | |
| `MauiUnit.Assertions` | `ShouldExtensions`, `ElementAssertions<T>`, `MauiUnitAssertionException` | |
| `MauiUnit.Snapshots` | `TreeSnapshot`, `SnapshotOptions` | |
| `MauiUnit.Internals` | | `MauiInternals` |
| `Bruinet.Maui.MauiUnit` | `MauiUnitDriver`, `MauiUnitElement`, `MauiUnitHead`, `DeviceOnlyException` | `MauiUnitLocators`, `MauiUnitState`, `MauiUnitAppBridge`, `MauiUnitRefusals` |

The tree a window shows, top down:
1. Open dialogs, top first.
2. Then the visible page: the top modal, else `Window.Page` down through NavigationPage, Shell and TabbedPage.

A Shell's children are its flyout (while presented), then its tab bar (while the current item has more than one section), then its page. A page's children are its toolbar items, then its content. An items view's children are its header, its rows (or its empty view), then its footer. A carousel's cards other than the one at `Position` read as not visible.

Interactions added: `Swipe(direction)`, `SetFocus()`, `ClearFocus()`, `ShellSection.Select()`, `ShellItem.Select()`, and `Tap(taps)`. Dialogs: `TestDialog.Choose(text)` and `Type(text)`; `DialogRequests.Of(window)` and `Top(window)`.

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

// Bruinet head
IMauiTestContext ctx2 = ctx.Render<ButtonsPage>().AsBruinetContext(timeouts?);
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
| Bruinet's `IMauiDriver`, `IMauiElement` | The head | `MauiUnitDriver`, `MauiUnitElement` |

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

## Bruinet head mapping

Every action runs through `MauiUnitRefusals`: a refusal for a hidden element is an `ElementNotReadyException` (`NotVisible`), one for a disabled element is `ElementNotReadyException` (`Disabled`), and any other refusal is a `BruinetException` with MauiUnit's reason.

| `IMauiElement` member | On MauiUnit |
| --- | --- |
| `Visible`, `Enabled` | Effective `IsVisible` and `IsEnabled` up the ancestors; a carousel card off screen is not visible |
| `Text` | The adapter's text: `Label` spans, `InputView`, `Button`, `Picker` item, date and time formatted, `MenuItem`, a Shell tab's or item's title, `Page.Title`, `IText` |
| `Name` | Semantic description, else `AutomationProperties.Name`, else a button's or label's text, a menu item's text, a tab's title, a dialog's title |
| `Rect`, `Size`, `Location` | 1x1 at the origin when visible, empty when hidden |
| `Click`, `Invoke`, `Select` | Button, ImageButton or MenuItem click; Switch or CheckBox toggle; RadioButton select; Shell tab or flyout item select; a realized row selects its item; the app's sink when it names Tap; otherwise a tap |
| `SendKeys(Keys or Paste)`, `AppendText` | Appends to `Text`; `SetValue` replaces it |
| `Clear`, `Submit` | `ClearText`; `Entry.Complete` or `SearchBar.Search` |
| `Checked`, `SetChecked`, `Toggle` | Switch, CheckBox, RadioButton |
| `RangeValue`, `RangeMinimum`, `RangeMaximum`, `SetRangeValue` | Slider, Stepper, and ProgressBar (read only) |
| A Stepper's `FindElements(ClassName RepeatButton)` or `(ControlType button)` | Its two parts, `-` then `+`, each stepping by `Increment`; disabled at its bound |
| `ReadState(name)` | The Windows bridge's names and formats (`MauiUnitState`), then the app's sink for `GetState` (`MauiUnitAppBridge`), else `NotSupportedException` |
| `PerformGesture` (element and driver) | The app's sink when it names the verb; else tap and double tap through recognizers, and swipes through MauiUnit's `Swipe`; long press, pan and pinch need a sink |
| `SelectIndex`, `SelectByText`, `ReadItemTexts`, `SelectedItemText` | Picker (refusing an item equal to an earlier one), `SelectableItemsView` rows, `CarouselView.Position`, the app's sink for `SelectIndex` |
| `OpenDropdown`, `CloseDropdown`, `IsDropdownOpen`, `ReadDropdownItemTexts` | A Picker's open flag, kept by the head; its items |
| `SetDate`, `SetTime` | DatePicker, TimePicker, with min and max checked |
| `DoubleClick`, `LongPress` | The gestures of the same names |
| `Focus`, `ClearFocus` | MauiUnit's `SetFocus` and `ClearFocus` |
| `ScrollTo(id)`, `ScrollToIndex`, `ScrollContent`, `ScrollTowards` | Succeed at once where the target exists (`NotMoved`, `Jumped`); a missing id or an index past the end is refused |
| `TryFindByScrolling` | null: nothing is off screen, so a scroll finds nothing a lookup missed |
| `ReadScrollPosition`, `RightClick`, `Hover`, `Swipe(points)` | `DeviceOnlyException` (`IEndsCall`) |
| `GetAttribute(name)` | Any `{name}Property` bindable property, as invariant text; `ClassName`, `Name` |
| App element `ReadAlert`, `TryFindActiveDialog` | The window's top `TestDialog`, as `AlertContents` or as an element |
| App element `TryFindDeclared(id)` | Any element with that id, hidden ones included |
| App element flyout members | `Shell.FlyoutIsPresented` or `FlyoutPage.IsPresented` |
| `IMauiDriver.FindElements` | `MauiUnitLocators`, leaving out `IsVisible="False"` elements as the device trees do |
| `ShellChrome` | `ClassName` `ShellTabBar`, `ShellSection`, `ShellFlyoutList` and `ShellItem` |
| `InvokeMenuItem`, `InvokeToolbarItem` | Menu bar and context menu items, toolbar items of the page and its containers; missing is a `BruinetException`, disabled an `ElementNotReadyException` |
| `NavigateTo`, `NavigateBack` | `Shell.GoToAsync`; `TestNavigator.GoBack` (nothing behind is a `BruinetException`) |
| `CurrentRoute` | Shell's location, else the top page's `AutomationId` |
| `CurrentWindowHandle` | `mauiunit-N`, one per rendered window |
| `GetPageSource` | `TreeSnapshot` |
| `GetScreenshot` | An empty byte array |

Locator strategies:
- `AutomationId`, `Id`, `AccessibilityId`, `DataAutomationId` and `DataTestId` match the `AutomationId`.
- `Name` matches the name above.
- `Text` and `LinkText` match the text exactly; `PartialLinkText` matches a substring.
- `ControlType` matches the platforms' words (`text`, `button`, `image`, `edit`, `entry`, `checkbox`, `radiobutton`, `combobox`, `slider`, `progressbar`, `tabitem`) as the MAUI types they draw; any other word, and `ClassName` and `TagName`, match the type name or any of its base types.
- The XPath `./*` gives the children.
- Any other strategy throws `DeviceOnlyException`.

## MAUI internals: used, and to explore

| Member | Used for | Status |
| --- | --- | --- |
| `Application.AddWindow(Window)`, `RemoveWindow(Window)` | Lifecycle; `Shell.Current` | Used (reflection) |
| `TapGestureRecognizer.SendTapped(View, Func<IElement, Point?>)` | `Tap` | Used (reflection) |
| `BindableObject.GetContext(BindableProperty)` and `.Bindings` | `BeBoundTo` | Used (reflection) |
| `VisualElement.IsFocusedPropertyKey` | `SetFocus`, `ClearFocus` | Used (reflection) |
| `Page._pendingActions`, the closures holding `AlertArguments`, `PromptArguments`, `ActionSheetArguments` | Dialogs (`TestDialog`) | Used (reflection); the arguments and their `SetResult` are public |
| `Page.SendAppearing` and `SendDisappearing`, `Button.SendClicked`, `SendPressed` and `SendReleased`, `Entry.SendCompleted`, `ISearchBarController`, `ISliderController`, `IMenuItemController`, `IShellContentController.GetOrCreateContent`, `SwipeGestureRecognizer.SendSwiped` | Interactions, lifecycle | Public (`EditorBrowsable(Never)`) |
| `Bruinet.Maui.AppSupport.Uia.GestureAutomation.SinkProperty` and a sink's `Verbs`, `Invoke` and `Exchange` | The head calling the app's own verb sinks | Found by name, so a copied-in AppSupport works too |
| `BindingDiagnostics.BindingFailed` | Turning binding errors into test failures | Explore in Phase 3 |
| `GroupableItemsView` grouping and `ListView` `TemplatedItemsList` | Realizing grouped rows and cells | Explore in Phase 1 |
| `VisualElement.IsLoaded`, `SendLoaded` and `OnLoaded` | `Loaded` and `Unloaded` | Explore in Phase 6 |
| `MauiAppBuilder` without a platform | Running the app's `MauiProgram` composition in tests | Explore: `CreateMauiApp` calls platform extensions |
| `ShellNavigationQueryParameters` and `IQueryAttributable` | Shell parameter tests | Explore in Phase 4 |

## Planned skeletons

### Phase 4: alerts (built, no longer a skeleton)

`DialogRequests.Of(window)` takes each pending request from the window's pages, reading it from
`Page._pendingActions`. It makes a `TestDialog` (a `ContentView`) from it, with:
- `#DialogTitle` and `#DialogMessage` labels;
- a `#DialogInput` entry for a prompt;
- one button per choice, then accept, then cancel.

The dialog's parent is the window. A button's click calls the arguments' `SetResult`:
- `true` or `false` for an alert;
- the field's text, or null, for a prompt;
- the choice for an action sheet.

After the click the dialog is removed, and the app's continuation runs at the next drain.

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
        // missing or different -> write <path>.received.txt and throw MauiUnitAssertionException with a unified diff;
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
// (approximate) and MauiUnitElement.Rect reports them.
```

## Tests in the proof of concept

| File | Tests |
| --- | --- |
| `MauiUnit.Tests/Examples/SettingsPageTests.cs` | Label text on appear; `SaveButton_UpdatesStatus`; save disabled until a name is typed; disabled click refused; busy while saving (gated service); failure message; two-way bindings; `BeBoundTo` |
| `MauiUnit.Tests/Examples/GreetingViewTests.cs` | Click handler on a ContentView; a bindable property set by the test |
| `MauiUnit.Tests/Examples/DependencyInjectionTests.cs` | Real service over in-memory preferences; `Replace`; services fixed after render |
| `MauiUnit.Tests/Examples/NavigationTests.cs` | Select, push, back; rows from the template; toolbar reload and empty view; Shell `GoToAsync` from the app's handler, back, `GoTo` |
| `MauiUnit.Tests/Framework/DispatcherTests.cs` | Timer on the virtual clock; delayed order; an async void exception fails the test; `WaitForAssertion` on a real delay; 8 contexts in parallel |
| `MauiUnit.Tests/Framework/TreeTests.cs` | Snapshot text; effective state; miss message; ambiguity; hidden input refused; assertion message; MaxLength; tap command |
| `MauiUnit.Tests/Framework/ChromeAndSwipeTests.cs` | Shell tab bar and a tab navigating; the flyout only while open; carousel swipe and its end; one card visible; focus moving; a swipe recognizer's direction; a picker's equal items; refusal reasons |
| `MauiUnit.Tests/Framework/DialogTests.cs` | An alert over the page answered; a prompt typed into; an action sheet's choice |
| `MauiUnit.Tests.NUnit/SettingsPageTests.cs` | Save through SetUp and TearDown; MauiUnit failure message under NUnit |
| `MauiUnit.Tests.MSTest/GreetingAndShellTests.cs` | Click; Shell help route; method-level parallel |
| `Bruinet.Maui.MauiUnit.Tests/SamplePagesOnMauiUnitTests.cs` | `ButtonsTestPage`: command and status; `ToggleTestPage`: check, switch, radio; page root found |
| `Bruinet.Maui.UITests` with `BRUINET_HEAD=mauiunit` | 360 tests; 325 pass, 35 skip as device-only ([suite-count.md](suite-count.md)) |
