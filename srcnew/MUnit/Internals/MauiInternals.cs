using System.Collections;
using System.Reflection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MUnit.Internals;

/// <summary>
/// The only place MUnit reaches into MAUI's internals, each by name and checked when first used:
/// a MAUI release that renames one fails here with that name, not somewhere in a test.
/// </summary>
internal static class MauiInternals
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // TapGestureRecognizer.SendTapped(View sender, Func<IElement?, Point?>? getPosition): the platform's
    // tap handler calls it; it runs the Command (when CanExecute) and raises Tapped.
    private static readonly Lazy<MethodInfo> SendTappedMethod = new(() =>
        typeof(TapGestureRecognizer).GetMethods(Instance)
            .SingleOrDefault(m => m.Name == "SendTapped" && m.GetParameters().Length == 2)
        ?? throw Missing("TapGestureRecognizer.SendTapped(View, Func<IElement, Point?>)"));

    // BindableObject.GetContext(BindableProperty) and its BindablePropertyContext.Bindings: where the
    // binding set on a property is kept.
    private static readonly Lazy<MethodInfo> GetContextMethod = new(() =>
        typeof(BindableObject).GetMethod("GetContext", Instance, [typeof(BindableProperty)])
        ?? throw Missing("BindableObject.GetContext(BindableProperty)"));

    // Application.AddWindow(Window) / RemoveWindow(Window): what IApplication.OpenWindow does once the
    // platform has made the window. It parents the window to the app and lists it in Windows, which
    // page lifecycle (SendAppearing) and Shell.Current need.
    private static readonly Lazy<MethodInfo> AddWindowMethod = new(() =>
        typeof(Application).GetMethod("AddWindow", Instance, [typeof(Window)])
        ?? throw Missing("Application.AddWindow(Window)"));

    private static readonly Lazy<MethodInfo> RemoveWindowMethod = new(() =>
        typeof(Application).GetMethod("RemoveWindow", Instance, [typeof(Window)])
        ?? throw Missing("Application.RemoveWindow(Window)"));

    /// <summary>Opens <paramref name="window"/> in <paramref name="application"/> without a platform.</summary>
    public static void AddWindow(Application application, Window window)
        => AddWindowMethod.Value.Invoke(application, [window]);

    /// <summary>Closes <paramref name="window"/> in <paramref name="application"/>.</summary>
    public static void RemoveWindow(Application application, Window window)
        => RemoveWindowMethod.Value.Invoke(application, [window]);

    /// <summary>Taps <paramref name="view"/> through <paramref name="recognizer"/>, as the platform does.</summary>
    public static void SendTapped(TapGestureRecognizer recognizer, View view)
        => SendTappedMethod.Value.Invoke(recognizer, [view, null]);

    /// <summary>The binding set on <paramref name="property"/> of <paramref name="bindable"/>, or null.</summary>
    public static BindingBase? GetBinding(BindableObject bindable, BindableProperty property)
    {
        var context = GetContextMethod.Value.Invoke(bindable, [property]);
        if (context is null)
            return null;

        var bindings = context.GetType().GetField("Bindings", Instance)?.GetValue(context)
            ?? throw Missing("BindablePropertyContext.Bindings");

        // A SetterSpecificityList<BindingBase>: the binding in force is its highest-specificity value.
        return bindings.GetType().GetMethod("GetValue", Instance, Type.EmptyTypes)?.Invoke(bindings, null) as BindingBase
            ?? (bindings as IEnumerable)?.Cast<object>().Select(Value).OfType<BindingBase>().LastOrDefault();

        static object? Value(object entry)
            => entry.GetType().GetProperty("Value")?.GetValue(entry) ?? entry;
    }

    private static MissingMemberException Missing(string member)
        => new($"MUnit needs MAUI's internal {member}, which this MAUI version ({typeof(Element).Assembly.GetName().Version}) does not have. See MUnit's 'MAUI internals' list.");
}
