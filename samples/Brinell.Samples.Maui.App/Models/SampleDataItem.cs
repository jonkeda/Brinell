namespace Brinell.Samples.Maui.App.Models;

/// <summary>
/// Represents sample data for collection demonstrations.
/// </summary>
public class SampleDataItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsStarred { get; set; }
}

/// <summary>
/// Represents a group of sample data items.
/// </summary>
public class SampleDataGroup : List<SampleDataItem>
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupKey { get; set; } = string.Empty;

    public SampleDataGroup(string name, string key, IEnumerable<SampleDataItem> items) : base(items)
    {
        GroupName = name;
        GroupKey = key;
    }
}
