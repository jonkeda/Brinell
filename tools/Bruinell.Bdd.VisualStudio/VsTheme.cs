using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Color = System.Drawing.Color;

namespace Bruinell.Bdd.VisualStudio;

// Visual Studio's theme as the page's CSS variables: its base palette (light, dark, high contrast)
// from the background's lightness, and the environment's own colours over it.
internal static class VsTheme
{
    public static object Message()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var background = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
        var kind = SystemParameters.HighContrast ? "high-contrast" : Lightness(background) < 0.5 ? "dark" : "light";
        var vars = new Dictionary<string, string>
        {
            ["--uat-bg"] = Css(background),
            ["--uat-fg"] = Css(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey)),
            ["--uat-muted"] = Css(VSColorTheme.GetThemedColor(EnvironmentColors.SystemGrayTextColorKey)),
            ["--uat-border"] = Css(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey)),
            ["--uat-surface"] = Css(VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarGradientBeginColorKey)),
            ["--uat-accent"] = Css(VSColorTheme.GetThemedColor(EnvironmentColors.ControlLinkTextColorKey)),
            ["--uat-font"] = "\"Segoe UI\", system-ui, sans-serif",
        };

        return new { type = "theme", kind, vars };
    }

    private static string Css(Color color) => $"#{color.R:x2}{color.G:x2}{color.B:x2}";

    private static double Lightness(Color color) => ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255;
}
