using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using Bruinet.Maui.Enums;

namespace Bruinet.Maui.Configuration;

/// <summary>
/// Whether the build a suite asks Appium to install is already the one on the device, so the
/// session can start without installing it again.
/// </summary>
/// <remarks>
/// <para>
/// A suite sets <c>enforceAppInstall</c> because MAUI does not bump versionCode between builds:
/// without it a run could test the previous build. Installing a Debug APK is most of a session's
/// start (9.3 s against 5.6 s, a button test, .my/android-performance). This keeps the guarantee and
/// drops the cost: the APK's fingerprint and the installed package's update time are written to
/// the device after an install, and a session whose APK and package still match both skips it.
/// </para>
/// <para>
/// Only the install is skipped. The app's data is still cleared when the session starts, by
/// UiAutomator2's <c>noReset=false</c>, the default, so each session starts clean as before.
/// <c>BRUINET_ALWAYS_REINSTALL=1</c> installs every time; anything adb cannot answer does too.
/// </para>
/// </remarks>
internal sealed class AndroidInstalledBuild
{
    private const string EnforceAppInstall = "enforceAppInstall";

    // Hashing a 100 MB Debug APK takes a few hundred milliseconds: once per build per process.
    private static readonly Dictionary<(string Path, DateTime Written, long Length), string> Fingerprints = [];

    private readonly string _adb;
    private readonly string[] _device;
    private readonly string _package;
    private readonly string _fingerprint;

    private AndroidInstalledBuild(string adb, string[] device, string package, string fingerprint, bool current)
    {
        _adb = adb;
        _device = device;
        _package = package;
        _fingerprint = fingerprint;
        IsCurrent = current;
    }

    /// <summary>Whether the device has this build installed, so the session skips the install.</summary>
    public bool IsCurrent { get; }

    /// <summary>
    /// Checks the device for a session that asks for <c>enforceAppInstall</c>. When the build there
    /// is this one, the option is turned off for this session. Null when it does not apply: not
    /// Android, no install asked for, no package named, no adb, or <c>BRUINET_ALWAYS_REINSTALL=1</c>.
    /// </summary>
    public static AndroidInstalledBuild? Check(MauiDriverOptions options)
    {
        if (options.Platform != MauiPlatform.Android
            || options.AppPath is not { } apk
            || !File.Exists(apk)
            || !(options.AdditionalCapabilities.TryGetValue(EnforceAppInstall, out var enforce) && enforce is true)
            || Environment.GetEnvironmentVariable("BRUINET_ALWAYS_REINSTALL") == "1"
            || PackageOf(options) is not { } package
            || Adb() is not { } adb)
        {
            return null;
        }

        string[] device = options.AdditionalCapabilities.TryGetValue("udid", out var udid) && udid is string serial && serial.Length > 0
            ? ["-s", serial]
            : [];

        try
        {
            var fingerprint = FingerprintOf(apk);
            var installed = InstalledAt(adb, device, package);
            var recorded = Run(adb, [.. device, "shell", "cat", MarkerPath(package)]).Trim();
            var current = installed is not null && recorded == $"{fingerprint} {installed}";
            if (current)
            {
                options.AdditionalCapabilities[EnforceAppInstall] = false;
            }

            return new AndroidInstalledBuild(adb, device, package, fingerprint, current);
        }
        catch (Exception error) when (error is InvalidOperationException or Win32Exception or IOException)
        {
            return null;
        }
    }

    /// <summary>After the session started: records what was installed, if this session installed it.</summary>
    public void RememberInstalled()
    {
        if (IsCurrent)
        {
            return;
        }

        try
        {
            if (InstalledAt(_adb, _device, _package) is { } installed)
            {
                Run(_adb, [.. _device, "shell", $"echo '{_fingerprint} {installed}' > {MarkerPath(_package)}"]);
            }
        }
        catch (Exception error) when (error is InvalidOperationException or Win32Exception or IOException)
        {
            // Not recorded: the next session installs again, as it always did.
        }
    }

    private static string? PackageOf(MauiDriverOptions options)
        => (options.AdditionalCapabilities.TryGetValue("appPackage", out var package) ? package as string : null)
           ?? (options.AdditionalCapabilities.TryGetValue("appWaitPackage", out var waited) ? waited as string : null);

    private static string MarkerPath(string package) => $"/data/local/tmp/bruinet-{package}.build";

    // The package's lastUpdateTime: it changes with every install, Appium's or anyone's, so a record
    // left by an install that was then replaced no longer matches.
    private static string? InstalledAt(string adb, string[] device, string package)
        => Run(adb, [.. device, "shell", "dumpsys", "package", package])
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("lastUpdateTime=", StringComparison.Ordinal))?
            ["lastUpdateTime=".Length..]
            .Replace(' ', 'T');

    private static string FingerprintOf(string apk)
    {
        var file = new FileInfo(apk);
        var key = (file.FullName, file.LastWriteTimeUtc, file.Length);
        lock (Fingerprints)
        {
            if (!Fingerprints.TryGetValue(key, out var fingerprint))
            {
                using var stream = file.OpenRead();
                fingerprint = Convert.ToHexString(SHA256.HashData(stream))[..32].ToLowerInvariant();
                Fingerprints[key] = fingerprint;
            }

            return fingerprint;
        }
    }

    /// <summary>adb: <c>ANDROID_HOME</c>'s platform-tools, else on <c>PATH</c>; null when neither has it.</summary>
    private static string? Adb()
    {
        var exe = OperatingSystem.IsWindows() ? "adb.exe" : "adb";
        var home = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (home is { Length: > 0 } && Path.Combine(home, "platform-tools", exe) is var sdk && File.Exists(sdk))
        {
            return sdk;
        }

        return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(folder => Path.Combine(folder.Trim('"'), exe))
            .FirstOrDefault(File.Exists);
    }

    private static string Run(string adb, IReadOnlyList<string> arguments)
    {
        var info = new ProcessStartInfo(adb)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("adb could not be started.");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}
