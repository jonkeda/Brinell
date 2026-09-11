using Brinell.Uia;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// Runs a verb against a MAUI element: on the UI thread, within a budget, down a fixed ladder.
/// </summary>
/// <remarks>
/// <para>
/// <b>The threading model is here from the start deliberately.</b> UI Automation calls a
/// provider on whatever thread it likes, and touching a MAUI element off the UI thread is
/// undefined at best. Retrofitting the marshalling later would mean revisiting every verb, so
/// no verb is ever written without it.
/// </para>
/// <para>
/// <b>And so is the timeout.</b> A provider that waits forever does not fail the client - it
/// hangs it, and UI Automation's own transaction timeout then stalls every accessibility client
/// on the desktop rather than only the test. A bounded wait that returns
/// <see cref="HResults.UIA_E_TIMEOUT"/> is the difference between a failing test and a wedged
/// machine.
/// </para>
/// <para>
/// <b>The ladder.</b> Sink first, so an app can always override; then the public MAUI API for
/// the control's own type; then a matching gesture recognizer's command; then refusal. Each
/// rung is tried only if the one above declined, and refusal is a real answer - it is what
/// tells a test to use another route rather than to retry.
/// </para>
/// </remarks>
internal static class MauiVerbDispatcher
{
    /// <summary>
    /// How long a verb may take before the bridge gives up on it.
    /// </summary>
    /// <remarks>
    /// Generous for a UI action and far below UI Automation's own transaction timeout, so a
    /// stuck verb reports itself rather than being reported by the client as a dead connection.
    /// </remarks>
    private const int TimeoutMs = 5_000;

    internal static int Invoke(
        VisualElement element, IBrinellGestureSink? sink, BrinellVerb verb, int arg1, int arg2)
    {
        var hr = OnUiThread(element, () => PerformInvoke(element, sink, verb, arg1, arg2));

        // Reported from inside the app, because from the test side a verb that ran and had no
        // effect is indistinguishable from one that never arrived.
        BridgeDiagnostics.Report(
            $"Invoke {verb}({arg1},{arg2}) on '{element.AutomationId}' "
            + $"[{element.GetType().Name}] -> 0x{hr:X8}");

        return hr;
    }

    internal static int Exchange(
        VisualElement element,
        IBrinellGestureSink? sink,
        BrinellVerb verb,
        string argument,
        out string result)
    {
        string? captured = null;

        var hr = OnUiThread(
            element,
            () => PerformExchange(element, sink, verb, argument, out captured));

        result = captured ?? string.Empty;
        return hr;
    }

    private static int PerformInvoke(
        VisualElement element, IBrinellGestureSink? sink, BrinellVerb verb, int arg1, int arg2)
    {
        // Rung 1: the app's own handling, which always wins.
        if (sink is not null && sink.TryInvoke(element, verb, arg1, arg2))
        {
            return HResults.S_OK;
        }

        // Rung 2: the public MAUI API for this control type.
        switch (element)
        {
            case SwipeView swipeView when MauiCapabilities.SwipeItemFor(verb) is not null:
                return MauiCapabilities.TryOpenSwipeView(swipeView, verb)
                    ? HResults.S_OK
                    : HResults.UIA_E_NOTSUPPORTED;

            case RefreshView refreshView when verb == BrinellVerb.SwipeDown:
                // S_FALSE, not a failure: a refresh already running is a real state, and
                // reporting it as success would let a test assert a refresh it did not cause.
                return MauiCapabilities.TryStartRefresh(refreshView)
                    ? HResults.S_OK
                    : HResults.S_FALSE;
        }

        // Rung 3: a gesture recognizer on the element itself.
        switch (verb)
        {
            case BrinellVerb.Tap when MauiCapabilities.TryTap(element, 1):
            case BrinellVerb.DoubleTap when MauiCapabilities.TryTap(element, 2):
                return HResults.S_OK;

            case BrinellVerb.SwipeLeft:
            case BrinellVerb.SwipeRight:
            case BrinellVerb.SwipeUp:
            case BrinellVerb.SwipeDown:
                if (MauiCapabilities.TrySwipeRecognizer(element, verb))
                {
                    return HResults.S_OK;
                }

                break;

            case BrinellVerb.Focus:
                return MauiCapabilities.TryFocus(element)
                    ? HResults.S_OK
                    : HResults.UIA_E_NOTSUPPORTED;

            case BrinellVerb.Unfocus:
                MauiCapabilities.Unfocus(element);
                return HResults.S_OK;

            case BrinellVerb.CloseFlyout when element is SwipeView openSwipeView:
                MauiCapabilities.CloseSwipeView(openSwipeView);
                return HResults.S_OK;

            case BrinellVerb.NavigateBack:
                // S_FALSE, not a failure: nothing to pop is a real state of the app, and
                // reporting it as success would let a test believe it had navigated.
                return MauiCapabilities.TryNavigateBack(element)
                    ? HResults.S_OK
                    : HResults.S_FALSE;
        }

        // Rung 4. Not a crash and not a retry - a fact about this element.
        return HResults.UIA_E_NOTSUPPORTED;
    }

