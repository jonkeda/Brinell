namespace Bruinell.Core.Testing;

/// <summary>How a MAUI suite runs its app (.my/mUnit): on a device, or rendered in memory by MUnit.</summary>
public enum MauiHead
{
    /// <summary>The built app on Windows (FlaUI), Android or iOS (Appium): the default, and the judge at every gate.</summary>
    Device,

    /// <summary>The app's pages rendered in memory by MUnit: no device, no layout, no pixels.</summary>
    Munit,
}

/// <summary>
/// The MAUI head this run uses, from <c>BRUINELL_HEAD</c> (<c>munit</c>; anything else is the device),
/// the variable <see cref="WebHeads"/> reads too, so one run picks the in-memory head of every suite.
/// A fixture, a <see cref="DeviceFactAttribute"/> and the scenario runner read it alike.
/// </summary>
public static class MauiHeads
{
    /// <summary>The environment variable that picks the head.</summary>
    public const string Variable = WebHeads.Variable;

    /// <summary>The head of this run.</summary>
    public static MauiHead Current => Parse(Environment.GetEnvironmentVariable(Variable));

    /// <summary>Whether this run renders with MUnit.</summary>
    public static bool IsMunit => Current == MauiHead.Munit;

    /// <summary>The head a value of <see cref="Variable"/> names; anything but <c>munit</c> is the device.</summary>
    public static MauiHead Parse(string? value)
        => string.Equals(value?.Trim(), "munit", StringComparison.OrdinalIgnoreCase) ? MauiHead.Munit : MauiHead.Device;
}
