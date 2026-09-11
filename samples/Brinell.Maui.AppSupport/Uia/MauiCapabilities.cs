using Brinell.Uia;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// Every public MAUI API the bridge drives, in one file.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why they are gathered here.</b> When MAUI changes, the failure should be a compile error
/// in one known file rather than a gesture that silently stops working in a suite nobody has
/// run this week. Everything the dispatcher does to a MAUI control goes through a method here.
/// </para>
/// <para>
/// <b>Nothing here reflects into MAUI internals.</b> <c>SendTapped</c>, <c>SendPinch</c> and
/// <c>SendPan</c> are internal in MAUI 10, and reaching them by reflection would make every
/// MAUI upgrade a runtime failure discovered by a test rather than a build failure discovered
/// by a compiler. Where no public API exists, the verb is not supported and says so.
/// </para>
/// </remarks>
internal static class MauiCapabilities
{
    // ---- Gestures -----------------------------------------------------------------

    /// <summary>
    /// Opens the swipe items a swipe in the given direction would reveal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The mapping is inverted, and that is correct.</b> Swiping your finger to the right
    /// drags the content right and uncovers what is behind its left edge, so
    /// <see cref="BrinellVerb.SwipeRight"/> opens <see cref="OpenSwipeItem.LeftItems"/>. It
    /// reads like a bug and is the single most likely thing here to be "fixed" backwards, which
    /// is why <c>SwipeMappingTests</c> pins it.
    /// </para>
    /// <para>
    /// Opened without animation. A test that waits for an animation is a test with a sleep in
    /// it, and the state the assertion is about is reached either way.
    /// </para>
    /// </remarks>
    /// <param name="swipeView">The view to open.</param>
    /// <param name="verb">The swipe verb.</param>
    /// <returns>Whether the verb named a direction.</returns>
    internal static bool TryOpenSwipeView(SwipeView swipeView, BrinellVerb verb)
    {
        var item = SwipeItemFor(verb);
        if (item is null)
        {
            return false;
        }

        swipeView.Open(item.Value, false);
        return true;
    }

    /// <summary>Which swipe items a swipe in this direction uncovers.</summary>
    /// <param name="verb">The swipe verb.</param>
    /// <returns>The items revealed, or null if the verb is not a swipe.</returns>
    internal static OpenSwipeItem? SwipeItemFor(BrinellVerb verb) => verb switch
    {
        BrinellVerb.SwipeRight => OpenSwipeItem.LeftItems,
        BrinellVerb.SwipeLeft => OpenSwipeItem.RightItems,
        BrinellVerb.SwipeDown => OpenSwipeItem.TopItems,
        BrinellVerb.SwipeUp => OpenSwipeItem.BottomItems,
        _ => null,
    };

    /// <summary>Closes an open swipe view.</summary>
    /// <param name="swipeView">The view to close.</param>
    internal static void CloseSwipeView(SwipeView swipeView) => swipeView.Close(false);

    /// <summary>
    /// Starts a pull-to-refresh.
    /// </summary>
    /// <remarks>
    /// Setting <c>IsRefreshing</c> is what the gesture itself does; the view raises
    /// <c>Refreshing</c> and runs its command from the property change. Returning false when it
    /// is already refreshing keeps a second call from being reported as a refresh that happened.
    /// </remarks>
    /// <param name="refreshView">The view to refresh.</param>
    /// <returns>Whether a refresh was started.</returns>
    internal static bool TryStartRefresh(RefreshView refreshView)
    {
        if (refreshView.IsRefreshing)
        {
            return false;
        }

        refreshView.IsRefreshing = true;
        return true;
    }

