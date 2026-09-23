namespace ClaudeSwitcher.Models;

public sealed class ProfileIndex
{
    public List<ProfileEntry> Profiles { get; set; } = new();
}

public sealed class ProfileEntry
{
    public string Name { get; set; } = string.Empty;
    public DateTime? LastActivatedUtc { get; set; }
}

public readonly record struct ClearLiveResult(bool RemovedCredentials, bool RemovedClaudeJson)
{
    public bool AnythingRemoved => RemovedCredentials || RemovedClaudeJson;
}
