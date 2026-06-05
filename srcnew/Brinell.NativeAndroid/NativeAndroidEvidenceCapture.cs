namespace Brinell.NativeAndroid;

public sealed class NativeAndroidEvidenceCapture
{
    private readonly NativeAndroidDriver driver;

    public NativeAndroidEvidenceCapture(NativeAndroidDriver driver)
    {
        this.driver = driver;
    }

    public string Capture(string directory, string name)
    {
        Directory.CreateDirectory(directory);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var basePath = Path.Combine(directory, $"{timestamp}-{safeName}");

        driver.SaveScreenshot($"{basePath}.png");
        driver.SavePageSource($"{basePath}.xml");

        return basePath;
    }
}
