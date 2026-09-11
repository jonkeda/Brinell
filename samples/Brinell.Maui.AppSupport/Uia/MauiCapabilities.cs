using System.Globalization;
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
    //
    // Every method below is either a question or a command, and none of them is both. That is
    // what removed the Try prefix from this section: TryTap used to search for a recognizer and
    // execute its command in one call, which is why it could not report which half had failed.
    // The searches now happen once, when an element is published (see VerbBindings), and what is
    // left here are plain actions that need no return value because the search already answered.

    /// <summary>
    /// Opens the swipe items a swipe in the given direction would reveal.
    /// </summary>
    /// <remarks>
    /// Opened without animation. A test that waits for an animation is a test with a sleep in
    /// it, and the state the assertion is about is reached either way.
    /// </remarks>
    /// <param name="swipeView">The view to open.</param>
    /// <param name="item">Which items to reveal, from <see cref="SwipeItemFor"/>.</param>
    internal static void OpenSwipeView(SwipeView swipeView, OpenSwipeItem item)
        => swipeView.Open(item, false);

    /// <summary>Which swipe items a swipe in this direction uncovers.</summary>
    /// <remarks>
    /// <para>
    /// <b>The mapping is inverted, and that is correct.</b> Swiping your finger to the right
    /// drags the content right and uncovers what is behind its left edge, so
    /// <see cref="BrinellVerb.SwipeRight"/> opens <see cref="OpenSwipeItem.LeftItems"/>. It
    /// reads like a bug and is the single most likely thing here to be "fixed" backwards, which
    /// is why <c>SwipeMappingTests</c> pins it.
    /// </para>
    /// <para>
    /// A question, and named like one: null means "that verb is not a swipe", which is an answer
    /// rather than a failure.
    /// </para>
    /// </remarks>
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
    /// <c>Refreshing</c> and runs its command from the property change. Whether a refresh is
    /// already running is the caller's question to ask - <c>RefreshView.IsRefreshing</c> answers
    /// it - and asking it there is what lets the bridge report "already refreshing" as its own
    /// state rather than as a failure to start one.
    /// </remarks>
    /// <param name="refreshView">The view to refresh.</param>
    internal static void StartRefresh(RefreshView refreshView) => refreshView.IsRefreshing = true;

    // ---- Dates and times ----------------------------------------------------------
    //
    // Two properties, and that is the entire feature. What it replaces on the client side is
    // roughly two hundred lines of WinUI calendar navigation - open the flyout, read the header,
    // page to the right month, find the day, select it - written because there was no other way
    // to set a date without a pointer. The app has always been able to just say so.

    /// <summary>
    /// The one format a date crosses the wire in.
    /// </summary>
    /// <remarks>
    /// Invariant and round-trippable, deliberately. The value is written by a test and read by an
    /// app that may be running under any culture, and a date that means one thing on one side of
    /// the wire and another on the other is the classic way for this to fail in June and pass in
    /// July.
    /// </remarks>
    internal const string DateFormat = "yyyy-MM-dd";

    /// <summary>The one format a time crosses the wire in. See <see cref="DateFormat"/>.</summary>
    /// <remarks>
    /// Twenty-four hour, and that is the point: <c>TimePicker</c> reading 15:30 back as 03:30 is
    /// an open defect against the flyout route this verb exists to replace.
    /// </remarks>
    internal const string TimeFormat = @"hh\:mm\:ss";

    /// <summary>Sets a date picker's date.</summary>
    /// <param name="picker">The picker.</param>
    /// <param name="date">The date, already parsed.</param>
    internal static void SetDate(DatePicker picker, DateTime date) => picker.Date = date;

    /// <summary>Sets a time picker's time.</summary>
    /// <param name="picker">The picker.</param>
    /// <param name="time">The time, already parsed.</param>
    internal static void SetTime(TimePicker picker, TimeSpan time) => picker.Time = time;

    /// <summary>Reads a date picker's date in the wire format.</summary>
    /// <remarks>
    /// <b>Both of these are nullable in MAUI 10</b> - <c>DatePicker.Date</c> is
    /// <c>DateTime?</c> and <c>TimePicker.Time</c> is <c>TimeSpan?</c> - which is easy to miss
    /// because the pickers always show something. An empty string is the honest reading of a
    /// picker holding no value, and it is distinguishable from every real one.
    /// </remarks>
    /// <param name="picker">The picker.</param>
    /// <returns>The date, or empty if it holds none.</returns>
    internal static string ReadDate(DatePicker picker)
        => picker.Date?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Reads a time picker's time in the wire format.</summary>
    /// <param name="picker">The picker.</param>
    /// <returns>The time, or empty if it holds none.</returns>
    internal static string ReadTime(TimePicker picker)
        => picker.Time?.ToString(TimeFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    // ---- Scrolling ------------------------------------------------------------------
    //
    // ScrollToAsync and ScrollTo are cross-platform MAUI APIs, which makes this the largest
    // parity win in the catalogue: the same verb does the same thing on Windows, Android and
    // iOS. What it replaces on Windows is a loop of mouse-wheel clicks - an unquantified unit,
    // with no completion signal, so the caller polled a scroll percentage and guessed when it
    // had stopped moving.

    /// <summary>
    /// Brings a named descendant into view.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Never animated.</b> An animated scroll finishes some time after the call returns, so a
    /// test either sleeps or races; unanimated, the layout is settled when the await completes
    /// and the position the caller reads back is the real one.
    /// </para>
    /// <para>
    /// The search is by <c>AutomationId</c> and covers the whole subtree, because the thing a
    /// test wants to scroll to is usually nested several layouts deep inside the scroller.
    /// </para>
    /// </remarks>
    /// <param name="scrollView">The scroller.</param>
    /// <param name="automationId">The descendant to reveal.</param>
    /// <returns>Whether a descendant with that id was found.</returns>
    internal static bool ScrollTo(ScrollView scrollView, string automationId)
    {
        var target = Descendants(scrollView)
            .FirstOrDefault(child => child.AutomationId == automationId);

        if (target is null)
        {
            return false;
        }

        // Fire and forget is deliberate and safe here: with animation off, MAUI applies the
        // scroll synchronously and the returned task completes on the next tick. Awaiting it
        // would mean marshalling a result back through a verb that has nothing to report.
        _ = scrollView.ScrollToAsync(target, ScrollToPosition.MakeVisible, false);
        return true;
    }

    /// <summary>
    /// Brings the item at an index into view.
    /// </summary>
    /// <remarks>
    /// <c>ItemsView</c> covers <c>CollectionView</c> and <c>CarouselView</c>; <c>ListView</c> is
    /// separate and scrolls to an <i>item</i> rather than an index, so its index has to be
    /// resolved against <c>ItemsSource</c> first. That difference is MAUI's, not ours.
    /// </remarks>
    /// <param name="element">The collection.</param>
    /// <param name="index">The item index.</param>
    /// <returns>Whether the element is a collection that could scroll.</returns>
    internal static bool ScrollToIndex(VisualElement element, int index)
    {
        switch (element)
        {
            case ItemsView itemsView:
                // Range-checked rather than left to MAUI. An index past the end is how a caller
                // walking a virtualized list finds out it has reached the end, so it has to be a
                // quiet no and not an exception crossing the bridge as a failure.
                if (index < 0 || index >= Count(itemsView.ItemsSource))
                {
                    return false;
                }

                itemsView.ScrollTo(index, position: ScrollToPosition.MakeVisible, animate: false);
                return true;

            case ListView listView:
                var item = listView.ItemsSource?.Cast<object>().ElementAtOrDefault(index);
                if (item is null)
                {
                    return false;
                }

                listView.ScrollTo(item, ScrollToPosition.MakeVisible, animated: false);
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Where a scroller is, and how much there is to scroll.
    /// </summary>
    /// <remarks>
    /// <b>Offset and extent together, because neither means anything alone.</b> A test asserting
    /// "we are at the bottom" needs the offset, the viewport and the content size to say so; a
    /// percentage - which is what UI Automation offers - cannot distinguish a short page that
    /// cannot scroll from a long one already at the end.
    /// </remarks>
    /// <param name="scrollView">The scroller.</param>
    /// <returns>"x,y,viewportWidth,viewportHeight,contentWidth,contentHeight", invariant.</returns>
    internal static string ReadScrollPosition(ScrollView scrollView)
        => string.Format(
            CultureInfo.InvariantCulture,
            "{0},{1},{2},{3},{4},{5}",
            scrollView.ScrollX,
            scrollView.ScrollY,
            scrollView.Width,
            scrollView.Height,
            scrollView.ContentSize.Width,
            scrollView.ContentSize.Height);

    /// <summary>How many items a source holds, without enumerating it if it can be asked.</summary>
    /// <param name="source">The items source, possibly null.</param>
    /// <returns>The count, or zero.</returns>
    private static int Count(System.Collections.IEnumerable? source) => source switch
    {
        null => 0,
        System.Collections.ICollection collection => collection.Count,
        _ => source.Cast<object>().Count(),
    };

    /// <summary>Every element below this one, depth first.</summary>
    /// <remarks>
    /// <c>IVisualTreeElement</c> rather than <c>LogicalChildrenInternal</c>, which is internal in
    /// MAUI 10 - and this file does not reflect into MAUI internals, for the reason stated at the
    /// top of it. The visual tree is also the more correct one to walk here: a test scrolls to
    /// something it can see.
    /// </remarks>
    /// <param name="root">Where to start. Not included in the result.</param>
    /// <returns>The descendants.</returns>
    private static IEnumerable<VisualElement> Descendants(IVisualTreeElement root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is VisualElement visual)
            {
                yield return visual;
            }

            foreach (var deeper in Descendants(child))
            {
                yield return deeper;
            }
        }
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
    internal static int NavigateBack(VisualElement element)
    {
        if (element is not Page page)
        {
            // Only a page can pop itself. Nothing else declares this verb today, and an element
            // inside a page that did would be asking on the page's behalf without being able to
            // check the one thing that makes the answer safe.
            return HResults.UIA_E_NOTSUPPORTED;
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
            // S_FALSE: we are at the root and there is nothing to pop. A true statement about
            // the app, and the commonest answer of all - every fixture reset that starts at the
            // hub gets it. It used to be indistinguishable from the three below, which is what
            // made the caller wait two seconds to find out something it was told immediately.
            return HResults.S_FALSE;
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
            // UIA_E_ELEMENTNOTAVAILABLE: this target is stale. Distinct from S_FALSE because the
            // caller should do something different - ask another target, not give up - and
            // because a stale page agreeing to pop is the defect described above.
            return HResults.UIA_E_ELEMENTNOTAVAILABLE;
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
            return HResults.S_FALSE;
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

        return HResults.S_OK;
    }

    /// <summary>
    /// How deep the app's navigation stack is.
    /// </summary>
    /// <remarks>
    /// <b>Any live target answers this correctly, stale or not</b>, which is what makes it usable
    /// as a whole-app question. A page's <c>Navigation</c> reports the <i>current</i> stack
    /// whoever is asked, so unlike <see cref="NavigateBack"/> - where a popped page will happily
    /// agree to pop and then not - there is no wrong element to reach here.
    /// </remarks>
    /// <param name="element">Any element that can see the navigation stack.</param>
    /// <returns>The depth, or null if this element has no navigation.</returns>
    internal static int? NavigationDepth(VisualElement element)
        => element is Page page ? page.Navigation?.NavigationStack.Count : null;

    /// <summary>
    /// Where the app is, in whatever terms its navigation model uses.
    /// </summary>
    /// <remarks>
    /// Shell has a route and says so. A <c>NavigationPage</c> app has no such thing, so the
    /// honest answer is the identity of the page on top - which is what a test asserting "we are
    /// on the hub" actually means. Returning a fabricated route for the second case would make
    /// the two look alike when they are not.
    /// </remarks>
    /// <param name="element">Any element that can see the navigation stack.</param>
    /// <returns>The Shell route, or the top page's AutomationId, or empty.</returns>
    internal static string CurrentRoute(VisualElement element)
    {
        if (Shell.Current is { } shell)
        {
            return shell.CurrentState?.Location?.ToString() ?? string.Empty;
        }

        if (element is not Page page)
        {
            return string.Empty;
        }

        var stack = page.Navigation?.NavigationStack;
        var top = stack is { Count: > 0 } ? stack[^1] : null;

        return top?.AutomationId ?? top?.GetType().Name ?? string.Empty;
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
