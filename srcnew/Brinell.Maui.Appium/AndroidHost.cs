using System.Diagnostics;

namespace Brinell.Maui.Appium;

/// <summary>
/// How an Android app reaches a server that runs on the test machine: a WireMock backend, or an
/// API started by the test.
/// </summary>
/// <remarks>
/// <para>
/// Inside the Android emulator, <c>localhost</c> is the emulator itself. The machine running it
/// is <c>10.0.2.2</c>, which the emulator routes to the host's loopback - so a server bound to
/// <c>localhost</c> on the host is reachable there without opening anything to the network.
/// </para>
/// <para>
/// A physical device has no such alias. <see cref="Reverse"/> asks adb to forward a port on the
/// device to the same port on the host, after which the device uses <c>localhost</c>.
/// </para>
/// </remarks>
public static class AndroidHost
{
    /// <summary>The emulator's address for the machine it runs on.</summary>
    public const string EmulatorHostAddress = "10.0.2.2";

    /// <summary>
    /// The URL an app in the emulator uses for <paramref name="hostUrl"/>: a loopback host
    /// (<c>localhost</c>, <c>127.0.0.1</c>, <c>[::1]</c>) becomes <see cref="EmulatorHostAddress"/>;
    /// any other host is returned unchanged.
    /// </summary>
    public static string EmulatorUrl(string hostUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostUrl);

        var uri = new Uri(hostUrl);
        if (!uri.IsLoopback)
        {
            return hostUrl;
        }

        // Uri renders a bare root as "/"; keep the caller's form, with or without it.
        var rest = uri.PathAndQuery == "/" && !hostUrl.EndsWith('/') ? string.Empty : uri.PathAndQuery;
        return $"{uri.Scheme}://{EmulatorHostAddress}:{uri.Port}{rest}";
    }

    /// <summary>
    /// Forwards <paramref name="port"/> on the device to the same port on this machine
    /// (<c>adb reverse tcp:port tcp:port</c>), for a physical device.
    /// </summary>
    /// <param name="port">The port a server on this machine listens on.</param>
    /// <param name="deviceSerial">The device, as <c>adb devices</c> names it; the only device when null.</param>
    /// <exception cref="InvalidOperationException">adb failed; the message carries its output.</exception>
    public static void Reverse(int port, string? deviceSerial = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);

        var arguments = deviceSerial is null
            ? $"reverse tcp:{port} tcp:{port}"
            : $"-s {deviceSerial} reverse tcp:{port} tcp:{port}";

        using var adb = Process.Start(new ProcessStartInfo("adb", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("adb could not be started. Is the Android SDK's platform-tools on PATH?");

        var output = adb.StandardOutput.ReadToEnd() + adb.StandardError.ReadToEnd();
        adb.WaitForExit();

        if (adb.ExitCode != 0)
        {
            throw new InvalidOperationException($"adb {arguments} failed ({adb.ExitCode}): {output.Trim()}");
        }
    }
}
