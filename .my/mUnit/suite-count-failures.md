# Failures of the first count, by group (agent document)

Run 2, 2026-10-25, `dotnet test testsnew/Brinell.Maui.UITests.Munit`. One test is regrouped by hand from Other to Device-only: `MenuVerbTests.MenuItems_AreInvisibleToTheAccessibilityTree`.

## 1 Not run: the Shell app is not compiled headless yet (17)
- Tests.Shell.ShellFlyoutTests.Flyout_Open_RevealsItems: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellFlyoutVerbTests.Flyout_OpensAndClosesWithoutPhysicalInput: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellStackTests.Shell_PushedPage_PopsBack: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellFlyoutTests.Flyout_OpenTwice_StaysOpen: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellTabTests.Shell_SelectTab_ShowsItsPage: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellFlyoutTests.Flyout_Item_NavigatesToItsPage: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellFlyoutTests.Flyout_LastItem_IsReachable: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellStackTests.Shell_ReselectingTheTab_DoesNotPop: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellFlyoutTests.Flyout_Close_LeavesTheTabsUsable: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Background.ShellCollectionParallelismTests.TheHubAppIsRunningAtTheSameTime: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Background.HubCollectionParallelismTests.TheShellAppIsRunningAtTheSameTime: Waited 60s for the other collection's app and it never came up. Either the collections are still being serialised - chec
- Tests.Shell.ShellStackTests.Shell_FixtureReset_ClearsAPushedPage: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellTabTests.Shell_SelectingTheCurrentTab_IsHarmless: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellFlyoutTests.Flyout_StartsShut: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellFlyoutVerbTests.Flyout_AskedTwiceForTheSameState_StaysThere: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellTabTests.Shell_ReportsTheCurrentTab: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
- Tests.Shell.ShellTabTests.Shell_ReportsItsTabs: System.NotSupportedException : ShellFixture has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-cou
## 2 Device-only: tests the Windows driver, its bridge, pixels or layout (16)
- Tests.Gestures.GestureBridgeTests.SwipeDown_OnDisabledRefresh_IsDeclined: Assert.True() Failure
- Tests.Gestures.GestureAddressabilityTests.SwipeView_IsUnfindableAndStillAnswersItsVerb: Assert.Null() Failure: Value is not null
- Tests.Diagnostics.AccessibilityAuditTests.Audit_ReportsWhatEachInstrumentedElementOffersWithoutAPointer: System.InvalidCastException : Unable to cast object of type 'Brinell.Maui.Munit.MunitMauiDriver' to type 'Brinell.Maui.F
- Tests.Gestures.GestureBridgeTests.Bridge_PublishesTheDeclaredElements: TestSwipeView does not offer SwipeRight. Either the app under test has no Brinell bridge, or the GestureAutomation.Verbs
- Tests.Gestures.GestureVerbTests.UndeclaredVerb_OnADeclaredRow_IsRefused: The double-tap row does not answer DoubleTap, so this test is not measuring what it thinks it is.
- Tests.Gestures.GestureBridgeTests.UndeclaredGesture_IsRefusedWithAUsefulMessage: Assert.Contains() Failure: Sub-string not found
- Tests.Scroll.ScrollTests.BottomLabel_IsVisible_IsFalseUntilScrolledTo: Assert.False() Failure
- Tests.Gestures.RecordedInteractionTests.ATap_AndAPullToRefresh_AreLogged_OverTheirControls_InPhysicalPixels: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Background.FocusVerbTests.Bridge_PublishesTheFocusVerbs: System.InvalidCastException : Unable to cast object of type 'Brinell.Maui.Munit.MunitMauiDriver' to type 'Brinell.Maui.F
- Tests.Container.ContainerModuleTests.BoxView_IsNotAddressableOnWindows: Assert.Null() Failure: Value is not null
- Tests.Diagnostics.AccessibilityAuditTests.Audit_LeavesReadOnlyElementsOffTheBacklog: System.InvalidCastException : Unable to cast object of type 'Brinell.Maui.Munit.MunitMauiDriver' to type 'Brinell.Maui.F
- Tests.Diagnostics.BridgeTreeDumpTests.DumpSwipeViewAfterOpening: System.InvalidCastException : Unable to cast object of type 'Brinell.Maui.Munit.MunitMauiDriver' to type 'Brinell.Maui.F
- Tests.Diagnostics.ForegroundWatchdogTests.Navigating_DoesNotTakeTheForeground: System.InvalidCastException : Unable to cast object of type 'Brinell.Maui.Munit.MunitMauiDriver' to type 'Brinell.Maui.F
- Tests.Gestures.GestureBridgeTests.UndeclaredElement_SaysWhatToAdd: Assert.Contains() Failure: Sub-string not found
- Tests.Diagnostics.OccludedScreenshotTests.Screenshot_OfOccludedWindow_ShowsTheApp: System.ArgumentException : Parameter is not valid.
- Tests.Diagnostics.BridgeTreeDumpTests.DumpRawTreeBelowTheWindow: System.InvalidCastException : Unable to cast object of type 'Brinell.Maui.Munit.MunitMauiDriver' to type 'Brinell.Maui.F
## 3 Toolkit MediaElement: no type on plain net10.0 (platform media) (12)
- Tests.CommunityToolkit.MediaElementTests.MediaElement_Progress_StartsAtZero: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_TransportButtons_AreControlObjects: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_SetPlaying_PlaysAndPauses: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_Play_StartsPlaybackAndAdvances: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_DoesNotAutoPlay: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_Stop_PausesAndReturnsToTheStart: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_BeforePlay_AllOfTheClipRemains: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_PlayThenPause_Pauses: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_PlayPauseButton_ReportsTheSameStateAsTheComponent: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_PauseWhenNotPlaying_IsNoOp: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_ResolvesAndIsVisible: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
- Tests.CommunityToolkit.MediaElementTests.MediaElement_Duration_IsClipLength: Microsoft.Maui.Controls.Xaml.XamlParseException : Position 25:10. Type MediaElement not found in xmlns http://schemas.mi
## 4 Head gap: semantic gestures (tap, swipe, pan, draw) not mapped (15)
- Tests.Gestures.GestureVerbTests.Tap_RaisesTheOneTapRecognizer: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Gestures.GestureBridgeTests.SwipeDown_OnRefreshWrapper_RunsTheRefresh: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Gestures.GestureVerbTests.Swipe_MatchesOnTheDirectionFlag: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Collection.CarouselViewTests.CarouselView_SwipePrevious_ShowsThePreviousCard: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiElement. On Windows they a
- Tests.Gestures.GestureVerbTests.DoubleTap_RaisesTheTwoTapRecognizer: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.CommunityToolkit.ExpanderTests.Expander_Toggle_FlipsState: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiElement. On Windows they a
- Tests.CommunityToolkit.DrawingViewTests.DrawingView_Clear_EmptiesView: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.CommunityToolkit.DrawingViewTests.DrawingView_DrawLine_AddsLineAndRaisesCompletion: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Collection.CarouselViewTests.CarouselView_SwipeNext_ShowsTheNextCard: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiElement. On Windows they a
- Tests.Collection.IndicatorViewTests.IndicatorView_CarouselSwiped_SelectsTheNextDot: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiElement. On Windows they a
- Tests.Gestures.GestureVerbTests.LongPress_ReachesTheSinkThatNamedIt: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Gestures.GestureVerbTests.Pan_CarriesItsArgumentsToTheSink: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Gestures.GestureBridgeTests.SwipeDown_OnRefreshView_RunsTheRefresh: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Gestures.GestureBridgeTests.SwipeDown_Repeats: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
- Tests.Gestures.GestureBridgeTests.SwipeRight_OnSwipeView_IsAcceptedAndReported: Brinell.Core.Exceptions.RouteUnavailableException : Gestures are not implemented for MunitMauiDriver. On Windows they ar
## 5 Head gap: refusals throw MUnit exceptions, not the Brinell types the tests expect (14)
- Tests.Background.NavigationVerbTests.NavigateBack_AtTheHub_FailsWithTheReason: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Navigation.ToolbarVerbTests.InvokeToolbarItem_WithNoSuchItem_IsRefusedWithTheReason: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Background.StateReadTests.ReadState_AnUnknownProperty_IsRefused: Assert.Throws() Failure: No exception was thrown
- Tests.Background.FocusVerbTests.ActionsWithNoSemanticRoute_ThrowInsteadOfUsingRealInput: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Background.SelectionVerbTests.SelectIndex_WhereMauiCannotHoldTheSelection_IsRefusedRatherThanAttempted: Assert.Throws() Failure: No exception was thrown
- Tests.DateTimes.DatePickerTests.DatePicker_DateAfterMaximum_IsRefused: Assert.Throws() Failure: Exception type was not an exact match
- Tests.CommunityToolkit.RatingViewTests.RatingView_SetValue_OutsideStars_Throws: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Navigation.ToolbarVerbTests.InvokeToolbarItem_WhenTheItemIsDisabled_IsRefusedAndDoesNothing: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Background.ScrollVerbTests.ScrollTo_AnIdThatIsNotThere_SaysSo: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Background.SelectionVerbTests.SelectIndex_PastTheEnd_IsRefusedWithTheReason: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Navigation.MenuVerbTests.InvokeMenuItem_WhenTheItemIsDisabled_IsRefusedAndDoesNothing: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Background.SelectionVerbTests.SelectByText_WithNoSuchItem_IsRefusedWithTheReason: Assert.Throws() Failure: Exception type was not an exact match
- Tests.Navigation.MenuVerbTests.InvokeMenuItem_WithNoSuchItem_IsRefusedWithTheReason: Assert.Throws() Failure: Exception type was not an exact match
- Tests.DateTimes.DatePickerTests.DatePicker_DateBeforeMinimum_IsRefused: Assert.Throws() Failure: Exception type was not an exact match
## 6 MUnit gap: alerts and prompts (Phase 4) (8)
- Tests.Dialogs.AlertReadTests.CurrentAlert_ReportsWhatTheAppAsked: Assert.True() Failure
- Tests.Dialogs.AlertReadTests.CurrentAlert_ForAPrompt_ReportsTheQuestionAndBothButtons: Assert.True() Failure
- Tests.Dialogs.ContentDialogTests.Prompt_AcceptsTextThroughScopedControls: Assert.True() Failure
- Tests.Dialogs.ContentDialogTests.Alert_DismissesThroughScopedButton: Assert.True() Failure
- Tests.Dialogs.AlertReadTests.CurrentAlert_AfterDismissal_IsNullAgain: Assert.True() Failure
- Tests.Dialogs.AlertReadTests.CurrentAlert_WithOneButton_ReportsNoAccept: Assert.True() Failure
- Tests.Dialogs.AlertReadTests.Dialog_PublishesItsTitleAndButtons: Assert.True() Failure
- Tests.Dialogs.ContentDialogTests.Confirmation_AcceptsThroughScopedButton: Assert.True() Failure
## 7 MUnit gap: focus (no handler, Phase 6) (6)
- Tests.Background.FocusVerbTests.Focus_LeavesTheForegroundWindowAlone: The editor did not report focus.
- Tests.Background.FocusVerbTests.Focus_MovesBetweenFields: The entry did not take focus.
- Tests.Background.FocusVerbTests.Blur_ClearsFocusWithoutPhysicalInput: The entry did not take focus.
- Tests.Background.FocusVerbTests.Focus_LandsWithoutPhysicalInput: The entry did not report focus after the Focus verb.
- Tests.DateTimes.DatePickerTests.DatePicker_Focus_IsReported: Brinell.Core.Exceptions.AssertionException : Expected Focused to be 'True'. Locator: AutomationId:TestDatePicker
- Tests.Text.EntryTests.Entry_Focus_IsReported: Brinell.Core.Exceptions.AssertionException : Expected Focused to be 'True'. Locator: AutomationId:TestEntry
## 8 Head gap: scroll routes (ScrollTo, ScrollToIndex, ScrollContent, ReadScrollPosition) (8)
- Tests.Background.ScrollVerbTests.ScrollToIndex_MaterializesARowFurtherDown: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement ScrollToIndex.
- Tests.Collection.ProductCollectionTests.SearchByContent_FindsTheRightRow: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement ScrollContent.
- Tests.Background.ScrollVerbTests.ScrollTo_PutsTheNamedElementOnScreen: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement ScrollTo.
- Tests.Collection.CollectionIndexProbeTests.RealizedRows_ReportLogicalPosition: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement ScrollToIndex.
- Tests.Background.ScrollVerbTests.ScrollToIndex_AtItemCount_IsRefusedWithTheCount: System.InvalidOperationException : ProductCollectionView does not answer GetState('ItemCount').
- Tests.Background.ScrollVerbTests.ScrollTo_MovesTheViewport_WithoutPhysicalInput: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement ReadScrollPosition.
- Tests.Collection.ProductCollectionTests.ByName_DeletesTheRightRow: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement ScrollContent.
- Tests.Background.ScrollVerbTests.ScrollPosition_ReportsExtentAsWellAsOffset: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement ReadScrollPosition.
## 9 Head gap: Stepper's + and - parts, which a device draws (10)
- Tests.Range.StepperTests.Stepper_Decrement_ChangesValueByStepSize: System.NotSupportedException : Stepper 'TestStepper' has no - button to press.
- Tests.Range.StepperTests.Stepper_Decrement_StopsAtMinimum: System.NotSupportedException : Stepper 'TestStepper' has no - button to press.
- Tests.Range.StepperTests.Stepper_SetValue_RespectsBounds_Min: System.NotSupportedException : Stepper 'TestStepper' has no - button to press.
- Tests.Range.StepperTests.Stepper_Increment_StopsAtMaximum: System.NotSupportedException : Stepper 'TestStepper' has no + button to press.
- Tests.Range.StepperTests.Stepper_SetValue_UpdatesStatus: System.NotSupportedException : Stepper 'TestStepper' has no + button to press.
- Tests.Range.StepperTests.Stepper_Reset_RestoresInitialValue: System.NotSupportedException : Stepper 'TestStepper' has no + button to press.
- Tests.Range.StepperTests.Stepper_Increment_ChangesValueByStepSize: System.NotSupportedException : Stepper 'TestStepper' has no + button to press.
- Tests.Range.StepperTests.Stepper_SetValue_UpdatesDisplay: System.NotSupportedException : Stepper 'TestStepper' has no + button to press.
- Tests.Range.StepperTests.Stepper_MultipleValueChanges_UpdatesDisplay: System.NotSupportedException : Stepper 'TestStepper' has no - button to press.
- Tests.Range.StepperTests.Stepper_SetValue_RespectsBounds_Max: System.NotSupportedException : Stepper 'TestStepper' has no + button to press.
## 10 Head gap: state the bridge reads (ReadState) and toolkit or carousel parts (18)
- Tests.CommunityToolkit.RatingViewTests.RatingView_SetValue_UpdatesRatingAndLabel: MUnit.Interaction.ElementNotInteractableException : RatingView #TestRatingView has no items to select.
- Tests.CommunityToolkit.AvatarViewTests.AvatarView_Initials_IsAPartWithTheInitials: Brinell.Core.Exceptions.AssertionException : Expected Exists to be 'True'. Locator: ControlType:text
- Tests.Collection.IndicatorViewTests.IndicatorView_CardAdded_AddsADot: Brinell.Core.Exceptions.AssertionException : Expected Count to be '6'. Locator: AutomationId:TestIndicatorView
- Tests.CommunityToolkit.ExpanderTests.Expander_SetExpanded_Null_DoesNothing: Brinell.Core.Exceptions.AssertionException : Expected Expanded to be 'False'. Locator: AutomationId:TestExpander
- Tests.CommunityToolkit.ExpanderTests.Expander_ExpandTwice_IsIdempotent: System.NotSupportedException : Expander 'TestExpander' does not report IsExpanded, so tapping it could not be made idemp
- Tests.Collection.CarouselViewTests.CarouselView_BoundPositionChanges_CarouselFollows: Brinell.Core.Exceptions.AssertionException : Expected Position to be '1'. Locator: AutomationId:TestCarouselView
- Tests.Background.StateReadTests.Progress_IsReportedInMauisOwnUnits: The progress bar answered '', which is not a number.
- Tests.CommunityToolkit.RatingViewTests.RatingView_TapStar_SetsRatingToThatStar: MUnit.Interaction.ElementNotInteractableException : RatingView #TestRatingView has no items to select.
- Tests.CommunityToolkit.RatingViewTests.RatingView_IncrementAndDecrement_MoveOneStar: MUnit.Interaction.ElementNotInteractableException : RatingView #TestRatingView has no items to select.
- Tests.Collection.IndicatorViewTests.IndicatorView_Open_ShowsOneDotPerCardWithTheFirstSelected: Brinell.Core.Exceptions.AssertionException : Expected Count to be '5'. Locator: AutomationId:TestIndicatorView
- Tests.CommunityToolkit.AvatarViewTests.AvatarView_GetInitials_ReturnsInitials: Assert.Equal() Failure: Strings differ
- Tests.CommunityToolkit.DrawingViewTests.DrawingView_StartsEmpty: Assert.Equal() Failure: Values differ
- Tests.Collection.CarouselViewTests.CarouselView_Open_ShowsTheFirstCardOnly: Brinell.Core.Exceptions.AssertionException : Expected Position to be '0'. Locator: AutomationId:TestCarouselView
- Tests.CommunityToolkit.ExpanderTests.Expander_Expand_ShowsContent: System.NotSupportedException : Expander 'TestExpander' does not report IsExpanded, so tapping it could not be made idemp
- Tests.CommunityToolkit.ExpanderTests.Expander_ExpandThenCollapse_HidesContent: System.NotSupportedException : Expander 'TestExpander' does not report IsExpanded, so tapping it could not be made idemp
- Tests.CommunityToolkit.RatingViewTests.RatingView_ReadsInitialRangeFromApp: Assert.Equal() Failure: Values differ
- Tests.Background.StateReadTests.ImageSource_SeparatesABrokenImageFromAWorkingOne: Assert.Equal() Failure: Strings differ
- Tests.CommunityToolkit.ExpanderTests.Expander_StartsCollapsed: Brinell.Core.Exceptions.AssertionException : Expected Expanded to be 'False'. Locator: AutomationId:TestExpander
## 11 Other: to look at one by one (8)
- Tests.Navigation.MenuVerbTests.InvokeMenuItem_RunsAContextMenuItem_WithoutOpeningTheFlyout: MUnit.Interaction.ElementNotInteractableException : The page has no toolbar item 'ContextMenuCopy'.
- Tests.Background.SelectionVerbTests.LongPicker_ReportsEveryItemAndItsOwnIndex: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement OpenDropdown.
- Tests.Collection.ProductCollectionTests.Clear_ShowsEmptyState_ResetRestoresSeed: Brinell.Core.Exceptions.AssertionException : Expected Exists to be 'False'. Locator: AutomationId:ProductListEmptyLabel
- Tests.Navigation.MenuVerbTests.MenuItems_AreInvisibleToTheAccessibilityTree: Assert.Null() Failure: Value is not null
- Tests.Navigation.MenuVerbTests.InvokeMenuItem_RunsAPageMenuItem_WithoutThePointer: MUnit.Interaction.ElementNotInteractableException : The page has no toolbar item 'PageMenuFileNew'.
- Tests.Background.SelectionVerbTests.OpenFlyout_ShowsTheDropdownAndCloseFlyoutPutsItBack: Brinell.Core.Exceptions.RouteUnavailableException : MunitMauiElement does not implement OpenDropdown.
- Tests.Navigation.NavigationControlTests.Menu_Exists_AndStartsClosed: Brinell.Core.Exceptions.AssertionException : Expected 0 items. Locator: AutomationId:ActionsMenu
- Tests.Background.SelectionVerbTests.Pickers_OfferTheSemanticRoute: Assert.NotNull() Failure: Value is null