    /// <summary>
    /// Raises the command of a tap recognizer expecting the given number of taps.
    /// </summary>
    /// <remarks>
    /// The command, not the <c>Tapped</c> event: <c>SendTapped</c> is internal in MAUI 10 and
    /// the event cannot be raised from outside. An element whose tap handling is an event
    /// handler rather than a command needs an <see cref="IBrinellGestureSink"/>, and this
    /// returns false so the ladder gets that far.
    /// </remarks>
    /// <param name="element">The element to tap.</param>
    /// <param name="taps">How many taps the gesture stands for.</param>
    /// <returns>Whether a recognizer's command was raised.</returns>
    internal static bool TryTap(VisualElement element, int taps)
    {
        // Gesture recognizers live on View, not on VisualElement. A Page or a Window can declare
        // verbs but cannot carry a recognizer, so there is nothing here for them.
        if (element is not View view)
        {
            return false;
        }

        foreach (var recognizer in view.GestureRecognizers.OfType<TapGestureRecognizer>())
        {
            if (recognizer.NumberOfTapsRequired != taps)
            {
                continue;
            }

            if (recognizer.Command is null || !recognizer.Command.CanExecute(recognizer.CommandParameter))
            {
                continue;
            }

            recognizer.Command.Execute(recognizer.CommandParameter);
            return true;
        }

        return false;
    }

    /// <summary>Raises the command of a swipe recognizer for the given direction.</summary>
    /// <param name="element">The element to swipe.</param>
    /// <param name="verb">The swipe verb.</param>
    /// <returns>Whether a recognizer's command was raised.</returns>
    internal static bool TrySwipeRecognizer(VisualElement element, BrinellVerb verb)
    {
        var direction = SwipeDirectionFor(verb);
        if (direction is null || element is not View view)
        {
            return false;
        }

        foreach (var recognizer in view.GestureRecognizers.OfType<SwipeGestureRecognizer>())
        {
            // Direction is a flags enum: one recognizer commonly declares several.
            if (!recognizer.Direction.HasFlag(direction.Value))
            {
                continue;
            }

            if (recognizer.Command is null || !recognizer.Command.CanExecute(recognizer.CommandParameter))
            {
                continue;
            }

            recognizer.Command.Execute(recognizer.CommandParameter);
            return true;
        }

        return false;
    }

    /// <summary>MAUI's name for the direction a swipe verb travels in.</summary>
    /// <param name="verb">The swipe verb.</param>
    /// <returns>The direction, or null if the verb is not a swipe.</returns>
    internal static SwipeDirection? SwipeDirectionFor(BrinellVerb verb) => verb switch
    {
        BrinellVerb.SwipeLeft => SwipeDirection.Left,
        BrinellVerb.SwipeRight => SwipeDirection.Right,
        BrinellVerb.SwipeUp => SwipeDirection.Up,
        BrinellVerb.SwipeDown => SwipeDirection.Down,
        _ => null,
    };


    // ---- Focus (step 13) ----------------------------------------------------------

    /// <summary>
    /// Gives the element logical focus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the verb the background-execution story rests on.</b> Keyboard focus and the
    /// desktop foreground window are two different things, and the framework had been
    /// conflating them: every physical action called <c>SetForeground</c> first, because global
    /// keystrokes go wherever the foreground is. <c>VisualElement.Focus</c> moves focus inside
    /// the app and touches no desktop-global state at all, so the machine stays with whoever is
    /// sitting at it.
    /// </para>
    /// <para>
    /// Returns MAUI's own answer rather than asserting success. A disabled or unfocusable
    /// element returns false, which travels back as a refusal and lets the client fall back.
    /// </para>
    /// </remarks>
    /// <param name="element">The element to focus.</param>
    /// <returns>Whether focus was taken.</returns>
    internal static bool TryFocus(VisualElement element) => element.Focus();

    /// <summary>Removes logical focus from the element.</summary>
    /// <param name="element">The element to unfocus.</param>
    internal static void Unfocus(VisualElement element) => element.Unfocus();

    // ---- Text (step 14) -----------------------------------------------------------

