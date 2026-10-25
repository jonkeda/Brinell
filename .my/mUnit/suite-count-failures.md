# The 33 that fail on the MUnit head, by group (agent document)

Run 7, 2026-10-25, `dotnet test testsnew/Bruinell.Maui.UITests.Munit`: 319 passed, 2 skipped, these 33 failed. Each line is the test and its first message line.

## 1 MediaElement: no type on plain net10.0 (12)

- `Tests.CommunityToolkit.MediaElementTests.MediaElement_BeforePlay_AllOfTheClipRemains`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_DoesNotAutoPlay`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_Duration_IsClipLength`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_PauseWhenNotPlaying_IsNoOp`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_Play_StartsPlaybackAndAdvances`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_PlayPauseButton_ReportsTheSameStateAsTheComponent`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_PlayThenPause_Pauses`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_Progress_StartsAtZero`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_ResolvesAndIsVisible`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_SetPlaying_PlaysAndPauses`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_Stop_PausesAndReturnsToTheStart`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- `Tests.CommunityToolkit.MediaElementTests.MediaElement_TransportButtons_AreControlObjects`: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi

## 2 The FlaUI driver itself (6)

- `Tests.Background.FocusVerbTests.Bridge_PublishesTheFocusVerbs`: System.InvalidCastException : Unable to cast object of type 'Bruinell.Maui.Munit.MunitMauiDriver' to type 'Bruinell.Maui
- `Tests.Diagnostics.AccessibilityAuditTests.Audit_LeavesReadOnlyElementsOffTheBacklog`: System.InvalidCastException : Unable to cast object of type 'Bruinell.Maui.Munit.MunitMauiDriver' to type 'Bruinell.Maui
- `Tests.Diagnostics.AccessibilityAuditTests.Audit_ReportsWhatEachInstrumentedElementOffersWithoutAPointer`: System.InvalidCastException : Unable to cast object of type 'Bruinell.Maui.Munit.MunitMauiDriver' to type 'Bruinell.Maui
- `Tests.Diagnostics.BridgeTreeDumpTests.DumpRawTreeBelowTheWindow`: System.InvalidCastException : Unable to cast object of type 'Bruinell.Maui.Munit.MunitMauiDriver' to type 'Bruinell.Maui
- `Tests.Diagnostics.BridgeTreeDumpTests.DumpSwipeViewAfterOpening`: System.InvalidCastException : Unable to cast object of type 'Bruinell.Maui.Munit.MunitMauiDriver' to type 'Bruinell.Maui
- `Tests.Diagnostics.ForegroundWatchdogTests.Navigating_DoesNotTakeTheForeground`: System.InvalidCastException : Unable to cast object of type 'Bruinell.Maui.Munit.MunitMauiDriver' to type 'Bruinell.Maui

## 3 The Windows tree and bridge (7)

- `Tests.Background.FocusVerbTests.ActionsWithNoSemanticRoute_ThrowInsteadOfUsingRealInput`: Assert.Throws() Failure: Exception type was not an exact match
- `Tests.Container.ContainerModuleTests.BoxView_IsNotAddressableOnWindows`: Assert.Null() Failure: Value is not null
- `Tests.Gestures.GestureAddressabilityTests.SwipeView_IsUnfindableAndStillAnswersItsVerb`: Assert.Null() Failure: Value is not null
- `Tests.Gestures.GestureBridgeTests.UndeclaredElement_SaysWhatToAdd`: Assert.ThrowsAny() Failure: Exception type was not compatible
- `Tests.Gestures.GestureBridgeTests.UndeclaredGesture_IsRefusedWithAUsefulMessage`: Assert.ThrowsAny() Failure: Exception type was not compatible
- `Tests.Gestures.GestureVerbTests.UndeclaredRow_IsNotOnTheBridgeAtAll`: A row that declares no verbs was published anyway, so the bridge is inferring.
- `Tests.Navigation.MenuVerbTests.MenuItems_AreInvisibleToTheAccessibilityTree`: Assert.Null() Failure: Value is not null

## 4 Layout and pixels (6)

- `Tests.Background.ScrollVerbTests.ScrollPosition_ReportsExtentAsWellAsOffset`: Bruinell.Maui.Munit.DeviceOnlyException : 'ReadScrollPosition' needs a device: the MUnit head has no layout, pixels or p
- `Tests.Background.ScrollVerbTests.ScrollTo_MovesTheViewport_WithoutPhysicalInput`: Bruinell.Maui.Munit.DeviceOnlyException : 'ReadScrollPosition' needs a device: the MUnit head has no layout, pixels or p
- `Tests.Background.ScrollVerbTests.ScrollTo_PutsTheNamedElementOnScreen`: Bruinell.Maui.Munit.DeviceOnlyException : 'ReadScrollPosition' needs a device: the MUnit head has no layout, pixels or p
- `Tests.Diagnostics.OccludedScreenshotTests.Screenshot_OfOccludedWindow_ShowsTheApp`: System.ArgumentException : Parameter is not valid.
- `Tests.Gestures.RecordedInteractionTests.ATap_AndAPullToRefresh_AreLogged_OverTheirControls_InPhysicalPixels`: Assert.Equal() Failure: Collections differ
- `Tests.Scroll.ScrollTests.BottomLabel_IsVisible_IsFalseUntilScrolledTo`: Assert.False() Failure

## 5 Popup virtualization (1)

- `Tests.Background.SelectionVerbTests.LongPicker_ReportsEveryItemAndItsOwnIndex`: The dropdown realized all 200 items, so reading them out of the popup would have been right after all and this test no l

## 6 Known difference: carousel cards off screen (1)

- `Tests.Collection.CarouselViewTests.CarouselView_Items_AreTheSeededCardsInOrder`: Bruinell.Core.Exceptions.ElementNotReadyException : The element 'AutomationId:CarouselCardLabel' was found but is not vi

