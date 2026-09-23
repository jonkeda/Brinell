# CommunityToolkit.Maui automation probe

Probed 2026-09-17 against CommunityToolkit.Maui 15.0.1 and Microsoft.Maui.Controls 10.0.90, sample
app built Debug for `net10.0-windows10.0.19041.0`, `BRINELL_UIA_BRIDGE=1`, AppSupport automation
handlers on. Pages: `SamplePage.CommunityToolkit` and `SamplePage.CommunityToolkitAlerts`.
Raw-view walk through `System.Windows.Automation`; no bridge verbs were declared at probe time.

**Android: not probed.** No emulator or device was attached (`adb devices` empty). Every section
below is Windows only.

## Expander (`TestExpander`)

```
Group  class=ContentView id=TestExpander        patterns=[ScrollItem]
  Group  class=Layout                            patterns=[ScrollItem]
    Text class=TextBlock id=TestExpanderHeader name='Show details' patterns=[Text,ScrollItem]
```

- No ExpandCollapse, no Invoke on the expander or its header. The header's tap is a
  `TapGestureRecognizer` with a `Tapped` handler inside the toolkit, not a `Command`, so the
  bridge's built-in `Tap` binding cannot reach it either.
- Collapsed content is not in the tree.

**Route:** AD-008 passes. (1) Blocked: no pattern answers expand or read state. (2) The physical
tap is not what is under test; the expanded content is. (3) Expanded/collapsed is visible to the
user. So an app sink owns `Tap` (toggles `IsExpanded`, as the header tap does) and `GetState`
(`IsExpanded`). No new verb numbers.

## AvatarView (`TestAvatarView`)

```
Group  class=ContentView id=TestAvatarView  patterns=[ScrollItem]
  Text class=TextBlock name='BR'            patterns=[Text,ScrollItem]
```

**Route:** plain tree. Text is the inner TextBlock's name; an image avatar shows an `Image` child
instead. No bridge.

## RatingView (`TestRatingView`)

```
Custom id=TestRatingView                           patterns=[ScrollItem]
  Group class=Layout
    Group class=ContentView  (x5, one per star)
      Image class=Image       patterns=[ScrollItem]
```

- No RangeValue, no Value, no Selection. Stars have no ids and no patterns; filled vs empty is
  paint only.

**Route:** AD-008 passes. (1) Blocked. (2) The tap is not under test; the rating is. (3) The
rating is what the user sees. Sink owns `GetState` (`Rating`, `MaximumRating`, `IsReadOnly`) and
`SelectIndex` (index `i` = tapping star `i`, so rating `i + 1`; declined when read-only).

## DrawingView (`TestDrawingView`)

- **Not in the tree by AutomationId.** It renders as `Image class=Image` with no AutomationId,
  sitting between the "DrawingView" heading and `DrawingStatusLabel`.

**Route:** reached only through the bridge declaration (`AppElement.TryFindDeclared`). AD-008:
(1) blocked, not even addressable. (2) A real stroke is the behaviour under test for a drawing
surface, so that test belongs on Android; on Windows the verb only *arranges* a line.
(3) The line count is echoed in a visible label. Sink owns `Pan` (one straight line from the
centre by `arg1`,`arg2`, raised through `IDrawingView.OnDrawingLineCompleted` so the app's own
event fires) and `GetState` (`LineCount`). Clear goes through the sample's Clear button.

## StateContainer (`TestStateContainer`)

```
Group class=Layout id=TestStateContainer
  Text class=TextBlock id=StateContentView name='Loaded content'
```

- The host layout is addressable (AppSupport layout handler), and whichever view the current
  state shows is its child in the tree. Switching state swaps the child.

**Route:** plain tree: the current state is "which state view is present". No bridge, and none
is justified: AD-008 test 1 fails, the tree already answers.

## Popup (`TestPopup`, raised by `ShowPopupButton`)