    /// <summary>
    /// Reads an element's editable text.
    /// </summary>
    /// <remarks>
    /// <see cref="InputView"/> rather than each of <c>Entry</c>, <c>Editor</c> and
    /// <c>SearchBar</c> in turn: it is their common MAUI base and the one that declares
    /// <c>Text</c>, so a control MAUI adds later is covered without an edit here. A
    /// <c>Label</c> has a <c>Text</c> too and is deliberately excluded - it is not input, and
    /// writing to one would let a test assert something no user could have done.
    /// </remarks>
    /// <param name="element">The element to read.</param>
    /// <param name="text">Its text, when this returns true.</param>
    /// <returns>Whether the element holds editable text.</returns>
    internal static bool TryGetText(VisualElement element, out string text)
    {
        if (element is InputView input)
        {
            text = input.Text ?? string.Empty;
            return true;
        }

        text = string.Empty;
        return false;
    }

    /// <summary>
    /// Replaces an element's text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What this replaces is worth naming.</b> The physical route for the same job is
    /// <c>TextInputMethod.Paste</c>, which writes the machine-wide clipboard and sends Ctrl+V -
    /// destroying whatever the person at the keyboard had copied, and corrupting a second run
    /// happening at the same time. This writes the property.
    /// </para>
    /// <para>
    /// <b>It is not a replacement for typing, and must not become one.</b> Setting <c>Text</c>
    /// raises <c>TextChanged</c> once for the whole value; a keyboard raises it per character,
    /// applies <c>MaxLength</c> as it goes, and lets a numeric keyboard refuse a letter. A test
    /// of input behaviour still types - that is what <c>TextInputMethod.Keys</c> is for.
    /// </para>
    /// </remarks>
    /// <param name="element">The element to write to.</param>
    /// <param name="text">The new text.</param>
    /// <returns>Whether the element accepted it.</returns>
    internal static bool TrySetText(VisualElement element, string text)
    {
        if (element is not InputView input)
        {
            return false;
        }

        // Refused here rather than left to the platform. WinUI enforces IsReadOnly on the
        // control it renders, but MAUI's own property setter does not - so a write through the
        // bridge would succeed on a field where every other route correctly fails, and a test
        // would pass against something a user cannot do.
        if (input is Entry { IsReadOnly: true } or Editor { IsReadOnly: true })
        {
            return false;
        }

        input.Text = text;
        return true;
    }

    /// <summary>
    /// Raises the element's completion action, as pressing Enter in it would.
    /// </summary>
    /// <remarks>
    /// The command, never the event. <c>Entry.SendCompleted</c> and its relatives are internal
    /// in MAUI 10, so an element whose completion handling is an event handler rather than a
    /// bound command cannot be reached from outside and this returns false - which travels back
    /// as a refusal and lets the client press a real Enter instead. That fallback is the honest
    /// outcome rather than a gap: the app has published no semantic route to its own behaviour.
    /// </remarks>
    /// <param name="element">The element to submit.</param>
    /// <returns>Whether a completion command was raised.</returns>
    internal static bool TrySubmit(VisualElement element) => element switch
    {
        Entry entry => Execute(entry.ReturnCommand, entry.ReturnCommandParameter),
        SearchBar search => Execute(search.SearchCommand, search.SearchCommandParameter),

        // Editor has no completion command in MAUI, and should not: Enter in a multi-line
        // field is a newline, so there is nothing to raise.
        _ => false,
    };

    // ---- Navigation ---------------------------------------------------------------

