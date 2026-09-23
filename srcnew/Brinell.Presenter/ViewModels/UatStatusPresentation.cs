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
    /// Status glyph colour. Every value clears 4.5:1 against the background, the panel
    /// and the selected-row fill of its own theme; re-measure all three if you change one.
    /// </summary>
    public static Color Color(string status, AppTheme theme)
    {
        var dark = theme == AppTheme.Dark;
        return Normalize(status) switch
        {
            "pass" => Parse(dark ? "#5DE8A8" : "#0F5F32"),
            "run" => Parse(dark ? "#9CC0FF" : "#1D4ED8"),
            "fail" => Parse(dark ? "#FF9B9B" : "#B91C1C"),
            "skip" => Parse(dark ? "#AEB7C6" : "#3F4857"),
            "cancel" => Parse(dark ? "#AEB7C6" : "#3F4857"),
            _ => Parse(dark ? "#AEB7C6" : "#4B5563")
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
            UatWorkspaceNodeKind.WorkflowConfig => Parse(dark ? "#9CC0FF" : "#1D4ED8"),
            UatWorkspaceNodeKind.MarkdownFile => Parse(dark ? "#5EE7DC" : "#095B55"),
            UatWorkspaceNodeKind.Suite => Parse(dark ? "#D3BBFF" : "#6D28D9"),
            UatWorkspaceNodeKind.Folder => Parse(dark ? "#BAC5D6" : "#3F4857"),
            _ => Parse(dark ? "#AEB7C6" : "#4B5563")
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