```
Group class=ContentView id=TestPopup
  Group class=Layout
    Text   id=PopupMessageLabel name='Popup message'
    Button id=PopupCloseButton  name='Close'  patterns=[Invoke,ScrollItem]
```

- Shown as a modal page inside the **main window's** tree, not a separate top-level window.
- Its own buttons have Invoke; closing through `PopupCloseButton` worked and the page recorded
  "popup closed".

**Route:** plain tree, a container scoped to the popup root. No bridge.

## Snackbar (`ShowSnackbarButton`)

- `Snackbar.Show()` throws `InvalidOperationException` on Windows: the toolkit requires
  `SetShouldEnableSnackbarOnWindows(true)`, which in turn needs a packaged app with a
  notification activator. The sample app is unpackaged (`WindowsPackageType=None`).
- Even when enabled it is an OS toast notification, outside the app's automation tree.

**Route:** Windows: out of reach, documented skip. Android: in the app's tree
(`com.google.android.material.snackbar`); not probed.

## Toast (`ShowToastButton`)

- `Toast.Show()` throws `COMException` on Windows (unpackaged app, no notification identity).
  Uncaught in an `async void` handler it took the app down (`0xc000027b` in
  `Microsoft.UI.Xaml.dll`); the sample now records the failure instead.

**Route:** Windows: out of reach, documented skip. Android: a system toast; Appium sees it only
through the toast XPath of UiAutomator2. Not probed.

## MediaElement (`TestMediaElement`, CommunityToolkit.Maui.MediaElement 10.0.0)

Probed when control 8 started, on its own page (`SamplePage.CommunityToolkitMedia`) with a
six-second local `tone.wav`.

```
Image      id=PosterImage (offscreen)
Slider     id=ProgressSlider  name='Seek'  patterns=[Value,RangeValue]  range=0[0..100]
Text       id=TimeElapsedElement    name='Time elapsed 00:00:00'   text='00:00:00'
Text       id=TimeRemainingElement  name='Time remaining 00:00:06'
ApplicationBar id=MediaControlsCommandBar
  Button id=PlayPauseButton name='Play'  patterns=[Invoke]
  Button id=VolumeMuteButton, RepeatButton (Toggle), RewindButton, FastForwardButton, ...
```

- **`TestMediaElement` is not in the tree.** The WinUI transport controls are, flattened into
  the page layout with fixed ids.
- Invoking `PlayPauseButton` plays: the name flips to 'Pause' and the elapsed time advances.
- **The toolkit's own state is wrong here.** After playback starts from the transport controls,
  `MediaElement.CurrentState` stays `Opening` and `StateChanged` never fires. A `GetState` sink
  was tried first and removed for this reason.
- While playing, the times' **names** lose their clock ('Time elapsed'); the clock stays in the
  Text pattern only, which `IMauiElement.Text` does not read (it falls back to Name). The seek
  slider's RangeValue advances (48 at ~3 s of 6 s).

**Route:** plain tree, no bridge. Play/pause through Invoke; `IsPlaying` from the button name
(localized: English "Pause"); progress 0-100 from the slider's RangeValue; duration from the two
clocks while not playing. One media element per scope, because the transport ids are fixed.

## Summary

| Control | In tree by id | Patterns | Route (Windows) |
| --- | --- | --- | --- |
| Expander | yes | none | sink: `Tap`, `GetState(IsExpanded)` |
| AvatarView | yes | none (text child) | tree |
| RatingView | yes | none | sink: `SelectIndex`, `GetState(Rating, MaximumRating, IsReadOnly)` |
| DrawingView | **no** | - | declared only; sink: `Pan`, `GetState(LineCount)` |
| StateContainer | yes | children swap | tree |
| Popup | yes, in main window | Invoke on its buttons | tree |
| Snackbar | - | - | throws; skip |
| Toast | - | - | throws; skip |
| MediaElement | **no** (transport controls are) | Invoke, RangeValue | tree |
