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

    public static Color Color(string status)
    {
        return Normalize(status) switch
        {
            "pass" => Microsoft.Maui.Graphics.Color.FromArgb("#16A34A"),
            "run" => Microsoft.Maui.Graphics.Color.FromArgb("#2563EB"),
            "fail" => Microsoft.Maui.Graphics.Color.FromArgb("#DC2626"),
            "skip" => Microsoft.Maui.Graphics.Color.FromArgb("#64748B"),
            "cancel" => Microsoft.Maui.Graphics.Color.FromArgb("#64748B"),
            _ => Microsoft.Maui.Graphics.Color.FromArgb("#94A3B8")
        };
    }

    private static string Normalize(string status)
    {
        return status.Trim().ToLowerInvariant();
    }
}
