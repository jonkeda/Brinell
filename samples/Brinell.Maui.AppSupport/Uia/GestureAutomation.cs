using Brinell.Uia;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// Declares in XAML which verbs an element answers.
/// </summary>
/// <remarks>
/// <para>
/// <b>Declared, never inferred.</b> The bridge could walk the visual tree and publish anything
/// that looks drivable, and that would be worse: an element becomes automatable by accident,
/// the published surface changes when someone refactors a layout, and there is no one place to
/// look to find out what a test can do. One attribute in the markup keeps the answer next to
/// the element and makes it reviewable.
/// </para>
/// <example>
/// <code>
/// &lt;SwipeView AutomationId="TestSwipeView"
///            uia:GestureAutomation.Verbs="SwipeRight" /&gt;
/// </code>
/// </example>
/// <para>
/// <b>An <c>AutomationId</c> is required</b>, because it is the only join between the element a
/// test found and the bridge element that acts on it. An element declaring verbs without one is
/// reported on the debug output and skipped; it cannot be addressed, so publishing it would
/// only make the raw tree harder to read.
/// </para>
/// </remarks>
public static class GestureAutomation
{
    /// <summary>
    /// The verbs this element answers: <see cref="BrinellVerb"/> names, comma or space separated.
    /// </summary>
    /// <remarks>
    /// Names rather than numbers, so markup reads as intent and a typo is caught at load rather
    /// than dispatching something unintended. The numbers are the wire format and stay there.
    /// </remarks>
    public static readonly BindableProperty VerbsProperty = BindableProperty.CreateAttached(
        "Verbs",
        typeof(string),
        typeof(GestureAutomation),
        defaultValue: null,
        propertyChanged: OnVerbsChanged);

    /// <summary>
    /// An <see cref="IBrinellGestureSink"/> that gets first refusal on this element's verbs.
    /// </summary>
    /// <remarks>
    /// Optional, and usually absent. It exists for controls whose behaviour the bridge cannot
    /// reach through a public MAUI API - the alternative for those is the pointer.
    /// </remarks>
    public static readonly BindableProperty SinkProperty = BindableProperty.CreateAttached(
        "Sink",
        typeof(IBrinellGestureSink),
        typeof(GestureAutomation),
        defaultValue: null,
        propertyChanged: OnSinkChanged);

    /// <summary>Reads the declared verbs.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The declaration, or null.</returns>
    public static string? GetVerbs(BindableObject element)
        => (string?)element.GetValue(VerbsProperty);

    /// <summary>Declares the verbs an element answers.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value"><see cref="BrinellVerb"/> names, comma or space separated.</param>
    public static void SetVerbs(BindableObject element, string? value)
        => element.SetValue(VerbsProperty, value);

    /// <summary>Reads the attached sink.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The sink, or null.</returns>
    public static IBrinellGestureSink? GetSink(BindableObject element)
        => (IBrinellGestureSink?)element.GetValue(SinkProperty);

    /// <summary>Attaches a sink that gets first refusal on this element's verbs.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The sink.</param>
    public static void SetSink(BindableObject element, IBrinellGestureSink? value)
        => element.SetValue(SinkProperty, value);

    /// <summary>
    /// Reads a declaration into verbs, reporting anything it could not.
    /// </summary>
    /// <remarks>
    /// Case-insensitive, and tolerant of either separator, because this is markup written by
    /// hand. An unrecognised name is reported and dropped rather than throwing: a typo in a test
    /// hook should not stop the app from starting.
    /// </remarks>
    /// <param name="declaration">The attached-property value.</param>
    /// <returns>The verbs named, in wire order.</returns>
    internal static IReadOnlyCollection<BrinellVerb> ParseVerbs(string? declaration)
    {
        if (string.IsNullOrWhiteSpace(declaration))
        {
            return [];
        }

        var verbs = new List<BrinellVerb>();

        foreach (var name in declaration.Split(
                     [',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse<BrinellVerb>(name, ignoreCase: true, out var verb)
                && verb != BrinellVerb.None)
            {
                verbs.Add(verb);
                continue;
            }

            BridgeDiagnostics.Report(
                $"'{name}' is not a Brinell verb and was ignored. Valid names are the "
                + $"members of {nameof(BrinellVerb)}.");
        }

        return verbs.Distinct().Order().ToArray();
    }

    private static void OnVerbsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        BridgeDiagnostics.Report($"declared on {bindable.GetType().Name}: {newValue}");

        if (bindable is not VisualElement element)
        {
            BridgeDiagnostics.Report(
                $"{nameof(VerbsProperty)} was set on {bindable.GetType().Name}, which is not a "
                + "VisualElement. Only visual elements can be published to the bridge.");
            return;
        }

        Attach(element);
    }

    private static void OnSinkChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // The sink is captured when the element is published, so changing it after the fact has
        // to republish. In practice it is set once in markup, before Loaded.
        if (bindable is VisualElement element && GetVerbs(element) is not null)
        {
            Attach(element);
        }
    }

    /// <summary>
    /// Subscribes the element to its own lifecycle, once.
    /// </summary>
    /// <remarks>
    /// Publishing happens on <c>Loaded</c> rather than here because the element has no handler
    /// yet when its properties are set from markup, and without a handler there is no window to
    /// attach a bridge to. Unsubscribing first makes this safe to call again when the
    /// declaration changes.
    /// </remarks>
    private static void Attach(VisualElement element)
    {
        element.Loaded -= OnLoaded;
        element.Unloaded -= OnUnloaded;

        element.Loaded += OnLoaded;
        element.Unloaded += OnUnloaded;

        // An element already loaded when the declaration arrives - set from code rather than
        // markup - would otherwise wait for a Loaded that has been and gone.
        if (element.Handler is not null)
        {
            Publish(element);
        }
    }

    private static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is VisualElement element)
        {
            Publish(element);
        }
    }

    private static void OnUnloaded(object? sender, EventArgs e)
    {
        if (sender is VisualElement element)
        {
            BrinellBridgeHost.Withdraw(element);
        }
    }

    private static void Publish(VisualElement element)
    {
        var verbs = ParseVerbs(GetVerbs(element));
        if (verbs.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(element.AutomationId))
        {
            BridgeDiagnostics.Report(
                $"a {element.GetType().Name} declares verbs ({string.Join(", ", verbs)}) but has "
                + "no AutomationId, so no test can address it. Give it an AutomationId or remove "
                + "the declaration.");
            return;
        }

        BrinellBridgeHost.Publish(element, verbs, GetSink(element));
    }
}
