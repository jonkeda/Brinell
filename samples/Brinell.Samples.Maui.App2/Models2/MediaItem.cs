namespace Brinell.Samples.Maui.App.Models2;

/// <summary>
/// Represents a media item for gallery demonstrations.
/// </summary>
public class MediaItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string FullUrl { get; set; } = string.Empty;
    public MediaType Type { get; set; } = MediaType.Image;
    public TimeSpan? Duration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Type of media content.
/// </summary>
public enum MediaType
{
    Image,
    Video,
    Audio,
    WebContent
}