    /// <summary>
    /// Pops the element's navigation stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pulled forward from step 22 because step 15 cannot be finished without it. Returning to
    /// the hub was the suite's entire physical-input footprint, and the two alternatives are a
    /// mouse click on a <c>ToolbarItem</c> - which is what this replaces, and which no
    /// automation pattern can drive, measured four ways - or a desktop-wide Alt+Left.
    /// </para>
    /// <para>
    /// <b>Not awaited.</b> A verb's contract is that the action has been delivered, not that the
    /// animation has finished; a test that needs the next page waits for that page. Awaiting
    /// here would also block the UI thread the pop itself has to run on. The continuation exists
    /// only so a failed pop is reported rather than lost in an unobserved task.
    /// </para>
    /// </remarks>
    /// <param name="element">Any element on the page to leave.</param>
    /// <returns>Whether there was something to pop.</returns>
    internal static bool TryNavigateBack(VisualElement element)
    {
        if (element is not Page page)
        {
            // Only a page can pop itself. Nothing else declares this verb today, and an element
            // inside a page that did would be asking on the page's behalf without being able to
            // check the one thing that makes the answer safe.
            return false;
        }

        var navigation = page.Navigation;
        var stack = navigation?.NavigationStack;

        BridgeDiagnostics.Report(
            $"NavigateBack asked of '{page.AutomationId}' [{page.GetType().Name}]: "
            + $"stack depth {stack?.Count ?? -1}, top is "
            + $"'{(stack is { Count: > 0 } ? stack[^1]?.GetType().Name : "none")}', "
            + $"modal depth {navigation?.ModalStack.Count ?? -1}");

        if (stack is null || stack.Count <= 1)
        {
            return false;
        }

        // A page may only pop itself, and only while it is the one on top.
        //
        // Without this the verb was a menace. Every page that has been visited leaves a bridge
        // target behind - the registration is withdrawn on Unloaded, but a popped page keeps its
        // handler for a while and stays answerable - and a caller asking the app to go back
        // reaches whichever of them the walk finds first, which is the oldest. A stale page's
        // Navigation still reports the live stack, so it agreed to pop, returned success, and
        // popped nothing. The caller then waited out its timeout for a page change that was
        // never coming, three times over, and gave up on the hub still not being there.
        //
        // That one line cost five failures and doubled the suite's runtime, and it presented as
        // flakiness scattered across unrelated areas rather than as anything to do with
        // navigation.
        if (!ReferenceEquals(stack[^1], page))
        {
            return false;
        }

        // A pop already under way is not another pop to report.
        //
        // PopAsync is deliberately not awaited - see the remarks - so the stack still reads two
        // deep for a moment afterwards. Without this, a caller that asks twice in quick
        // succession is told "yes" twice: it pops a second page it never meant to, or, at the
        // root, hears that something was popped when nothing was. Both were observed.
        if (!PopsInFlight.Add(page))
        {
            BridgeDiagnostics.Report("NavigateBack: a pop is already in flight");
            return false;
        }

        BridgeDiagnostics.Report("NavigateBack: popping");

        _ = navigation!.PopAsync().ContinueWith(
            popped =>
            {
                PopsInFlight.Remove(page);

                if (popped.IsFaulted)
                {
                    BridgeDiagnostics.Report($"PopAsync failed: {popped.Exception}");
                }
            },
            TaskScheduler.Default);

        return true;
    }

    /// <summary>
    /// Pages whose pop has been started and has not yet finished.
    /// </summary>
    /// <remarks>
    /// A conditional weak table rather than a set, so a page that is popped and dropped is not
    /// kept alive by the bookkeeping that tracked its own removal.
    /// </remarks>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Page, object>
        InFlight = new();

    private static class PopsInFlight
    {
        internal static bool Add(Page page)
        {
            lock (InFlight)
            {
                if (InFlight.TryGetValue(page, out _))
                {
                    return false;
                }

                InFlight.Add(page, Marker);
                return true;
            }
        }

        internal static void Remove(Page page)
        {
            lock (InFlight)
            {
                InFlight.Remove(page);
            }
        }

        private static readonly object Marker = new();
    }

    /// <summary>Runs a bound command if it will accept the call.</summary>
    /// <param name="command">The command, which is commonly null.</param>
    /// <param name="parameter">Its parameter.</param>
    /// <returns>Whether the command ran.</returns>
    private static bool Execute(System.Windows.Input.ICommand? command, object? parameter)
    {
        if (command is null || !command.CanExecute(parameter))
        {
            return false;
        }

        command.Execute(parameter);
        return true;
    }
}