    private static int PerformExchange(
        VisualElement element,
        IBrinellGestureSink? sink,
        BrinellVerb verb,
        string argument,
        out string? result)
    {
        result = null;

        if (sink is not null && sink.TryExchange(element, verb, argument, out var handled))
        {
            result = handled;
            return HResults.S_OK;
        }

        switch (verb)
        {
            case BrinellVerb.IsFocused:
                result = element.IsFocused ? bool.TrueString : bool.FalseString;
                return HResults.S_OK;

            case BrinellVerb.GetState:
                result = ReadState(element, argument);
                return result is null ? HResults.UIA_E_NOTSUPPORTED : HResults.S_OK;

            case BrinellVerb.GetText:
                if (!MauiCapabilities.TryGetText(element, out var current))
                {
                    return HResults.UIA_E_NOTSUPPORTED;
                }

                result = current;
                return HResults.S_OK;

            case BrinellVerb.SetText:
                return Written(element, argument, out result);

            case BrinellVerb.ClearText:
                // The argument is ignored rather than rejected. Clearing is setting to empty,
                // and a client that sends the empty string it is about to write should not have
                // to know which of the two spellings this end prefers.
                return Written(element, string.Empty, out result);

            case BrinellVerb.AppendText:
                // Read and write on the same UI-thread pass, which is what makes this safe:
                // splitting it into two verbs would let anything the app does in between land
                // in the middle, and the client would be writing over a value it no longer has.
                if (!MauiCapabilities.TryGetText(element, out var existing))
                {
                    return HResults.UIA_E_NOTSUPPORTED;
                }

                return Written(element, existing + argument, out result);

            case BrinellVerb.Submit:
                return MauiCapabilities.TrySubmit(element)
                    ? HResults.S_OK
                    : HResults.UIA_E_NOTSUPPORTED;
        }

        // Dates, routes and selection are steps 20 through 26. Refusing them by falling through
        // to a named refusal rather than to nothing keeps the boundary explicit: this build
        // genuinely does not implement them, and a client should be told so rather than left to
        // infer it from a value that never changed.
        return HResults.UIA_E_NOTSUPPORTED;
    }

    /// <summary>
    /// Writes text to the element and reports what the element made of it.
    /// </summary>
    /// <remarks>
    /// The write is not assumed to have taken. A field with a <c>MaxLength</c>, a converter or
    /// a two-way binding that rejects the value leaves something other than what was sent, and
    /// returning the value now in the control lets the client see that without a second round
    /// trip. <see cref="HResults.S_FALSE"/> says so explicitly.
    /// </remarks>
    private static int Written(VisualElement element, string text, out string? actual)
    {
        actual = null;

        if (!MauiCapabilities.TrySetText(element, text))
        {
            return HResults.UIA_E_NOTSUPPORTED;
        }

        MauiCapabilities.TryGetText(element, out var landed);
        actual = landed;

        return landed == text ? HResults.S_OK : HResults.S_FALSE;
    }

