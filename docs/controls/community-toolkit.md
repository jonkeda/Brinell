# CommunityToolkit.Maui Controls

`Brinell.Maui.CommunityToolkit` has control objects for the views in
[CommunityToolkit.Maui](https://github.com/CommunityToolkit/Maui) that a test can drive. Like
`Brinell.Maui`, the library does not reference MAUI or the toolkit: the controls wrap automation
elements, and only the app under test references the toolkit.

Measured against CommunityToolkit.Maui 15.0.1, CommunityToolkit.Maui.MediaElement 10.0.0 and
Microsoft.Maui.Controls 10.0.90, on Windows.

## Inventory

| Toolkit view | Control | Namespace | Base |
| --- | --- | --- | --- |
| `Expander` | `Expander<TScope>` | `Brinell.Maui.CommunityToolkit.Controls.Views` | `ViewBase` |
| `AvatarView` | `AvatarView<TScope>`, parts `AvatarInitials<TScope>`, `AvatarImage<TScope>` | `...Controls.Views` | `ComponentObjectBase` |
| `RatingView` | `RatingView<TScope>` | `...Controls.Views` | `RangeControlBase` |
| `DrawingView` | `DrawingView<TScope>` | `...Controls.Views` | `ViewBase` |
| `Popup` | `Popup<TParent>` / `Popup<TParent, TSelf>` | `...Controls.Views` | `ContainerObjectBase` |
| `StateContainer` | `StateContainer<TParent>` / `StateContainer<TParent, TSelf>` | `...Controls.Layouts` | `ContainerObjectBase` |
| `DockLayout` | `DockLayout<TParent>` / `DockLayout<TParent, TSelf>` | `...Controls.Layouts` | `ContainerObjectBase` |
| `MediaElement` | `MediaElement<TScope>` | `...Controls.Media` | `ComponentObjectBase` |

`MediaElement` moved here from `Brinell.Maui.Controls.Media`, where it was an empty stub.

## What UI Automation Sees

Probed 2026-09-17 on Windows (`net10.0-windows10.0.19041.0`, Debug, `BRINELL_UIA_BRIDGE=1`,
AppSupport automation handlers on) with a raw-view walk, before any bridge verb was declared.
Android was not probed. These trees are why each route below is what it is: a control gets a
bridge verb only where the tree has no pattern for the job and the AD-008 checks pass.

**Expander**: no ExpandCollapse and no Invoke on the expander or its header. The header's tap is
a toolkit-internal `TapGestureRecognizer` handler, not a `Command`, so the bridge's built-in
`Tap` cannot reach it. Collapsed content is not in the tree.

```text
Group  class=ContentView id=TestExpander        patterns=[ScrollItem]
  Group  class=Layout                            patterns=[ScrollItem]
    Text class=TextBlock id=TestExpanderHeader name='Show details' patterns=[Text,ScrollItem]
```

**AvatarView**: the initials are the inner text's name; an image avatar has an `Image` child
instead.

```text
Group  class=ContentView id=TestAvatarView  patterns=[ScrollItem]
  Text class=TextBlock name='BR'            patterns=[Text,ScrollItem]
```

**RatingView**: no RangeValue, Value or Selection. The stars have no ids and no patterns, and
filled or empty is paint only.

```text
Custom id=TestRatingView                           patterns=[ScrollItem]
  Group class=Layout
    Group class=ContentView  (x5, one per star)
      Image class=Image       patterns=[ScrollItem]
```

**DrawingView**: not in the tree by AutomationId. It renders as an `Image` with no id, so it is
reached only through the bridge declaration.

**StateContainer**: the host layout is addressable, and the view the current state shows is its
only child. Switching state swaps the child.

```text
Group class=Layout id=TestStateContainer
  Text class=TextBlock id=StateContentView name='Loaded content'
```

**Popup**: a modal page inside the main window's tree, not a separate window. Its buttons answer
Invoke.

```text
Group class=ContentView id=TestPopup
  Group class=Layout
    Text   id=PopupMessageLabel name='Popup message'
    Button id=PopupCloseButton  name='Close'  patterns=[Invoke,ScrollItem]
```

**MediaElement** (MediaElement 10.0.0): `TestMediaElement` itself is not in the tree. The WinUI
transport controls are, flattened into the page with fixed ids. While playing, the two clocks'
names lose their time ('Time elapsed'); the time stays in the Text pattern only, and the seek
slider's RangeValue advances.

```text
Image      id=PosterImage (offscreen)
Slider     id=ProgressSlider  name='Seek'  patterns=[Value,RangeValue]  range=0[0..100]
Text       id=TimeElapsedElement    name='Time elapsed 00:00:00'   text='00:00:00'
Text       id=TimeRemainingElement  name='Time remaining 00:00:06'
ApplicationBar id=MediaControlsCommandBar
  Button id=PlayPauseButton name='Play'  patterns=[Invoke]
  Button id=VolumeMuteButton, RepeatButton (Toggle), RewindButton, FastForwardButton, ...
```

**Snackbar and Toast** are OS notifications outside the app's tree, and `Show()` throws in an
unpackaged app (see Platform Gaps).

## Routes On Windows

Every route follows AD-005 and AD-008: a UI Automation pattern where one exists, and the gesture
bridge only where none does.

| Control | Public API | Route |
| --- | --- | --- |
| Expander | `IsExpanded`, `Expand`, `Collapse`, `Toggle`, `SetExpanded` | App sink: `Tap`, `GetState(IsExpanded)`. No pattern on the expander or its header. |
| AvatarView | `GetInitials` (+ `Empty`), `IsShowingImage`; parts `Initials`, `Image` | Tree: the child text or image element. The parts own the reads; the component forwards through shortcuts. No initials while an image is shown is an answer (null), not a wait. |
| RatingView | `GetValue`/`SetValue` (rating), `GetMaximum`, `Increment`, `Decrement`, `TapStar`, `IsReadOnly` | App sink: `SelectIndex` (tap star *i*), `GetState(Rating, MaximumRating, IsReadOnly)`. No RangeValue. |
| DrawingView | `DrawLine(dx, dy)`, `GetLineCount` | Not in the tree by id; resolved through the bridge declaration. App sink: `Pan` (one line), `GetState(LineCount)`. |
| Popup | `IsOpen`, `WaitOpen`, `CloseWith(buttonId)`, plus scoped children | Tree: the popup is a modal page in the main window. Buttons answer Invoke. `CloseWith` is two calls in sequence: press the button, then wait for the popup to go. |
| StateContainer | `IsShowing(id)`, `WaitShowing(id)`, `AssertShowing(id)`, generated; ids only, no locators | Tree: the current state's view is the only child. A container that is absent shows nothing (false). |
| DockLayout | Scoping only: the base's `AssertExists`/`AssertVisible` and scoped children (`Child<T>(id)`, `Label(id)`, `Button(id)`, ...) | Tree: `Group class=Layout` with the AutomationId (AppSupport's `Layout` handler covers it), children beneath it in declaration order. Dock position, spacing and `ShouldExpandLastChild` are not published, so there are no reads for them. |
| MediaElement | `Play`, `Pause`, `Stop`, `SetPlaying`, `IsPlaying`, `WaitOpened`, `GetProgress` (+ `GreaterThan`/`AtLeast`), `GetElapsed`, `GetRemaining` (+ ordered comparisons), `GetDuration`; parts `PlayPauseButton` (`MediaPlayPauseButton`), `ProgressSlider`, `TimeElapsedLabel` / `TimeRemainingLabel` (`MediaTimeLabel`), and the other transport buttons | Tree: WinUI transport controls. Play/pause through Invoke, progress and seeking through the seek slider's RangeValue. The parts own the behaviour; the component forwards through generated shortcuts. `Play` waits for the media to open first; `Stop` pauses and seeks to 0, because the transport has no stop button. The component resolves its flattened transport children from its parent scope. |

