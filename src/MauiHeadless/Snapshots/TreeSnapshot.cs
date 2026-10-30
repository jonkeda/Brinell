using System.Globalization;
using System.Text;
using Microsoft.Maui.Controls;
using MauiHeadless.Inspection;

namespace MauiHeadless.Snapshots;

/// <summary>How a tree is printed.</summary>
/// <param name="IncludeHidden">Whether hidden elements and what is under them are printed.</param>
/// <param name="IncludeBindingContext">Whether a node whose binding context differs from its parent's names the context's type.</param>
/// <param name="MaxLines">The most lines printed; the rest is counted on one last line.</param>
public sealed record SnapshotOptions(bool IncludeHidden = true, bool IncludeBindingContext = false, int MaxLines = int.MaxValue);

/// <summary>
/// The tree as indented text, one element per line: the format of assertion messages today and of
/// snapshot files later (see the proposal's Phase 5). Only what a user sees and what a test asserts
/// is printed, never hash codes or positions, so the text is the same on every run and machine.
/// </summary>
/// <example>
/// <code>
/// ContentPage #SettingsPage "Settings"
///   VerticalStackLayout
///     Entry #Name "John" placeholder="Your name"
///     Switch #Notifications [on]
///     Button #SaveButton "Save" [disabled]
///     Label #Status "Saved" [hidden]
/// </code>
/// </example>
public static class TreeSnapshot
{
    /// <summary>The options for assertion messages: the whole tree, cut at 80 lines.</summary>
    public static readonly SnapshotOptions ForMessages = new(MaxLines: 80);

    /// <summary>Prints <paramref name="root"/> and everything under it.</summary>
    public static string Format(VisualNode root, SnapshotOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        options ??= new SnapshotOptions();
        var text = new StringBuilder();
        var lines = 0;
        var skipped = 0;
        Append(root, 0);
        if (skipped > 0)
            text.Append(CultureInfo.InvariantCulture, $"... {skipped} more\n");
        return text.ToString();

        void Append(VisualNode node, int indent)
        {
            if (!options.IncludeHidden && !node.IsVisible)
                return;

            if (lines >= options.MaxLines)
            {
                skipped++;
            }
            else
            {
                text.Append(' ', indent * 2).Append(Describe(node, options.IncludeBindingContext)).Append('\n');
                lines++;
            }

            foreach (var child in node.Children)
                Append(child, indent + 1);
        }
    }

    /// <summary>One node on one line.</summary>
    public static string Describe(VisualNode node) => Describe(node, includeBindingContext: false);

    private static string Describe(VisualNode node, bool includeBindingContext)
    {
        var line = new StringBuilder(node.Type.Name);
        if (!string.IsNullOrEmpty(node.AutomationId))
            line.Append(" #").Append(node.AutomationId);
        if (node.Text is not null)
            line.Append(' ').Append(Quote(node.Text));

        var element = node.Element;
        if (element is InputView { Placeholder: { Length: > 0 } placeholder } input && string.IsNullOrEmpty(input.Text))
            line.Append(" placeholder=").Append(Quote(placeholder));
        if (State(element) is { } state)
            line.Append(" [").Append(state).Append(']');

        // Hidden and disabled are printed where they start, not on every node beneath.
        var parentVisible = node.Parent?.IsVisible ?? true;
        var parentEnabled = node.Parent?.IsEnabled ?? true;
        if (!node.IsVisible && parentVisible)
            line.Append(" [hidden]");
        if (!node.IsEnabled && parentEnabled)
            line.Append(" [disabled]");

        if (includeBindingContext && node.BindingContext is { } context
            && !ReferenceEquals(context, node.Parent?.BindingContext))
            line.Append(" {").Append(context.GetType().Name).Append('}');

        return line.ToString();
    }

    private static string? State(Element element) => element switch
    {
        Switch toggle => toggle.IsToggled ? "on" : "off",
        CheckBox box => box.IsChecked ? "checked" : null,
        RadioButton radio => radio.IsChecked ? "checked" : null,
        Slider slider => "value=" + slider.Value.ToString(CultureInfo.InvariantCulture),
        Stepper stepper => "value=" + stepper.Value.ToString(CultureInfo.InvariantCulture),
        ActivityIndicator { IsRunning: true } => "running",
        RefreshView { IsRefreshing: true } => "refreshing",
        _ => null,
    };

    private static string Quote(string text)
        => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}