    /// <summary>
    /// Reads a property of the element that a user could perceive.
    /// </summary>
    /// <remarks>
    /// <c>BindingContext</c> and arbitrary reflection stay out, deliberately. If an assertion
    /// needs the view model then it is a view-model test and belongs in
    /// <c>Brinell.Maui.Tests</c>, where it runs in milliseconds without a window.
    /// </remarks>
    private static string? ReadState(VisualElement element, string property) => property switch
    {
        "IsVisible" => element.IsVisible.ToString(),
        "IsEnabled" => element.IsEnabled.ToString(),
        "IsFocused" => element.IsFocused.ToString(),
        "AutomationId" => element.AutomationId ?? string.Empty,

        // Where the element is, as "left,top,width,height" in physical pixels.
        //
        // A verb rather than a published property, and the difference is not cosmetic. UI
        // Automation reads an element's bounds every time anything walks past it, and reading
        // MAUI layout means a hop onto the app's UI thread - so publishing this made every tree
        // walk in the client cost a round trip per bridge element, and took the suite from four
        // minutes to over twenty. Asked as a verb, it is paid for once by whoever wants it.
        "Bounds" => ElementBounds.TryGetScreenBounds(element, out var bounds)
            ? string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3}",
                bounds.Left, bounds.Top, bounds.Width, bounds.Height)
            : null,

        _ => null,
    };

    /// <summary>
    /// Runs the work on the element's dispatcher and waits, bounded.
    /// </summary>
    /// <remarks>
    /// The <c>IsDispatchRequired</c> check is not an optimisation. UI Automation can call a
    /// provider on the UI thread; queueing the work there and then blocking that same thread
    /// waiting for it is a deadlock that resolves only when the timeout expires.
    /// </remarks>
    private static int OnUiThread(VisualElement element, Func<int> work)
        => Marshal(element, work, HResults.UIA_E_TIMEOUT, HResults.UIA_E_ELEMENTNOTAVAILABLE);

    /// <summary>
    /// Runs anything that touches the UI framework on the UI thread, bounded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not only verbs.</b> UI Automation calls a provider's property getters from its own
    /// threads, far more often than it calls its methods, and a WinUI object read from the wrong
    /// thread does not throw something catchable - it raises a stowed exception that fails the
    /// process fast, inside <c>Microsoft.UI.Xaml.dll</c>, with no managed stack. That looks
    /// exactly like the automation tree collapsing, which is a different and far more alarming
    /// diagnosis. Everything that reaches a platform view goes through here.
    /// </para>
    /// <para>
    /// The <c>IsDispatchRequired</c> check is not an optimisation. UI Automation can call a
    /// provider on the UI thread; queueing work there and then blocking that same thread waiting
    /// for it is a deadlock that resolves only when the timeout expires.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">What the work returns.</typeparam>
    /// <param name="element">The element whose dispatcher to use.</param>
    /// <param name="work">The work. Must not throw; it is guarded anyway.</param>
    /// <param name="onTimeout">What to return if the UI thread does not get to it in time.</param>
    /// <param name="onFailure">What to return if the work throws or cannot be queued.</param>
    /// <returns>The result, or one of the two fallbacks.</returns>
    internal static T Marshal<T>(VisualElement element, Func<T> work, T onTimeout, T onFailure)
    {
        var dispatcher = element.Dispatcher;

        if (dispatcher is null || !dispatcher.IsDispatchRequired)
        {
            return Guarded(work, onFailure);
        }

        var result = onTimeout;
        using var completed = new ManualResetEventSlim(false);

        if (!dispatcher.Dispatch(() =>
            {
                try
                {
                    result = Guarded(work, onFailure);
                }
                finally
                {
                    completed.Set();
                }
            }))
        {
            return onFailure;
        }

        // On timeout the queued work may still run later. It is idempotent per verb and acts on
        // the app's own UI, so letting it complete is safer than trying to cancel it; the client
        // is told the verb did not finish in time, which is the truth.
        return completed.Wait(TimeoutMs) ? result : onTimeout;
    }

    private static T Guarded<T>(Func<T> work, T onFailure)
    {
        try
        {
            return work();
        }
        catch (Exception)
        {
            // The app's own code runs inside a verb - a command, a sink, a property setter.
            // When it throws, the bridge reports a failure and the app carries on; it does not
            // take the process down and it does not let the exception reach COM.
            return onFailure;
        }
    }
}
