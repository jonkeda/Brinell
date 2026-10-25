using System.Collections;
using System.Reflection;
using Microsoft.Maui.Controls;

namespace Bruinell.Maui.Munit;

/// <summary>
/// The app's own verb sinks, called in process: what an app instrumented for Bruinell's Windows
/// bridge (<c>Bruinell.Maui.AppSupport</c>) attaches with <c>GestureAutomation.Sink</c> answers here
/// as it answers there, so a long press, a pan, a toolkit control's state run the app's own code.
/// </summary>
/// <remarks>
/// Found by name, not referenced: AppSupport is a library an app may copy in as source, so its
/// types can live in the app's own assembly. An app without it simply has no sinks.
/// </remarks>
internal static class MunitAppBridge
{
    private const string AutomationTypeName = "Bruinell.Maui.AppSupport.Uia.GestureAutomation";

    private static readonly Lazy<BindableProperty?> SinkProperty = new(Find);

    /// <summary>Whether <paramref name="element"/>'s sink names <paramref name="verb"/>.</summary>
    public static bool Names(Element element, string verb) => Sink(element, verb) is not null;

    /// <summary>Runs a gesture verb on the element's sink; false where no sink names it.</summary>
    /// <exception cref="Core.Exceptions.BruinellException">The sink refused it.</exception>
    public static bool TryInvoke(Element element, string verb, int arg1, int arg2)
    {
        if (Sink(element, verb) is not { } sink)
            return false;

        var method = sink.GetType().GetMethod("Invoke")!;
        var verbValue = Enum.Parse(method.GetParameters()[1].ParameterType, verb);
        var result = (int)Unwrap(() => method.Invoke(sink, [element, verbValue, arg1, arg2]))!;
        if (result < 0)
            throw new Core.Exceptions.BruinellException($"The app's {sink.GetType().Name} declined {verb} on {element.GetType().Name} #{element.AutomationId} (0x{result:X8}).");
        return true;
    }

    /// <summary>Asks the element's sink a string verb (GetState); null where no sink names it.</summary>
    public static string? TryExchange(Element element, string verb, string argument)
    {
        if (Sink(element, verb) is not { } sink)
            return null;

        var method = sink.GetType().GetMethod("Exchange")
            ?? sink.GetType().GetInterfaces().Select(i => i.GetMethod("Exchange")).First(m => m is not null)!;
        var verbValue = Enum.Parse(method.GetParameters()[1].ParameterType, verb);
        return (string?)Unwrap(() => method.Invoke(sink, [element, verbValue, argument]));
    }

    private static object? Sink(Element element, string verb)
    {
        if (SinkProperty.Value is not { } sinkProperty || element.GetValue(sinkProperty) is not { } sink)
            return null;

        var verbs = sink.GetType().GetProperty("Verbs")?.GetValue(sink) as IEnumerable;
        return verbs?.Cast<object>().Any(named => named.ToString() == verb) == true ? sink : null;
    }

    private static BindableProperty? Find()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || assembly.GetType(AutomationTypeName) is not { } type)
                continue;

            return type.GetField("SinkProperty", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as BindableProperty;
        }

        return null;
    }

    private static object? Unwrap(Func<object?> call)
    {
        try
        {
            return call();
        }
        catch (TargetInvocationException wrapped) when (wrapped.InnerException is { } inner)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

}
