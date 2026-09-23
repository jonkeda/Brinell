namespace Brinell.Core.Settings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class TestSettingsSectionAttribute : Attribute
{
    public TestSettingsSectionAttribute(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path.Trim();
    }

    public string Path { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class TestSettingsRootAttribute : Attribute
{
}
