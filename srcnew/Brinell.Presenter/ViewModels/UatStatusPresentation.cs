using Microsoft.Maui.Graphics;

namespace Brinell.Presenter.ViewModels;

internal static class UatStatusPresentation
{
    public static string Icon(string status)
    {
        return Normalize(status) switch
        {
            "pass" => "✓",
            "run" => "▶",
            "fail" => "✕",
            "skip" => "→",
            "cancel" => "■",
            _ => "○"
        };
    }

    /// <summary>
    /// The same meaning as <see cref="Icon" />, drawn in the Segoe icon font.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="Icon" /> on purpose: <c>Icon</c> feeds the hidden text mirrors
    /// the UAT suite asserts against, and those stay plain ASCII.
    /// </remarks>
    public static string Glyph(string status)
    {
        return Normalize(status) switch
        {
            "pass" => PresenterIcons.StatusPass,
            "run" => PresenterIcons.StatusRun,
            "fail" => PresenterIcons.StatusFail,
            "skip" => PresenterIcons.StatusSkip,
            "cancel" => PresenterIcons.StatusCancel,
            _ => PresenterIcons.StatusWait
        };
    }

    public static string Description(string status)
    {
        return Normalize(status) switch
        {
            "pass" => "Passed",
            "run" => "Running",
            "fail" => "Failed",
            "skip" => "Skipped",
            "cancel" => "Canceled",
            _ => "Waiting"
        };
    }

    /// <summary>
    /// The effective theme, or <see cref="AppTheme.Light" /> when there is no MAUI
    /// application — which is the case in the headless view-model tests.
    /// </summary>
    public static AppTheme CurrentTheme
    {
        get
        {
            var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
            return theme == AppTheme.Dark ? AppTheme.Dark : AppTheme.Light;
        }
    }

    public static Color Color(string status)
    {
        return Color(status, CurrentTheme);
    }

    /// <summary>
    /// Status glyph colour. Every value clears 3:1 against both the background and the
    /// panel of its own theme; re-measure if you change one.
    /// </summary>
    public static Color Color(string status, AppTheme theme)
    {
        var dark = theme == AppTheme.Dark;
        return Normalize(status) switch
        {
            "pass" => Parse(dark ? "#3DD68C" : "#15803D"),
            "run" => Parse(dark ? "#7AA5FF" : "#2563EB"),
            "fail" => Parse(dark ? "#FF6B6B" : "#DC2626"),
            "skip" => Parse(dark ? "#9AA3B2" : "#5B6472"),
            "cancel" => Parse(dark ? "#9AA3B2" : "#5B6472"),
            _ => Parse(dark ? "#9CA3AF" : "#6B7280")
        };
    }

    /// <summary>
    /// Node-kind glyph colour, used when a node has no aggregate run status.
    /// </summary>
    public static Color KindColor(UatWorkspaceNodeKind kind, AppTheme theme)
    {
        var dark = theme == AppTheme.Dark;
        return kind switch
        {
            UatWorkspaceNodeKind.WorkflowConfig => Parse(dark ? "#7AA5FF" : "#2563EB"),
            UatWorkspaceNodeKind.MarkdownFile => Parse(dark ? "#4FD1C5" : "#0F766E"),
            UatWorkspaceNodeKind.Suite => Parse(dark ? "#C4A6FF" : "#7C3AED"),
            UatWorkspaceNodeKind.Folder => Parse(dark ? "#B6C2D2" : "#475569"),
            _ => Parse(dark ? "#9AA3B2" : "#64748B")
        };
    }

    private static Color Parse(string hex)
    {
        return Microsoft.Maui.Graphics.Color.FromArgb(hex);
    }

    private static string Normalize(string status)
    {
        return status.Trim().ToLowerInvariant();
    }
}
