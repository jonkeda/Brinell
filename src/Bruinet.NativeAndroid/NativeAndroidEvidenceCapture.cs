namespace Bruinet.NativeAndroid;

/// <summary>Saves the screen and the view hierarchy side by side, for a failure's evidence.</summary>
public sealed class NativeAndroidEvidenceCapture
{
    private readonly INativeAndroidDriver _driver;

    /// <summary>Captures from <paramref name="driver"/>'s session.</summary>
    public NativeAndroidEvidenceCapture(INativeAndroidDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    /// <summary>Saves <c>&lt;timestamp&gt;-&lt;name&gt;.png</c> and <c>.xml</c> in <paramref name="directory"/>.</summary>
    /// <returns>The files' path without their extension.</returns>
    public string Capture(string directory, string name)
    {
        Directory.CreateDirectory(directory);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var basePath = Path.Combine(directory, $"{timestamp}-{safeName}");

        _driver.SaveScreenshot($"{basePath}.png");
        File.WriteAllText($"{basePath}.xml", _driver.GetPageSource());

        return basePath;
    }
}