### The app side

`Brinell.Maui.AppSupport` knows MAUI, not the toolkit, so it cannot answer verbs for toolkit
types. The app declares the verbs and attaches a sink that can:

```xml
<!-- Working directory: the app under test -->
<toolkit:Expander AutomationId="Details"
                  uia:GestureAutomation.Verbs="Tap,GetState"
                  uia:GestureAutomation.Sink="{x:Static automation:ToolkitVerbSink.Instance}" />
```

Copy `samples/Brinell.Samples.Maui.App/Automation/ToolkitVerbSink.cs` into the app. It answers
Expander, RatingView and DrawingView and adds no new verb numbers.

## Platform Gaps

| Control | Windows | Android |
| --- | --- | --- |
| Snackbar | **No control.** `Show()` throws `InvalidOperationException`: an unpackaged app cannot enable it, and when enabled it is an OS notification outside the app's tree. | Not probed. |
| Toast | **No control.** `Show()` throws `COMException` in an unpackaged app; uncaught in an `async void` handler it ends the process. | Not probed. |
| MediaElement | The toolkit's `CurrentState` stays `Opening` and `StateChanged` never fires while playback started from the transport controls runs, so state is read from the transport instead. `IsPlaying` matches the English "Pause" button name. One media element per scope. `GetDuration`, `GetElapsed` and `GetRemaining` answer null while playing, and `WaitOpened` only answers while paused. The play/pause button is enabled before the media opens, and a press made then is dropped. | Transport controls publish no ids yet. |
| Expander, RatingView, DrawingView | Need the app sink. | No state reads: `IsExpanded`, rating and line count answer null; gestures are touch input. |
| DrawingView | A line is arranged through the sink, not drawn. A test of real strokes belongs on Android. | - |

`UniformItemsLayout`, `LazyView` and `SemanticOrderView` have no controls of their
own: they are plain layouts, so use `Grid`/`ContentView`-style containers or the child controls.
`CameraView` and `Map` are out of scope (hardware, keys).

## Tests

- Sample pages: `SamplePage.CommunityToolkit`, `CommunityToolkitAlerts`, `CommunityToolkitMedia`.
- Page objects: `testsnew/Brinell.Maui.UITests/Pages/CommunityToolkit*TestPage.cs`.
- Tests: `testsnew/Brinell.Maui.UITests/Tests/CommunityToolkit`, one class per control, each with
  `[Trait("Control", "<Name>")]`.

```powershell
# Working directory: Brinell root
dotnet test testsnew\Brinell.Maui.UITests\Brinell.Maui.UITests.csproj --filter "Control=Expander|Control=RatingView"
```
