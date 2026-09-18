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
| `MediaElement` | `MediaElement<TScope>` | `...Controls.Media` | `ComponentObjectBase` |

`MediaElement` moved here from `Brinell.Maui.Controls.Media`, where it was an empty stub.

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

`DockLayout`, `UniformItemsLayout`, `LazyView` and `SemanticOrderView` have no controls of their
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

The probe notes, with the raw automation trees, are in `.my/communitytoolkit/probe.md`.
