using System.Globalization;

namespace Bruinell.Core.Diagnostics;

/// <summary>
/// How a harness shows the app under test. <c>BRUINELL_AUT_WINDOW</c>: <c>background</c> (the
/// default) keeps it out of the way of the person at the machine during a test run; <c>shown</c>
/// leaves it visible, for someone watching it (the Presenter). <c>BRUINELL_AUT_BOUNDS</c>
/// (<c>x,y,width,height</c>) is where a shown app should open, for a launcher that can say so up
/// front (a browser).
/// </summary>
public static class AutWindow
{
    /// <summary>The variable that says how the app's window is shown.</summary>
    public const string Variable = "BRUINELL_AUT_WINDOW";

    /// <summary>The variable that says where a shown app should open.</summary>
    public const string BoundsVariable = "BRUINELL_AUT_BOUNDS";

    /// <summary>Whether the app's window is to be left shown rather than kept in the background.</summary>
    /// <param name="launchEnvironment">The app's launch environment, which wins over the process's.</param>
    /// <param name="read">Reads a process variable; <see cref="Environment.GetEnvironmentVariable(string)"/> by default.</param>
    public static bool IsShown(IReadOnlyDictionary<string, string>? launchEnvironment = null, Func<string, string?>? read = null)
    {
        var value = launchEnvironment is not null && launchEnvironment.TryGetValue(Variable, out var perLaunch)
            ? perLaunch
            : (read ?? Environment.GetEnvironmentVariable)(Variable);
        return string.Equals(value?.Trim(), "shown", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Where a shown app should open; null when not said, or not four whole numbers.</summary>
    /// <param name="read">Reads a process variable; <see cref="Environment.GetEnvironmentVariable(string)"/> by default.</param>
    public static (int X, int Y, int Width, int Height)? Bounds(Func<string, string?>? read = null)
    {
        var parts = ((read ?? Environment.GetEnvironmentVariable)(BoundsVariable) ?? string.Empty).Split(',');
        if (parts.Length != 4)
        {
            return null;
        }

        var numbers = new int[4];
        for (var index = 0; index < 4; index++)
        {
            if (!int.TryParse(parts[index].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out numbers[index]))
            {
                return null;
            }
        }

        return numbers[2] > 0 && numbers[3] > 0 ? (numbers[0], numbers[1], numbers[2], numbers[3]) : null;
    }
}
